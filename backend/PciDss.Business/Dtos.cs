using PciDss.Entities;

namespace PciDss.Business.Dtos;

// Auth
public record RegisterDto(string FullName, string Email, string Password, string OrganizationName);
public record LoginDto(string Email, string Password);
public record AuthResultDto(string Token, string FullName, string OrganizationName);

// Scope
public record ScopeDto(EntityType EntityType, bool StoresCardDataElectronically,
                       EcommerceMethod? Ecommerce, CardPresentMethod? CardPresent, MotoMethod? Moto);
public record ScopeResultDto(ScopeDto Scope, SaqType SaqType, string Explanation, int QuestionCount);

// Survey
public record QuestionDto(int Id, int Order, string Text, bool EvidenceRequired, string? EvidenceHint,
                          string RequirementCode, string RequirementTitle, string RequirementDescription, int Section,
                          AnswerValue? CurrentAnswer, string? CurrentComment, bool EvidenceUploaded,
                          EvidenceAiDto? EvidenceAi = null);
// Gereksinime yüklenen belgelerden YZ'nin en iyi değerlendirdiği
public record EvidenceAiDto(ComplianceVerdict Verdict, string? Summary, List<string> Gaps, string FileName);
public record SurveyDto(SaqType SaqType, List<QuestionDto> Questions);
public record AnswerItemDto(int QuestionId, AnswerValue Value, string? Comment);
public record SubmitAnswersDto(List<AnswerItemDto> Answers);
public record SurveyProgressDto(SaqType SaqType, int Total, int Answered, int InPlace, int InPlaceWithCcw,
                                int NotApplicable, int NotTested, int NotInPlace, int MissingEvidence);

// Gap analizi
public record FindingDto(int QuestionId, string RequirementCode, string RequirementTitle, int Section,
                         FindingKind Kind, FindingSeverity Severity, string Issue, string Recommendation);
public record SectionScoreDto(int Section, int Total, int Score, int CriticalCount);
public record GapAnalysisDto(SaqType SaqType, int Score, int Total, int Answered, int InPlace, int NotInPlace,
                             int MissingEvidence, List<SectionScoreDto> Sections, List<FindingDto> Findings);

// Files
public record PolicyDto(int Id, string FileName, long SizeBytes, string? RequirementCode,
                        AnalysisStatus Status, DateTime UploadedAt, AiResultDto? Ai = null);
public record AiResultDto(ComplianceVerdict? Verdict, double? Confidence, string? Summary,
                          List<string> Gaps, List<string> Quotes, string? Model, DateTime? AnalyzedAt);
