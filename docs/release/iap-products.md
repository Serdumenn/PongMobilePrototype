# Uygulama içi ürünler (Google Play)

15 ürün, hepsi **tek seferlik** (bir kez alınır, kalıcıdır). Ürün kimlikleri oyundaki kimliklerle birebir aynı olmalıdır: `CosmeticCatalog.cs` (`pingi_pass`, `remove_ads`) ve `Assets/_Game/Data/Cosmetics/cos_*.asset`. Fiyatlar ABD doları olarak girilir; Play diğer ülkelerin yerel fiyatlarını kendisi hesaplar.

| # | Ürün kimliği | Ad (en-US) | Açıklama (en-US) | Ad (tr-TR) | Açıklama (tr-TR) | Fiyat |
|---|---|---|---|---|---|---|
| 1 | `pingi_pass` | Pingi Pass | Unlocks every Pingi face, paddle and theme, plus the exclusive Golden set. | Pingi Pass | Tüm Pingi yüzlerini, raketleri ve temaları açar; özel Altın seti de dahil. | $4.99 |
| 2 | `remove_ads` | Remove ads | Removes the ads between games. Optional reward ads stay available. | Reklamları kaldır | Oyunlar arasındaki reklamları kaldırır. İsteğe bağlı ödüllü reklamlar kalır. | $2.99 |
| 3 | `skin_minty` | Minty | A fresh mint Pingi face. | Nane | Ferah nane renkli Pingi yüzü. | $0.99 |
| 4 | `skin_grape` | Grape | A juicy grape Pingi face. | Üzüm | Sulu üzüm renkli Pingi yüzü. | $0.99 |
| 5 | `skin_sunny` | Sunny | A bright sunny Pingi face. | Güneş | Parlak güneş renkli Pingi yüzü. | $0.99 |
| 6 | `skin_panda` | Panda | A cuddly panda Pingi face. | Panda | Sevimli panda Pingi yüzü. | $1.49 |
| 7 | `skin_kitty` | Kitty | A playful kitty Pingi face. | Kitty | Oyuncu kedi Pingi yüzü. | $1.49 |
| 8 | `skin_froggy` | Froggy | A happy froggy Pingi face. | Kurbi | Neşeli kurbağa Pingi yüzü. | $1.99 |
| 9 | `paddle_coral` | Coral paddle | A coral red paddle. | Mercan raket | Mercan kırmızısı raket. | $0.99 |
| 10 | `paddle_candy` | Candy paddle | A sweet candy striped paddle. | Şeker raket | Tatlı şeker desenli raket. | $0.99 |
| 11 | `paddle_wood` | Wood paddle | A classic wooden paddle. | Ahşap raket | Klasik ahşap raket. | $1.49 |
| 12 | `paddle_rainbow` | Rainbow paddle | A colorful rainbow paddle. | Gökkuşağı raket | Rengârenk gökkuşağı raket. | $1.99 |
| 13 | `theme_mint` | Mint theme | A cool mint table theme. | Nane tema | Serin nane renkli masa teması. | $0.99 |
| 14 | `theme_sunset` | Sunset theme | A warm sunset table theme. | Gün Batımı tema | Sıcak gün batımı masa teması. | $1.49 |
| 15 | `theme_night` | Night theme | A calm night table theme. | Gece tema | Sakin gece masa teması. | $1.99 |

## Play Console'da oluşturma

1. **Google Play ile para kazanın → Ürünler → Tek seferlik ürünler → Tek seferlik ürün oluştur.**
2. Ürün kimliği, ad ve açıklama tablodan girilir. Türkçe metinler **Çeviri ekle** ile eklenir.
3. **Satın alma seçeneği**: kimlik `buy`, tür **Satın al**, fiyat tablodaki dolar fiyatı, **Fiyatı tüm bölgelere uygula**.
4. **Kaydet** → **Etkinleştir**. Ürün kimliği sonradan değiştirilemez ve silinen kimlik tekrar kullanılamaz.

## Test

- **Ayarlar → Lisans testi**: geliştirici ve test kullanıcılarının Gmail adresleri eklenir, yanıt **RESPOND_NORMALLY**. Bu hesaplar satın alırken "Test kartı, her zaman onaylanır" seçeneğini görür ve ücret alınmaz.
- Lisans testi listesinde olmayan kapalı test kullanıcıları gerçek ödeme yapar.
