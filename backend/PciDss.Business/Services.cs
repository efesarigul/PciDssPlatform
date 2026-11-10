using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        var ai = await BestAiResultsAsync(orgId);

        var list = await questions.Query().Include(q => q.Requirement)
            .Where(SaqRules.QuestionsFor(saq)).OrderBy(q => q.Order).ToListAsync();
        return new SurveyDto(saq, list.Select(q =>
        {
            myAnswers.TryGetValue(q.Id, out var a);
            return new QuestionDto(q.Id, q.Order, q.Text, q.EvidenceRequired, q.EvidenceHint,
                q.Requirement.Code, q.Requirement.Title, q.Requirement.Description, q.Requirement.Section,
                a?.Value, a?.Comment, withEvidence.Contains(q.RequirementId), ai.GetValueOrDefault(q.RequirementId));
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

    // Gereksinim başına YZ'nin en iyi değerlendirdiği belge: bir belge uyumluysa diğerleri eksik sayılmaz
    private async Task<Dictionary<int, EvidenceAiDto>> BestAiResultsAsync(int orgId) =>
        (await policies.Query()
            .Where(p => p.OrganizationId == orgId && p.RequirementId != null &&
                        p.Status == AnalysisStatus.Completed && p.AiVerdict != null)
            .Select(p => new { RequirementId = p.RequirementId!.Value, Verdict = p.AiVerdict!.Value, p.AiSummary, p.AiGaps, p.OriginalFileName })
            .ToListAsync())
        .GroupBy(p => p.RequirementId)
        .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Verdict)
            .Select(p => new EvidenceAiDto(p.Verdict, p.AiSummary, AiText.Lines(p.AiGaps), p.OriginalFileName)).First());
}

// ---------------- GAP ANALİZİ ----------------
public interface IGapService
{
    Task<GapAnalysisDto> GetAsync(int orgId);
}

public class GapService(ISurveyService survey) : IGapService
{
    public async Task<GapAnalysisDto> GetAsync(int orgId)
    {
        var s = await survey.GetQuestionsAsync(orgId);
        var qs = s.Questions;
        // OrderBy kararlıdır: aynı önem seviyesinde soru sırası korunur
        var findings = qs.Select(ToFinding).OfType<FindingDto>().OrderBy(f => f.Severity).ToList();
        var sections = qs.GroupBy(q => q.Section).OrderBy(g => g.Key)
            .Select(g => new SectionScoreDto(g.Key, g.Count(), Score(g),
                findings.Count(f => f.Section == g.Key && f.Severity == FindingSeverity.Critical)))
            .ToList();

        return new GapAnalysisDto(s.SaqType, Score(qs), qs.Count, qs.Count(q => q.CurrentAnswer != null),
            qs.Count(q => q.CurrentAnswer is AnswerValue.InPlace or AnswerValue.InPlaceWithCcw),
            qs.Count(q => q.CurrentAnswer == AnswerValue.NotInPlace),
            qs.Count(MissingEvidence), sections, findings);
    }

    private static bool MissingEvidence(QuestionDto q) => q.EvidenceRequired && !q.EvidenceUploaded &&
        q.CurrentAnswer is AnswerValue.InPlace or AnswerValue.InPlaceWithCcw;

    // Uygulanıyor = 1 puan; kanıtı eksikse veya YZ kısmen uyumlu bulduysa 0,5; YZ uyumsuz bulduysa 0.
    // Uygulanmıyor / test edilmedi / cevapsız = 0. "Uygulanamaz" cevapları paydaya katılmaz.
    private static double Points(QuestionDto q) => q.CurrentAnswer switch
    {
        AnswerValue.InPlace or AnswerValue.InPlaceWithCcw when MissingEvidence(q) => 0.5,
        AnswerValue.InPlace or AnswerValue.InPlaceWithCcw => q.EvidenceAi?.Verdict switch
        {
            ComplianceVerdict.NonCompliant => 0,
            ComplianceVerdict.Partial => 0.5,
            _ => 1
        },
        _ => 0
    };

