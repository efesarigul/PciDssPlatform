using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.IsAuditor ? UserRole.Auditor : UserRole.OrgUser
        };
        await users.AddAsync(user);
        await users.SaveChangesAsync();
        return new AuthResultDto(CreateToken(user), user.FullName, org.Name, user.Role);
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var user = await users.Query().Include(u => u.Organization).FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new BusinessException("E-posta veya parola hatalı.");
        return new AuthResultDto(CreateToken(user), user.FullName, user.Organization.Name, user.Role);
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
                           IRepository<ScopeProfile> scopes, IRepository<Policy> policies,
                           IRepository<AuditorReview> reviews) : ISurveyService
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
        var myReviews = await ReviewsAsync(orgId);

        var list = await questions.Query().Include(q => q.Requirement)
            .Where(SaqRules.QuestionsFor(saq)).OrderBy(q => q.Order).ToListAsync();
        return new SurveyDto(saq, list.Select(q =>
        {
            myAnswers.TryGetValue(q.Id, out var a);
            return new QuestionDto(q.Id, q.Order, q.Text, q.EvidenceRequired, q.EvidenceHint,
                q.Requirement.Code, q.Requirement.Title, q.Requirement.Description, q.Requirement.Section,
                a?.Value, a?.Comment, withEvidence.Contains(q.RequirementId), ai.GetValueOrDefault(q.RequirementId),
                myReviews.GetValueOrDefault(q.RequirementId));
        }).ToList());
    }

    // Denetçi kararları. Karardan sonra gereksinime yeni belge yüklendiyse veya belge yeniden analiz edildiyse
    // karar "eskimiş" sayılır: denetçi baktığı kanıttan farklı bir kanıtı onaylamış olmasın.
    private async Task<Dictionary<int, AuditorReviewDto>> ReviewsAsync(int orgId)
    {
        var lastEvidence = await policies.Query().Where(p => p.OrganizationId == orgId && p.RequirementId != null)
            .GroupBy(p => p.RequirementId!.Value)
            .Select(g => new { g.Key, Last = g.Max(p => p.AnalyzedAt > p.CreatedAt ? p.AnalyzedAt!.Value : p.CreatedAt) })
            .ToDictionaryAsync(x => x.Key, x => x.Last);
        return (await reviews.Query().Where(r => r.OrganizationId == orgId)
                .Select(r => new { r.RequirementId, r.Decision, r.Note, r.AuditorUser.FullName, r.ReviewedAt }).ToListAsync())
            .ToDictionary(r => r.RequirementId, r => new AuditorReviewDto(r.Decision, r.Note, r.FullName, r.ReviewedAt,
                lastEvidence.TryGetValue(r.RequirementId, out var last) && last > r.ReviewedAt));
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

    // Gereksinim başına en son YZ değerlendirmesi. Her analiz gereksinime o ana kadar yüklenmiş tüm belgeleri
    // birlikte incelediği için en yenisi en kapsamlısıdır (en iyisini seçmek, uyumsuz belgeyi gizlerdi).
    private async Task<Dictionary<int, EvidenceAiDto>> BestAiResultsAsync(int orgId) =>
        (await policies.Query()
            .Where(p => p.OrganizationId == orgId && p.RequirementId != null &&
                        p.Status == AnalysisStatus.Completed && p.AiVerdict != null)
            .Select(p => new { RequirementId = p.RequirementId!.Value, Verdict = p.AiVerdict!.Value, p.AiSummary, p.AiGaps,
                               p.AiChecks, p.OriginalFileName, p.AnalyzedAt, p.Id })
            .ToListAsync())
        .GroupBy(p => p.RequirementId)
        .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.AnalyzedAt).ThenByDescending(p => p.Id)
            .Select(p => new EvidenceAiDto(p.Verdict, p.AiSummary, AiText.Lines(p.AiGaps), p.OriginalFileName,
                AiText.Checks(p.AiChecks))).First());
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
            qs.Count(MissingEvidence), sections, findings,
            qs.Count(q => ReviewIs(q, ReviewDecision.Approved)), qs.Count(q => ReviewIs(q, ReviewDecision.Rejected)));
    }

    private static bool MissingEvidence(QuestionDto q) => q.EvidenceRequired && !q.EvidenceUploaded &&
        q.CurrentAnswer is AnswerValue.InPlace or AnswerValue.InPlaceWithCcw;

    // Denetçi kararı yalnızca güncelse (karardan sonra kanıt değişmediyse) geçerlidir
    private static bool ReviewIs(QuestionDto q, ReviewDecision d) =>
        q.CurrentAnswer is AnswerValue.InPlace or AnswerValue.InPlaceWithCcw && q.Review is { Stale: false } r && r.Decision == d;

    // Denetçi kararı YZ kararının önüne geçer: onay = 1, ret = 0.
    // Aksi halde: Uygulanıyor = 1 puan; kanıtı eksikse veya YZ kısmen uyumlu bulduysa 0,5; YZ uyumsuz bulduysa 0.
    // Uygulanmıyor / test edilmedi / cevapsız = 0. "Uygulanamaz" cevapları paydaya katılmaz.
    private static double Points(QuestionDto q) => q.CurrentAnswer switch
    {
        AnswerValue.InPlace or AnswerValue.InPlaceWithCcw when ReviewIs(q, ReviewDecision.Approved) => 1,
        AnswerValue.InPlace or AnswerValue.InPlaceWithCcw when ReviewIs(q, ReviewDecision.Rejected) => 0,
        AnswerValue.InPlace or AnswerValue.InPlaceWithCcw when MissingEvidence(q) => 0.5,
        AnswerValue.InPlace or AnswerValue.InPlaceWithCcw => q.EvidenceAi?.Verdict switch
        {
            ComplianceVerdict.NonCompliant => 0,
            ComplianceVerdict.Partial => 0.5,
            _ => 1
        },
        _ => 0
    };

    // Denetçinin onayladığı kanıt için YZ bulgusu gösterilmez
    private static bool AiVerdictIs(QuestionDto q, ComplianceVerdict v) =>
        q.CurrentAnswer is AnswerValue.InPlace or AnswerValue.InPlaceWithCcw && q.EvidenceAi?.Verdict == v &&
        !ReviewIs(q, ReviewDecision.Approved);

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
            _ when ReviewIs(q, ReviewDecision.Rejected) => (FindingKind.AuditorRejected, FindingSeverity.Critical,
                "Denetçi kanıtlı beyanı reddetti.", $"{q.Review!.AuditorName}: {q.Review.Note}"),
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

