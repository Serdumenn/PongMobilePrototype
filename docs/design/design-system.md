# Design System — Soft Pop

*v1 · 2026-09-30 · Kapı 1'de seçilen yön: B · Soft Pop*

Tüm değerler **referans pikseldir** (1080×1920 tuval). Style frame'lerdeki ölçüler ×3.6 ile çevrilip 8 px grid'e yuvarlanmıştır. Uygulamada tek kaynak `Assets/_Game/UI/Theme/Tokens.uss` dosyasıdır; bu doküman onun açıklamasıdır.

## Renkler

| Token | Değer | Kullanım |
|---|---|---|
| `--color-bg` | `#FFF1E0` | Zemin (kamera arka planı) |
| `--color-blob-peach` | `#FFE2C4` | Dekoratif leke, sağ üst |
| `--color-blob-pink` | `#FFDCE4` | Dekoratif leke, sol alt |
| `--color-surface` | `#FFFFFF` | Kart, ikon butonu, çip |
| `--color-surface-edge` | `#F0D9C0` | Beyaz yüzeylerin alt kenarı (basılabilirlik) |
| `--color-ink` | `#2B2D42` | Ana metin ve ikon |
| `--color-ink-muted` | `#6B6D82` | İkincil metin |
| `--color-pingi` | `#FF6B57` | Pingi (top), dekoratif mercan |
| `--color-coral` | `#E04E3A` | Metin taşıyan mercan: birincil buton, skor rakamı |
| `--color-coral-edge` | `#B83A28` | Birincil butonun alt kenarı |
| `--color-teal` | `#2EC4B6` | Raket, vurgu ikonları |
| `--color-teal-edge` | `#1E9C90` | Raketin alt kenarı |
| `--color-sun` | `#FFC93C` | Play butonu, "New best" rozeti |
| `--color-sun-edge` | `#E5A800` | Sarı yüzeylerin alt kenarı |
| `--color-scrim` | `rgba(43,45,66,.38)` | Pause ve Game Over arka perdesi |

### Style frame'den erişilebilirlik sapmaları

| Öğe | Mockup | Sistem | Kontrast (önce → sonra) |
|---|---|---|---|
| İkincil metin | `#8D8FA3` | `#6B6D82` | 2.87 → 4.57 (krem zeminde) |
| Birincil buton zemini (beyaz yazı) | `#FF6B57` | `#E04E3A` | 2.80 → 3.95 (büyük yazı eşiği 3:1) |
| Game Over skor rakamı | `#FF6B57` | `#E04E3A` | 2.80 → 3.95 |

Pingi'nin kendi rengi `#FF6B57` olarak kalır; metin değil grafik olduğu için kontrast şartı yoktur. Menüdeki "Pingi Pongi" başlığı logotype sayılır (WCAG logotype istisnası), bu yüzden style frame renkleri korunur.

## Tipografi

Tek aile: **Fredoka** (SIL OFL). Ağırlıklar: 500 Medium, 600 SemiBold, 700 Bold.

| Token | Boyut | Ağırlık | Kullanım |
|---|---|---|---|
| `--font-size-display` | 200 | 700 | Menü başlığı "Pingi Pongi" |
| `--font-size-score` | 264 | 700 | Oyun içi skor |
| `--font-size-number` | 224 | 700 | Game Over skoru |
| `--font-size-title` | 96 | 700 | Kart başlıkları ("So close!", "Paused") |
| `--font-size-button` | 72 | 700 | Buton etiketleri |
| `--font-size-body` | 56 | 600 | Çip, satır etiketleri |
| `--font-size-caption` | 48 | 500 | İpuçları, açıklamalar |

## Boşluk ve şekil