    private static bool AiVerdictIs(QuestionDto q, ComplianceVerdict v) =>
        q.CurrentAnswer is AnswerValue.InPlace or AnswerValue.InPlaceWithCcw && q.EvidenceAi?.Verdict == v;

    private static int Score(IEnumerable<QuestionDto> qs)
    {
        var scored = qs.Where(q => q.CurrentAnswer != AnswerValue.NotApplicable).ToList();
        return scored.Count == 0 ? 100 : (int)Math.Round(100 * scored.Sum(Points) / scored.Count);
    }

    private static FindingDto? ToFinding(QuestionDto q)
    {
        var hint = q.EvidenceHint is null ? "" : $" Beklenen kanıt: {q.EvidenceHint}.";
        var ai = q.EvidenceAi;
        // Başlık kısa tutulur; YZ'nin eksikleri öneri olarak gösterilir (özetin tamamı Kanıt Yükleme ekranında)
        var aiGaps = ai is { Gaps.Count: > 0 } ? string.Join(" ", ai.Gaps) : ai?.Summary ?? "";
        (FindingKind, FindingSeverity, string, string)? f = q.CurrentAnswer switch
        {
            AnswerValue.NotInPlace => (FindingKind.NotInPlace, FindingSeverity.Critical, "Kontrol uygulanmıyor.",
                "Kontrolü devreye alın; sorumlu kişiyi ve hedef tarihi belirleyin." + hint),
            AnswerValue.NotTested => (FindingKind.NotTested, FindingSeverity.Medium, "Kontrol test edilmedi.",
                "Kontrolü test edin ve sonucunu belgeleyin." + hint),
            null => (FindingKind.Unanswered, FindingSeverity.Medium, "Beyan verilmedi.",
                "Bu gereksinim için beyanınızı tamamlayın."),
            _ when MissingEvidence(q) => (FindingKind.MissingEvidence, FindingSeverity.Medium,
                "Uygulandığı beyan edildi ancak kanıt yüklenmedi.", "Kanıt belgesini yükleyin." + hint),
            _ when AiVerdictIs(q, ComplianceVerdict.NonCompliant) => (FindingKind.AiNonCompliant, FindingSeverity.Critical,
                "YZ analizi: yüklenen kanıt gereksinimi karşılamıyor.", aiGaps),
            _ when AiVerdictIs(q, ComplianceVerdict.Partial) => (FindingKind.AiPartial, FindingSeverity.Medium,
                "YZ analizi: yüklenen kanıt gereksinimi kısmen karşılıyor.", aiGaps),
            AnswerValue.InPlaceWithCcw => (FindingKind.Compensating, FindingSeverity.Low, "Telafi edici kontrolle karşılanıyor.",
                "Telafi edici kontrol çalışma sayfasını (CCW) hazırlayın ve yılda en az bir kez gözden geçirin."),
            _ => null
        };
        return f is (var kind, var severity, var issue, var rec)
            ? new FindingDto(q.Id, q.RequirementCode, q.RequirementTitle, q.Section, kind, severity, issue, rec)
            : null;
    }
}

// ---------------- DEMO ----------------
public interface IDemoService
{
    Task<AuthResultDto> ResetAndLoginAsync();
}