// ---------------- DENETÇİ ERİŞİMİ (kurum tarafı) ----------------
public interface IAuditorAccessService
{
    Task<List<AuditorAccessDto>> ListAsync(int orgId);
    Task<AuditorAccessDto> GrantAsync(int orgId, GrantAuditorDto dto);
    Task RevokeAsync(int orgId, int accessId);
}

public class AuditorAccessService(IRepository<AuditorAccess> access, IRepository<User> users) : IAuditorAccessService
{
    public async Task<List<AuditorAccessDto>> ListAsync(int orgId) =>
        await access.Query().Where(a => a.OrganizationId == orgId).OrderBy(a => a.CreatedAt)
            .Select(a => new AuditorAccessDto(a.Id, a.AuditorUser.FullName, a.AuditorUser.Email,
                a.AuditorUser.Organization.Name, a.CreatedAt))
            .ToListAsync();

    public async Task<AuditorAccessDto> GrantAsync(int orgId, GrantAuditorDto dto)
    {
        var email = dto.Email?.Trim() ?? "";
        var auditor = await users.Query().Include(u => u.Organization)
                          .FirstOrDefaultAsync(u => u.Email == email && u.Role == UserRole.Auditor)
                      ?? throw new BusinessException("Bu e-postayla kayıtlı bir denetçi hesabı bulunamadı.");
        if (await access.Query().AnyAsync(a => a.OrganizationId == orgId && a.AuditorUserId == auditor.Id))
            throw new BusinessException("Bu denetçinin zaten erişimi var.");
        var a = new AuditorAccess { OrganizationId = orgId, AuditorUserId = auditor.Id };
        await access.AddAsync(a);
        await access.SaveChangesAsync();
        return new AuditorAccessDto(a.Id, auditor.FullName, auditor.Email, auditor.Organization.Name, a.CreatedAt);
    }

