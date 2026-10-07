# Play Console formlarının cevapları

Bu cevaplar oyunun bugünkü koduna göre hazırlandı (2026-10-05). Kullanılan servisler: Unity Gaming Services (Authentication, Multiplayer/Relay, Leaderboards, Cloud Save, Cloud Code, Friends), Google Play Games, Google AdMob (UMP onayıyla), Google Play Billing (Unity IAP).

## Veri güvenliği (Data safety)

| Soru | Cevap |
|---|---|
| Uygulama gerekli kullanıcı veri türlerini topluyor ya da paylaşıyor mu? | **Evet** |
| Tüm kullanıcı verileri aktarım sırasında şifreleniyor mu? | **Evet** |
| Kullanıcılar verilerinin silinmesini isteyebiliyor mu? | **Evet**. Uygulama içinde: Ayarlar → Online data → Delete. Web: `https://serdumenn.github.io/pingi-pongi/delete-account/` |
| Hesap oluşturma | Oyun içinde otomatik online hesap oluşur (anonim ya da Google Play Games). Hesap silme yolu yukarıda |

| Veri türü | Toplanıyor | Paylaşılıyor | İsteğe bağlı mı | Amaç |
|---|---|---|---|---|
| **Konum → Yaklaşık konum** (AdMob, IP'den) | Evet | Evet (reklam için Google) | Hayır | Reklamcılık, analiz, dolandırıcılık önleme |
| **Kişisel bilgiler → Kullanıcı kimlikleri** (Unity oyuncu kimliği, Google Play Games kimliği) | Evet | Hayır | Hayır | Uygulama işlevselliği, hesap yönetimi |
| **Finansal bilgiler → Satın alma geçmişi** (Pingi Pass / Reklamsız durumu yedekte) | Evet | Hayır | Hayır | Uygulama işlevselliği |
| **Uygulama etkinliği → Uygulama etkileşimleri** (skorlar, ilerleme, arkadaş listesi; AdMob reklam etkileşimleri) | Evet | Evet (reklam etkileşimleri Google'a) | Hayır | Uygulama işlevselliği, reklamcılık, analiz |
| **Uygulama bilgileri ve performansı → Teşhis** (AdMob SDK) | Evet | Evet | Hayır | Reklamcılık, analiz |
| **Cihaz veya diğer kimlikler → Reklam kimliği** (AdMob) | Evet | Evet | Hayır | Reklamcılık, analiz, dolandırıcılık önleme |

Notlar:
- Unity ve Google'a hizmet sağlayıcı olarak giden veriler Play'in tanımına göre "paylaşım" sayılmaz. AdMob'un reklam için kullandığı veriler paylaşım sayılır; Google'ın AdMob için önerdiği beyan budur.
- Sohbet, fotoğraf, kişiler, tam konum, sağlık, mesaj içeriği **yok**. Arkadaş davetleri yalnızca oda kodu ve mod içerir.
- Oyuncu adları serbest metin değildir; üretilmiş adlardır.

## İçerik derecelendirmesi (IARC anketi)

| Soru | Cevap |
|---|---|
| Kategori | Oyun |
| Şiddet, korku, cinsellik, küfür, madde kullanımı, kumar, kaba mizah | **Hayır** (hepsi) |
| Kullanıcılar birbiriyle etkileşebiliyor mu? | **Evet**: online maçlar ve arkadaş listesi. Sohbet ve serbest metin yok |
| Kullanıcının fiziksel konumu paylaşılıyor mu? | Hayır |
| Dijital satın alma | **Evet** |
| Beklenen sonuç | PEGI 3, ESRB Everyone; "Users Interact", "In-Game Purchases" notları |

## Hedef kitle ve içerik

| Konu | Cevap |
|---|---|
| Hedef yaş grubu | **13–15, 16–17, 18+** (13 yaş altı seçilmez; seçilirse Aileler politikası, sertifikalı reklam ağları ve ek kurallar gerekir) |
| Uygulama çocukların ilgisini çekebilir mi? | Tarz sevimli olduğu için Google sorabilir. Cevap: oyun genel kitleye yöneliktir; mağaza metni ve görseller çocuklara özel değildir |
| Reklam içeriyor mu? | **Evet** |
| Uygulama erişimi (incelemeciler için) | Tüm işlevler giriş yapmadan kullanılabilir; Google Play Games isteğe bağlı |
| Haber, sağlık, finans, devlet uygulaması | Hayır |
