using PciDss.Entities;

namespace PciDss.Business.Dtos;

// Auth
// IsAuditor: denetçi hesabı; OrganizationName bu durumda denetim firmasının adıdır
public record RegisterDto(string FullName, string Email, string Password, string OrganizationName, bool IsAuditor = false);
public record LoginDto(string Email, string Password);
public record AuthResultDto(string Token, string FullName, string OrganizationName, UserRole Role);

// Scope
public record ScopeDto(EntityType EntityType, bool StoresCardDataElectronically,
                       EcommerceMethod? Ecommerce, CardPresentMethod? CardPresent, MotoMethod? Moto);
public record ScopeResultDto(ScopeDto Scope, SaqType SaqType, string Explanation, int QuestionCount);

// Survey
public record QuestionDto(int Id, int Order, string Text, bool EvidenceRequired, string? EvidenceHint,
                          string RequirementCode, string RequirementTitle, string RequirementDescription, int Section,
                          AnswerValue? CurrentAnswer, string? CurrentComment, bool EvidenceUploaded,
                          AuditorReviewDto? Review = null);
// Stale: karardan sonra yeni kanıt yüklendi; karar artık geçerli sayılmaz
public record AuditorReviewDto(ReviewDecision Decision, string? Note, string AuditorName, DateTime ReviewedAt, bool Stale);
public record SurveyDto(SaqType SaqType, List<QuestionDto> Questions);
public record AnswerItemDto(int QuestionId, AnswerValue Value, string? Comment);
public record SubmitAnswersDto(List<AnswerItemDto> Answers);
public record SurveyProgressDto(SaqType SaqType, int Total, int Answered, int InPlace, int InPlaceWithCcw,
                                int NotApplicable, int NotTested, int NotInPlace, int MissingEvidence);

// Gap analizi
public record FindingDto(int QuestionId, string RequirementCode, string RequirementTitle, int Section,
                         FindingKind Kind, FindingSeverity Severity, string Issue, string Recommendation);
// Eksik = Kritik veya Orta bulgusu olan gereksinim. Tamam = uygulanabilir olup eksiği olmayan gereksinim.
// "Uygulanamaz" cevaplananlar ne tamam ne eksik sayılır (NotApplicable).
public record SectionStatusDto(int Section, int Total, int Complete, int Open, int NotApplicable, int CriticalCount);
public record GapAnalysisDto(SaqType SaqType, int Total, int Complete, int Open, int NotApplicable, int Answered,
                             int InPlace, int NotInPlace,
                             int MissingEvidence, List<SectionStatusDto> Sections, List<FindingDto> Findings,
                             int AuditorApproved = 0, int AuditorRejected = 0);

// Denetçi erişimi (kurum tarafı)
public record GrantAuditorDto(string Email);
public record AuditorAccessDto(int Id, string FullName, string Email, string FirmName, DateTime GrantedAt);

// Denetçi paneli
public enum AuditItemState { Ready, Stale, Approved, Rejected }
public record AuditOrgDto(int OrganizationId, string Name, SaqType? SaqType, DateTime GrantedAt,
                          int Evidenced, int Ready, int Approved, int Rejected);
public record AuditItemDto(QuestionDto Question, AuditItemState State, List<PolicyDto> Files);
public record AuditOrgDetailDto(int OrganizationId, string Name, SaqType SaqType, int DeclarationOnly, List<AuditItemDto> Items);
public record ReviewDto(ReviewDecision Decision, string? Note);

// Files
public record PolicyDto(int Id, string FileName, long SizeBytes, string? RequirementCode, DateTime UploadedAt);