    // Erişim kaldırılır; denetçinin verdiği kararlar kurumda kayıt olarak kalır
    public async Task RevokeAsync(int orgId, int accessId)
    {
        var a = await access.Query().FirstOrDefaultAsync(x => x.Id == accessId && x.OrganizationId == orgId)
                ?? throw new BusinessException("Erişim kaydı bulunamadı.");
        access.Remove(a);
        await access.SaveChangesAsync();
    }
}

// ---------------- DENETÇİ PANELİ ----------------
public interface IAuditService
{
    Task<List<AuditOrgDto>> OrganizationsAsync(int auditorId);
    Task<AuditOrgDetailDto> OrganizationAsync(int auditorId, int orgId);
    Task<AuditorReviewDto> ReviewAsync(int auditorId, int orgId, int questionId, ReviewDto dto);
    Task<(string Path, string ContentType, string FileName)> FileAsync(int auditorId, int orgId, int policyId);
}

/// <summary>
/// Denetçi yalnızca kendisine erişim verilmiş kurumları görür ve hiçbir kurum verisini değiştiremez; yalnızca karar verir.
/// Kuyruğa yalnızca kanıtlı beyanlar girer; YZ kontrolü bitmeden karar verilemez.
/// </summary>
public class AuditService(IRepository<AuditorAccess> access, IRepository<AuditorReview> reviews,
                          IRepository<SurveyQuestion> questions, IRepository<Policy> policies,
                          ISurveyService survey, IFileService files, IConfiguration cfg) : IAuditService
{
    public async Task<List<AuditOrgDto>> OrganizationsAsync(int auditorId)
    {
        var grants = await access.Query().Where(a => a.AuditorUserId == auditorId).OrderBy(a => a.Organization.Name)
            .Select(a => new { a.OrganizationId, a.Organization.Name, a.CreatedAt, Saq = (SaqType?)a.Organization.Scope!.SaqType })
            .ToListAsync();
        var result = new List<AuditOrgDto>();
        foreach (var g in grants)
        {
            // Kapsamı belirlenmemiş kurumda henüz incelenecek beyan yoktur
            var items = g.Saq is null ? [] : await ItemsAsync(g.OrganizationId);
            result.Add(new AuditOrgDto(g.OrganizationId, g.Name, g.Saq, g.CreatedAt, items.Count,
                items.Count(i => i.State is AuditItemState.Ready or AuditItemState.Stale),
                items.Count(i => i.State == AuditItemState.Approved),
                items.Count(i => i.State == AuditItemState.Rejected)));
        }
        return result;
    }

    public async Task<AuditOrgDetailDto> OrganizationAsync(int auditorId, int orgId)
    {
        var name = await RequireAccessAsync(auditorId, orgId);
        var s = await survey.GetQuestionsAsync(orgId);
        var items = await ItemsAsync(orgId, s);
        var declarationOnly = s.Questions.Count(q => q.CurrentAnswer is AnswerValue.InPlace or AnswerValue.InPlaceWithCcw &&
                                                     items.All(i => i.Question.Id != q.Id));
        return new AuditOrgDetailDto(orgId, name, s.SaqType, declarationOnly, items);
    }

    public async Task<AuditorReviewDto> ReviewAsync(int auditorId, int orgId, int questionId, ReviewDto dto)
    {
        await RequireAccessAsync(auditorId, orgId);
        var note = dto.Note?.Trim();
        if (dto.Decision == ReviewDecision.Rejected && string.IsNullOrEmpty(note))
            throw new BusinessException("Ret kararı için gerekçe yazmalısınız; kurum bu gerekçeyi bulgu olarak görür.");
        if (note?.Length > 2000) throw new BusinessException("Not en fazla 2000 karakter olabilir.");

        var item = (await ItemsAsync(orgId)).FirstOrDefault(i => i.Question.Id == questionId)
                   ?? throw new BusinessException("Bu gereksinim için incelenecek kanıtlı beyan yok.");
        if (item.State == AuditItemState.WaitingSystem)
            throw new BusinessException("Kanıtın sistem (YZ) kontrolü tamamlanmadan karar verilemez.");

        var requirementId = await questions.Query().Where(q => q.Id == questionId).Select(q => q.RequirementId).FirstAsync();
        var r = await reviews.Query().FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.RequirementId == requirementId);
        if (r is null) await reviews.AddAsync(r = new AuditorReview { OrganizationId = orgId, RequirementId = requirementId });
        r.AuditorUserId = auditorId;
        r.Decision = dto.Decision;
        r.Note = string.IsNullOrEmpty(note) ? null : note;
        r.ReviewedAt = DateTime.UtcNow;
        await reviews.SaveChangesAsync();

        var reviewed = (await survey.GetQuestionsAsync(orgId)).Questions.First(q => q.Id == questionId);
        return reviewed.Review!;
    }

    // Denetçinin belgeyi kendisinin okuyabilmesi için. Dosya adı diskte GUID'dir; istemciden yol alınmaz.
    public async Task<(string Path, string ContentType, string FileName)> FileAsync(int auditorId, int orgId, int policyId)
    {
        await RequireAccessAsync(auditorId, orgId);
        var p = await policies.Query().FirstOrDefaultAsync(x => x.Id == policyId && x.OrganizationId == orgId)
                ?? throw new BusinessException("Belge bulunamadı.");
        var path = Path.GetFullPath(Path.Combine(cfg["Storage:Root"] ?? "uploads", orgId.ToString(), p.StoredFileName));
        if (!File.Exists(path)) throw new BusinessException("Belge dosyası sunucuda bulunamadı.");
        // İçerik türü istemcinin bildirdiğinden değil uzantıdan belirlenir
        var type = Path.GetExtension(p.StoredFileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "text/plain; charset=utf-8"
        };
        return (path, type, p.OriginalFileName);
    }

    private async Task<string> RequireAccessAsync(int auditorId, int orgId) =>
        await access.Query().Where(a => a.AuditorUserId == auditorId && a.OrganizationId == orgId)
            .Select(a => a.Organization.Name).FirstOrDefaultAsync()
        ?? throw new BusinessException("Bu kuruma erişim yetkiniz yok.");

    // Kanıt yüklenmiş ve cevaplanmış gereksinimler; durum YZ analizine ve denetçi kararına göre belirlenir
    private async Task<List<AuditItemDto>> ItemsAsync(int orgId, SurveyDto? s = null)
    {
        s ??= await survey.GetQuestionsAsync(orgId);
        var byReq = (await files.ListAsync(orgId)).Where(f => f.RequirementCode != null)
            .GroupBy(f => f.RequirementCode!).ToDictionary(g => g.Key, g => g.ToList());
        return s.Questions
            .Where(q => q.CurrentAnswer != null && byReq.ContainsKey(q.RequirementCode))
            .Select(q =>
            {
                var docs = byReq[q.RequirementCode];
                // Yeni yüklenen belge analiz edilirken de beklenir: denetçi YZ'nin görmediği kanıta karar vermesin
                var state = docs.Any(f => f.Status is AnalysisStatus.Pending or AnalysisStatus.Processing) ||
                            !docs.Any(f => f.Status == AnalysisStatus.Completed) ? AuditItemState.WaitingSystem
                    : q.Review is null ? AuditItemState.Ready
                    : q.Review.Stale ? AuditItemState.Stale
                    : q.Review.Decision == ReviewDecision.Approved ? AuditItemState.Approved : AuditItemState.Rejected;
                return new AuditItemDto(q, state, docs);
            })
            .ToList();
    }
}

