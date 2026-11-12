using Microsoft.EntityFrameworkCore;
using PciDss.Entities;

namespace PciDss.DataAccess;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<ScopeProfile> ScopeProfiles => Set<ScopeProfile>();
    public DbSet<AuditorAccess> AuditorAccesses => Set<AuditorAccess>();
    public DbSet<AuditorReview> AuditorReviews => Set<AuditorReview>();

    // Seed satırlarında sabit tarih: aksi halde her derlemede sahte migration üretilir
    private static readonly DateTime Seed = new(2026, 1, 1);

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().Property(u => u.Email).HasMaxLength(200);
        b.Entity<Organization>().Property(o => o.Name).HasMaxLength(200);
        b.Entity<Requirement>().HasIndex(r => r.Code).IsUnique();

        // Kurum başına tek kapsam beyanı
        b.Entity<ScopeProfile>()
            .HasOne(s => s.Organization).WithOne(o => o.Scope)
            .HasForeignKey<ScopeProfile>(s => s.OrganizationId);
        b.Entity<ScopeProfile>().HasIndex(s => s.OrganizationId).IsUnique();

        // Kurum başına her soruya tek cevap
        b.Entity<SurveyAnswer>()
            .HasIndex(a => new { a.OrganizationId, a.SurveyQuestionId }).IsUnique();
        b.Entity<SurveyAnswer>()
            .HasOne(a => a.SurveyQuestion).WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Policy>()
            .HasOne(p => p.Requirement).WithMany()
            .OnDelete(DeleteBehavior.SetNull);

        // Denetçi erişimi: kurum + denetçi başına tek kayıt. Denetçi kullanıcısı başka bir kuruma (denetim firmasına)
        // bağlı olduğundan kullanıcı ilişkisi kaskat silinmez (SQL Server çoklu kaskat yolu hatası).
        b.Entity<AuditorAccess>()
            .HasIndex(a => new { a.OrganizationId, a.AuditorUserId }).IsUnique();
        b.Entity<AuditorAccess>()
            .HasOne(a => a.AuditorUser).WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // Kurum + gereksinim başına tek denetçi kararı (son karar geçerli)
        b.Entity<AuditorReview>()
            .HasIndex(r => new { r.OrganizationId, r.RequirementId }).IsUnique();
        b.Entity<AuditorReview>()
            .HasOne(r => r.AuditorUser).WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuditorReview>()
            .HasOne(r => r.Requirement).WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuditorReview>().Property(r => r.Note).HasMaxLength(2000);

        // PCI DSS v4.0.1 alt gereksinimleri. Metinler resmi belgelerden kopyalanmamış Türkçe özetlerdir.
        // Applicability: gereksinimin hangi SAQ'larda sorulduğu (SAQ D hepsini kapsar). Eşleme ön değerlendirme
        // amaçlıdır; resmi SAQ belgeleriyle doğrulanmalıdır.
        b.Entity<Requirement>().HasData(
            new Requirement { Id = 1, Code = "1.2.1", Section = 1, Applicability = SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C,
                Title = "Ağ güvenlik kontrolleri yapılandırma standartları",
                Description = "NSC kural setleri için yapılandırma standartları tanımlanmalı, uygulanmalı ve güncel tutulmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 5, Code = "1.2.7", Section = 1, Applicability = SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C,
                Title = "NSC yapılandırmalarının periyodik gözden geçirilmesi",
                Description = "NSC yapılandırmaları en az altı ayda bir gözden geçirilerek gereksiz kurallar kaldırılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 6, Code = "1.3.1", Section = 1, Applicability = SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C,
                Title = "Kart verisi ortamına gelen trafiğin kısıtlanması",
                Description = "CDE'ye gelen trafik yalnızca gerekli olanla sınırlandırılmalı, geri kalanı engellenmelidir.", CreatedAt = Seed },
            new Requirement { Id = 7, Code = "1.4.1", Section = 1, Applicability = SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C,
                Title = "Güvenilir ve güvenilmeyen ağlar arasında NSC",
                Description = "Güvenilir iç ağlar ile internet gibi güvenilmeyen ağlar arasına NSC yerleştirilmelidir.", CreatedAt = Seed },
            new Requirement { Id = 8, Code = "2.2.1", Section = 2, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "Sistem bileşenleri için sıkılaştırma standartları",
                Description = "Tüm sistem bileşenleri için bilinen açıkları kapatan, sektörce kabul görmüş sıkılaştırma standartları uygulanmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 9, Code = "2.2.2", Section = 2, Applicability = SaqApplicability.A | SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Üretici varsayılan hesapları",
                Description = "Üretici varsayılan hesapları kullanılacaksa parolaları değiştirilmeli, kullanılmayacaksa kaldırılmalı veya devre dışı bırakılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 10, Code = "2.3.1", Section = 2, Applicability = SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C,
                Title = "Kablosuz ağ varsayılanları",
                Description = "CDE'ye bağlı kablosuz ağlarda şifreleme anahtarları, parolalar ve SNMP gibi tüm varsayılan ayarlar değiştirilmelidir.", CreatedAt = Seed },
            new Requirement { Id = 11, Code = "3.2.1", Section = 3, Applicability = SaqApplicability.AEP | SaqApplicability.B | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT | SaqApplicability.P2PE | SaqApplicability.SPoC,
                Title = "Hesap verisi saklamanın en aza indirilmesi",
                Description = "Hesap verisi saklama süresi ve yöntemi tanımlı bir saklama/imha politikasıyla sınırlandırılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 12, Code = "3.3.1", Section = 3, Applicability = SaqApplicability.AEP | SaqApplicability.B | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT | SaqApplicability.P2PE | SaqApplicability.SPoC,
                Title = "Hassas doğrulama verisinin saklanmaması",
                Description = "Yetkilendirme sonrasında tam iz verisi, CVV/CVC ve PIN bilgisi saklanmamalıdır.", CreatedAt = Seed },
            new Requirement { Id = 13, Code = "3.4.1", Section = 3, Applicability = SaqApplicability.AEP | SaqApplicability.B | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "PAN'ın maskelenerek gösterilmesi",
                Description = "PAN ekranlarda ve çıktılarda maskelenmeli; ilk 6 / son 4 haneden fazlasını yalnızca iş ihtiyacı olanlar görebilmelidir.", CreatedAt = Seed },
            new Requirement { Id = 2, Code = "3.5.1", Section = 3, Applicability = SaqApplicability.None,
                Title = "Saklanan PAN'ın okunamaz hale getirilmesi",
                Description = "PAN saklandığı her yerde güçlü kriptografi, tokenizasyon, anahtarlı hash veya kırpma ile okunamaz yapılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 14, Code = "3.6.1", Section = 3, Applicability = SaqApplicability.None,
                Title = "Kriptografik anahtarların korunması",
                Description = "PAN'ı korumak için kullanılan anahtarların ifşa ve kötüye kullanımına karşı prosedürler tanımlanmalı ve uygulanmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 15, Code = "3.6.1.1", Section = 3, Applicability = SaqApplicability.None, ServiceProviderOnly = true,
                Title = "Kriptografik mimarinin dokümantasyonu (SP)",
                Description = "Servis sağlayıcılar kullandıkları algoritma, protokol ve anahtarları içeren güncel bir kriptografik mimari dokümanı tutmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 16, Code = "4.2.1", Section = 4, Applicability = SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Açık ağlarda güçlü kriptografi",
                Description = "PAN açık/genel ağlar üzerinden iletilirken güçlü kriptografi ve güvenilir sertifikalar kullanılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 17, Code = "4.2.2", Section = 4, Applicability = SaqApplicability.AEP | SaqApplicability.B | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT | SaqApplicability.P2PE | SaqApplicability.SPoC,
                Title = "Son kullanıcı mesajlaşmasında PAN",
                Description = "PAN e-posta, anlık mesajlaşma, SMS gibi kanallarla gönderiliyorsa güçlü kriptografi ile korunmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 18, Code = "5.2.1", Section = 5, Applicability = SaqApplicability.AEP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Kötü amaçlı yazılımdan korunma",
                Description = "Kötü amaçlı yazılım riski taşıyan tüm sistem bileşenlerinde anti-malware çözümü kurulu olmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 19, Code = "5.3.1", Section = 5, Applicability = SaqApplicability.AEP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Anti-malware güncelliği",
                Description = "Anti-malware çözümü ve imzaları otomatik olarak güncel tutulmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 20, Code = "5.4.1", Section = 5, Applicability = SaqApplicability.AEP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Oltalama (phishing) koruması",
                Description = "Kullanıcıları oltalama saldırılarına karşı korumak için tespit ve koruma mekanizmaları bulunmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 21, Code = "6.2.1", Section = 6, Applicability = SaqApplicability.None,
                Title = "Güvenli yazılım geliştirme",
                Description = "Özel ve ısmarlama yazılımlar güvenli geliştirme standartlarına göre geliştirilmelidir.", CreatedAt = Seed },
            new Requirement { Id = 22, Code = "6.3.1", Section = 6, Applicability = SaqApplicability.A | SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Güvenlik açıklarının tespiti ve önceliklendirilmesi",
                Description = "Yeni güvenlik açıkları güvenilir kaynaklardan izlenmeli ve risk seviyesine göre sınıflandırılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 23, Code = "6.3.3", Section = 6, Applicability = SaqApplicability.A | SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Kritik yamaların zamanında uygulanması",
                Description = "Kritik güvenlik yamaları yayımlanmalarından itibaren bir ay içinde uygulanmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 24, Code = "6.4.1", Section = 6, Applicability = SaqApplicability.AEP,
                Title = "Herkese açık web uygulamalarının korunması",
                Description = "Herkese açık web uygulamaları bilinen saldırılara karşı düzenli test veya otomatik bir çözümle (WAF) korunmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 25, Code = "6.4.3", Section = 6, Applicability = SaqApplicability.AEP,
                Title = "Ödeme sayfası betiklerinin yönetimi",
                Description = "Ödeme sayfasında çalışan tüm betikler yetkilendirilmeli, bütünlükleri doğrulanmalı ve gerekçeleriyle envanteri tutulmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 26, Code = "7.2.1", Section = 7, Applicability = SaqApplicability.AEP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Erişim kontrol modeli",
                Description = "İş ve erişim ihtiyacına göre erişimi düzenleyen bir erişim kontrol modeli tanımlanmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 27, Code = "7.2.2", Section = 7, Applicability = SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "En az yetki ilkesi",
                Description = "Kullanıcılara yalnızca görevleri için gereken en az yetki verilmelidir.", CreatedAt = Seed },
            new Requirement { Id = 28, Code = "8.2.1", Section = 8, Applicability = SaqApplicability.A | SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Benzersiz kullanıcı kimlikleri",
                Description = "Sistemlere ve kart verisine erişen her kullanıcıya benzersiz bir kimlik atanmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 29, Code = "8.2.2", Section = 8, Applicability = SaqApplicability.A | SaqApplicability.AEP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Ortak hesapların sınırlandırılması",
                Description = "Grup, paylaşılan veya ortak hesaplar yalnızca istisnai durumlarda, onaylı ve izlenebilir şekilde kullanılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 30, Code = "8.2.6", Section = 8, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "Pasif hesapların kaldırılması",
                Description = "90 gün boyunca kullanılmayan kullanıcı hesapları kaldırılmalı veya devre dışı bırakılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 31, Code = "8.3.1", Section = 8, Applicability = SaqApplicability.A | SaqApplicability.AEP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Kullanıcı kimlik doğrulaması",
                Description = "Tüm kullanıcı erişimleri parola, token veya biyometri gibi en az bir kimlik doğrulama faktörüyle doğrulanmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 3, Code = "8.3.6", Section = 8, Applicability = SaqApplicability.A | SaqApplicability.AEP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Parola karmaşıklığı",
                Description = "Parolalar en az 12 karakter (sistem desteklemiyorsa 8) olmalı ve hem harf hem rakam içermelidir.", CreatedAt = Seed },
            new Requirement { Id = 32, Code = "8.4.1", Section = 8, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "Yönetici erişiminde çok faktörlü doğrulama",
                Description = "CDE'ye konsol dışı yönetici erişimlerinde çok faktörlü kimlik doğrulama (MFA) kullanılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 33, Code = "8.4.3", Section = 8, Applicability = SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Uzaktan ağ erişiminde çok faktörlü doğrulama",
                Description = "Kurum ağına dışarıdan yapılan ve CDE'ye ulaşabilen tüm uzaktan erişimlerde MFA kullanılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 34, Code = "9.4.1", Section = 9, Applicability = SaqApplicability.All,
                Title = "Kart verisi içeren ortamların fiziksel güvenliği",
                Description = "Kart verisi içeren tüm fiziksel ve elektronik ortamlar fiziksel olarak güvence altına alınmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 35, Code = "9.4.6", Section = 9, Applicability = SaqApplicability.All,
                Title = "Basılı kart verisinin imhası",
                Description = "Kart verisi içeren basılı materyaller artık gerekmediğinde geri getirilemeyecek şekilde imha edilmelidir.", CreatedAt = Seed },
            new Requirement { Id = 36, Code = "9.5.1", Section = 9, Applicability = SaqApplicability.B | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.P2PE | SaqApplicability.SPoC,
                Title = "POI cihazlarının kurcalamaya karşı korunması",
                Description = "Kartla doğrudan etkileşen POI cihazları kurcalama ve değiştirilmeye karşı korunmalı, envanteri tutulmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 37, Code = "9.5.1.3", Section = 9, Applicability = SaqApplicability.B | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.P2PE | SaqApplicability.SPoC,
                Title = "POI kurcalama farkındalık eğitimi",
                Description = "POI cihazlarının bulunduğu alanlardaki personel kurcalama ve değiştirme girişimlerini tanıyacak şekilde eğitilmelidir.", CreatedAt = Seed },
            new Requirement { Id = 4, Code = "10.2.1", Section = 10, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "Denetim kayıtlarının etkinleştirilmesi",
                Description = "Tüm sistem bileşenlerinde ve kart verisinde denetim kayıtları (audit log) etkin ve aktif olmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 38, Code = "10.4.1", Section = 10, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "Denetim kayıtlarının günlük incelenmesi",
                Description = "Güvenlik olayları ve kritik sistemlerin logları en az günde bir kez incelenmelidir.", CreatedAt = Seed },
            new Requirement { Id = 39, Code = "10.5.1", Section = 10, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "Denetim kayıtlarının saklanması",
                Description = "Denetim kayıtları en az 12 ay saklanmalı, son üç ayı anında erişilebilir olmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 40, Code = "10.6.1", Section = 10, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "Zaman senkronizasyonu",
                Description = "Sistem saatleri zaman senkronizasyon teknolojisiyle eşitlenmelidir.", CreatedAt = Seed },
            new Requirement { Id = 41, Code = "11.2.1", Section = 11, Applicability = SaqApplicability.C,
                Title = "Yetkisiz kablosuz erişim noktalarının tespiti",
                Description = "Yetkili ve yetkisiz kablosuz erişim noktaları en az üç ayda bir tespit edilip yönetilmelidir.", CreatedAt = Seed },
            new Requirement { Id = 42, Code = "11.3.1", Section = 11, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "İç zafiyet taramaları",
                Description = "İç zafiyet taramaları en az üç ayda bir yapılmalı, yüksek ve kritik açıklar giderilip yeniden taranmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 43, Code = "11.3.2", Section = 11, Applicability = SaqApplicability.A | SaqApplicability.AEP | SaqApplicability.BIP | SaqApplicability.C,
                Title = "Dış zafiyet taramaları (ASV)",
                Description = "Dış zafiyet taramaları en az üç ayda bir PCI onaylı tarama sağlayıcısı (ASV) tarafından yapılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 44, Code = "11.4.1", Section = 11, Applicability = SaqApplicability.AEP,
                Title = "Sızma testi metodolojisi",
                Description = "Sektörce kabul görmüş bir metodolojiye dayanan sızma testi en az yılda bir yapılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 45, Code = "11.5.1", Section = 11, Applicability = SaqApplicability.AEP,
                Title = "Saldırı tespit/önleme",
                Description = "Ağa yönelik saldırılar IDS/IPS gibi tekniklerle tespit edilmeli ve/veya önlenmelidir.", CreatedAt = Seed },
            new Requirement { Id = 46, Code = "11.6.1", Section = 11, Applicability = SaqApplicability.AEP,
                Title = "Ödeme sayfası değişiklik tespiti",
                Description = "Ödeme sayfalarında yetkisiz değişiklikleri tespit edip uyarı üreten bir mekanizma bulunmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 47, Code = "12.1.1", Section = 12, Applicability = SaqApplicability.AEP | SaqApplicability.B | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT,
                Title = "Bilgi güvenliği politikası",
                Description = "Genel bir bilgi güvenliği politikası oluşturulmalı, yayımlanmalı, güncel tutulmalı ve ilgili herkese duyurulmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 48, Code = "12.3.1", Section = 12, Applicability = SaqApplicability.AEP | SaqApplicability.C,
                Title = "Hedefli risk analizi",
                Description = "Sıklığı esnek bırakılan gereksinimler için hedefli risk analizi yapılmalı ve belgelenmelidir.", CreatedAt = Seed },
            new Requirement { Id = 49, Code = "12.4.2", Section = 12, Applicability = SaqApplicability.None, ServiceProviderOnly = true,
                Title = "Operasyonel görevlerin üç aylık gözden geçirilmesi (SP)",
                Description = "Servis sağlayıcılar güvenlik görevlerinin yerine getirildiğini en az üç ayda bir gözden geçirmelidir.", CreatedAt = Seed },
            new Requirement { Id = 50, Code = "12.5.2", Section = 12, Applicability = SaqApplicability.None,
                Title = "PCI DSS kapsamının belgelenmesi",
                Description = "PCI DSS kapsamı en az yılda bir ve önemli değişikliklerde belgelenip doğrulanmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 51, Code = "12.6.1", Section = 12, Applicability = SaqApplicability.AEP | SaqApplicability.B | SaqApplicability.BIP | SaqApplicability.C | SaqApplicability.CVT | SaqApplicability.P2PE | SaqApplicability.SPoC,
                Title = "Güvenlik farkındalık programı",
                Description = "Tüm personeli bilgi güvenliği politikası ve sorumlulukları konusunda bilgilendiren bir farkındalık programı uygulanmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 52, Code = "12.8.1", Section = 12, Applicability = SaqApplicability.All,
                Title = "Üçüncü taraf servis sağlayıcı listesi",
                Description = "Hesap verisini paylaşılan veya güvenliğini etkileyebilecek tüm üçüncü taraf servis sağlayıcıların (TPSP) listesi tutulmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 53, Code = "12.8.2", Section = 12, Applicability = SaqApplicability.All,
                Title = "TPSP ile yazılı sözleşmeler",
                Description = "TPSP'lerle, kart verisinin güvenliğinden sorumluluklarını kabul ettikleri yazılı sözleşmeler yapılmalıdır.", CreatedAt = Seed },
            new Requirement { Id = 54, Code = "12.8.4", Section = 12, Applicability = SaqApplicability.All,
                Title = "TPSP uyum durumunun izlenmesi",
                Description = "TPSP'lerin PCI DSS uyum durumu en az yılda bir izlenmelidir.", CreatedAt = Seed },
            new Requirement { Id = 55, Code = "12.10.1", Section = 12, Applicability = SaqApplicability.All,
                Title = "Olay müdahale planı",
                Description = "Şüpheli veya doğrulanmış bir güvenlik olayında hemen uygulanacak bir olay müdahale planı bulunmalıdır.", CreatedAt = Seed }
        );
        b.Entity<SurveyQuestion>().HasData(
            new SurveyQuestion { Id = 1, RequirementId = 1, Order = 1, EvidenceRequired = true, EvidenceHint = "Firewall/NSC yapılandırma standardı dokümanı",
                Text = "Firewall ve diğer ağ güvenlik kontrolleriniz (NSC) için yazılı bir yapılandırma standardı var mı ve cihazlar bu standarda göre yapılandırılıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 5, RequirementId = 5, Order = 2, EvidenceRequired = true, EvidenceHint = "Son kural seti gözden geçirme kaydı / toplantı tutanağı",
                Text = "Firewall/NSC kural setleriniz en az altı ayda bir gözden geçiriliyor ve gereksiz kurallar kaldırılıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 6, RequirementId = 6, Order = 3, EvidenceRequired = true, EvidenceHint = "CDE'ye gelen trafik kuralları dökümü",
                Text = "Kart verisi ortamına (CDE) gelen ağ trafiği yalnızca iş için gerekli olanla sınırlı mı ve diğer tüm trafik engelleniyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 7, RequirementId = 7, Order = 4, EvidenceRequired = true, EvidenceHint = "Ağ topolojisi / veri akış diyagramı",
                Text = "İç ağınız ile internet gibi güvenilmeyen ağlar arasında firewall/NSC bulunuyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 8, RequirementId = 8, Order = 5, EvidenceRequired = true, EvidenceHint = "Sunucu/cihaz sıkılaştırma (hardening) standardı",
                Text = "Sunucular, ağ cihazları ve diğer sistem bileşenleri için yazılı bir sıkılaştırma (hardening) standardınız var mı ve uygulanıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 9, RequirementId = 9, Order = 6, EvidenceRequired = false, EvidenceHint = "Varsayılan hesap kontrol listesi",
                Text = "Cihaz ve yazılımlardaki üretici varsayılan hesap ve parolaları değiştiriliyor ya da devre dışı bırakılıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 10, RequirementId = 10, Order = 7, EvidenceRequired = false, EvidenceHint = "Kablosuz erişim noktası yapılandırma kaydı",
                Text = "Kart verisi ortamına bağlı kablosuz ağlarınızdaki varsayılan anahtar, parola ve ayarlar değiştirildi mi?", CreatedAt = Seed },
            new SurveyQuestion { Id = 11, RequirementId = 11, Order = 8, EvidenceRequired = true, EvidenceHint = "Veri saklama ve imha politikası",
                Text = "Kart verisini (kağıt dahil) ne kadar süre ve nerede sakladığınızı belirleyen, süresi dolanın imha edilmesini öngören bir politikanız var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 12, RequirementId = 12, Order = 9, EvidenceRequired = false, EvidenceHint = "Veri keşif taraması sonucu veya süreç beyanı",
                Text = "Yetkilendirme tamamlandıktan sonra kartın manyetik şerit/çip verisi, güvenlik kodu (CVV/CVC) veya PIN bilgisi hiçbir yerde saklanmıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 13, RequirementId = 13, Order = 10, EvidenceRequired = false, EvidenceHint = "Ekran görüntüsü / fiş örneği",
                Text = "Kart numarası ekranlarda, raporlarda ve fişlerde maskeli mi gösteriliyor; tamamını yalnızca iş ihtiyacı olan kişiler mi görebiliyor?", CreatedAt = Seed },
            new SurveyQuestion { Id = 2, RequirementId = 2, Order = 11, EvidenceRequired = true, EvidenceHint = "Veritabanı şifreleme/tokenizasyon yapılandırması",
                Text = "Saklanan kart numaraları (PAN) veritabanı, yedek ve loglar dahil her yerde şifreli, tokenize ya da kırpılmış olarak mı tutuluyor?", CreatedAt = Seed },
            new SurveyQuestion { Id = 14, RequirementId = 14, Order = 12, EvidenceRequired = true, EvidenceHint = "Anahtar yönetimi prosedürü",
                Text = "Kart verisini şifrelemekte kullandığınız anahtarların yetkisiz erişime karşı nasıl korunacağını tanımlayan yazılı prosedürler var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 15, RequirementId = 15, Order = 13, EvidenceRequired = true, EvidenceHint = "Kriptografik mimari dokümanı",
                Text = "Kullandığınız şifreleme algoritmaları, protokoller, anahtarlar ve bunların kullanım yerlerini gösteren güncel bir kriptografik mimari dokümanınız var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 16, RequirementId = 16, Order = 14, EvidenceRequired = true, EvidenceHint = "TLS yapılandırma raporu (ör. SSL tarama çıktısı)",
                Text = "Kart verisi internet gibi açık ağlar üzerinden iletilirken güncel TLS sürümleri ve geçerli sertifikalarla şifreleniyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 17, RequirementId = 17, Order = 15, EvidenceRequired = false, EvidenceHint = "Kart verisi iletişim politikası",
                Text = "Kart numarası e-posta, SMS veya anlık mesajlaşma ile asla şifresiz gönderilmiyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 18, RequirementId = 18, Order = 16, EvidenceRequired = true, EvidenceHint = "Anti-malware konsolu kurulum raporu",
                Text = "Kötü amaçlı yazılım riski taşıyan tüm sistemlerde anti-malware (antivirüs/EDR) çözümü kurulu mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 19, RequirementId = 19, Order = 17, EvidenceRequired = false, EvidenceHint = "Güncelleme durumu ekran görüntüsü",
                Text = "Anti-malware çözümünüz ve imza veritabanı otomatik olarak güncelleniyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 20, RequirementId = 20, Order = 18, EvidenceRequired = false, EvidenceHint = "E-posta güvenlik ağ geçidi yapılandırması",
                Text = "Çalışanlarınızı oltalama e-postalarına karşı koruyan teknik önlemler (e-posta filtresi, DMARC vb.) var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 21, RequirementId = 21, Order = 19, EvidenceRequired = true, EvidenceHint = "Güvenli yazılım geliştirme prosedürü",
                Text = "Kendi geliştirdiğiniz veya ısmarlattığınız yazılımlar için güvenli kodlama standartlarını içeren bir geliştirme süreciniz var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 22, RequirementId = 22, Order = 20, EvidenceRequired = true, EvidenceHint = "Zafiyet takip ve risk derecelendirme prosedürü",
                Text = "Yeni güvenlik açıklarını güvenilir kaynaklardan takip edip risk seviyesine göre (kritik/yüksek/orta) sınıflandırıyor musunuz?", CreatedAt = Seed },
            new SurveyQuestion { Id = 23, RequirementId = 23, Order = 21, EvidenceRequired = true, EvidenceHint = "Yama yönetimi raporu",
                Text = "Kritik güvenlik yamaları yayımlandıktan sonra en geç bir ay içinde sistemlerinize uygulanıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 24, RequirementId = 24, Order = 22, EvidenceRequired = true, EvidenceHint = "WAF yapılandırması veya uygulama güvenlik testi raporu",
                Text = "İnternete açık web uygulamalarınız WAF ile ya da düzenli uygulama güvenlik testleriyle korunuyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 25, RequirementId = 25, Order = 23, EvidenceRequired = true, EvidenceHint = "Ödeme sayfası betik envanteri",
                Text = "Ödeme sayfanızda çalışan tüm JavaScript dosyalarının envanteri, yetkilendirmesi ve bütünlük kontrolü yapılıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 26, RequirementId = 26, Order = 24, EvidenceRequired = true, EvidenceHint = "Rol/yetki matrisi",
                Text = "Hangi rolün hangi sistem ve veriye erişebileceğini tanımlayan bir erişim kontrol modeliniz (rol/yetki matrisi) var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 27, RequirementId = 27, Order = 25, EvidenceRequired = false, EvidenceHint = "Yetki onay kayıtları",
                Text = "Kullanıcılara yalnızca işlerini yapmaları için gereken en az yetki mi veriliyor?", CreatedAt = Seed },
            new SurveyQuestion { Id = 28, RequirementId = 28, Order = 26, EvidenceRequired = false, EvidenceHint = "Kullanıcı hesap listesi",
                Text = "Sistemlere ve kart verisine erişen her kullanıcının kendine ait benzersiz bir kullanıcı hesabı var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 29, RequirementId = 29, Order = 27, EvidenceRequired = false, EvidenceHint = "Ortak hesap istisna kayıtları",
                Text = "Paylaşılan/ortak hesaplar yalnızca zorunlu durumlarda, onaylı ve kimin kullandığı izlenebilir şekilde mi kullanılıyor?", CreatedAt = Seed },
            new SurveyQuestion { Id = 30, RequirementId = 30, Order = 28, EvidenceRequired = false, EvidenceHint = "Pasif hesap raporu",
                Text = "90 gündür kullanılmayan kullanıcı hesapları kaldırılıyor ya da devre dışı bırakılıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 31, RequirementId = 31, Order = 29, EvidenceRequired = false, EvidenceHint = "Kimlik doğrulama politikası",
                Text = "Sistemlere tüm kullanıcı erişimleri en az bir kimlik doğrulama faktörü (parola, token, biyometri) ile doğrulanıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 3, RequirementId = 3, Order = 30, EvidenceRequired = true, EvidenceHint = "Parola politikası ve sistem ayarı ekran görüntüsü",
                Text = "Parola politikanız en az 12 karakter uzunluk ve hem harf hem rakam içerme zorunluluğu getiriyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 32, RequirementId = 32, Order = 31, EvidenceRequired = true, EvidenceHint = "MFA yapılandırması",
                Text = "Kart verisi ortamındaki sistemlere uzaktan yönetici erişimlerinde çok faktörlü kimlik doğrulama (MFA) kullanılıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 33, RequirementId = 33, Order = 32, EvidenceRequired = true, EvidenceHint = "VPN/uzaktan erişim MFA yapılandırması",
                Text = "Kurum ağına dışarıdan yapılan tüm uzaktan erişimlerde (VPN, uzak masaüstü vb.) MFA kullanılıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 34, RequirementId = 34, Order = 33, EvidenceRequired = false, EvidenceHint = "Fiziksel güvenlik prosedürü",
                Text = "Kart verisi içeren kağıt belgeler, yedekler ve diğer ortamlar kilitli ve erişimi kısıtlı yerlerde mi saklanıyor?", CreatedAt = Seed },
            new SurveyQuestion { Id = 35, RequirementId = 35, Order = 34, EvidenceRequired = false, EvidenceHint = "İmha prosedürü / imha tutanağı",
                Text = "Kart verisi içeren basılı belgeler artık gerekmediğinde parçalama, yakma gibi yöntemlerle geri getirilemez şekilde imha ediliyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 36, RequirementId = 36, Order = 35, EvidenceRequired = true, EvidenceHint = "POS/POI cihaz envanteri",
                Text = "Ödeme terminallerinizin (POS/POI) seri numaralı envanteri tutuluyor ve cihazlar kurcalanmaya karşı düzenli kontrol ediliyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 37, RequirementId = 37, Order = 36, EvidenceRequired = false, EvidenceHint = "Eğitim katılım kaydı",
                Text = "Kasiyerler ve terminal başındaki personel, cihaz kurcalama ve sahte teknisyen girişimlerini tanıyacak şekilde eğitildi mi?", CreatedAt = Seed },
            new SurveyQuestion { Id = 4, RequirementId = 4, Order = 37, EvidenceRequired = true, EvidenceHint = "Log yapılandırması / örnek log kaydı",
                Text = "Kart verisi ortamındaki tüm sistemlerde denetim kayıtları (audit log) açık ve aktif mi?", CreatedAt = Seed },
            new SurveyQuestion { Id = 38, RequirementId = 38, Order = 38, EvidenceRequired = true, EvidenceHint = "SIEM/log inceleme raporu",
                Text = "Güvenlik olayları ve kritik sistemlerin logları en az günde bir kez (manuel veya SIEM ile) inceleniyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 39, RequirementId = 39, Order = 39, EvidenceRequired = false, EvidenceHint = "Log saklama politikası",
                Text = "Denetim kayıtları en az 12 ay saklanıyor ve son üç aylık kısım anında erişilebilir durumda mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 40, RequirementId = 40, Order = 40, EvidenceRequired = false, EvidenceHint = "NTP yapılandırması",
                Text = "Tüm sistemlerin saati NTP gibi bir zaman senkronizasyon mekanizmasıyla eşitleniyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 41, RequirementId = 41, Order = 41, EvidenceRequired = true, EvidenceHint = "Kablosuz tarama raporu",
                Text = "Yetkisiz kablosuz erişim noktalarını tespit etmek için en az üç ayda bir tarama yapıyor musunuz?", CreatedAt = Seed },
            new SurveyQuestion { Id = 42, RequirementId = 42, Order = 42, EvidenceRequired = true, EvidenceHint = "Son dört çeyreğe ait iç tarama raporları",
                Text = "İç ağınızda en az üç ayda bir zafiyet taraması yapılıyor ve yüksek/kritik bulgular giderilip yeniden taranıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 43, RequirementId = 43, Order = 43, EvidenceRequired = true, EvidenceHint = "Son dört çeyreğe ait ASV tarama raporları",
                Text = "İnternete açık sistemleriniz en az üç ayda bir PCI onaylı tarama sağlayıcısı (ASV) tarafından taranıyor ve tarama 'geçti' sonucu alıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 44, RequirementId = 44, Order = 44, EvidenceRequired = true, EvidenceHint = "Sızma testi raporu",
                Text = "Sektörce kabul görmüş bir metodolojiye göre en az yılda bir iç ve dış sızma testi yaptırıyor musunuz?", CreatedAt = Seed },
            new SurveyQuestion { Id = 45, RequirementId = 45, Order = 45, EvidenceRequired = true, EvidenceHint = "IDS/IPS yapılandırması",
                Text = "Kart verisi ortamının çevresinde ve kritik noktalarında saldırı tespit/önleme (IDS/IPS) sistemi çalışıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 46, RequirementId = 46, Order = 46, EvidenceRequired = true, EvidenceHint = "Değişiklik/kurcalama tespit aracı yapılandırması",
                Text = "Müşterinin tarayıcısında görüntülenen ödeme sayfasındaki yetkisiz değişiklikleri tespit edip uyarı üreten bir mekanizmanız var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 47, RequirementId = 47, Order = 47, EvidenceRequired = true, EvidenceHint = "Bilgi güvenliği politikası",
                Text = "Yönetimce onaylanmış, yılda en az bir kez gözden geçirilen ve çalışanlara duyurulmuş bir bilgi güvenliği politikanız var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 48, RequirementId = 48, Order = 48, EvidenceRequired = true, EvidenceHint = "Hedefli risk analizi dokümanı",
                Text = "Sıklığını kendinizin belirlediği kontroller (ör. log inceleme sıklığı) için yazılı bir hedefli risk analizi yaptınız mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 49, RequirementId = 49, Order = 49, EvidenceRequired = true, EvidenceHint = "Üç aylık gözden geçirme kayıtları",
                Text = "Log inceleme, firewall gözden geçirme gibi güvenlik görevlerinin yapıldığı en az üç ayda bir kontrol ediliyor ve kayıt altına alınıyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 50, RequirementId = 50, Order = 50, EvidenceRequired = true, EvidenceHint = "Kapsam dokümanı ve veri akış diyagramı",
                Text = "Kart verisinin aktığı tüm sistem, süreç ve lokasyonları içeren PCI DSS kapsamınızı en az yılda bir belgeleyip doğruluyor musunuz?", CreatedAt = Seed },
            new SurveyQuestion { Id = 51, RequirementId = 51, Order = 51, EvidenceRequired = true, EvidenceHint = "Farkındalık eğitimi materyali ve katılım kayıtları",
                Text = "Tüm çalışanlara işe girişte ve en az yılda bir güvenlik farkındalık eğitimi veriliyor mu?", CreatedAt = Seed },
            new SurveyQuestion { Id = 52, RequirementId = 52, Order = 52, EvidenceRequired = true, EvidenceHint = "TPSP listesi",
                Text = "Kart verisini paylaştığınız veya kart verisinin güvenliğini etkileyebilecek tüm hizmet sağlayıcıların (ödeme kuruluşu, barındırma vb.) güncel bir listesi var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 53, RequirementId = 53, Order = 53, EvidenceRequired = true, EvidenceHint = "Sözleşmelerin ilgili maddeleri",
                Text = "Bu hizmet sağlayıcılarla, kart verisinin güvenliğinden sorumlu olduklarını kabul ettikleri yazılı sözleşmeleriniz var mı?", CreatedAt = Seed },
            new SurveyQuestion { Id = 54, RequirementId = 54, Order = 54, EvidenceRequired = true, EvidenceHint = "TPSP uyum sertifikaları (AOC)",
                Text = "Hizmet sağlayıcılarınızın PCI DSS uyum durumunu (AOC belgeleri vb.) en az yılda bir kontrol ediyor musunuz?", CreatedAt = Seed },
            new SurveyQuestion { Id = 55, RequirementId = 55, Order = 55, EvidenceRequired = true, EvidenceHint = "Olay müdahale planı",
                Text = "Kart verisinin ele geçirildiğinden şüphelenildiğinde uygulanacak, sorumluların ve iletişim adımlarının tanımlı olduğu yazılı bir olay müdahale planınız var mı?", CreatedAt = Seed }
        );
    }
}