/// <summary>
/// Sunum için hazır demo kurumu. Her girişte aynı başlangıç durumuna sıfırlanır: SAQ A-EP, cevapların çoğu dolu,
/// birkaç kritik bulgu, eksik kanıt ve canlı cevaplanmak üzere boş bırakılmış sorular.
/// </summary>
public class DemoService(IAuthService auth, IScopeService scope, IRepository<User> users,
                         IRepository<SurveyQuestion> questions, IRepository<SurveyAnswer> answers,
                         IRepository<Policy> policies, IConfiguration cfg) : IDemoService
{
    private const string Email = "demo@ornek-eticaret.test";
    private const string Password = "DemoSunum-2026!";
    private const string OrgName = "Örnek E-Ticaret A.Ş. (Demo)";

    // Varsayılan cevap "Uygulanıyor"; senaryoyu bunlar belirler
    private static readonly Dictionary<string, (AnswerValue Value, string Comment)> Scenario = new()
    {
        ["6.4.3"] = (AnswerValue.NotInPlace, "Ödeme sayfasındaki betiklerin envanteri henüz çıkarılmadı."),
        ["11.6.1"] = (AnswerValue.NotInPlace, "Ödeme sayfası için değişiklik/kurcalama tespit mekanizması yok."),
        ["8.4.1"] = (AnswerValue.NotInPlace, "Yönetim paneline yalnızca parola ile giriş yapılıyor."),
        ["11.4.1"] = (AnswerValue.NotTested, "Yıllık sızma testi 2027'nin ilk çeyreğine planlandı."),
        ["8.3.6"] = (AnswerValue.InPlaceWithCcw,
            "Eski ERP sistemi 12 karakter parolayı desteklemiyor; 8 karakter + MFA + hesap kilitleme ile telafi ediliyor."),
        ["2.3.1"] = (AnswerValue.NotApplicable, "Kart verisi ortamında kablosuz ağ bulunmuyor."),
    };
    // Sunumda canlı cevaplanmak üzere boş bırakılır
    private static readonly string[] LeftEmpty = ["12.8.2", "12.10.1"];
    // Uygulanıyor denip kanıtı yüklenmeyenler -> gap analizinde "eksik kanıt".
    // 1.2.1 sunumda gerçek bir politika (ör. SANS Network Device Management Policy) yüklenerek YZ'ye gösterilir.
    private static readonly string[] NoEvidence = ["1.2.1", "1.2.7", "10.4.1", "12.6.1"];

    public async Task<AuthResultDto> ResetAndLoginAsync()
    {
        if (!cfg.GetValue<bool>("Demo:Enabled")) throw new BusinessException("Demo hesabı bu ortamda kapalı.");

        if (!await users.Query().AnyAsync(u => u.Email == Email))
            await auth.RegisterAsync(new RegisterDto("Demo Kullanıcı", Email, Password, OrgName));
        var orgId = await users.Query().Where(u => u.Email == Email).Select(u => u.OrganizationId).FirstAsync();

        // Önceki sunumdan kalanları temizle. Klasör toptan silinmez: yalnızca bu kurumun kayıtlı dosyaları silinir
        // (farklı veritabanları aynı uploads klasörünü paylaşabilir, aynı orgId başka kuruma ait olabilir).
        var dir = Path.Combine(cfg["Storage:Root"] ?? "uploads", orgId.ToString());
        foreach (var a in await answers.ListAsync(a => a.OrganizationId == orgId)) answers.Remove(a);
        foreach (var p in await policies.ListAsync(p => p.OrganizationId == orgId))
        {
            try { File.Delete(Path.Combine(dir, p.StoredFileName)); }
            catch (IOException) { } // o an açık olan dosya diskte kalabilir; kaydı yine silinir
            policies.Remove(p);
        }
        await answers.SaveChangesAsync();
        Directory.CreateDirectory(dir);

        await scope.SaveAsync(orgId, new ScopeDto(EntityType.Merchant, false, EcommerceMethod.PartialOutsourced, null, null));

        var list = await questions.Query().Include(q => q.Requirement)
            .Where(SaqRules.QuestionsFor(SaqType.AEP)).OrderBy(q => q.Order).ToListAsync();
        var day = list.Count;
        foreach (var q in list)
        {
            var code = q.Requirement.Code;
            day--;
            if (LeftEmpty.Contains(code)) continue;
            var (value, comment) = Scenario.TryGetValue(code, out var s) ? s : (AnswerValue.InPlace, (string?)null);
            await answers.AddAsync(new SurveyAnswer
            {
                OrganizationId = orgId, SurveyQuestionId = q.Id, Value = value, Comment = comment
            });

            if (!q.EvidenceRequired || NoEvidence.Contains(code) ||
                value is not (AnswerValue.InPlace or AnswerValue.InPlaceWithCcw)) continue;
            var fileName = $"{code} {q.EvidenceHint?.Replace('/', '-')}.txt";
            var content = Encoding.UTF8.GetBytes(
                $"{OrgName}\nPCI DSS {code} · {q.Requirement.Title}\n\n{q.EvidenceHint}\n\n(Demo amaçlı örnek kanıt belgesi.)\n");
            var stored = $"{Guid.NewGuid():N}.txt";
            await File.WriteAllBytesAsync(Path.Combine(dir, stored), content);
            await policies.AddAsync(new Policy
            {
                OrganizationId = orgId, OriginalFileName = fileName, StoredFileName = stored,
                ContentType = "text/plain", SizeBytes = content.Length,
                Sha256 = Convert.ToHexString(SHA256.HashData(content)), RequirementId = q.RequirementId,
                CreatedAt = DateTime.UtcNow.AddDays(-day / 3).AddHours(-day), // yüklemeler birkaç güne yayılsın
                // Hazır demo belgeleri YZ'ye gönderilmez (içerikleri yer tutucudur); değerlendirilmiş kabul edilir.
                // Sunumda canlı yüklenen belgeler gerçek YZ analizinden geçer.
                Status = AnalysisStatus.Completed, AiVerdict = ComplianceVerdict.Compliant, AiConfidence = 1,
                AiSummary = "Demo verisi: bu örnek kanıt önceden değerlendirilmiş kabul edilmiştir.", AiModel = "demo",
                AnalyzedAt = DateTime.UtcNow.AddDays(-day / 3)
            });
        }
        await answers.SaveChangesAsync();
        return await auth.LoginAsync(new LoginDto(Email, Password));
    }
}

