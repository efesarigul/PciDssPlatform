using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PciDss.Business.Dtos;
using PciDss.DataAccess;
using PciDss.Entities;

namespace PciDss.Business.Services;

public class BusinessException(string message) : Exception(message);

// ---------------- AUTH ----------------
public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterDto dto);
    Task<AuthResultDto> LoginAsync(LoginDto dto);
}

public class AuthService(IRepository<User> users, IRepository<Organization> orgs, IConfiguration cfg) : IAuthService
{
    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password.Length < 12) throw new BusinessException("Parola en az 12 karakter olmalı (PCI DSS 8.3.6).");
        if (await users.Query().AnyAsync(u => u.Email == dto.Email))
            throw new BusinessException("Bu e-posta zaten kayıtlı.");

        var org = new Organization { Name = dto.OrganizationName };
        await orgs.AddAsync(org);
        var user = new User
        {
            FullName = dto.FullName, Email = dto.Email, Organization = org,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        await users.AddAsync(user);
        await users.SaveChangesAsync();
        return new AuthResultDto(CreateToken(user), user.FullName, org.Name);
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var user = await users.Query().Include(u => u.Organization).FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new BusinessException("E-posta veya parola hatalı.");
        return new AuthResultDto(CreateToken(user), user.FullName, user.Organization.Name);
    }

    private string CreateToken(User u)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Email, u.Email),
            new Claim(ClaimTypes.Role, u.Role.ToString()),
            new Claim("org", u.OrganizationId.ToString())
        };
        var token = new JwtSecurityToken(cfg["Jwt:Issuer"], cfg["Jwt:Issuer"], claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ---------------- SCOPE (SAQ belirleme) ----------------
public static class SaqRules
{
    /// <summary>
    /// Kapsam beyanından SAQ tipini belirler. PCI SSC'nin SAQ uygunluk kriterlerinin sadeleştirilmiş halidir;
    /// kesin SAQ tipini kurumun edinci bankası (acquirer) onaylar.
    /// </summary>
    public static (SaqType Saq, string Explanation) Determine(ScopeDto s)
    {
        if (s.EntityType == EntityType.ServiceProvider)
            return (SaqType.DServiceProvider, "Servis sağlayıcılar için uygun tek SAQ tipi SAQ D (Servis Sağlayıcı)'dır.");
        if (s.Ecommerce is null && s.CardPresent is null && s.Moto is null)
            throw new BusinessException("En az bir ödeme kanalı seçmelisiniz.");
        if (s.StoresCardDataElectronically)
            return (SaqType.DMerchant, "Kart verisini elektronik olarak sakladığınız için SAQ D (Üye İşyeri) gerekir.");

        var perChannel = new List<SaqType>();
        if (s.Ecommerce is { } e) perChannel.Add(e switch
        {
            EcommerceMethod.FullyOutsourced => SaqType.A,
            EcommerceMethod.PartialOutsourced => SaqType.AEP,
            _ => SaqType.DMerchant
        });
        if (s.CardPresent is { } c) perChannel.Add(c switch
        {
            CardPresentMethod.StandaloneDialup => SaqType.B,
            CardPresentMethod.StandaloneIp => SaqType.BIP,
            CardPresentMethod.ValidatedP2pe => SaqType.P2PE,
            CardPresentMethod.SoftwarePinOnCots => SaqType.SPoC,
            _ => SaqType.C
        });
        if (s.Moto is { } m) perChannel.Add(m switch
        {
            MotoMethod.FullyOutsourced => SaqType.A,
            MotoMethod.VirtualTerminal => SaqType.CVT,
            _ => SaqType.DMerchant
        });

        var distinct = perChannel.Distinct().ToList();
        if (distinct.Contains(SaqType.DMerchant))
            return (SaqType.DMerchant, "Kart verisini kendi sistemlerinizle aldığınız bir kanal olduğu için SAQ D (Üye İşyeri) gerekir.");
        if (distinct.Count == 1)
            return (distinct[0], $"Beyan ettiğiniz ödeme kanalları SAQ {Label(distinct[0])} uygunluk kriterlerine uyuyor.");
        return (SaqType.DMerchant,
            $"Ödeme kanallarınız farklı SAQ tiplerine düşüyor ({string.Join(", ", distinct.Select(Label))}). " +
            "Tek bir SAQ ile beyan için SAQ D gerekir; edinci bankanız kanal başına ayrı SAQ kabul edebilir.");
    }

    public static string Label(SaqType t) => t switch
    {
        SaqType.AEP => "A-EP", SaqType.BIP => "B-IP", SaqType.CVT => "C-VT",
        SaqType.DMerchant => "D (Üye İşyeri)", SaqType.DServiceProvider => "D (Servis Sağlayıcı)",
        _ => t.ToString()
    };

    /// <summary>SAQ tipine göre sorulacak soruların filtresi (SQL'e çevrilir)</summary>
    public static Expression<Func<SurveyQuestion, bool>> QuestionsFor(SaqType t)
    {
        if (t == SaqType.DServiceProvider) return q => true;
        if (t == SaqType.DMerchant) return q => !q.Requirement.ServiceProviderOnly;
        var flag = (int)Enum.Parse<SaqApplicability>(t.ToString());
        return q => ((int)q.Requirement.Applicability & flag) != 0;
    }
}

public interface IScopeService
{
    Task<ScopeResultDto?> GetAsync(int orgId);
    Task<ScopeResultDto> SaveAsync(int orgId, ScopeDto dto);
}

public class ScopeService(IRepository<ScopeProfile> scopes, IRepository<SurveyQuestion> questions) : IScopeService
{
    public async Task<ScopeResultDto?> GetAsync(int orgId)
    {
        var p = await scopes.Query().FirstOrDefaultAsync(x => x.OrganizationId == orgId);
        if (p is null) return null;
        var dto = new ScopeDto(p.EntityType, p.StoresCardDataElectronically, p.Ecommerce, p.CardPresent, p.Moto);
        return await ResultAsync(dto, SaqRules.Determine(dto));
    }

    public async Task<ScopeResultDto> SaveAsync(int orgId, ScopeDto dto)
    {
        var result = SaqRules.Determine(dto);
        var p = await scopes.Query().FirstOrDefaultAsync(x => x.OrganizationId == orgId);
        if (p is null) await scopes.AddAsync(p = new ScopeProfile { OrganizationId = orgId });
        p.EntityType = dto.EntityType;
        p.StoresCardDataElectronically = dto.StoresCardDataElectronically;
        p.Ecommerce = dto.Ecommerce; p.CardPresent = dto.CardPresent; p.Moto = dto.Moto;
        p.SaqType = result.Saq;
        p.UpdatedAt = DateTime.UtcNow;
        await scopes.SaveChangesAsync();
        return await ResultAsync(dto, result);
    }

    private async Task<ScopeResultDto> ResultAsync(ScopeDto dto, (SaqType Saq, string Explanation) r) =>
        new(dto, r.Saq, r.Explanation, await questions.Query().Where(SaqRules.QuestionsFor(r.Saq)).CountAsync());
}

// ---------------- SURVEY ----------------
public interface ISurveyService
{
    Task<SurveyDto> GetQuestionsAsync(int orgId);
    Task SaveAnswersAsync(int orgId, SubmitAnswersDto dto);
    Task<SurveyProgressDto> GetProgressAsync(int orgId);
}

public class SurveyService(IRepository<SurveyQuestion> questions, IRepository<SurveyAnswer> answers,
                           IRepository<ScopeProfile> scopes, IRepository<Policy> policies) : ISurveyService
{
    // SAQ'ta bu cevaplar gerekçe (açıklama) olmadan kabul edilmez
    private static readonly AnswerValue[] NeedsJustification =
        [AnswerValue.InPlaceWithCcw, AnswerValue.NotApplicable, AnswerValue.NotTested];

    public async Task<SurveyDto> GetQuestionsAsync(int orgId)
    {
        var saq = await RequireSaqAsync(orgId);
        var myAnswers = await answers.Query().Where(a => a.OrganizationId == orgId)
            .ToDictionaryAsync(a => a.SurveyQuestionId);
        var withEvidence = await EvidenceRequirementIdsAsync(orgId);

        var list = await questions.Query().Include(q => q.Requirement)
            .Where(SaqRules.QuestionsFor(saq)).OrderBy(q => q.Order).ToListAsync();
        return new SurveyDto(saq, list.Select(q =>
        {
            myAnswers.TryGetValue(q.Id, out var a);
            return new QuestionDto(q.Id, q.Order, q.Text, q.EvidenceRequired, q.EvidenceHint,
                q.Requirement.Code, q.Requirement.Title, q.Requirement.Description, q.Requirement.Section,
                a?.Value, a?.Comment, withEvidence.Contains(q.RequirementId));
        }).ToList());
    }

    // Upsert: wizard her adımda kaydedebilir
    public async Task SaveAnswersAsync(int orgId, SubmitAnswersDto dto)
    {
        var saq = await RequireSaqAsync(orgId);
        var ids = dto.Answers.Select(a => a.QuestionId).Distinct().ToList();
        var allowed = await questions.Query().Where(SaqRules.QuestionsFor(saq))
            .CountAsync(q => ids.Contains(q.Id));
        if (allowed != ids.Count)
            throw new BusinessException("Bazı sorular kurumunuzun SAQ kapsamında değil.");

        if (dto.Answers.Any(a => NeedsJustification.Contains(a.Value) && string.IsNullOrWhiteSpace(a.Comment)))
            throw new BusinessException("Telafi edici kontrol, Uygulanamaz ve Test edilmedi cevapları için açıklama zorunludur.");

        var existing = await answers.Query()
            .Where(a => a.OrganizationId == orgId && ids.Contains(a.SurveyQuestionId))
            .ToDictionaryAsync(a => a.SurveyQuestionId);

        foreach (var item in dto.Answers)
        {
            if (existing.TryGetValue(item.QuestionId, out var a))
            {
                a.Value = item.Value; a.Comment = item.Comment;
            }
            else
            {
                await answers.AddAsync(new SurveyAnswer
                {
                    OrganizationId = orgId, SurveyQuestionId = item.QuestionId,
                    Value = item.Value, Comment = item.Comment
                });
            }
        }
        await answers.SaveChangesAsync();
    }

    public async Task<SurveyProgressDto> GetProgressAsync(int orgId)
    {
        var survey = await GetQuestionsAsync(orgId);
        var qs = survey.Questions;
        int Count(AnswerValue v) => qs.Count(q => q.CurrentAnswer == v);
        // "Uygulanıyor" beyan edilip zorunlu kanıtı yüklenmemiş sorular
        var missingEvidence = qs.Count(q => q.EvidenceRequired && !q.EvidenceUploaded &&
            q.CurrentAnswer is AnswerValue.InPlace or AnswerValue.InPlaceWithCcw);
        return new SurveyProgressDto(survey.SaqType, qs.Count, qs.Count(q => q.CurrentAnswer != null),
            Count(AnswerValue.InPlace), Count(AnswerValue.InPlaceWithCcw), Count(AnswerValue.NotApplicable),
            Count(AnswerValue.NotTested), Count(AnswerValue.NotInPlace), missingEvidence);
    }

    private async Task<SaqType> RequireSaqAsync(int orgId) =>
        await scopes.Query().Where(s => s.OrganizationId == orgId).Select(s => (SaqType?)s.SaqType).FirstOrDefaultAsync()
        ?? throw new BusinessException("Önce Kapsam Belirleme adımını tamamlayın.");

    private async Task<HashSet<int>> EvidenceRequirementIdsAsync(int orgId) =>
        (await policies.Query().Where(p => p.OrganizationId == orgId && p.RequirementId != null)
            .Select(p => p.RequirementId!.Value).Distinct().ToListAsync()).ToHashSet();
}

// ---------------- FILES ----------------
public interface IFileService
{
    Task<PolicyDto> UploadAsync(int orgId, IFormFile file, string? requirementCode);
    Task<List<PolicyDto>> ListAsync(int orgId);
}

public class FileService(IRepository<Policy> policies, IRepository<Requirement> reqs, IConfiguration cfg) : IFileService
{
    private static readonly string[] Allowed = [".pdf", ".docx", ".txt", ".md"];
    private const long MaxBytes = 10 * 1024 * 1024;

    public async Task<PolicyDto> UploadAsync(int orgId, IFormFile file, string? requirementCode)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext)) throw new BusinessException($"Desteklenmeyen dosya türü: {ext}");
        if (file.Length is 0 or > MaxBytes) throw new BusinessException("Dosya boş veya 10 MB'tan büyük.");

        Requirement? req = null;
        if (!string.IsNullOrWhiteSpace(requirementCode))
            req = await reqs.Query().FirstOrDefaultAsync(r => r.Code == requirementCode)
                  ?? throw new BusinessException("Gereksinim bulunamadı.");

        // Dosya adı kullanıcıdan alınmaz (path traversal önlemi), kurum bazlı klasör
        var dir = Path.Combine(cfg["Storage:Root"] ?? "uploads", orgId.ToString());
        Directory.CreateDirectory(dir);
        var stored = $"{Guid.NewGuid():N}{ext}";

        string hash;
        await using (var fs = File.Create(Path.Combine(dir, stored)))
        {
            await file.CopyToAsync(fs);
            fs.Position = 0;
            hash = Convert.ToHexString(await SHA256.HashDataAsync(fs));
        }

        var p = new Policy
        {
            OrganizationId = orgId, OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = stored, ContentType = file.ContentType, SizeBytes = file.Length,
            Sha256 = hash, RequirementId = req?.Id
        };
        await policies.AddAsync(p);
        await policies.SaveChangesAsync();
        return new PolicyDto(p.Id, p.OriginalFileName, p.SizeBytes, req?.Code, p.Status, p.CreatedAt);
    }

    public Task<List<PolicyDto>> ListAsync(int orgId) =>
        policies.Query().Where(p => p.OrganizationId == orgId).OrderByDescending(p => p.CreatedAt)
            .Select(p => new PolicyDto(p.Id, p.OriginalFileName, p.SizeBytes,
                p.Requirement != null ? p.Requirement.Code : null, p.Status, p.CreatedAt))
            .ToListAsync();
}
