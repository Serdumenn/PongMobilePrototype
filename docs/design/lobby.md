# Özellik: Ana menü arka planı ve kaydırarak mod geçişi

*v1 · 2026-11-06 · Durum: **Uygulandı, Editor'de doğrulandı***

Görsel öneri: [lobby/index.html](lobby/index.html) (1A Mod dünyaları, 2A Komşu kartlar, 1C Canlı lekeler).

"Lobi" bu belgede oyunu açınca gelen ana menü demektir (`MenuScreen`: mod kartı ve Oyna düğmesi). Çevrimiçi oda ekranı (`LobbyScreen`) kapsam dışıdır.

## Neden

Ürün sahibinin tespiti: butonlar, kartlar, ikonlar, küçük ve tatlı stil ve temalar yeterli ve güzel. Zayıf olan, arayüzün arkasındaki tasarım: her ekranda aynı düzen var (düz zemin ve iki büyük leke), tema yalnızca renkleri değiştiriyor, mod değişince arka planda hiçbir şey olmuyor. Ana menüde modlar arası geçiş de sıradan: oklarla kart değişiyor.

İstenen: arka planın değişken ve canlı olması; ana menüde mod geçişinin farklı ve etkileyici olması.

## Hedefler

- Ana menüde her modun kendi canlı arka plan dünyası olsun; mod değişince arka plan o dünyaya geçsin.
- Mod, parmakla kaydırılarak değiştirilsin; yandaki modların kartları kenardan görünsün.
- Diğer menü ekranlarının arka planı da tek tip olmaktan çıksın (canlı lekeler).
- Oyuncu hareketi kapatabilsin.

## Hedef olmayanlar

- Butonların, kartların, ikonların, yazı tiplerinin ve tema renklerinin değişmesi.
- Oyun alanının (topun oynadığı sahanın) yeniden tasarımı.
- Yeni tema eklemek.
- Çevrimiçi oda ekranı, maç ekranları, Macera haritası.

## Kaçınılacaklar

Bunlar olursa iş "çalışsa bile" başarısız sayılır:

- Arka plan yazıları, kartları ya da topu okunmaz hale getirirse.
- Hareket göz yorar, mide bulandırır ya da dikkati oyundan çalarsa.
- Bir tema (özellikle Gece) dünyalarla bozuk, çiğ ya da kontrastsız görünürse.
- Kaydırma kazara tetiklenirse ya da kaydırmayı bilmeyen oyuncu başka modları hiç keşfedemezse.
- Düşük donanımlı telefonda menü takılırsa.

## Ürün sahibinin kararları

- Önerinin tamamı onaylandı: 1A Mod dünyaları, 2A Komşu kartlar ve diğer menü ekranları için 1C Canlı lekeler.
- **Mod değiştirme okları kalkıyor.** Mod yalnızca parmakla kaydırarak değişir.
- Ayarlar'a "Hareketli arka plan" anahtarı eklenir (önerinin parçası olarak onaylandı).

## Davranış

### 1. Mod dünyaları (ana menü)

Her mod kartının arkasında kendi sahnesi var. Sahne, temanın renkleriyle (arka plan ve iki leke rengi) boyanır; modun vurgu rengi yalnızca hafifçe karışır. Tüm öğeler yavaş ve soluk kalır, yazılar ve kartlar her zaman okunur.

| Mod | Dünya | Hareket |
|---|---|---|
| Arcade | Ortada sıcak bir ışık, yükselen altınlar, parıltılar | Altınlar aşağıdan yukarı salınarak yükselir, parıltılar yanıp söner |
| Macera | Üç katman tepe, bulutlar | Tepeler farklı hızlarda kayar, bulutlar soldan sağa geçer |
| Klasik | Yukarıdan görülen masa çizgileri ve file | Soluk bir hayalet top masada gidip gelir, arkasında silik iz |
| Rush | Çapraz hız şeritleri, kısa hız çizgileri | Şeritler hızlı kayar, ortadan saat nabzı halkaları yayılır |
| Birlikte | İki Pingi'nin karşılıklı oynadığı silik bir masa | Toplar iki taraf arasında yavaşça gidip gelir |