// ---------------- DEMO ----------------
public interface IDemoService
{
    Task<AuthResultDto> ResetAndLoginAsync();
    Task<AuthResultDto> AuditorLoginAsync();
}

/// <summary>
/// Sunum için hazır demo kurumu. Her girişte aynı başlangıç durumuna sıfırlanır: SAQ A-EP, cevapların çoğu dolu,
/// birkaç kritik bulgu, eksik kanıt ve canlı cevaplanmak üzere boş bırakılmış sorular.
/// </summary>
public class DemoService(IAuthService auth, IScopeService scope, IRepository<User> users,
                         IRepository<SurveyQuestion> questions, IRepository<SurveyAnswer> answers,
                         IRepository<Policy> policies, IRepository<AuditorAccess> access,
                         IRepository<AuditorReview> reviews, IConfiguration cfg) : IDemoService
{
    private const string Email = "demo@ornek-eticaret.test";
    private const string Password = "DemoSunum-2026!";
    private const string OrgName = "Örnek E-Ticaret A.Ş. (Demo)";
    // Demo kurumunun erişim verdiği demo denetçi
    private const string AuditorEmail = "demo-denetci@ornek-denetim.test";
    private const string AuditorFirm = "Örnek Denetim Ltd. (Demo)";

    /// <summary>
    /// Demo denetçiyle giriş. Demo kurumu sıfırlamaz: sunumda kurum tarafında yapılanları (canlı yüklenen belge vb.)
    /// denetçi tarafında göstermek için. Demo kurumu hiç oluşturulmadıysa bir kez hazırlanır.
    /// </summary>
    public async Task<AuthResultDto> AuditorLoginAsync()
    {
        if (!cfg.GetValue<bool>("Demo:Enabled")) throw new BusinessException("Demo hesabı bu ortamda kapalı.");
        if (!await users.Query().AnyAsync(u => u.Email == Email)) await ResetAndLoginAsync();
        await EnsureAuditorAccessAsync(await users.Query().Where(u => u.Email == Email).Select(u => u.OrganizationId).FirstAsync());
        return await auth.LoginAsync(new LoginDto(AuditorEmail, Password));
    }

    private async Task EnsureAuditorAccessAsync(int orgId)
    {
        if (!await users.Query().AnyAsync(u => u.Email == AuditorEmail))
            await auth.RegisterAsync(new RegisterDto("Demo Denetçi", AuditorEmail, Password, AuditorFirm, IsAuditor: true));
        var auditorId = await users.Query().Where(u => u.Email == AuditorEmail).Select(u => u.Id).FirstAsync();
        if (!await access.Query().AnyAsync(a => a.OrganizationId == orgId && a.AuditorUserId == auditorId))
        {
            await access.AddAsync(new AuditorAccess { OrganizationId = orgId, AuditorUserId = auditorId });
            await access.SaveChangesAsync();
        }
    }

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
        foreach (var r in await reviews.ListAsync(r => r.OrganizationId == orgId)) reviews.Remove(r);
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
        await EnsureAuditorAccessAsync(orgId); // "Denetçi Erişimi" ekranında hazır bir denetçi görünsün
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
                    AiText.Lines(p.AiQuotes), p.AiModel, p.AnalyzedAt, AiText.Checks(p.AiChecks))
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

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static string? ChecksJson(List<AiCheckDto>? checks) => checks is { Count: > 0 } ? JsonSerializer.Serialize(checks, Json) : null;
    public static List<AiCheckDto>? Checks(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<AiCheckDto>>(json, Json);
}

