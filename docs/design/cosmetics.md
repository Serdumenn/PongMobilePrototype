# Özellik: Pingi Dolabı (kostümler, raketler, temalar)

*v1 · 2026-09-30 · Durum: **Uygulandı, Editor'de doğrulandı** · Sırada: cihazda ve Play Internal testing'de test*

Görsel öneri: [cosmetics/index.html](cosmetics/index.html)

## Amaç
- Oyuncuya "bir el daha" için ek bir sebep vermek (koleksiyon ve hedef)
- Gelir: ödüllü reklam, tek tek satış, Pingi Pass paketi ve reklamları kaldırma
- Oyun dengesine dokunmamak: hepsi kozmetik, fizik ve collider'lar aynı kalır

## Katalog ve ürünler

Tek kaynak: `Assets/_Game/Data/CosmeticCatalog.asset` ve `Assets/_Game/Data/Cosmetics/cos_*.asset`.

| İçerik | Kategori | Açılma yolu | Ürün kimliği | Önerilen fiyat |
|---|---|---|---|---|
| Pingi | Pingi | Ücretsiz | — | — |
| Minty | Pingi | Best 10 | `skin_minty` | $0.99 |
| Grape | Pingi | 5 reklam | `skin_grape` | $0.99 |
| Sunny | Pingi | Best 25 | `skin_sunny` | $0.99 |
| Panda | Pingi | 10 reklam | `skin_panda` | $1.49 |
| Kitty | Pingi | 10 reklam | `skin_kitty` | $1.49 |
| Froggy | Pingi | 15 reklam | `skin_froggy` | $1.99 |
| Golden | Pingi | Yalnızca Pass | — | — |
| Astro | Pingi | 7 gün üst üste oyna | — | — |
| Teal | Raket | Ücretsiz | — | — |
| Coral | Raket | Best 15 | `paddle_coral` | $0.99 |
| Candy | Raket | 5 reklam | `paddle_candy` | $0.99 |
| Wood | Raket | 10 reklam | `paddle_wood` | $1.49 |
| Rainbow | Raket | 15 reklam | `paddle_rainbow` | $1.99 |
| Gold | Raket | Yalnızca Pass | — | — |
| Soft Pop | Tema | Ücretsiz | — | — |
| Mint | Tema | Best 30 | `theme_mint` | $0.99 |
| Sunset | Tema | 10 reklam | `theme_sunset` | $1.49 |
| Night | Tema | 15 reklam | `theme_night` | $1.99 |

| Paket | Ürün kimliği | Önerilen fiyat | Etki |
|---|---|---|---|
| Pingi Pass | `pingi_pass` | $4.99 | Her şeyi açar + Golden set |
| Reklamları kaldır | `remove_ads` | $2.99 | Geçiş reklamları kapanır; ödüllü reklamlar isteğe bağlı kalır |

Hepsi **tek seferlik (non-consumable)**, toplam 15 ürün. Fiyatlar Play Console'da belirlenir ve ülke bazında yerelleşir; oyun fiyatı mağazadan okur.

## Davranış
- Reklam ilerlemesi içerik başına saklanır (ör. Kitty 7/10). "Watch ad" her zaman kullanıcının seçimidir ve gereken reklam sayısı önceden gösterilir.
- **Astro** yalnızca 7 günlük oyun serisiyle açılır: Pass'e dahil değildir ve satılmaz. Seri, Skorlar ekranında ve Dolap'taki kilit penceresinde görünür ([modes.md](modes.md)).
- Skor kilitleri Game Over'da kontrol edilir ve her zaman **Classic** rekoruna bakar. Yeni açılan içerik "New costume / paddle / theme!" penceresiyle gelir ve pencereden hemen takılabilir. Güncellemeden önce kazanılmış skorlar sessizce açılır.
- Satın alma akışı: ödül verilir, `cosmetics.json`'a kaydedilir, **ancak kayıt başarılıysa** `ConfirmPurchase` çağrılır. Aynı sipariş ikinci kez gelirse sipariş kimliğiyle engellenir.
- Satın almalar uygulama açılışında mağazadan geri yüklenir (`FetchPurchases`).

## Teknik parçalar

| Dosya | Sorumluluk |
|---|---|
| `Scripts/Cosmetics/CosmeticItem.cs` · `CosmeticCatalog.cs` | Veri (ScriptableObject) |
| `Scripts/Cosmetics/CosmeticInventory.cs` | Sahiplik, takılı olanlar, reklam ilerlemesi, işlenmiş siparişler. `persistentDataPath/cosmetics.json` dosyasına atomik yazılır |
| `Scripts/Cosmetics/CosmeticsService.cs` | Kilit kuralları, takma, reklam ve satın alma ile ödül verme, skor ödülleri |
| `Scripts/Cosmetics/SkinApplier.cs` | Top ifadeleri, raket, kamera, lekeler ve UI tema sınıfı |
| `Scripts/Services/PurchaseService.cs` | Unity IAP 5.4.3 (Billing 9): bağlanma, ürünler, iki adımlı satın alma |
| `Scripts/Services/AdManager.cs` | Ödüllü reklam, reklamsız bayrağı, Editor simülasyonu |
| `Scripts/UI/ShopScreen.cs` · `RewardScreen.cs` + `UI/Screens/*.uxml` · `UI/Theme/Shop.uss` · `Themes.uss` | Dolap ekranı, kilit penceresi, ödül penceresi, temalar |
| `ArtSource/tools/generate_skins.py` | Kostüm ve raket SVG üreticisi |

## Editor'de test
- Ödüllü reklam Editor'de simüle edilir (her tıklama +1 ilerleme). Gerçek reklam için `AdManager` → `EnableAdsInEditor`.
- Satın almada Unity'nin sahte mağazası "Buy / Cancel" penceresi açar, fiyatlar $0.01 görünür.
- Test verisini sıfırlamak için `%USERPROFILE%/AppData/LocalLow/SERDUMEN/Pingi Pongi/cosmetics.json` dosyası silinir.

## Yayından önce yapılacaklar (hesap işleri)
1. **AdMob:** "Rewarded" reklam birimi oluştur. Kimliği `AdManager` → `RewardedAdUnitId` alanına girilecek (şu an Google test kimliği).
2. **Play Console:** Ödeme profili (merchant) oluştur, yukarıdaki 15 ürünü aynı kimliklerle ekle, fiyatlandır ve etkinleştir.
3. **Gerçek satın alma testi:** Build'i Internal testing kanalına yükle ve kendini lisans test kullanıcısı olarak ekle.
4. **Makbuz doğrulama (önerilir):** Play Console'daki lisans anahtarıyla Unity'de *Services → In-App Purchasing → Receipt Validation Obfuscator* çalıştırılır, ardından yerel doğrulama açılır. Şu an satın almalar mağaza sonucuna güvenilerek veriliyor.
5. **Mağaza formları:** IARC yaş derecelendirmesinde "Uygulama içi satın alma" işaretlenir, Data safety formuna "Satın alma geçmişi" eklenir.

## Kapı kararları
- [x] Katalog ve kilit atamaları: panodaki gibi (2026-09-30)
- [x] Gelir modeli: paketler + tek tek satış (2026-09-30)
- [x] Unity IAP 5.4.3 eklendi (2026-09-30)
- [x] Seri ödülü kostümü Astro eklendi (2026-09-30)
- [ ] Fiyatların son onayı (Play Console'da)