- Mod değişince eski dünya yaklaşık 0,6-0,7 sn'de solarak yenisine geçer.
- Kilitli bir modun kartı bugünkü gibi kilitli görünür; dünyası yine gösterilir (merak uyandırır).
- Tema değişince dünyalar anında yeni renklere geçer.

### 2. Kaydırarak mod geçişi (ana menü)

- Sol ve sağ oklar (`mode-prev`, `mode-next`) kaldırılır.
- Ortadaki kart tam boyda; bir önceki ve bir sonraki modun kartı yaklaşık %80 boyda, yarı saydam olarak ekran kenarından görünür. Kenarda kalan kısım, orada başka bir kart olduğunu anlatacak kadar olmalı.
- **Sürükleme:** Parmak kartı yatayda tutup sürükleyebilir; kartlar parmağı birebir takip eder. Bırakınca:
  - Kart aralığının yaklaşık dörtte birinden fazla sürüklendiyse ya da hızlı bir fiske yapıldıysa bir sonraki/önceki moda geçer.
  - Değilse yerine geri döner.
  - Oturma hareketi yaylıdır (hafif taşıp geri gelir, yaklaşık 0,5 sn).
- **Dokunma:** Kenardan görünen karta dokunmak o moda geçirir. Ortadaki karta dokunmak bir şey yapmaz (Oyna düğmesi ayrı).
- Liste döngüseldir: son moddan sonra ilki gelir (bugünkü gibi; Birlikte kartı da döngüde).
- Mod değişince hafif titreşim (ayar açıksa) ve mevcut arayüz tık sesi.
- Alttaki noktalar kalır ve seçili modu gösterir.
- Seçilen mod bugünkü gibi hatırlanır.
- Kaydırma yalnızca kart şeridi bölgesinde algılanır; üst çubuk ve Oyna düğmesi etkilenmez. Yatay harekete göre karar verilir, küçük titremeler kaydırma sayılmaz.

**Kaydırmayı öğretmek:** Oklar gittiği için oyuncu ilk birkaç açılışta (ya da ilk kez kaydırana kadar) kartın altında küçük bir "kaydır" eli animasyonu görür (yazısız; oyundaki mevcut sürükleme el ikonu `ui_icon_drag` kullanılır). İlk başarılı kaydırmadan sonra bir daha çıkmaz.

### 3. Canlı lekeler (diğer menü ekranları)

Dolap, Skorlar, Ayarlar ve Birlikte merkezi (bugün düz renkli tam ekranlar). Arkadaşlar ekranı başka bir ekranın üstünde açıldığı için düz zeminli kalır. Ana menünün üstünde açılan kartlar (Görevler, Pingi Yolu, ödül penceresi) bunun dışında; onların arkasında menünün dünyası görünmeye devam eder.

- Bugünkü lekeler kalır ama yumuşakça şekil değiştirir, yavaşça döner; aralardan küçük baloncuklar yükselir.
- Her ekranın lekelerinin yeri ve sayısı farklıdır, böylece ekranlar birbirinin aynısı görünmez.
- Renkler temanın leke renkleridir.

### 4. Oyun sırasında

Koşu başlayınca o modun dünyası sakinleşir (Günlük, Hayalet ve Rush Battle koşularında koşunun ait olduğu modun dünyası kullanılır): hareket yavaşlar ve öğeler belirgin şekilde solar; top ve raket her zaman en net şeylerdir. Menüye dönünce dünya eski canlılığına döner. Mevcut "ısınan top" arka plan tepkisi (`BackdropFeel` nabzı) korunur.

### 5. Ayar: Hareketli arka plan

