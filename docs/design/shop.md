# Özellik: Koleksiyon genişlemesi (altın içeriği)

*v1 · 2026-11-15 · Durum: **Uygulandı, Editor'de doğrulandı***

Görsel öneri: [shop/index.html](shop/index.html). Ürün sahibi sayfadaki seçeneklerin **hepsini** seçti: 1A, 1B, 1C, 1D, 1E ve 2A, 2B, 2C, 2D.

## Neden

Koleksiyon'da 25 eşya var; altınla alınabilen 13'ünün toplamı 5.800 altın. Bir Arcade koşusu ikiye katlamayla birlikte 40-100 altın getiriyor, yani her gün oynayan biri her şeyi 2-3 haftada alıyor. Sonrasında altının alacak bir şeyi kalmıyor ve Arcade'in ana ödülü anlamını yitiriyor. Klan fikri ürün sahibi tarafından bırakıldı.

## Hedefler

- Altınla alınacak yeterli ve çekici içerik: her gün oynayan biri için 2-3 aylık hedef.
- Oyunda görünen yeni türler: top izi, vuruş efekti, rekor kutlaması ve başkalarının gördüğü profil çerçevesi.
- Mevcut türlere (Pingi, raket, tema) yeni eşyalar.
- Oyuncuyu her gün geri getiren ve uzun vadeli hedef veren satış düzenleri: günün vitrini, setler, sürpriz kutu, sezon eşyaları.

## Hedef olmayanlar

- Altını gerçek parayla satmak. Altın sadece oynayarak ve ödüllü reklamla kazanılır.
- Klan, sohbet ve yazılabilen isimler.
- Mevcut kart, buton ve sekme stilinin değişmesi.
- Oyun alanının, topun fiziğinin ya da puanlamanın değişmesi. Efektler tamamen görseldir.

## Kaçınılacaklar

- Efektler topu, raketi ya da skoru okunmaz yaparsa.
- Satın alınan bir eşya kaybolursa ya da Pingi Pass'ın sözü bozulursa.
- Sürpriz kutu kumar hissi verirse: gizli olasılık, parayla açılma, tekrar çıkan eşya.
- Düşük donanımlı telefonda maç takılırsa.
- Bir tema (özellikle Gece) yeni eşyalarla çiğ ya da kontrastsız görünürse.

## Ürün sahibinin kararları

- Seçeneklerin hepsi: 1A Top izi, 1B Vuruş efekti, 1C Rekor kutlaması, 1D Profil çerçevesi, 1E Mevcut türlere yenileri; 2A Günün vitrini, 2B Setler, 2C Sürpriz kutu, 2D Sezon eşyaları.
- Klan sistemi yok.

## Geliştiricinin verdiği kararlar (itiraz edilebilir)

1. **Pingi Pass'ın sözü korunur.** Pass "tüm Pingi yüzlerini, raketleri ve temaları açar" diye satıldı. Yeni Pingi yüzleri, raketler ve temalar (sezon eşyaları dahil) Pass sahiplerine de açılır. Yeni türler (iz, vuruş, kutlama, çerçeve) Pass'a dahil değildir; bunlar yalnızca altınla, vitrinle, setle ya da kutuyla alınır.
2. **Sürpriz kutu yalnızca altınla açılır.** Olasılıklar kutunun üstünde yazar. Kutu sadece sende **olmayan** eşyalardan seçer, böylece tekrar çıkmaz. Hepsi sende ise kutu kapanır.
3. **Günün vitrini cihaz saatine göre her gece yerel 00:00'da yenilenir.** Herkese aynı 3 eşya çıkmaz; seçim oyuncunun kimliği ve tarihten türetilir, sahip olunan eşya gelmez. Vitrinin bir yeri "sadece vitrinde" eşyalara ayrılır. Günde bir kez, vitrindeki bir eşya reklam izleyerek %30 indirimle alınabilir.
4. **İlk sezon "Kış Şenliği"dir: 15 Aralık - 15 Ocak.** Sezon eşyaları sadece bu tarihlerde satılır. Alınan eşya sezon bitince de oyuncuda kalır. Tarihler veri dosyasından değişir, yeni sürüm gerekmez.
5. **Çerçeve, sıralama kaydının ek bilgisine ve arkadaş durum bilgisine eklenir.** Sunucu kodu değişmez. Eski sürümdeki oyuncular çerçeveyi göremez, sadece adı görür.
6. **Yeni efekt türleri tek bir "Efekt" sekmesinde durur:** İz, Vuruş, Kutlama ve Çerçeve küçük filtrelerle ayrılır. Sekmeler: Pingi · Raket · Tema · Efekt.