- Grid: 8 px. Ölçek: `8 · 16 · 24 · 32 · 48 · 56 · 64 · 96`
- Ekran kenar boşluğu: 56 (safe area'nın içinde)
- Köşe yarıçapı: ikon butonu 56 · birincil buton 64 · kart 104 · çip ve hap: tam yuvarlak

## Bileşenler

**Basılabilirlik:** Tüm butonlar alt kenarında yüzey renginden koyu, kalın bir şerit taşır (`border-bottom`). Basınca şerit incelir ve buton aynı miktarda aşağı iner. Bu, style frame'deki "3D" hissini gölge efekti olmadan verir; UI Toolkit'te gölge (box-shadow) yok.

| Bileşen | Boyut | Zemin / kenar | Etiket |
|---|---|---|---|
| `IconButton` | 176×176 | surface / surface-edge 16 | ikon 88, ink |
| `PlayButton` | 416 daire (+28 kenar) | sun / sun-edge 28 | ikon 176, ink |
| `PrimaryButton` | yükseklik 208 | coral / coral-edge 20 | 72 bold, beyaz |
| `SecondaryButton` | 208×208 | surface / surface-edge 20 + 6 kenar çizgisi | ikon 88 |
| `Chip` | yükseklik 128 | surface / surface-edge 12 | 56 semibold |
| `Card` | ekran − 2×96 | surface / surface-edge 28 | — |
| `Toggle` | 192×112 | kapalı: `#E6D6C4` · açık: teal | beyaz düğme |

Durumlar: **normal · basılı** (kenar 16 → 4, 12 px aşağı) · **pasif** (%40 opaklık, dokunma yok).

## Hareket

| Token | Süre | Eğri | Kullanım |
|---|---|---|---|
| `--dur-press` | 90 ms | ease-out | Buton basma |
| `--dur-screen` | 220 ms | ease-out-cubic | Ekran geçişleri (fade ve 48 px kayma) |
| `--dur-pop` | 320 ms | ease-out-back | Skor artışı, rozetler, kart girişi |

Kural: UI animasyonları `Time.timeScale`'dan etkilenmez (pause ve game over sırasında da çalışır).

## Pingi'nin ifadeleri

| Durum | Görsel | Ne zaman |
|---|---|---|
| `idle` | Açık gözler, hafif gülümseme | Varsayılan |
| `happy` | Kısık "^ ^" gözler, geniş gülüş | Raket vuruşunda 150 ms |
| `sad` | Düşük kaşlar, ters ağız | Game Over |

## Ses paleti (Faz 4) — uygulandı

Tüm efektler bize ait. `ArtSource/tools/generate_sfx.py` ile sentezleniyor (mono, 44.1 kHz, 16 bit, tepe -3 dBFS). Lisans gerektirmez; betik yeniden çalıştırılınca aynı dosyalar üretilir.

| Efekt | Karakter | Süre | Oyun içi ses seviyesi | Ne zaman |
|---|---|---|---|---|
| `sfx_paddle_hit` | Marimba "tok", C5 | 0.20 s | 0.80 | Raket vuruşu. Seri boyunca her vuruşta +0.5 yarım ton, en fazla 1 oktav; top yeniden servis edilince sıfırlanır |
| `sfx_wall_bounce` | Yumuşak, alçak "tup" | 0.12 s | 0.45 | Duvar ve tavan |
| `sfx_perfect` | Parlak "ding", E6 + B6 | 0.50 s | 0.55 | Rush'ta Perfect vuruş (raket sesinin üstüne) |
| `sfx_ui_tap` | Yukarı kayan baloncuk | 0.08 s | 0.50 | Buton, kart, sekme |
| `sfx_ui_back` | Aşağı kayan baloncuk | 0.09 s | 0.50 | Geri, ana menü, "Later" |
| `sfx_new_best` | Marimba arpej C–E–G–C | 0.97 s | 0.80 | Rekorla biten oyun |
| `sfx_game_over` | İnen iki nota, E4 → C4 | 0.77 s | 0.70 | Rekorsuz oyun sonu (cezalandırıcı değil) |
| `sfx_unlock` | Yükselen arpej ve pırıltı | 0.71 s | 0.80 | Ödül penceresi ve Dolap'ta kilit açılması |
| `sfx_countdown_tick` | Kısa marimba G5 | 0.18 s | 0.60 | Maç öncesi 3-2-1 |
| `sfx_countdown_go` | C6 + E6 ikilisi ve pırıltı | 0.40 s | 0.75 | Geri sayımın sonu |
| `sfx_point` | E5 → A5 iki nota | 0.48 s | 0.70 | Yerel maçta sayı ya da can kaybı |
| `sfx_match_win` | C-E-G-C arpeji, uzun E6 ve pırıltı | 1.24 s | 0.85 | Düello ve partide kazanan; ortak rallide yeni rekor |

- **Yönlendirme:** `AudioManager` (Services) 6 kanalı döngüyle kullanır ve hepsini `MainMixer` → `SFX` grubuna gönderir. `Music` grubu ileride müzik için hazır.
- **Kamera:** Kamerada `AudioListener` olmalı.
- **Ayarlar:** "Sound" anahtarı (`GameSettings.SoundEnabled`) kapatılınca tüm sesler anında susar.

## Oyuncu renkleri (çok oyunculu, M1)

| Oyuncu | Taraf | Renk | Kenar | Metin |
|---|---|---|---|---|
| P1 | Alt | `#E04E3A` | `#B83A28` | `#E04E3A` |
| P2 | Üst | `#2EC4B6` | `#1E9C90` | `#1E8C81` |
| P3 | Sol | `#FFC93C` | `#E5A800` | `#9A6E00` |
| P4 | Sağ | `#8C6CF2` | `#6A4FD0` | `#6A4FD0` |

- Token'lar `UI/Theme/Match.uss` içindedir (`--color-player-N`).
- Raketler `Art/Sprites/Players/spr_paddle_player_*.svg` ile oyuncu renginde çizilir.
- Oyuncu kimliği renk ve konumla birlikte verilir; yalnız renge dayanılmaz.
- Üst oyuncunun yazıları 180°, yan oyuncularınkiler ±90° döndürülür, böylece her oyuncu kendi yazısını düz okur.

## Online bileşenleri (M2, 2026-10-04)

Ölçüler 1080 genişlikli panel birimindedir. Stiller `UI/Theme/Online.uss` içindedir.

| Bileşen | Boyut | Zemin / kenar | İçerik |
|---|---|---|---|
| `MeCard` ("You play as") | yükseklik 208, köşe 64 | surface / surface-edge 16 | top 144, açıklama 38 medium, ad 60 bold, askı butonu 128×136 |
| Mod listesi satırı | yükseklik 172, köşe 40 | seçiliyken `#E6F7F5` | seçim halkası 56 (seçili: teal 18 kenar), ad 50 bold, açıklama 36, oyuncu çipi ya da "Soon" çipi |
| `CodeField` | yükseklik 224, köşe 56 | `#FFF8EF`, kenar 6 divider (odakta teal) | 128 bold, harf aralığı 18, ortalı |
| `CodeCard` | köşe 72 | surface / surface-edge 20 | kod 150 bold, harf aralığı 22; Kopyala ve Paylaş; "Expires in m:ss" |
| Lobi yuvası | yükseklik 232, köşe 64 | surface / surface-edge 14; bekleyen yuva yarı saydam, 6 kenar | top 160, ad 58 bold, açıklama 40, hazır işareti 96 |
| Oyuncu kartı (VS) | yükseklik 500, köşe 64 | üst kenar 18 oyuncu renginde | top 230 (hazırsa mutlu yüz), ad 50 bold, gecikme 38 |
| Hazır butonu | yükseklik 192 | teal / teal-edge 18; hazırken surface | "Ready" ya da "Not ready", 64 bold |
| Yeniden bağlanma halkası | 240 daire, 16 teal kenar | — | kalan saniye 104 bold |

- **Yeni ikonlar:** `bolt`, `share`, `copy`, `join`, `wifi`, `wifi_off`. Mevcut setin kuralları geçerli: 24 px ızgara, beyaz çizgi, 2.5 kalınlık, yuvarlak uç; renk USS'ten verilir.
- **Onaylı tasarımdan iki bilinçli sapma:**
  - "Join code" butonunda kopyala ikonu yerine "içeri gir" oku var, çünkü kopyala ikonu Kopyala butonuyla karışıyordu.
  - Kod yazısında IBM Plex Mono yerine Fredoka Bold ve geniş harf aralığı kullanılıyor; yeni font dosyası gerekmiyor. Yüksek sesle okunabilirlik korunuyor.
- **Hazır olma:** Lobide tasarımdaki tepki çubuğunun yerinde "Ready" butonu duruyor; tepkiler maç ekranında.

**Maç ekranı (M3):**

| Bileşen | Boyut | Zemin / kenar | İçerik |
|---|---|---|---|
| Rakip kartı | yükseklik 176, köşe 64 | surface / surface-edge 12 | top 120, ad 50 bold, skor 88 bold ya da ortak canlar, bağlantı ikonu 56 (180 ms üstü mercan) |
| Menü butonu | 160×168, köşe 48 | surface / surface-edge 12 | duraklat ikonu 72; "Leave the match?" penceresini açar |
| Büyük skor | 360 bold | ink %12 | kendi skorun; ortak rallide pas sayısı |
| Tepki butonu ve çubuğu | 152×160 yuvarlak; çubukta 4 × 136×144 | surface; çubuk %85 beyaz | başparmak, şaşkın, gülen, ateş. Gönderilen baloncuk sağ alttan yukarı, gelen baloncuk rakip kartının altından aşağı 1,5 sn'de süzülür |
| Portal | teal çizgi 0,12 birim; halka 3,2 birim | `#2EC4B6` %55 | halka çıkışta 1 → 1,35 büyüyüp söner, girişte 0,35 → 1,05 belirir |
| Sonuç kartı | Game Over kartıyla aynı | — | "You win!" / "So close!" / "Great teamwork!", skor 200 bold, rövanş notu, ana sayfa + Rematch (rakip istediyse "Accept") |

**Rush Battle (M4a):**

| Bileşen | Boyut | Zemin / kenar | İçerik |
|---|---|---|---|
| Rakip listesi | 470 genişlik, sağda, yüksekliğin %36'sı | surface / surface-edge 12, köşe 48 | satır 92: top 60, ad 40 bold, skor 50 bold; lider skoru mercan; biten %55, ayrılan %35 opak |
| Saldırı bandı | yan boşluk 56, yüksekliğin %56'sı | sun / sun-edge 14, köşe 64 | saldırı ikonu 64, "Fog incoming! Hit a Perfect to block" 44 bold |
| Sis | ekranın %30–72'si | krem, ortada %94, kenarlarda %60 | 350 ms'de belirip kaybolur |
| Durum notu | ink çip, %66 | ink | 1,8 sn görünür |
| Sıralama kartı | Game Over kartı | — | satır 120: yer rozeti 72 (1. altın), top 76, ad 46 bold, skor 58 bold; senin satırın `#E6F7F5` |

**Meydan okumalar ve Dünya sıralaması (M4b):**

| Bileşen | Boyut | Zemin / kenar | İçerik |
|---|---|---|---|
| Meydan okuma kartı | iç boşluk 44, köşe 64, alt boşluk 44 | surface / surface-edge 16 | ikon 64, başlık 58 bold, açıklama 40 medium (ink-muted) |
| Tarih rozeti | köşe 40 | `#FFF0C2` | "Oct 5", 40 bold, sarı oyuncu metin rengi |
| Günlük istatistik şeridi | köşe 40, üç eşit sütun | `#FFF4E6` | etiket 34 medium ("Your best", "Rank", "Resets in"), değer 60 bold; değer yoksa "–" |
| "Play today's run" | birincil buton, tam genişlik | mercan | oynat ikonu 80 |
| Yakında kartı | meydan okuma kartı | aynı | başlık, ikon ve metin %50 opak, "Soon" çipi |
| Dünya tablosu | yan boşluk 56, köşe 64 | surface / surface-edge 16 | başlık "Top 10" ya da "Today · Oct 5"; satırlar Skorlar ekranındaki sıralama satırı (1. altın, senin satırın vurgulu ve "(you)"); Co-op'ta "Ad & Eş". İlk 10'da değilsen "•••" ve kendi satırın. Boş, çevrimdışı, hata ve yükleniyor notu 40 medium; not boşsa gizlenir |
| Tablo çipleri | mod çipi | — | Classic, Rush, Daily, Co-op |

**Canlı Düello (M5):**

| Bileşen | Kural |
|---|---|
| Saha | 9:16 sabit; telefona sığacak en büyük boy, alt kenar ekranın altında, üstte rakip kartı payı (Portal ile aynı, 1,9 birim). Kenar çizgileri ve kesikli orta çizgi; tablette ortada |
| Raketler | Ben altta (kendi kostümüm), rakip üstte; renkler ayırt edilir (oyuncu paletinden mercan, benimki mercansa teal). Hareket yönüne en fazla 22° eğim |
| Top | Ölçek sahaya göre; son vuranın kostümü |
| HUD ve sonuç | Online maç bileşenleri (M3) aynen |

**Hayalet Meydan Okuma (M4c):**

| Bileşen | Boyut | Zemin / kenar | İçerik |
|---|---|---|---|
| Kod alanı (buton) | yükseklik 160, köşe 48, kenar 6 | `#FFF8EF` / divider; basılınca teal | "Enter code" 52 semibold ink-muted, sağda teal "join" ikonu 64 |
| Paylaş bağlantısı | — | zeminsiz | teal-edge paylaş ikonu 56 + "Share my last Rush run" 46 bold; koşu yoksa gri "Play a Rush run to share it" |
| Kod penceresi | online-card | surface | hayalet ikonu 104, "Your ghost code", kod 150 bold ink, 7 gün notu, Copy / Share çifti, "Done" |
| Hayalet kartı (yarış) | 470 genişlik, sağda %36 | surface / surface-edge 12, köşe 48 | hayaletin topu 76 (%55), ad 40 bold, hayalet ikonu + "Ghost" 32, skor 56 bold (hayalet öndeyse mercan) |
| Fark çipi | köşe 40 | önde teal-edge, geride mercan, eşitken ink | "+2 ahead" / "2 behind" / "Tied", 44 bold beyaz |
| Dünyadaki hayalet | top ve raket kostümü | top %45, raket %32 opak | gerçek top ve raketin bir katman altında |
| Sonuç kartı | Game Over kartı | — | başlık, "Ghost Challenge · Ad", skorlar 128 bold (sen mercan, hayalet ink-muted) "vs", skor-zaman çizgisi 200 yükseklik `#FFF4E6` zeminde (sen 9 px düz mercan, hayalet `#B8A99A` kesikli), "Send my run back" birincil, ev + "Try again" |

- **Günlük oyun sonu:** Game Over kartı kullanılır. Başlık "New best today!" ya da tek kişilik başlıklar, alt satır "Daily Challenge · Oct 5", en alt satır "Today's best N", sıra gelince "· Rank #N" eklenir.
- **Yeni ikonlar:** `calendar`, `ghost`.

## Diller (6b, 2026-10-05)

| Bileşen | Boyut | Zemin / kenar | Etiket |
|---|---|---|---|
| Dil satırı (Ayarlar) | diğer satırlarla aynı, buton | zeminsiz | `language` ikonu + "Language"; sağda seçili dilin kendi adı teal-edge 56 semibold ve sağ ok (`chevron-right`) 56 ink-muted |
| Dil penceresi | kart, üst boşluk 72 | surface | "Language" başlığı; 8 satır, her biri 136 yükseklik, köşe 40, ad 56 semibold ink; seçili satır `#E6F7F5` zemin ve teal-edge onay ikonu; en altta "Cancel" bağlantısı |

- **Dil adları** her zaman kendi dilinde yazılır ve çevrilmez: English, Türkçe, 日本語, 한국어, Deutsch, Español, Português, Français.
- **Fontlar:** Latin harfleri Fredoka'dan gelir. Japonca harfler M PLUS Rounded 1c'den (ağırlıklar Fredoka'ya karşılık gelir), Korece harfler Jua'dan gelir. Üçü de yuvarlak hatlıdır.
- **Uzun metinler:**
  - Açıklamalar ve uyarılar kaydırılır (birden çok satır).
  - Başlıklar, buton etiketleri, sekmeler ve çipler tek satırdır. Sığmazlarsa font en fazla %40 küçülür (`TextFit`). Sekmeler birlikte küçülür.
  - Oyuncu adları küçülmez, sonu "…" ile kesilir.
- **Noktalama:** Japoncada tam genişlik `！？：` kullanılır. Fransızcada `! ? :` öncesinde bölünmez boşluk vardır.

## Arkadaşlar (M6, 2026-10-05)

| Bileşen | Boyut | Zemin / kenar | Etiket |
|---|---|---|---|
| Arkadaş kartı (Online sekmesi) | yükseklik 176, köşe 64 | surface / surface-edge 16 | teal-edge `people` ikonu 80; "Friends" 54 bold; alt satır 38 medium ink-muted ("2 online · 5 in total", "1 friend request"); istek varsa mercan rozet 64; sağ ok 56 |
| Arkadaşlar ekranı | tam ekran | bg | Üstte kod kartı: "Your friend code", kod 84 bold, Copy / Share çifti. Altında "Add a friend by code" alanı (Ghost Challenge'daki kod alanıyla aynı), sonra kartlar: Friend requests, Your friends, Sent requests |
| Arkadaş satırı | en az 136 yükseklik, aralarda 4 px çizgi | — | durum noktası 28 (çevrimiçi teal, oyunda sun, çevrimdışı track-off), ad 50 bold (uzunsa "…"), durum 36 medium ink-muted; çevrimiçiyse teal "Invite" hapı 232×112; "…" düğmesi 112 daire |
| İstek satırı | aynı | — | nokta yok; "Wants to be friends"; teal onay ve gri çarpı daire düğmeleri |
| Arkadaş menüsü | kart pencere | surface | ad, durum; "Remove friend" ve kırmızı "Block" (176 yükseklik, çerçeveli), "Cancel" |
| Ekleme penceresi | kart pencere | surface | "Add a friend", örnek kod açıklaması, kod alanı 176 yükseklik 64 px yazı, teal "Paste", "Send request" birincil, "Cancel" |
| Davet şeridi | %92 genişlik (en çok 968), köşe 64 | surface, teal 6 çerçeve, teal-edge alt kenar 16 | `people` ikonu 72; "Brave Otter invites you" 44 bold; mod adı 36; mercan "Join" 220×128; gri çarpı. Bildirimle aynı yerden kayarak iner, 20 sn sonra kapanır |
| Sonuç ekranları | — | — | Rakibin altında teal "Add friend" bağlantısı; arkadaşsa gri onaylı "You're friends". Rush sonucunda her rakibin satırında 96'lık ekle düğmesi |
| Lobi | — | — | Kod kartında "Invite a friend" bağlantısı |
| Skorlar | — | — | Üçüncü sekme "Friends"; başlık "You and your friends"; arkadaş yoksa "Find friends" bağlantısı. Dünya başlığı "Top 50" |

- **Yeni ikonlar:** `person_add`, `close`, `more`.

## Ayarlar: hesap ve gizlilik (yayın, 2026-10-06)

- **Google Play Games satırı:** gamepad ikonu. İki satırlı etiket: "Google Play Games" (56 semibold) ve durum (38 medium ink-muted): "Connected · Ad", "Not connected", "Available on Android". Bağlı değilse sağda "Sign in" hapı. Play Games kurulmamışsa satır gizlenir.
- **Privacy choices satırı:** kilit ikonu ve sağ ok. Yalnızca reklam onayının değiştirilebildiği bölgelerde görünür.
- **Alt bilgi:**
  - teal "Privacy policy" bağlantısı;
  - dokununca kopyalanan "Player ID: …" (34 px);
  - sürüm etiketi.

## Erişilebilirlik

- Metin kontrastı ≥ 4.5:1, büyük metin (≥ 72 px bold) ≥ 3:1
- Dokunma hedefi ≥ 176 px (≈ 64dp)
- Titreşim ve ses ayrı ayrı kapatılabilir (Ayarlar)
