namespace PciDss.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Organization : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? TaxNumber { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
    public ScopeProfile? Scope { get; set; }
}

public enum UserRole { Admin, OrgUser }

public class User : BaseEntity
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.OrgUser;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}

/// <summary>PCI DSS gereksinimi (ör. 3.5.1 - Saklanan PAN şifrelenmeli)</summary>
public class Requirement : BaseEntity
{
    public string Code { get; set; } = null!;        // "3.5.1"
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Section { get; set; }                  // 1-12 ana gereksinim
    public SaqApplicability Applicability { get; set; } // SAQ D hariç hangi SAQ'larda yer aldığı (D tümünü kapsar)
    public bool ServiceProviderOnly { get; set; }       // Yalnızca servis sağlayıcılara (SAQ D-SP) sorulur
    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
}

public class SurveyQuestion : BaseEntity
{
    public string Text { get; set; } = null!;
    public int Order { get; set; }
    public bool EvidenceRequired { get; set; }
    public string? EvidenceHint { get; set; }        // Beklenen kanıt türü (ör. "Firewall kural seti dökümü")
    public int RequirementId { get; set; }
    public Requirement Requirement { get; set; } = null!;
}

// SAQ cevap seçenekleri. CCW, NotApplicable ve NotTested gerekçe ister.
public enum AnswerValue { InPlace, InPlaceWithCcw, NotApplicable, NotTested, NotInPlace }

public class SurveyAnswer : BaseEntity
{
    public AnswerValue Value { get; set; }
    public string? Comment { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int SurveyQuestionId { get; set; }
    public SurveyQuestion SurveyQuestion { get; set; } = null!;
}

/// <summary>Kurumun yüklediği kanıt belgesi (politika, sistem raporu vb.)</summary>
public class Policy : BaseEntity
{
    public string OriginalFileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = null!;
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending; // AnalysisWorker günceller

    // YZ (RAG) analiz sonucu. Failed durumunda AiSummary hata mesajını taşır.
    public ComplianceVerdict? AiVerdict { get; set; }
    public double? AiConfidence { get; set; }
    public string? AiSummary { get; set; }
    public string? AiGaps { get; set; }    // satır satır eksikler
    public string? AiQuotes { get; set; }  // satır satır belgeden alıntılar
    public string? AiModel { get; set; }
    public DateTime? AnalyzedAt { get; set; }

    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int? RequirementId { get; set; }
    public Requirement? Requirement { get; set; }
}

public enum AnalysisStatus { Pending, Processing, Completed, Failed }

// YZ'nin kanıt belgesi için kararı. Sıra önemli: küçük değer daha iyi (birden fazla belgede en iyisi seçilir).
public enum ComplianceVerdict { Compliant, Partial, NonCompliant }

// ---------------- GAP ANALİZİ ----------------
// Şimdilik beyan ve kanıt durumundan anlık hesaplanır; Faz 3'te kalıcı Finding varlığına dönüşecek.
public enum FindingSeverity { Critical, Medium, Low }

public enum FindingKind
{
    NotInPlace,      // Kontrol uygulanmıyor
    NotTested,       // Kontrol test edilmedi
    Unanswered,      // Beyan verilmedi
    MissingEvidence, // Uygulanıyor denildi ama zorunlu kanıt yok
    AiNonCompliant,  // Kanıt yüklendi ama YZ'ye göre gereksinimi karşılamıyor
    AiPartial,       // Kanıt YZ'ye göre gereksinimi kısmen karşılıyor
    Compensating     // Telafi edici kontrolle karşılanıyor (CCW)
}

// ---------------- KAPSAM (SAQ belirleme) ----------------
public enum EntityType { Merchant, ServiceProvider }

public enum SaqType { A, AEP, B, BIP, C, CVT, P2PE, SPoC, DMerchant, DServiceProvider }

[Flags]
public enum SaqApplicability
{
    None = 0, A = 1, AEP = 2, B = 4, BIP = 8, C = 16, CVT = 32, P2PE = 64, SPoC = 128,
    All = A | AEP | B | BIP | C | CVT | P2PE | SPoC
}

/// <summary>E-ticaret ödeme sayfasının nasıl sunulduğu</summary>
public enum EcommerceMethod
{
    FullyOutsourced,   // Yönlendirme veya iframe: ödeme sayfası tamamen PCI uyumlu sağlayıcıda (SAQ A)
    PartialOutsourced, // Direct post / JavaScript: sayfa kurumun sitesinden geliyor, veri sağlayıcıya gidiyor (SAQ A-EP)
    OwnSystems         // Kart verisini kurumun kendi sunucusu alıyor/işliyor (SAQ D)
}

/// <summary>Kart-mevcut (yüz yüze) işlemlerde kullanılan terminal/çözüm</summary>
public enum CardPresentMethod
{
    StandaloneDialup,       // Telefon hattı/mobil bağlantılı, başka sisteme bağlı olmayan terminal (SAQ B)
    StandaloneIp,           // IP bağlantılı, başka sisteme bağlı olmayan PTS onaylı terminal (SAQ B-IP)
    ValidatedP2pe,          // PCI listesinde yer alan doğrulanmış P2PE çözümü (SAQ P2PE)
    SoftwarePinOnCots,      // PCI listesinde yer alan SPoC çözümü (SAQ SPoC)
    PaymentAppConnected     // İnternete bağlı ödeme uygulaması/POS sistemi (SAQ C)
}

/// <summary>Posta/telefon siparişi (MOTO) işlemleri</summary>
public enum MotoMethod
{
    FullyOutsourced,  // Tamamen PCI uyumlu sağlayıcıya devredilmiş (SAQ A)
    VirtualTerminal,  // Sağlayıcının web tabanlı sanal terminaline elle giriş (SAQ C-VT)
    OwnSystems        // Kurumun kendi sistemleriyle (SAQ D)
}

/// <summary>Kurumun kapsam beyanı ve buna göre belirlenen SAQ tipi</summary>
public class ScopeProfile : BaseEntity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public EntityType EntityType { get; set; }
    public bool StoresCardDataElectronically { get; set; }
    public EcommerceMethod? Ecommerce { get; set; }     // null = bu kanal kullanılmıyor
    public CardPresentMethod? CardPresent { get; set; }
    public MotoMethod? Moto { get; set; }
    public SaqType SaqType { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