## Davranış

### Yeni eşya türleri

| Tür | Nerede görünür | Nasıl çalışır |
|---|---|---|
| Top izi | Her maçta, top uçarken | Bugünkü izin rengi ve kalınlığı değişir; bazı izler arkada şekil bırakır (yıldız, kalp, baloncuk, kar tanesi). Gökkuşağı izi renk geçişlidir. Topun ısınma parlaması korunur. |
| Vuruş efekti | Raket vuruşunda (duvar vuruşu bugünkü gibi kalır) | Bugünkü vuruş parçacıklarının şekli, renkleri ve sayısı değişir: konfeti, yıldız, nota, su sıçraması, şimşek, kalp. |
| Rekor kutlaması | Yeni rekor anı, Macera'da 3 yıldız, Arcade etkinliği kazanılınca | Bugünkü iki yandan konfeti yerine seçilen gösteri oynar: havai fişek, Pingi dansı, konfeti yağmuru, yıldız yağmuru. |
| Profil çerçevesi | Skorlar (dünya ve arkadaş), Arkadaşlar listesi, çevrimiçi oda | Satırdaki adın çevresinde çerçeve: taç, alev, gökkuşağı, buz, yıldız. |

- Her türün bir ücretsiz varsayılanı vardır ve bugünkü görünümle aynıdır. Hiçbir şey almayan oyuncu farkı görmez.
- Efektler topun kendi rengiyle değil, eşyanın renkleriyle çizilir. Hepsi kısa ve yarı saydamdır; top ve raket her zaman en net şeylerdir.
- "Hareketli arka plan" ayarı efektleri etkilemez. Mevcut titreşim ve ses ayarları geçerlidir.

### Mevcut türlere yeni eşyalar

- 6 Pingi yüzü: Tavşan, Ayıcık, Penguen, Ahtapot, Ejder, Tilki.
- 4 raket: Karpuz, Lav, Buz, Bambu.
- 3 tema: Orman, Uzay, Şeker.
- Çizimler mevcut üretim betikleriyle (`ArtSource/tools/generate_skins.py`, `generate_fx.py`) ve bugünkü stil kurallarıyla üretilir. Her Pingi yüzünün 4 ifadesi vardır (normal, mutlu, üzgün, şaşkın).

### Fiyat basamakları

| Basamak | Altın | İlk partide |
|---|---|---|
| Yaygın | 300 | 12 |
| Nadir | 700 | 12 |
| Destansı | 1.500 | 7 |
| Efsane | 3.000 | 3 |

İlk parti: 13 yeni Pingi, raket ve tema + 6 iz + 6 vuruş + 4 kutlama + 5 çerçeve = 34 eşya, toplam 31.500 altın. Bunlara ek olarak 5 set ödülü (satılmaz) ve 3 sezon eşyası vardır.

Kesin eşya listesi ve fiyatlar `docs/design/cosmetics.md` dosyasına eklenir.

### Günün vitrini

- Koleksiyon'un en üstünde, Pass kartının üstünde durur: 3 eşya ve yenilenmeye kalan süre.
- Eşyalar altın fiyatıyla satılır. "Sadece vitrinde" eşyalar başka yoldan satın alınamaz, ama sette ve kutuda çıkabilir.
- Günde bir kez "Reklam izle, %30 indirim" seçeneği vardır.

### Setler

