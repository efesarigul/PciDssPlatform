using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PciDss.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SaqScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski cevaplar (Yes=0, No=1, Partial=2, NotApplicable=3) SAQ seçeneklerine dönüştürülür
            // (InPlace=0, InPlaceWithCcw=1, NotApplicable=2, NotTested=3, NotInPlace=4). "Kısmen" PCI'da
            // karşılanmış sayılmadığından NotInPlace olur.
            migrationBuilder.Sql(
                "UPDATE SurveyAnswers SET Value = CASE Value WHEN 1 THEN 4 WHEN 2 THEN 4 WHEN 3 THEN 2 ELSE Value END");

            migrationBuilder.AddColumn<string>(
                name: "EvidenceHint",
                table: "SurveyQuestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Applicability",
                table: "Requirements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ServiceProviderOnly",
                table: "Requirements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ScopeProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    StoresCardDataElectronically = table.Column<bool>(type: "bit", nullable: false),
                    Ecommerce = table.Column<int>(type: "int", nullable: true),
                    CardPresent = table.Column<int>(type: "int", nullable: true),
                    Moto = table.Column<int>(type: "int", nullable: true),
                    SaqType = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopeProfiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Applicability", "Description", "ServiceProviderOnly", "Title" },
                values: new object[] { 26, "NSC kural setleri için yapılandırma standartları tanımlanmalı, uygulanmalı ve güncel tutulmalıdır.", false, "Ağ güvenlik kontrolleri yapılandırma standartları" });

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Applicability", "Description", "ServiceProviderOnly", "Title" },
                values: new object[] { 0, "PAN saklandığı her yerde güçlü kriptografi, tokenizasyon, anahtarlı hash veya kırpma ile okunamaz yapılmalıdır.", false, "Saklanan PAN'ın okunamaz hale getirilmesi" });

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Applicability", "Description", "ServiceProviderOnly" },
                values: new object[] { 51, "Parolalar en az 12 karakter (sistem desteklemiyorsa 8) olmalı ve hem harf hem rakam içermelidir.", false });

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Applicability", "Description", "ServiceProviderOnly", "Title" },
                values: new object[] { 18, "Tüm sistem bileşenlerinde ve kart verisinde denetim kayıtları (audit log) etkin ve aktif olmalıdır.", false, "Denetim kayıtlarının etkinleştirilmesi" });

            migrationBuilder.InsertData(
                table: "Requirements",
                columns: new[] { "Id", "Applicability", "Code", "CreatedAt", "Description", "Section", "ServiceProviderOnly", "Title" },
                values: new object[,]
                {
                    { 5, 26, "1.2.7", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "NSC yapılandırmaları en az altı ayda bir gözden geçirilerek gereksiz kurallar kaldırılmalıdır.", 1, false, "NSC yapılandırmalarının periyodik gözden geçirilmesi" },
                    { 6, 26, "1.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "CDE'ye gelen trafik yalnızca gerekli olanla sınırlandırılmalı, geri kalanı engellenmelidir.", 1, false, "Kart verisi ortamına gelen trafiğin kısıtlanması" },
                    { 7, 26, "1.4.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvenilir iç ağlar ile internet gibi güvenilmeyen ağlar arasına NSC yerleştirilmelidir.", 1, false, "Güvenilir ve güvenilmeyen ağlar arasında NSC" },
                    { 8, 18, "2.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tüm sistem bileşenleri için bilinen açıkları kapatan, sektörce kabul görmüş sıkılaştırma standartları uygulanmalıdır.", 2, false, "Sistem bileşenleri için sıkılaştırma standartları" },
                    { 9, 59, "2.2.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Üretici varsayılan hesapları kullanılacaksa parolaları değiştirilmeli, kullanılmayacaksa kaldırılmalı veya devre dışı bırakılmalıdır.", 2, false, "Üretici varsayılan hesapları" },
                    { 10, 26, "2.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "CDE'ye bağlı kablosuz ağlarda şifreleme anahtarları, parolalar ve SNMP gibi tüm varsayılan ayarlar değiştirilmelidir.", 2, false, "Kablosuz ağ varsayılanları" },
                    { 11, 254, "3.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hesap verisi saklama süresi ve yöntemi tanımlı bir saklama/imha politikasıyla sınırlandırılmalıdır.", 3, false, "Hesap verisi saklamanın en aza indirilmesi" },
                    { 12, 254, "3.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yetkilendirme sonrasında tam iz verisi, CVV/CVC ve PIN bilgisi saklanmamalıdır.", 3, false, "Hassas doğrulama verisinin saklanmaması" },
                    { 13, 62, "3.4.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "PAN ekranlarda ve çıktılarda maskelenmeli; ilk 6 / son 4 haneden fazlasını yalnızca iş ihtiyacı olanlar görebilmelidir.", 3, false, "PAN'ın maskelenerek gösterilmesi" },
                    { 14, 0, "3.6.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "PAN'ı korumak için kullanılan anahtarların ifşa ve kötüye kullanımına karşı prosedürler tanımlanmalı ve uygulanmalıdır.", 3, false, "Kriptografik anahtarların korunması" },
                    { 15, 0, "3.6.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Servis sağlayıcılar kullandıkları algoritma, protokol ve anahtarları içeren güncel bir kriptografik mimari dokümanı tutmalıdır.", 3, true, "Kriptografik mimarinin dokümantasyonu (SP)" },
                    { 16, 58, "4.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "PAN açık/genel ağlar üzerinden iletilirken güçlü kriptografi ve güvenilir sertifikalar kullanılmalıdır.", 4, false, "Açık ağlarda güçlü kriptografi" },
                    { 17, 254, "4.2.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "PAN e-posta, anlık mesajlaşma, SMS gibi kanallarla gönderiliyorsa güçlü kriptografi ile korunmalıdır.", 4, false, "Son kullanıcı mesajlaşmasında PAN" },
                    { 18, 50, "5.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kötü amaçlı yazılım riski taşıyan tüm sistem bileşenlerinde anti-malware çözümü kurulu olmalıdır.", 5, false, "Kötü amaçlı yazılımdan korunma" },
                    { 19, 50, "5.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anti-malware çözümü ve imzaları otomatik olarak güncel tutulmalıdır.", 5, false, "Anti-malware güncelliği" },
                    { 20, 50, "5.4.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kullanıcıları oltalama saldırılarına karşı korumak için tespit ve koruma mekanizmaları bulunmalıdır.", 5, false, "Oltalama (phishing) koruması" },
                    { 21, 0, "6.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Özel ve ısmarlama yazılımlar güvenli geliştirme standartlarına göre geliştirilmelidir.", 6, false, "Güvenli yazılım geliştirme" },
                    { 22, 59, "6.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yeni güvenlik açıkları güvenilir kaynaklardan izlenmeli ve risk seviyesine göre sınıflandırılmalıdır.", 6, false, "Güvenlik açıklarının tespiti ve önceliklendirilmesi" },
                    { 23, 59, "6.3.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kritik güvenlik yamaları yayımlanmalarından itibaren bir ay içinde uygulanmalıdır.", 6, false, "Kritik yamaların zamanında uygulanması" },
                    { 24, 2, "6.4.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Herkese açık web uygulamaları bilinen saldırılara karşı düzenli test veya otomatik bir çözümle (WAF) korunmalıdır.", 6, false, "Herkese açık web uygulamalarının korunması" },
                    { 25, 2, "6.4.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ödeme sayfasında çalışan tüm betikler yetkilendirilmeli, bütünlükleri doğrulanmalı ve gerekçeleriyle envanteri tutulmalıdır.", 6, false, "Ödeme sayfası betiklerinin yönetimi" },
                    { 26, 50, "7.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İş ve erişim ihtiyacına göre erişimi düzenleyen bir erişim kontrol modeli tanımlanmalıdır.", 7, false, "Erişim kontrol modeli" },
                    { 27, 58, "7.2.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kullanıcılara yalnızca görevleri için gereken en az yetki verilmelidir.", 7, false, "En az yetki ilkesi" },
                    { 28, 59, "8.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sistemlere ve kart verisine erişen her kullanıcıya benzersiz bir kimlik atanmalıdır.", 8, false, "Benzersiz kullanıcı kimlikleri" },
                    { 29, 51, "8.2.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Grup, paylaşılan veya ortak hesaplar yalnızca istisnai durumlarda, onaylı ve izlenebilir şekilde kullanılmalıdır.", 8, false, "Ortak hesapların sınırlandırılması" },
                    { 30, 18, "8.2.6", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "90 gün boyunca kullanılmayan kullanıcı hesapları kaldırılmalı veya devre dışı bırakılmalıdır.", 8, false, "Pasif hesapların kaldırılması" },
                    { 31, 51, "8.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tüm kullanıcı erişimleri parola, token veya biyometri gibi en az bir kimlik doğrulama faktörüyle doğrulanmalıdır.", 8, false, "Kullanıcı kimlik doğrulaması" },
                    { 32, 18, "8.4.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "CDE'ye konsol dışı yönetici erişimlerinde çok faktörlü kimlik doğrulama (MFA) kullanılmalıdır.", 8, false, "Yönetici erişiminde çok faktörlü doğrulama" },
                    { 33, 58, "8.4.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kurum ağına dışarıdan yapılan ve CDE'ye ulaşabilen tüm uzaktan erişimlerde MFA kullanılmalıdır.", 8, false, "Uzaktan ağ erişiminde çok faktörlü doğrulama" },
                    { 34, 255, "9.4.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi içeren tüm fiziksel ve elektronik ortamlar fiziksel olarak güvence altına alınmalıdır.", 9, false, "Kart verisi içeren ortamların fiziksel güvenliği" },
                    { 35, 255, "9.4.6", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi içeren basılı materyaller artık gerekmediğinde geri getirilemeyecek şekilde imha edilmelidir.", 9, false, "Basılı kart verisinin imhası" },
                    { 36, 220, "9.5.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kartla doğrudan etkileşen POI cihazları kurcalama ve değiştirilmeye karşı korunmalı, envanteri tutulmalıdır.", 9, false, "POI cihazlarının kurcalamaya karşı korunması" },
                    { 37, 220, "9.5.1.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "POI cihazlarının bulunduğu alanlardaki personel kurcalama ve değiştirme girişimlerini tanıyacak şekilde eğitilmelidir.", 9, false, "POI kurcalama farkındalık eğitimi" },
                    { 38, 18, "10.4.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvenlik olayları ve kritik sistemlerin logları en az günde bir kez incelenmelidir.", 10, false, "Denetim kayıtlarının günlük incelenmesi" },
                    { 39, 18, "10.5.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Denetim kayıtları en az 12 ay saklanmalı, son üç ayı anında erişilebilir olmalıdır.", 10, false, "Denetim kayıtlarının saklanması" },
                    { 40, 18, "10.6.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sistem saatleri zaman senkronizasyon teknolojisiyle eşitlenmelidir.", 10, false, "Zaman senkronizasyonu" },
                    { 41, 16, "11.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yetkili ve yetkisiz kablosuz erişim noktaları en az üç ayda bir tespit edilip yönetilmelidir.", 11, false, "Yetkisiz kablosuz erişim noktalarının tespiti" },
                    { 42, 18, "11.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İç zafiyet taramaları en az üç ayda bir yapılmalı, yüksek ve kritik açıklar giderilip yeniden taranmalıdır.", 11, false, "İç zafiyet taramaları" },
                    { 43, 27, "11.3.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dış zafiyet taramaları en az üç ayda bir PCI onaylı tarama sağlayıcısı (ASV) tarafından yapılmalıdır.", 11, false, "Dış zafiyet taramaları (ASV)" },
                    { 44, 2, "11.4.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sektörce kabul görmüş bir metodolojiye dayanan sızma testi en az yılda bir yapılmalıdır.", 11, false, "Sızma testi metodolojisi" },
                    { 45, 2, "11.5.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ağa yönelik saldırılar IDS/IPS gibi tekniklerle tespit edilmeli ve/veya önlenmelidir.", 11, false, "Saldırı tespit/önleme" },
                    { 46, 2, "11.6.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ödeme sayfalarında yetkisiz değişiklikleri tespit edip uyarı üreten bir mekanizma bulunmalıdır.", 11, false, "Ödeme sayfası değişiklik tespiti" },
                    { 47, 62, "12.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Genel bir bilgi güvenliği politikası oluşturulmalı, yayımlanmalı, güncel tutulmalı ve ilgili herkese duyurulmalıdır.", 12, false, "Bilgi güvenliği politikası" },
                    { 48, 18, "12.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sıklığı esnek bırakılan gereksinimler için hedefli risk analizi yapılmalı ve belgelenmelidir.", 12, false, "Hedefli risk analizi" },
                    { 49, 0, "12.4.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Servis sağlayıcılar güvenlik görevlerinin yerine getirildiğini en az üç ayda bir gözden geçirmelidir.", 12, true, "Operasyonel görevlerin üç aylık gözden geçirilmesi (SP)" },
                    { 50, 0, "12.5.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "PCI DSS kapsamı en az yılda bir ve önemli değişikliklerde belgelenip doğrulanmalıdır.", 12, false, "PCI DSS kapsamının belgelenmesi" },
                    { 51, 254, "12.6.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tüm personeli bilgi güvenliği politikası ve sorumlulukları konusunda bilgilendiren bir farkındalık programı uygulanmalıdır.", 12, false, "Güvenlik farkındalık programı" },
                    { 52, 255, "12.8.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hesap verisini paylaşılan veya güvenliğini etkileyebilecek tüm üçüncü taraf servis sağlayıcıların (TPSP) listesi tutulmalıdır.", 12, false, "Üçüncü taraf servis sağlayıcı listesi" },
                    { 53, 255, "12.8.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "TPSP'lerle, kart verisinin güvenliğinden sorumluluklarını kabul ettikleri yazılı sözleşmeler yapılmalıdır.", 12, false, "TPSP ile yazılı sözleşmeler" },
                    { 54, 255, "12.8.4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "TPSP'lerin PCI DSS uyum durumu en az yılda bir izlenmelidir.", 12, false, "TPSP uyum durumunun izlenmesi" },
                    { 55, 255, "12.10.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Şüpheli veya doğrulanmış bir güvenlik olayında hemen uygulanacak bir olay müdahale planı bulunmalıdır.", 12, false, "Olay müdahale planı" }
                });

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EvidenceHint", "Text" },
                values: new object[] { "Firewall/NSC yapılandırma standardı dokümanı", "Firewall ve diğer ağ güvenlik kontrolleriniz (NSC) için yazılı bir yapılandırma standardı var mı ve cihazlar bu standarda göre yapılandırılıyor mu?" });

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EvidenceHint", "Order", "Text" },
                values: new object[] { "Veritabanı şifreleme/tokenizasyon yapılandırması", 11, "Saklanan kart numaraları (PAN) veritabanı, yedek ve loglar dahil her yerde şifreli, tokenize ya da kırpılmış olarak mı tutuluyor?" });

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EvidenceHint", "EvidenceRequired", "Order", "Text" },
                values: new object[] { "Parola politikası ve sistem ayarı ekran görüntüsü", true, 30, "Parola politikanız en az 12 karakter uzunluk ve hem harf hem rakam içerme zorunluluğu getiriyor mu?" });

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EvidenceHint", "Order", "Text" },
                values: new object[] { "Log yapılandırması / örnek log kaydı", 37, "Kart verisi ortamındaki tüm sistemlerde denetim kayıtları (audit log) açık ve aktif mi?" });

            migrationBuilder.InsertData(
                table: "SurveyQuestions",
                columns: new[] { "Id", "CreatedAt", "EvidenceHint", "EvidenceRequired", "Order", "RequirementId", "Text" },
                values: new object[,]
                {
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Son kural seti gözden geçirme kaydı / toplantı tutanağı", true, 2, 5, "Firewall/NSC kural setleriniz en az altı ayda bir gözden geçiriliyor ve gereksiz kurallar kaldırılıyor mu?" },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "CDE'ye gelen trafik kuralları dökümü", true, 3, 6, "Kart verisi ortamına (CDE) gelen ağ trafiği yalnızca iş için gerekli olanla sınırlı mı ve diğer tüm trafik engelleniyor mu?" },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ağ topolojisi / veri akış diyagramı", true, 4, 7, "İç ağınız ile internet gibi güvenilmeyen ağlar arasında firewall/NSC bulunuyor mu?" },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sunucu/cihaz sıkılaştırma (hardening) standardı", true, 5, 8, "Sunucular, ağ cihazları ve diğer sistem bileşenleri için yazılı bir sıkılaştırma (hardening) standardınız var mı ve uygulanıyor mu?" },
                    { 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Varsayılan hesap kontrol listesi", false, 6, 9, "Cihaz ve yazılımlardaki üretici varsayılan hesap ve parolaları değiştiriliyor ya da devre dışı bırakılıyor mu?" },
                    { 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kablosuz erişim noktası yapılandırma kaydı", false, 7, 10, "Kart verisi ortamına bağlı kablosuz ağlarınızdaki varsayılan anahtar, parola ve ayarlar değiştirildi mi?" },
                    { 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Veri saklama ve imha politikası", true, 8, 11, "Kart verisini (kağıt dahil) ne kadar süre ve nerede sakladığınızı belirleyen, süresi dolanın imha edilmesini öngören bir politikanız var mı?" },
                    { 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Veri keşif taraması sonucu veya süreç beyanı", false, 9, 12, "Yetkilendirme tamamlandıktan sonra kartın manyetik şerit/çip verisi, güvenlik kodu (CVV/CVC) veya PIN bilgisi hiçbir yerde saklanmıyor mu?" },
                    { 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ekran görüntüsü / fiş örneği", false, 10, 13, "Kart numarası ekranlarda, raporlarda ve fişlerde maskeli mi gösteriliyor; tamamını yalnızca iş ihtiyacı olan kişiler mi görebiliyor?" },
                    { 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anahtar yönetimi prosedürü", true, 12, 14, "Kart verisini şifrelemekte kullandığınız anahtarların yetkisiz erişime karşı nasıl korunacağını tanımlayan yazılı prosedürler var mı?" },
                    { 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kriptografik mimari dokümanı", true, 13, 15, "Kullandığınız şifreleme algoritmaları, protokoller, anahtarlar ve bunların kullanım yerlerini gösteren güncel bir kriptografik mimari dokümanınız var mı?" },
                    { 16, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "TLS yapılandırma raporu (ör. SSL tarama çıktısı)", true, 14, 16, "Kart verisi internet gibi açık ağlar üzerinden iletilirken güncel TLS sürümleri ve geçerli sertifikalarla şifreleniyor mu?" },
                    { 17, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi iletişim politikası", false, 15, 17, "Kart numarası e-posta, SMS veya anlık mesajlaşma ile asla şifresiz gönderilmiyor mu?" },
                    { 18, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anti-malware konsolu kurulum raporu", true, 16, 18, "Kötü amaçlı yazılım riski taşıyan tüm sistemlerde anti-malware (antivirüs/EDR) çözümü kurulu mu?" },
                    { 19, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güncelleme durumu ekran görüntüsü", false, 17, 19, "Anti-malware çözümünüz ve imza veritabanı otomatik olarak güncelleniyor mu?" },
                    { 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "E-posta güvenlik ağ geçidi yapılandırması", false, 18, 20, "Çalışanlarınızı oltalama e-postalarına karşı koruyan teknik önlemler (e-posta filtresi, DMARC vb.) var mı?" },
                    { 21, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvenli yazılım geliştirme prosedürü", true, 19, 21, "Kendi geliştirdiğiniz veya ısmarlattığınız yazılımlar için güvenli kodlama standartlarını içeren bir geliştirme süreciniz var mı?" },
                    { 22, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Zafiyet takip ve risk derecelendirme prosedürü", true, 20, 22, "Yeni güvenlik açıklarını güvenilir kaynaklardan takip edip risk seviyesine göre (kritik/yüksek/orta) sınıflandırıyor musunuz?" },
                    { 23, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yama yönetimi raporu", true, 21, 23, "Kritik güvenlik yamaları yayımlandıktan sonra en geç bir ay içinde sistemlerinize uygulanıyor mu?" },
                    { 24, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "WAF yapılandırması veya uygulama güvenlik testi raporu", true, 22, 24, "İnternete açık web uygulamalarınız WAF ile ya da düzenli uygulama güvenlik testleriyle korunuyor mu?" },
                    { 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ödeme sayfası betik envanteri", true, 23, 25, "Ödeme sayfanızda çalışan tüm JavaScript dosyalarının envanteri, yetkilendirmesi ve bütünlük kontrolü yapılıyor mu?" },
                    { 26, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Rol/yetki matrisi", true, 24, 26, "Hangi rolün hangi sistem ve veriye erişebileceğini tanımlayan bir erişim kontrol modeliniz (rol/yetki matrisi) var mı?" },
                    { 27, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yetki onay kayıtları", false, 25, 27, "Kullanıcılara yalnızca işlerini yapmaları için gereken en az yetki mi veriliyor?" },
                    { 28, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kullanıcı hesap listesi", false, 26, 28, "Sistemlere ve kart verisine erişen her kullanıcının kendine ait benzersiz bir kullanıcı hesabı var mı?" },
                    { 29, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ortak hesap istisna kayıtları", false, 27, 29, "Paylaşılan/ortak hesaplar yalnızca zorunlu durumlarda, onaylı ve kimin kullandığı izlenebilir şekilde mi kullanılıyor?" },
                    { 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pasif hesap raporu", false, 28, 30, "90 gündür kullanılmayan kullanıcı hesapları kaldırılıyor ya da devre dışı bırakılıyor mu?" },
                    { 31, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kimlik doğrulama politikası", false, 29, 31, "Sistemlere tüm kullanıcı erişimleri en az bir kimlik doğrulama faktörü (parola, token, biyometri) ile doğrulanıyor mu?" },
                    { 32, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "MFA yapılandırması", true, 31, 32, "Kart verisi ortamındaki sistemlere uzaktan yönetici erişimlerinde çok faktörlü kimlik doğrulama (MFA) kullanılıyor mu?" },
                    { 33, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VPN/uzaktan erişim MFA yapılandırması", true, 32, 33, "Kurum ağına dışarıdan yapılan tüm uzaktan erişimlerde (VPN, uzak masaüstü vb.) MFA kullanılıyor mu?" },
                    { 34, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fiziksel güvenlik prosedürü", false, 33, 34, "Kart verisi içeren kağıt belgeler, yedekler ve diğer ortamlar kilitli ve erişimi kısıtlı yerlerde mi saklanıyor?" },
                    { 35, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İmha prosedürü / imha tutanağı", false, 34, 35, "Kart verisi içeren basılı belgeler artık gerekmediğinde parçalama, yakma gibi yöntemlerle geri getirilemez şekilde imha ediliyor mu?" },
                    { 36, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "POS/POI cihaz envanteri", true, 35, 36, "Ödeme terminallerinizin (POS/POI) seri numaralı envanteri tutuluyor ve cihazlar kurcalanmaya karşı düzenli kontrol ediliyor mu?" },
                    { 37, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eğitim katılım kaydı", false, 36, 37, "Kasiyerler ve terminal başındaki personel, cihaz kurcalama ve sahte teknisyen girişimlerini tanıyacak şekilde eğitildi mi?" },
                    { 38, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "SIEM/log inceleme raporu", true, 38, 38, "Güvenlik olayları ve kritik sistemlerin logları en az günde bir kez (manuel veya SIEM ile) inceleniyor mu?" },
                    { 39, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Log saklama politikası", false, 39, 39, "Denetim kayıtları en az 12 ay saklanıyor ve son üç aylık kısım anında erişilebilir durumda mı?" },
                    { 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "NTP yapılandırması", false, 40, 40, "Tüm sistemlerin saati NTP gibi bir zaman senkronizasyon mekanizmasıyla eşitleniyor mu?" },
                    { 41, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kablosuz tarama raporu", true, 41, 41, "Yetkisiz kablosuz erişim noktalarını tespit etmek için en az üç ayda bir tarama yapıyor musunuz?" },
                    { 42, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Son dört çeyreğe ait iç tarama raporları", true, 42, 42, "İç ağınızda en az üç ayda bir zafiyet taraması yapılıyor ve yüksek/kritik bulgular giderilip yeniden taranıyor mu?" },
                    { 43, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Son dört çeyreğe ait ASV tarama raporları", true, 43, 43, "İnternete açık sistemleriniz en az üç ayda bir PCI onaylı tarama sağlayıcısı (ASV) tarafından taranıyor ve tarama 'geçti' sonucu alıyor mu?" },
                    { 44, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sızma testi raporu", true, 44, 44, "Sektörce kabul görmüş bir metodolojiye göre en az yılda bir iç ve dış sızma testi yaptırıyor musunuz?" },
                    { 45, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "IDS/IPS yapılandırması", true, 45, 45, "Kart verisi ortamının çevresinde ve kritik noktalarında saldırı tespit/önleme (IDS/IPS) sistemi çalışıyor mu?" },
                    { 46, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Değişiklik/kurcalama tespit aracı yapılandırması", true, 46, 46, "Müşterinin tarayıcısında görüntülenen ödeme sayfasındaki yetkisiz değişiklikleri tespit edip uyarı üreten bir mekanizmanız var mı?" },
                    { 47, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bilgi güvenliği politikası", true, 47, 47, "Yönetimce onaylanmış, yılda en az bir kez gözden geçirilen ve çalışanlara duyurulmuş bir bilgi güvenliği politikanız var mı?" },
                    { 48, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hedefli risk analizi dokümanı", true, 48, 48, "Sıklığını kendinizin belirlediği kontroller (ör. log inceleme sıklığı) için yazılı bir hedefli risk analizi yaptınız mı?" },
                    { 49, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Üç aylık gözden geçirme kayıtları", true, 49, 49, "Log inceleme, firewall gözden geçirme gibi güvenlik görevlerinin yapıldığı en az üç ayda bir kontrol ediliyor ve kayıt altına alınıyor mu?" },
                    { 50, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kapsam dokümanı ve veri akış diyagramı", true, 50, 50, "Kart verisinin aktığı tüm sistem, süreç ve lokasyonları içeren PCI DSS kapsamınızı en az yılda bir belgeleyip doğruluyor musunuz?" },
                    { 51, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Farkındalık eğitimi materyali ve katılım kayıtları", true, 51, 51, "Tüm çalışanlara işe girişte ve en az yılda bir güvenlik farkındalık eğitimi veriliyor mu?" },
                    { 52, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "TPSP listesi", true, 52, 52, "Kart verisini paylaştığınız veya kart verisinin güvenliğini etkileyebilecek tüm hizmet sağlayıcıların (ödeme kuruluşu, barındırma vb.) güncel bir listesi var mı?" },
                    { 53, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sözleşmelerin ilgili maddeleri", true, 53, 53, "Bu hizmet sağlayıcılarla, kart verisinin güvenliğinden sorumlu olduklarını kabul ettikleri yazılı sözleşmeleriniz var mı?" },
                    { 54, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "TPSP uyum sertifikaları (AOC)", true, 54, 54, "Hizmet sağlayıcılarınızın PCI DSS uyum durumunu (AOC belgeleri vb.) en az yılda bir kontrol ediyor musunuz?" },
                    { 55, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Olay müdahale planı", true, 55, 55, "Kart verisinin ele geçirildiğinden şüphelenildiğinde uygulanacak, sorumluların ve iletişim adımlarının tanımlı olduğu yazılı bir olay müdahale planınız var mı?" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScopeProfiles_OrganizationId",
                table: "ScopeProfiles",
                column: "OrganizationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Silinecek sorulara bağlı cevaplar kaldırılır (FK Restrict), kalanlar eski seçeneklere döner
            migrationBuilder.Sql("DELETE FROM SurveyAnswers WHERE SurveyQuestionId > 4");
            migrationBuilder.Sql(
                "UPDATE SurveyAnswers SET Value = CASE Value WHEN 1 THEN 0 WHEN 2 THEN 3 WHEN 3 THEN 1 WHEN 4 THEN 1 ELSE Value END");

            migrationBuilder.DropTable(
                name: "ScopeProfiles");

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DropColumn(
                name: "EvidenceHint",
                table: "SurveyQuestions");

            migrationBuilder.DropColumn(
                name: "Applicability",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "ServiceProviderOnly",
                table: "Requirements");

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Title" },
                values: new object[] { "NSC kural setleri tanımlanmalı, uygulanmalı ve sürdürülmelidir.", "Ağ güvenlik kontrolleri yapılandırması" });

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Title" },
                values: new object[] { "PAN, saklandığı her yerde güçlü kriptografi ile okunamaz yapılmalıdır.", "Saklanan PAN okunamaz hale getirilmeli" });

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "Parolalar en az 12 karakter, harf ve rakam içermelidir.");

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "Title" },
                values: new object[] { "Tüm sistem bileşenleri için denetim logları etkin olmalıdır.", "Denetim kayıtları" });

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Text",
                value: "Firewall/NSC kural setleriniz dokümante edilmiş ve en az 6 ayda bir gözden geçiriliyor mu?");

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Order", "Text" },
                values: new object[] { 2, "Kart numaraları (PAN) veritabanında şifreli/tokenize olarak mı saklanıyor?" });

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EvidenceRequired", "Order", "Text" },
                values: new object[] { false, 3, "Parola politikanız en az 12 karakter zorunluluğu içeriyor mu?" });

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Order", "Text" },
                values: new object[] { 4, "Kart verisi ortamındaki tüm sistemlerde denetim logları açık ve merkezi olarak toplanıyor mu?" });
        }
    }
}
