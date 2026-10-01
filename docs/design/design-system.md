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

## Erişilebilirlik

- Metin kontrastı ≥ 4.5:1, büyük metin (≥ 72 px bold) ≥ 3:1
- Dokunma hedefi ≥ 176 px (≈ 64dp)
- Titreşim ve ses ayrı ayrı kapatılabilir (Ayarlar)