- Aynı temadaki 4 eşya bir settir; ilk partide 5 set var (örnek: Uzay, Tatlı, Okyanus, Kış, Orman).
- Setin ilerlemesi Efekt sekmesindeki "Setler" filtresinde görünür. Eksik parçaya dokununca o eşyanın satın alma penceresi açılır.
- Seti tamamlayan oyuncu sete özel bir ödül alır: bir çerçeve ya da kutlama. Bu ödül başka yoldan alınamaz.

### Sürpriz kutu

- Efekt sekmesinde durur; 500 altın.
- Olasılıklar kutunun altında yazar: Yaygın %70, Nadir %25, Destansı %5. Seçilen basamakta sende olmayan eşya kalmamışsa olasılıklar kalan basamaklara bölüştürülür ve ekranda güncel hali gösterilir.
- Kutu sadece Yaygın, Nadir ve Destansı eşyalardan çıkar. Efsane, sezon ve set ödülleri kutudan çıkmaz.

### Sezon eşyaları

- Sezon açıkken Koleksiyon'da sezon kartı görünür: ad, kalan gün ve 3 eşya (Kardan Pingi, Kar Tanesi raket, Kar izi).
- Sezon eşyaları "Sezon" rozeti taşır. Sezon bitince kart kaybolur; alınan eşyalar kalır, alınmayanlar bir sonraki yıla kadar satılmaz.

## Kısıtlar

- Unity 6000.3, Built-in render hattı, arayüz UI Toolkit, hedef Android (dikey).
- Arayüz renkleri yalnızca `Tokens.uss` / `Themes.uss` belirteçlerinden gelir (`ThemeColorTests`).
- Kodda yorum satırı yok; ayarlanabilir değerler `SerializeField` olarak durur.
- Kayıt dosyası (`cosmetics.json`) eski sürümlerle uyumlu kalır; yeni alanlar boşsa varsayılan kullanılır. Bulut yedeği yeni alanları da birleştirir.
- Parçacık sayıları dünya katmanındaki mevcut havuzla sınırlıdır; yeni efektler havuzu büyütmez.
- Yeni metinler 8 dilde eklenir ve yazı tipi alt kümeleri yenilenir.

## Bugünkü parçalar ve nereye oturur

- `CosmeticCategory` (Ball, Paddle, Theme) yeni türlerle genişler: Trail, Hit, Celebration, Frame. `UnlockKind`'a altın, vitrin, set, sezon ve kutu yolları eklenir.
- `BallFeel` bugün topun `TrailRenderer`'ını ve parıltısını yönetiyor; iz eşyası buradan uygulanır.
- `GameFeel.Impact` vuruş parçacıklarını çiziyor; vuruş eşyası buradaki şekli, renkleri ve sayıyı belirler.
- `RunMoments`, `AdventureRun` ve `ArcadeEvents` kutlamaları `GameFeel.Celebrate` ile yapıyor; kutlama eşyası buradan seçilir.
- `OnlineScores` sıralama kaydına ad bilgisini ek bilgi olarak yazıyor; çerçeve de oraya eklenir. Arkadaş durum bilgisi `UgsFriendsBackend` üzerinden gidiyor.
- `ShopScreen` sekmeleri ve kartları kuruyor; vitrin, sezon kartı, setler ve kutu buraya eklenir.

## Test ve doğrulama

- EditMode: vitrin seçimi (aynı gün aynı sonuç, sahip olunan gelmez), kutu (olasılıklar, tekrar çıkmama, boş kutu), setin tamamlanması, sezon tarih aralığı, Pass kuralları, kayıt uyumluluğu ve bulut birleştirme.
- PlayMode: Efekt sekmesi ve filtreler, altınla alma ve kuşanma, vitrin reklam indirimi, kutu açma, sıralama satırında çerçeve.
- Görüntü kontrolü: 4 temada ve iki ekran oranında Koleksiyon; maçta her iz ve vuruş efekti; rekor kutlamaları.
- Tüm mevcut testler yeşil kalmalı.

## Uygulayana bırakılanlar

- Efektlerin tam renkleri, parçacık sayıları ve süreleri (taslak ölçü verir).
- Eşya adlarının 8 dildeki karşılıkları.
- Vitrindeki "sadece vitrinde" eşyaların hangileri olduğu.
