using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PciDss.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SmallSaqRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Requirements",
                columns: new[] { "Id", "Applicability", "Code", "CreatedAt", "Description", "Section", "ServiceProviderOnly", "Title" },
                values: new object[,]
                {
                    { 56, 10, "1.2.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi ortamı ile diğer ağlar arasındaki tüm bağlantıları (kablosuz dahil) gösteren güncel bir ağ diyagramı tutulmalıdır.", 1, false, "Doğru ağ diyagramı" },
                    { 57, 10, "1.2.5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İzin verilen tüm servis, protokol ve portlar belirlenmeli, onaylanmalı ve her biri için iş gerekçesi bulunmalıdır.", 1, false, "İzin verilen servis, protokol ve portlar" },
                    { 58, 10, "1.2.6", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kullanımda olan ve güvensiz kabul edilen servis, protokol ve portlar için riski azaltan güvenlik önlemleri tanımlanmalı ve uygulanmalıdır.", 1, false, "Güvensiz servisler için güvenlik önlemleri" },
                    { 59, 58, "1.3.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi ortamından çıkan trafik yalnızca gerekli olanla sınırlandırılmalı, geri kalanı engellenmelidir.", 1, false, "CDE'den çıkan trafiğin kısıtlanması" },
                    { 60, 58, "1.3.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tüm kablosuz ağlar ile kart verisi ortamı arasında, varsayılan olarak trafiği engelleyen ve yalnızca yetkili trafiğe izin veren NSC bulunmalıdır.", 1, false, "Kablosuz ağlar ile CDE arasında NSC" },
                    { 61, 10, "1.4.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sahte kaynak IP adresli trafiğin güvenilir ağa girmesini engelleyen önlemler (anti-spoofing) uygulanmalıdır.", 1, false, "Sahte IP adreslerine karşı önlem" },
                    { 62, 34, "1.5.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hem güvenilmeyen ağlara hem kart verisi ortamına bağlanan kurum veya çalışan cihazlarında, kullanıcının değiştiremediği güvenlik kontrolleri uygulanmalıdır.", 1, false, "Hem internete hem CDE'ye bağlanan cihazlar" },
                    { 63, 50, "2.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sistem bileşenlerinin güvenli yapılandırılmasına ilişkin politika ve prosedürler belgelenmeli, güncel tutulmalı, kullanılmalı ve ilgili herkese bildirilmelidir.", 2, false, "Gereksinim 2 politika ve prosedürleri" },
                    { 64, 50, "2.2.4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yalnızca gerekli servis, protokol ve işlevler etkin olmalı; gereksiz olanlar kaldırılmalı veya devre dışı bırakılmalıdır.", 2, false, "Yalnızca gerekli servislerin etkin olması" },
                    { 65, 50, "2.2.5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvensiz servis, protokol veya daemon varsa iş gerekçesi belgelenmeli ve riski azaltan ek güvenlik özellikleri uygulanmalıdır.", 2, false, "Güvensiz servisler için iş gerekçesi" },
                    { 66, 50, "2.2.6", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sistem güvenlik parametreleri kötüye kullanımı önleyecek şekilde yapılandırılmalıdır.", 2, false, "Sistem güvenlik parametreleri" },
                    { 67, 58, "2.2.7", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Konsol dışından yapılan tüm yönetici erişimleri güçlü kriptografiyle şifrelenmelidir.", 2, false, "Yönetici erişiminin şifrelenmesi" },
                    { 68, 56, "2.3.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "CDE'ye bağlı kablosuz ağların şifreleme anahtarları, anahtarı bilen personel ayrıldığında veya anahtarın ele geçirildiğinden şüphelenildiğinde değiştirilmelidir.", 2, false, "Kablosuz şifreleme anahtarlarının değiştirilmesi" },
                    { 69, 127, "3.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Saklanan hesap verisinin korunmasına ilişkin politika ve prosedürler belgelenmeli, güncel tutulmalı, kullanılmalı ve ilgili herkese bildirilmelidir.", 3, false, "Gereksinim 3 politika ve prosedürleri" },
                    { 70, 12, "3.3.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yetkilendirme tamamlandıktan sonra kartın iz (track) verisinin tamamı saklanmamalıdır.", 3, false, "Manyetik şerit / çip verisinin saklanmaması" },
                    { 71, 126, "3.3.1.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yetkilendirme tamamlandıktan sonra kart doğrulama kodu (CVV2/CVC2 vb.) saklanmamalıdır.", 3, false, "Kart doğrulama kodunun saklanmaması" },
                    { 72, 30, "3.3.1.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yetkilendirme tamamlandıktan sonra PIN ve şifreli PIN bloğu saklanmamalıdır.", 3, false, "PIN ve PIN bloğunun saklanmaması" },
                    { 73, 48, "4.2.1.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi ileten veya CDE'ye bağlı kablosuz ağlarda kimlik doğrulama ve iletim için güçlü kriptografi kullanılmalıdır.", 4, false, "Kablosuz ağlarda güçlü kriptografi" },
                    { 74, 50, "5.2.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anti-malware çözümü bilinen tüm kötü amaçlı yazılım türlerini tespit etmeli ve kaldırmalı, engellemeli veya kontrol altına almalıdır.", 5, false, "Anti-malware çözümünün yetenekleri" },
                    { 75, 50, "5.3.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anti-malware çözümü periyodik tarama ile aktif veya gerçek zamanlı tarama yapmalı ya da sürekli davranış analizi uygulamalıdır.", 5, false, "Periyodik ve gerçek zamanlı tarama" },
                    { 76, 50, "5.3.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Çıkarılabilir elektronik medya takıldığında anti-malware otomatik tarama veya sürekli davranış analizi yapmalıdır.", 5, false, "Çıkarılabilir medyanın taranması" },
                    { 77, 50, "5.3.4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anti-malware çözümünün denetim kayıtları etkin olmalı ve log saklama gereksinimine uygun saklanmalıdır.", 5, false, "Anti-malware loglarının tutulması" },
                    { 78, 50, "5.3.5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anti-malware çözümü, yönetimin vaka bazında ve süreli onayı olmadan kullanıcılarca devre dışı bırakılamamalı veya değiştirilememelidir.", 5, false, "Anti-malware'in devre dışı bırakılamaması" },
                    { 79, 58, "8.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kullanıcı tanımlama ve kimlik doğrulamaya ilişkin politika ve prosedürler belgelenmeli, güncel tutulmalı, kullanılmalı ve ilgili herkese bildirilmelidir.", 8, false, "Gereksinim 8 politika ve prosedürleri" },
                    { 80, 50, "8.2.4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kullanıcı kimliklerinin ve kimlik doğrulama faktörlerinin eklenmesi, silinmesi ve değiştirilmesi onaylı ve belgelenmiş yetkiyle yapılmalıdır.", 8, false, "Kullanıcı hesabı ekleme, silme ve değiştirme" },
                    { 81, 51, "8.2.5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İşten ayrılan kullanıcıların erişimi derhal kaldırılmalıdır.", 8, false, "İşten ayrılanların erişiminin kaldırılması" },
                    { 82, 26, "8.2.7", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Üçüncü tarafların uzaktan erişim hesapları yalnızca ihtiyaç süresince etkin tutulmalı ve kullanımı izlenmelidir.", 8, false, "Üçüncü tarafların uzaktan erişimi" },
                    { 83, 19, "8.3.5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İlk kullanım veya sıfırlama için verilen parolalar her kullanıcıya özgü olmalı ve ilk kullanımda değiştirilmelidir.", 8, false, "İlk ve sıfırlanan parolalar" },
                    { 84, 19, "8.3.7", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kullanıcılar yeni parola olarak son dört parolasından birini kullanamamalıdır.", 8, false, "Parola geçmişi" },
                    { 85, 19, "8.3.9", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Parola tek kimlik doğrulama faktörüyse en az 90 günde bir değiştirilmeli veya hesabın güvenlik durumu dinamik olarak analiz edilmelidir.", 8, false, "Tek faktörde parola değişim süresi" },
                    { 86, 120, "9.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisine fiziksel erişimin kısıtlanmasına ilişkin politika ve prosedürler belgelenmeli, güncel tutulmalı, kullanılmalı ve ilgili herkese bildirilmelidir.", 9, false, "Gereksinim 9 politika ve prosedürleri" },
                    { 87, 50, "9.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi ortamındaki sistemlere fiziksel erişimi kısıtlamak için uygun tesis giriş kontrolleri bulunmalıdır.", 9, false, "Tesis giriş kontrolleri" },
                    { 88, 24, "9.2.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ziyaretçilerin erişebildiği alanlardaki fiziksel ve mantıksal ağ prizleri kısıtlanmalıdır.", 9, false, "Herkese açık ağ prizlerinin kısıtlanması" },
                    { 89, 127, "9.4.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi içeren çevrimdışı medya yedekleri güvenli bir yerde saklanmalıdır.", 9, false, "Çevrimdışı medya yedeklerinin güvenliği" },
                    { 90, 63, "9.4.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi içeren tüm medya, verinin hassasiyetine göre sınıflandırılmalıdır.", 9, false, "Medyanın sınıflandırılması" },
                    { 91, 63, "9.4.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tesis dışına gönderilen kart verisi içeren medya güvenli kurye veya takip edilebilir bir yöntemle gönderilmeli ve kayıt altına alınmalıdır.", 9, false, "Tesis dışına gönderilen medya" },
                    { 92, 63, "9.4.4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tesis dışına taşınan tüm kart verisi içeren medya yönetim tarafından onaylanmalıdır.", 9, false, "Medya gönderiminde yönetim onayı" },
                    { 93, 92, "9.5.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cihaz markası, modeli, konumu ve seri numarasını içeren güncel bir POI cihaz listesi tutulmalıdır.", 9, false, "POI cihaz listesinin tutulması" },
                    { 94, 92, "9.5.1.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "POI cihaz yüzeyleri kurcalama ve yetkisiz değişime karşı periyodik olarak incelenmelidir.", 9, false, "POI cihazlarının periyodik muayenesi" },
                    { 95, 19, "11.3.2.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Önemli değişikliklerden sonra dış zafiyet taraması yapılmalı, yüksek riskli bulgular giderilip yeniden taranmalıdır.", 11, false, "Önemli değişiklik sonrası dış tarama" },
                    { 96, 26, "11.4.5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Segmentasyon kullanılıyorsa segmentasyon kontrolleri en az yılda bir ve değişikliklerden sonra sızma testiyle doğrulanmalıdır.", 11, false, "Segmentasyon kontrollerinin testi" },
                    { 97, 126, "12.1.2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bilgi güvenliği politikası en az yılda bir ve ortam değiştiğinde gözden geçirilip güncellenmelidir.", 12, false, "Güvenlik politikasının gözden geçirilmesi" },
                    { 98, 94, "12.1.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Politika tüm personelin bilgi güvenliği rol ve sorumluluklarını tanımlamalı ve personel bunları kabul etmelidir.", 12, false, "Güvenlik rol ve sorumlulukları" },
                    { 99, 50, "12.6.3.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvenlik farkındalık eğitimi oltalama ve sosyal mühendislik gibi kart verisi ortamının güvenliğini etkileyebilecek tehditleri içermelidir.", 12, false, "Oltalama ve sosyal mühendislik farkındalığı" },
                    { 100, 127, "12.8.3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hizmet sağlayıcılarla çalışmaya başlamadan önce yerleşik bir durum tespiti (due diligence) süreci uygulanmalıdır.", 12, false, "Hizmet sağlayıcı seçiminde durum tespiti" },
                    { 101, 127, "12.8.5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hangi PCI DSS gereksinimlerinin hizmet sağlayıcı, hangilerinin kurum ya da ikisi tarafından yönetildiğine ilişkin bilgi tutulmalıdır.", 12, false, "Hizmet sağlayıcı sorumluluk matrisi" },
                    { 102, 24, "A2.1.1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "SSL veya erken TLS kullanan kart-mevcut POS terminallerinin, bu protokollerin bilinen açıklarından etkilenmediği doğrulanmalıdır.", 4, false, "Ek A2: POS cihazlarında SSL / erken TLS" }
                });

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Order",
                value: 28);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 3,
                column: "Order",
                value: 58);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Order",
                value: 76);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Order",
                value: 5);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Order",
                value: 6);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 7,
                column: "Order",
                value: 9);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 8,
                column: "Order",
                value: 13);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 9,
                column: "Order",
                value: 14);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 10,
                column: "Order",
                value: 19);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 11,
                column: "Order",
                value: 22);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 12,
                column: "Order",
                value: 23);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 13,
                column: "Order",
                value: 27);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 14,
                column: "Order",
                value: 29);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 15,
                column: "Order",
                value: 30);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 16,
                column: "Order",
                value: 31);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 17,
                column: "Order",
                value: 33);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 18,
                column: "Order",
                value: 34);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 19,
                column: "Order",
                value: 36);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 20,
                column: "Order",
                value: 41);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 21,
                column: "Order",
                value: 42);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 22,
                column: "Order",
                value: 43);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 23,
                column: "Order",
                value: 44);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 24,
                column: "Order",
                value: 45);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 25,
                column: "Order",
                value: 46);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 26,
                column: "Order",
                value: 47);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 27,
                column: "Order",
                value: 48);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 28,
                column: "Order",
                value: 50);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 29,
                column: "Order",
                value: 51);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 30,
                column: "Order",
                value: 54);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 31,
                column: "Order",
                value: 56);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 32,
                column: "Order",
                value: 61);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 33,
                column: "Order",
                value: 62);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 34,
                column: "Order",
                value: 66);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 35,
                column: "Order",
                value: 71);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 36,
                column: "Order",
                value: 72);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 37,
                column: "Order",
                value: 75);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 38,
                column: "Order",
                value: 77);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 39,
                column: "Order",
                value: 78);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 40,
                column: "Order",
                value: 79);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 41,
                column: "Order",
                value: 80);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 42,
                column: "Order",
                value: 81);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 43,
                column: "Order",
                value: 82);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 44,
                column: "Order",
                value: 84);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 45,
                column: "Order",
                value: 86);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 46,
                column: "Order",
                value: 87);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 47,
                column: "Order",
                value: 88);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 48,
                column: "Order",
                value: 91);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 49,
                column: "Order",
                value: 92);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 50,
                column: "Order",
                value: 93);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 51,
                column: "Order",
                value: 94);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 52,
                column: "Order",
                value: 96);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 53,
                column: "Order",
                value: 97);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 54,
                column: "Order",
                value: 99);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 55,
                column: "Order",
                value: 101);

            migrationBuilder.InsertData(
                table: "SurveyQuestions",
                columns: new[] { "Id", "CreatedAt", "EvidenceHint", "EvidenceRequired", "Order", "RequirementId", "Text" },
                values: new object[,]
                {
                    { 56, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güncel ağ diyagramı", true, 2, 56, "Kart verisi ortamını ve diğer ağlarla tüm bağlantılarını (kablosuz ağlar dahil) gösteren güncel bir ağ diyagramınız var mı?" },
                    { 57, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "İzinli port/servis listesi ve iş gerekçeleri", true, 3, 57, "Ağ güvenlik kontrollerinde izin verilen tüm servis, protokol ve portların listesi, onayı ve iş gerekçesi var mı?" },
                    { 58, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvensiz servis listesi ve alınan önlemler", false, 4, 58, "Telnet, FTP gibi güvensiz kabul edilen bir servis veya protokol kullanıyorsanız, bunun riskini azaltan ek güvenlik önlemleri tanımlı ve uygulanıyor mu?" },
                    { 59, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "CDE çıkış trafiği kuralları dökümü", true, 7, 59, "Kart verisi ortamından (CDE) dışarı çıkan ağ trafiği yalnızca iş için gerekli olanla sınırlı mı ve diğer tüm çıkış trafiği engelleniyor mu?" },
                    { 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kablosuz ağ segmentasyon kuralları", false, 8, 60, "Tüm kablosuz ağlarınız ile kart verisi ortamı arasında, yalnızca yetkili trafiğe izin veren bir firewall/NSC var mı?" },
                    { 61, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "NSC anti-spoofing ayarı", false, 10, 61, "Sahte (spoof) kaynak IP adresiyle gelen trafiğin iç ağınıza girmesini engelleyen önlemler uygulanıyor mu?" },
                    { 62, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Uç nokta güvenlik politikası / kişisel firewall ayarı", false, 11, 62, "Hem internete hem de kart verisi ortamına bağlanan dizüstü bilgisayar gibi cihazlarda, kullanıcının kapatamadığı güvenlik kontrolleri (kişisel firewall vb.) var mı?" },
                    { 63, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvenli yapılandırma politikası", true, 12, 63, "Sistem bileşenlerinin güvenli yapılandırılmasına ilişkin politika ve prosedürleriniz belgelenmiş, güncel ve ilgili çalışanlarca biliniyor mu?" },
                    { 64, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Etkin servis listesi / sıkılaştırma kontrol çıktısı", false, 15, 64, "Sistemlerde yalnızca gerekli servis ve işlevler etkin mi; gereksiz olanlar kaldırılıyor veya devre dışı bırakılıyor mu?" },
                    { 65, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvensiz servis gerekçesi ve önlemleri", false, 16, 65, "Sistemlerinizde güvensiz servis veya protokol bulunuyorsa bunların iş gerekçesi belgelenmiş ve ek güvenlik önlemleriyle korunuyor mu?" },
                    { 66, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Güvenlik parametresi yapılandırma çıktısı", false, 17, 66, "İşletim sistemi, veritabanı ve uygulamalardaki güvenlik parametreleri kötüye kullanımı önleyecek şekilde (sıkılaştırma kılavuzlarına göre) ayarlanmış mı?" },
                    { 67, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yönetici erişim protokolleri yapılandırması", true, 18, 67, "Sistemlere uzaktan yapılan tüm yönetici erişimleri (SSH, HTTPS, RDP vb.) güçlü şifrelemeyle mi yapılıyor; telnet gibi şifresiz yöntemler kapalı mı?" },
                    { 68, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kablosuz anahtar değişim prosedürü", false, 20, 68, "Kart verisi ortamına bağlı kablosuz ağlarınızın şifreleme anahtarları, anahtarı bilen biri işten ayrıldığında ya da anahtarın ele geçtiğinden şüphelenildiğinde değiştiriliyor mu?" },
                    { 69, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kart verisi koruma politikası", true, 21, 69, "Saklanan kart verisinin korunmasına ilişkin politika ve prosedürleriniz belgelenmiş, güncel ve ilgili çalışanlarca biliniyor mu?" },
                    { 70, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Veri keşif taraması sonucu veya süreç beyanı", false, 24, 70, "Yetkilendirme tamamlandıktan sonra kartın manyetik şerit veya çip (iz) verisi hiçbir sistemde, logda veya belgede saklanmıyor mu?" },
                    { 71, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Form örneği / veri keşif taraması", false, 25, 71, "Yetkilendirme tamamlandıktan sonra kartın güvenlik kodu (CVV2/CVC2) kağıt formlar dahil hiçbir yerde saklanmıyor mu?" },
                    { 72, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Süreç beyanı / veri keşif taraması", false, 26, 72, "Yetkilendirme tamamlandıktan sonra PIN veya şifreli PIN bloğu hiçbir yerde saklanmıyor mu?" },
                    { 73, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kablosuz ağ şifreleme ayarı", false, 32, 73, "Kart verisi ileten ya da kart verisi ortamına bağlı kablosuz ağlarınızda WPA2/WPA3 gibi güçlü şifreleme kullanılıyor mu?" },
                    { 74, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anti-malware ürün özellikleri / politika ayarları", false, 35, 74, "Kullandığınız anti-malware çözümü bilinen tüm zararlı yazılım türlerini tespit edip kaldırabiliyor veya engelleyebiliyor mu?" },
                    { 75, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tarama zamanlaması ve gerçek zamanlı koruma ayarı", false, 37, 75, "Anti-malware çözümünüz düzenli tarama ile birlikte gerçek zamanlı koruma (veya sürekli davranış analizi) yapıyor mu?" },
                    { 76, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Çıkarılabilir medya tarama ayarı", false, 38, 76, "USB bellek gibi çıkarılabilir medya takıldığında anti-malware tarafından otomatik olarak taranıyor mu?" },
                    { 77, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anti-malware log ayarları", false, 39, 77, "Anti-malware çözümünüzün logları açık mı ve en az 12 ay saklanıyor mu?" },
                    { 78, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Merkezi yönetim / kurcalama koruması ayarı", false, 40, 78, "Kullanıcılar anti-malware çözümünü, yönetimin belirli bir süre için verdiği onay olmadan kapatamıyor veya değiştiremiyor mu?" },
                    { 79, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kimlik ve erişim yönetimi politikası", true, 49, 79, "Kullanıcı hesapları ve kimlik doğrulamaya ilişkin politika ve prosedürleriniz belgelenmiş, güncel ve ilgili çalışanlarca biliniyor mu?" },
                    { 80, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hesap talep/onay kayıtları", false, 52, 80, "Kullanıcı hesaplarının açılması, kapatılması ve yetki değişiklikleri onaylı bir talep ile ve kayıt altına alınarak mı yapılıyor?" },
                    { 81, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ayrılan personel hesap kapatma kayıtları", false, 53, 81, "İşten ayrılan çalışanların tüm sistemlere erişimi derhal kapatılıyor mu?" },
                    { 82, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tedarikçi erişim prosedürü ve kayıtları", false, 55, 82, "Tedarikçi ve üçüncü tarafların uzaktan erişim hesapları yalnızca ihtiyaç olduğunda açılıyor, kullanım sonrası kapatılıyor ve izleniyor mu?" },
                    { 83, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Parola sıfırlama prosedürü", false, 57, 83, "Yeni hesaplara veya parola sıfırlamada verilen parolalar her kullanıcı için farklı mı ve ilk girişte değiştirilmesi zorunlu mu?" },
                    { 84, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Parola geçmişi ayarı", false, 59, 84, "Sistemleriniz kullanıcıların son dört parolasından birini yeniden kullanmasını engelliyor mu?" },
                    { 85, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Parola süre sonu ayarı", false, 60, 85, "Parolanın tek kimlik doğrulama yöntemi olduğu hesaplarda parolalar en az 90 günde bir değiştiriliyor mu (veya risk tabanlı dinamik analiz yapılıyor mu)?" },
                    { 86, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fiziksel güvenlik politikası", true, 63, 86, "Kart verisine fiziksel erişimin kısıtlanmasına ilişkin politika ve prosedürleriniz belgelenmiş, güncel ve ilgili çalışanlarca biliniyor mu?" },
                    { 87, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Giriş kontrol sistemi kayıtları / fotoğraf", false, 64, 87, "Kart verisi ortamındaki sistemlerin bulunduğu alanlara (sunucu odası vb.) fiziksel giriş kartlı geçiş, kilit gibi kontrollerle kısıtlanıyor mu?" },
                    { 88, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ağ prizi / port güvenliği ayarı", false, 65, 88, "Ziyaretçilerin bulunabildiği alanlardaki ağ prizleri kapatılmış veya yalnızca yetkili cihazların bağlanabileceği şekilde kısıtlanmış mı?" },
                    { 89, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yedek saklama yeri ve erişim prosedürü", false, 67, 89, "Kart verisi içeren yedekler (harici disk, teyp vb.) güvenli ve erişimi kısıtlı bir yerde mi saklanıyor?" },
                    { 90, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Veri sınıflandırma politikası", false, 68, 90, "Kart verisi içeren tüm ortamlar (kağıt, disk vb.) hassasiyetine göre sınıflandırılıp etiketleniyor mu?" },
                    { 91, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Medya gönderim kayıtları", false, 69, 91, "Kart verisi içeren belge veya ortamlar tesis dışına gönderilirken güvenli kurye ya da takip edilebilir bir yöntem kullanılıyor ve kayıt tutuluyor mu?" },
                    { 92, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Medya çıkış onay kayıtları", false, 70, 92, "Kart verisi içeren ortamların tesis dışına çıkarılması için yönetim onayı alınıyor mu?" },
                    { 93, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "POI cihaz envanteri", true, 73, 93, "Ödeme terminallerinizin marka, model, konum ve seri numarasını içeren güncel bir listesi var mı?" },
                    { 94, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cihaz muayene kontrol listesi / kayıtları", false, 74, 94, "Ödeme terminalleri kurcalama, sahte parça (skimmer) ve cihaz değişimine karşı düzenli olarak inceleniyor mu?" },
                    { 95, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Değişiklik sonrası tarama raporu", false, 83, 95, "İnternete açık sistemlerde önemli bir değişiklikten sonra dış zafiyet taraması yapılıyor ve bulgular giderilip yeniden taranıyor mu?" },
                    { 96, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Segmentasyon testi raporu", true, 85, 96, "Kapsamı daraltmak için ağ segmentasyonu kullanıyorsanız, segmentasyonun etkinliği en az yılda bir sızma testiyle doğrulanıyor mu?" },
                    { 97, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Politika revizyon geçmişi", true, 89, 97, "Bilgi güvenliği politikanız en az yılda bir ve önemli değişikliklerde gözden geçirilip güncelleniyor mu?" },
                    { 98, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Rol tanımları ve kabul kayıtları", false, 90, 98, "Bilgi güvenliği politikanız tüm çalışanların güvenlik sorumluluklarını tanımlıyor ve çalışanlar bunu okuyup kabul ettiklerini onaylıyor mu?" },
                    { 99, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eğitim içeriği", false, 95, 99, "Güvenlik farkındalık eğitiminiz oltalama (phishing) ve sosyal mühendislik saldırılarını tanımayı ve bildirmeyi içeriyor mu?" },
                    { 100, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tedarikçi değerlendirme prosedürü", false, 98, 100, "Yeni bir hizmet sağlayıcıyla çalışmaya başlamadan önce PCI DSS uyumu dahil güvenlik durumunu değerlendirdiğiniz bir süreciniz var mı?" },
                    { 101, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sorumluluk matrisi", true, 100, 101, "Her hizmet sağlayıcı için hangi PCI DSS gereksinimlerinden onun, hangilerinden sizin sorumlu olduğunuzu gösteren bir sorumluluk matrisiniz var mı?" },
                    { 102, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Terminal üreticisi beyanı / TLS yapılandırması", false, 102, 102, "Kart-mevcut ödeme terminalleriniz SSL veya eski TLS (1.0/1.1) kullanıyorsa, bu cihazların bu protokollerin bilinen açıklarından etkilenmediği doğrulandı mı? Kullanmıyorsa 'Uygulanamaz' seçin." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Order",
                value: 11);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 3,
                column: "Order",
                value: 30);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Order",
                value: 37);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Order",
                value: 2);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Order",
                value: 3);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 7,
                column: "Order",
                value: 4);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 8,
                column: "Order",
                value: 5);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 9,
                column: "Order",
                value: 6);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 10,
                column: "Order",
                value: 7);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 11,
                column: "Order",
                value: 8);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 12,
                column: "Order",
                value: 9);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 13,
                column: "Order",
                value: 10);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 14,
                column: "Order",
                value: 12);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 15,
                column: "Order",
                value: 13);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 16,
                column: "Order",
                value: 14);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 17,
                column: "Order",
                value: 15);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 18,
                column: "Order",
                value: 16);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 19,
                column: "Order",
                value: 17);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 20,
                column: "Order",
                value: 18);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 21,
                column: "Order",
                value: 19);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 22,
                column: "Order",
                value: 20);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 23,
                column: "Order",
                value: 21);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 24,
                column: "Order",
                value: 22);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 25,
                column: "Order",
                value: 23);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 26,
                column: "Order",
                value: 24);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 27,
                column: "Order",
                value: 25);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 28,
                column: "Order",
                value: 26);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 29,
                column: "Order",
                value: 27);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 30,
                column: "Order",
                value: 28);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 31,
                column: "Order",
                value: 29);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 32,
                column: "Order",
                value: 31);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 33,
                column: "Order",
                value: 32);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 34,
                column: "Order",
                value: 33);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 35,
                column: "Order",
                value: 34);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 36,
                column: "Order",
                value: 35);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 37,
                column: "Order",
                value: 36);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 38,
                column: "Order",
                value: 38);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 39,
                column: "Order",
                value: 39);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 40,
                column: "Order",
                value: 40);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 41,
                column: "Order",
                value: 41);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 42,
                column: "Order",
                value: 42);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 43,
                column: "Order",
                value: 43);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 44,
                column: "Order",
                value: 44);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 45,
                column: "Order",
                value: 45);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 46,
                column: "Order",
                value: 46);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 47,
                column: "Order",
                value: 47);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 48,
                column: "Order",
                value: 48);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 49,
                column: "Order",
                value: 49);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 50,
                column: "Order",
                value: 50);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 51,
                column: "Order",
                value: 51);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 52,
                column: "Order",
                value: 52);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 53,
                column: "Order",
                value: 53);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 54,
                column: "Order",
                value: 54);

            migrationBuilder.UpdateData(
                table: "SurveyQuestions",
                keyColumn: "Id",
                keyValue: 55,
                column: "Order",
                value: 55);
        }
    }
}