// ---------------- YZ ANALİZİ (Faz 2) ----------------
/// <summary>
/// Bekleyen kanıt belgelerini Python RAG servisine (ai-service) gönderir ve sonucu Policy'ye yazar.
/// Yalnızca bir gereksinimle ilişkilendirilmiş belgeler analiz edilir. Kanıt çoğu zaman birkaç belgeye dağıldığı için
/// (politika + tutanak + log) gereksinime ait tüm belgeler birlikte gönderilir ve kontrol listesiyle karşılaştırılır.
/// </summary>
public class AnalysisService(HttpClient http, IRepository<Policy> policies, IRepository<SurveyQuestion> questions,
                             IConfiguration cfg)
{
    private record AiResponse(string Verdict, double Confidence, string Summary, List<string> Gaps,
                              List<string> Quotes, string Model, List<AiCheckDto>? Checks);

    /// <summary>Sıradaki bekleyen belgeyi analiz eder; bekleyen yoksa false döner.</summary>
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var p = await policies.Query().Include(x => x.Requirement)
            .Where(x => x.Status == AnalysisStatus.Pending && x.RequirementId != null)
            .OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (p is null) return false;

        p.Status = AnalysisStatus.Processing;
        await policies.SaveChangesAsync();
        // Aynı analize dahil edilen, sırada bekleyen diğer belgeler de bu sonucu alır (ayrıca analiz edilmez)
        var batchPending = new List<Policy>();
        try
        {
            var q = await questions.Query().FirstOrDefaultAsync(x => x.RequirementId == p.RequirementId, ct);
            var dir = Path.Combine(cfg["Storage:Root"] ?? "uploads", p.OrganizationId.ToString());
            var docs = await policies.Query()
                .Where(x => x.OrganizationId == p.OrganizationId && x.RequirementId == p.RequirementId)
                .OrderBy(x => x.CreatedAt).ToListAsync(ct);
            // Dosyalar belleğe okunur: analiz sürerken diskte kilitli kalmasın (demo sıfırlama silebilsin)
            using var form = new MultipartFormDataContent();
            foreach (var d in docs)
            {
                var path = Path.Combine(dir, d.StoredFileName);
                if (d.Id != p.Id && !File.Exists(path)) continue;
                form.Add(new ByteArrayContent(await File.ReadAllBytesAsync(path, ct)), "files", d.OriginalFileName);
                form.Add(new StringContent(d.Id.ToString()), "policy_ids");
                if (d.Id != p.Id && d.Status == AnalysisStatus.Pending) batchPending.Add(d);
            }
            form.Add(new StringContent(p.Id.ToString()), "target_policy_id");
            foreach (var (value, name) in new[]
            {
                (p.OrganizationId.ToString(), "org_id"),
                (p.Requirement!.Code, "requirement_code"),
                (p.Requirement.Title, "requirement_title"),
                (p.Requirement.Description, "requirement_description"),
                (q?.Text ?? "", "question"),
                (q?.EvidenceHint ?? "", "evidence_hint"),
            })
                form.Add(new StringContent(value), name);
            using var res = await http.PostAsync("analyze", form, ct);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>(ct);
                throw new InvalidOperationException(err?.GetValueOrDefault("detail")?.ToString() ?? $"YZ servisi {(int)res.StatusCode} döndü.");
            }
            var r = (await res.Content.ReadFromJsonAsync<AiResponse>(ct))!;
            var now = DateTime.UtcNow;
            foreach (var d in batchPending.Prepend(p))
            {
                d.AiVerdict = Enum.Parse<ComplianceVerdict>(r.Verdict);
                d.AiConfidence = r.Confidence;
                d.AiSummary = r.Summary;
                // Satır bazlı saklanır; YZ'nin metnindeki satır sonları maddeleri bölmesin
                d.AiGaps = string.Join('\n', r.Gaps.Select(g => g.ReplaceLineEndings(" ")));
                d.AiQuotes = string.Join('\n', r.Quotes.Select(x => x.ReplaceLineEndings(" ")));
                d.AiChecks = AiText.ChecksJson(r.Checks);
                d.AiModel = r.Model;
                d.Status = AnalysisStatus.Completed;
                d.AnalyzedAt = now;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            p.AiVerdict = null;
            p.AiChecks = null;
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