// ---------------- FILES ----------------
public interface IFileService
{
    Task<PolicyDto> UploadAsync(int orgId, IFormFile file, string? requirementCode);
    Task<List<PolicyDto>> ListAsync(int orgId);
    Task ReanalyzeAsync(int orgId, int policyId);
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

    public async Task<List<PolicyDto>> ListAsync(int orgId) =>
        (await policies.Query().Include(p => p.Requirement).Where(p => p.OrganizationId == orgId)
            .OrderByDescending(p => p.CreatedAt).ToListAsync())
        .Select(p => new PolicyDto(p.Id, p.OriginalFileName, p.SizeBytes, p.Requirement?.Code, p.Status, p.CreatedAt,
            p.Status is AnalysisStatus.Completed or AnalysisStatus.Failed
                ? new AiResultDto(p.AiVerdict, p.AiConfidence, p.AiSummary, AiText.Lines(p.AiGaps),
                    AiText.Lines(p.AiQuotes), p.AiModel, p.AnalyzedAt)
                : null))
        .ToList();

    // Belgeyi yeniden analiz kuyruğuna alır (ör. YZ servisi o an yanıt vermediyse)
    public async Task ReanalyzeAsync(int orgId, int policyId)
    {
        var p = await policies.Query().FirstOrDefaultAsync(x => x.Id == policyId && x.OrganizationId == orgId)
                ?? throw new BusinessException("Belge bulunamadı.");
        if (p.RequirementId is null) throw new BusinessException("Analiz için belgenin bir gereksinimle ilişkilendirilmesi gerekir.");
        p.Status = AnalysisStatus.Pending;
        await policies.SaveChangesAsync();
    }
}