- Ayarlar'da yeni bir anahtar, varsayılanı **açık**.
- Kapalıyken her yerde bugünkü sade görünüm (iki leke, hafif kayma) kullanılır; dünyalar, canlı lekeler ve baloncuklar çıkmaz. Kaydırarak mod geçişi yine çalışır.
- Metin 8 dilde eklenir.

## Kısıtlar

- Unity 6000.3, Built-in render hattı, arayüz UI Toolkit, hedef Android (dikey).
- Tema renkleri `CosmeticItem` tema varlıklarından gelir (`BackgroundColor`, `BlobPrimary`, `BlobSecondary`) ve `SkinApplier` ile uygulanır; yeni sahneler de bu renkleri kullanmalı, sabit renk gömmemeli.
- Arayüz renkleri yalnızca `Tokens.uss` / `Themes.uss` belirteçlerinden gelir (`ThemeColorTests` bunu denetler).
- Yeni çizimler projedeki üretim betikleriyle (`ArtSource/tools/generate_fx.py`) SVG olarak üretilir ve mevcut içe aktarma ayarlarına uyar; elle rastgele görsel eklenmez.
- Kodda yorum satırı yok; değerler `SerializeField` olarak ayarlanabilir.
- Düşük donanımlı telefonlarda menü akıcı kalmalı; ekrandaki hareketli öğe sayısı dünya başına küçük tutulmalı (taslaktaki sayılar yeterli).

## Bugünkü parçalar ve nereye oturur

- Arka plan lekeleri dünya katmanında (`BackdropFeel` + `ViewportAnchor`, sprite). Mod dünyaları ve canlı lekeler aynı katmanda sprite olarak çalışır.
- Ana menü kartı `MenuScreen` içinde tek bir kart; içerik `SetMode` / `SetTogether` ile değişiyor. Bugünkü kaydırma, kart üzerinde basit bir eşikle (`SwipeThreshold` 90) çalışıyor; yerine sürüklenebilir kart şeridi gelir.
- Mod sırası `SoloGameManager` mod listesi ve Birlikte kartı (`GameUI.StepMode`, `RefreshMenuMode`).
- Dolap, Skorlar, Ayarlar, Arkadaşlar ve Birlikte merkezi bugün düz renkli tam ekranlar (`screen--solid`) olduğu için arkadaki dünya katmanı görünmüyor. Canlı lekelerin bu ekranlarda görünmesi için bu ekranların zemini dünyayı gösterecek hale gelmeli; açıkken top ve raket bugünkü gibi gizli kalır.
- Ayarlar `GameSettings` içinde PlayerPrefs ile tutuluyor; yeni anahtar da orada.

## Test ve doğrulama

- Sürükleyerek, fiskeyle ve kenar karta dokunarak mod değişimi; geri dönen kısa sürükleme; döngüde son moddan ilke geçiş (oyun içi testler, mevcut dokunma yardımcılarıyla).
- Okların kalkmasıyla bunlara dayanan mevcut testler (`LocalMatchTests` Birlikte kartına okla gidiyor) kaydırmaya göre güncellenir.
- Mod değişince doğru dünyanın açıldığı; ayar kapalıyken dünyaların çıkmadığı; ipucu elinin ilk kaydırmadan sonra gizlendiği.
- 4 temada (Soft Pop, Gün Batımı, Nane, Gece) ve iki ekran oranında (uzun telefon, 16:9) görüntü kontrolü; tema renk testleri yeşil kalmalı.

## Uygulayana bırakılanlar

- Sahne öğelerinin tam şekilleri, sayıları, hızları ve saydamlıkları (taslak ölçü verir).
- Sürükleme eşiği, fiske hızı ve yay ayarının ince değerleri.
- Kenar kartın görünen genişliği (yaklaşık 50-60 px / 300 px genişlikte taslaktaki gibi).
- Oyun sırasında solma oranı.

## Sonraya kalanlar

- Oyna'ya basınca "portal" geçişi (taslakta 2B): şimdilik dahil değil.