public static class AiText
{
    public static List<string> Lines(string? s) =>
        string.IsNullOrWhiteSpace(s) ? [] : s.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

// ---------------- YZ ANALİZİ (Faz 2) ----------------
/// <summary>
/// Bekleyen kanıt belgelerini Python RAG servisine (ai-service) gönderir ve sonucu Policy'ye yazar.
/// Yalnızca bir gereksinimle ilişkilendirilmiş belgeler analiz edilir.
/// </summary>
public class AnalysisService(HttpClient http, IRepository<Policy> policies, IRepository<SurveyQuestion> questions,
                             IConfiguration cfg)
{
    private record AiResponse(string Verdict, double Confidence, string Summary, List<string> Gaps,
                              List<string> Quotes, string Model);

    /// <summary>Sıradaki bekleyen belgeyi analiz eder; bekleyen yoksa false döner.</summary>
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var p = await policies.Query().Include(x => x.Requirement)
            .Where(x => x.Status == AnalysisStatus.Pending && x.RequirementId != null)
            .OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (p is null) return false;

        p.Status = AnalysisStatus.Processing;
        await policies.SaveChangesAsync();
        try
        {
            var q = await questions.Query().FirstOrDefaultAsync(x => x.RequirementId == p.RequirementId, ct);
            var path = Path.Combine(cfg["Storage:Root"] ?? "uploads", p.OrganizationId.ToString(), p.StoredFileName);
            // Dosya belleğe okunur: analiz sürerken diskte kilitli kalmasın (demo sıfırlama silebilsin)
            using var form = new MultipartFormDataContent
            {
                { new ByteArrayContent(await File.ReadAllBytesAsync(path, ct)), "file", p.OriginalFileName },
                { new StringContent(p.Id.ToString()), "policy_id" },
                { new StringContent(p.OrganizationId.ToString()), "org_id" },
                { new StringContent(p.Requirement!.Code), "requirement_code" },
                { new StringContent(p.Requirement.Title), "requirement_title" },
                { new StringContent(p.Requirement.Description), "requirement_description" },
                { new StringContent(q?.Text ?? ""), "question" },
                { new StringContent(q?.EvidenceHint ?? ""), "evidence_hint" },
            };
            using var res = await http.PostAsync("analyze", form, ct);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(ct);
                throw new InvalidOperationException(err?.GetValueOrDefault("detail")?.ToString() ?? $"YZ servisi {(int)res.StatusCode} döndü.");
            }
            var r = (await res.Content.ReadFromJsonAsync<AiResponse>(ct))!;
            p.AiVerdict = Enum.Parse<ComplianceVerdict>(r.Verdict);
            p.AiConfidence = r.Confidence;
            p.AiSummary = r.Summary;
            p.AiGaps = string.Join('\n', r.Gaps);
            p.AiQuotes = string.Join('\n', r.Quotes);
            p.AiModel = r.Model;
            p.Status = AnalysisStatus.Completed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            p.AiVerdict = null;
            p.AiSummary = ex switch
            {
                HttpRequestException => "YZ servisine ulaşılamadı. ai-service çalışıyor mu?",
                TaskCanceledException => "YZ servisi zamanında yanıt vermedi.",
                _ => ex.Message
            };
            p.Status = AnalysisStatus.Failed;
        }
        p.AnalyzedAt = DateTime.UtcNow;
        try { await policies.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { } // belge analiz sürerken silindi (ör. demo sıfırlandı)
        return true;
    }
}

/// <summary>Arka planda bekleyen belgeleri sırayla analiz eder.</summary>
public class AnalysisWorker(IServiceScopeFactory scopes, ILogger<AnalysisWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Önceki çalışmada yarıda kalanları kuyruğa geri al
        using (var scope = scopes.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Policy>>();
            foreach (var p in await repo.ListAsync(p => p.Status == AnalysisStatus.Processing)) p.Status = AnalysisStatus.Pending;
            await repo.SaveChangesAsync();
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<AnalysisService>();
                while (await svc.ProcessNextAsync(ct)) { }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested) { log.LogError(ex, "YZ analiz döngüsünde hata"); }
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
}
