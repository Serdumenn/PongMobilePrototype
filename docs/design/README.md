# Pingi Pongi — Tasarım Süreci

Bu klasör oyunun görsel ve UX kararlarının tek kaynağıdır. Her faz bir **onay kapısı** ile biter; kapıdan geçmeyen iş bir sonraki faza taşınmaz.

| Doküman | İçerik |
|---|---|
| [brief.md](brief.md) | Creative Brief: kime, neden, hangi hisle |
| [style-frames/index.html](style-frames/index.html) | Görsel yön seçenekleri (Kapı 1) |
| design-system.md | Renk, tipografi, boşluk, bileşen, animasyon ve ses kuralları (Faz 2) |
| ux-flow.md | Ekran envanteri, akış, wireframe'ler (Faz 3) |
| [tech-spec.md](tech-spec.md) | Teknik şartname: çözünürlük, formatlar, adlandırma, klasörler |
| [cosmetics.md](cosmetics.md) | Kostümler, temalar, kilitler ve satın alma |
| [modes.md](modes.md) | Oyun modları (Classic, Rush), skor tablosu, günlük seri |

## Roller

| Rol | Kim | Sorumluluk |
|---|---|---|
| Creative Director / Product Owner | Fatih | Vizyon, kapı onayları, cihazda his testi |
| Art Direction, UX, Technical Art, UI Dev, QA | Claude | Üretim, uygulama, doğrulama |

## Fazlar

| Faz | Çıktı | Kapı | Durum |
|---|---|---|---|
| 0 · Altyapı | Klasör yapısı, adlandırma, import preset'leri, Git LFS, placeholder temizliği | — | ✅ Tamam |
| 1 · Art Direction | Creative Brief + 3 style frame | Kapı 1: stil seçimi | ✅ B · Soft Pop seçildi |
| 2 · Design System | Token'lar, tipografi, bileşenler, animasyon, ses paleti | Kapı 2 | ✅ Kapı 3 ile birleştirildi |
| 3 · UX | Ekran envanteri, akış, 3 oranda doğrulama | Kapı 3 | ✅ Ekranlar uygulandı |
| 4 · Asset üretimi | İkon, sprite, font, ses, uygulama ikonu ✅ | Her asset DoD'den geçer | Kısmen |
| 5 · Uygulama | UI Toolkit ekranları, tema, safe area, animasyon, AudioMixer ✅ | Kapı 4: cihaz testi | Cihaz testi bekliyor |
| 6 · Polish & QA | Cihaz matrisi, erişilebilirlik, performans ve boyut bütçesi | Kapı 5 | — |
| 7 · Mağaza | İkon, feature graphic, ekran görüntüleri, metinler, gizlilik politikası | Kapı 6: yayın | — |

## Karar kaydı

| Tarih | Karar | Gerekçe |
|---|---|---|
| 2026-09-30 | Unity 6.3 LTS (6000.3.25f1) | 6.0 LTS desteği Ekim 2026'da bitiyor; 6.3 Aralık 2027'ye kadar destekli |
| 2026-09-30 | UI sistemi: **UI Toolkit** (uGUI bırakılıyor) | USS değişkenleri doğrudan design token; 6.3'te yerleşik SVG; metin tabanlı, okunabilir diff |
| 2026-09-30 | Prinbles paketleri projeden çıkarıldı | 3 farklı görsel dil; lisans yeniden dağıtımı yasaklıyor ve repo public |
| 2026-09-30 | Git LFS yalnızca bundan sonraki binary'ler için | Geçmiş yeniden yazılmaz, force push gerekmez |
| 2026-09-30 | Tasarım dokümanları `docs/design/` altında | Kod ile birlikte versiyonlanır |
| 2026-09-30 | Hedef kitle 13+ genel | Çocuk hedeflemesi Google Play Families ve reklam kısıtları getirir |
| 2026-09-30 | Görsel yön: **B · Soft Pop** | Adla uyum, Pingi maskotu marka taşıyıcısı |
| 2026-09-30 | Fredoka'ya Ğ ğ İ Ş ş eklendi | Font Türkçeyi tam desteklemiyordu; harfler fontun kendi aksanlarından ve GPOS anchor'larından birleştirildi (OFL izin veriyor, ayrılmış font adı yok) |
| 2026-09-30 | UI ölçekleme: **Expand** | Genişliğe göre ölçekleme 4:3 tablette yerleşimi sıkıştırıyordu; Expand ile 1080×1920 alan her cihazda tam sığar |
| 2026-09-30 | Sıralama ve Beğen butonları kaldırıldı | Karar verilene kadar çalışmayan buton gösterilmez (brief · açık sorular) |
| 2026-09-30 | Top ve raket görselleri collider ölçüsünde | Görsel ile fizik birebir; oyun zorluğu değişmedi |
| 2026-09-30 | **Kapsam değişikliği:** kozmetikler ve uygulama içi satın alma v1'e alındı | Koleksiyon hedefi + gelir; ayrıntı [cosmetics.md](cosmetics.md) |
| 2026-09-30 | Gelir modeli: Pingi Pass + Reklamları kaldır + tek tek satış (15 ürün) | Ürün sahibinin kararı; reklamla açılan her içeriğin parayla da alınabilmesi AdMob'un "isteğe bağlı" ilkesini destekliyor |
| 2026-09-30 | Unity IAP 5.4.3 | Google Play Billing 9 kullanıyor; 2026'dan beri zorunlu olan Billing 8+ şartını karşılıyor |
| 2026-09-30 | v1 modları: **Classic + Rush** | Ürün sahibinin kararı; Party ve Daily yedekte ([modes.md](modes.md)) |
| 2026-09-30 | Skor tablosu: kişisel şimdi, **Google Play Games** hesaplar hazır olunca | Ücretsiz, Android'de standart; UGS'nin uzun vadeli fiyatı belirsiz |
| 2026-09-30 | Sıralama butonu menüye geri döndü | Skorlar ekranı artık çalışıyor (Benim sekmesi); Dünya sekmesi "yakında" durumunda |
| 2026-09-30 | 7 günlük oyun serisi → **Astro** kostümü | Geri dönüşü ödüllendirir; Pass'e dahil değil, satın alınamaz |
| 2026-09-30 | Menüde "Best" rozeti ve "Tap to play" kaldırıldı | Rekor mod kartına taşındı; dikey alan mod seçiciye açıldı |
| 2026-09-30 | SVG preset'leri Preset Manager yerine `SvgImportPresets.cs` ile uygulanıyor | Unity 6.3 SVG importer kaydını her açılışta bozuyordu; yeni ikonlar yanlış türde geliyordu |
| 2026-09-30 | Android en-boy: Custom 3.0, tam ekran açık | 2.1 sınırı 20:9 ve daha uzun telefonlarda siyah bant çıkarıyordu |
| 2026-09-30 | **Sabit saha** (en fazla 9:16) + tablette kenar çizgileri | Ürün sahibinin kararı; her cihazda aynı zorluk, adil dünya sıralaması |
| 2026-09-30 | Geniş ekranlarda UI 1080 px içerik sütunu | Tablette kartlar ve butonlar gereksiz genişlemiyor; saha ile hizalı |
| 2026-09-30 | Giriş: **Input System 1.20** (Input Manager kapatıldı) | Unity 6.3'te Input Manager kullanımdan kalkıyor. Raket tüm parmaklar arasından alt yarıdakini izliyor; Android geri tuşu `Keyboard.escapeKey`. IAP sahte mağazası için Editor'e özel `InputSystemUIInputModule` |
| 2026-10-01 | Ses paleti sentezle üretildi (8 efekt) + AudioMixer + Ses anahtarı | Eski aday seslerin lisansı belirsizdi, stereo ve uzun kuyrukluydu. Kendi seslerimiz lisans derdi olmadan tarzımıza uygun |
| 2026-10-01 | Reklam eklentisi androidlib paket adı `com.google.unity.ads.plugin` | Unity 6.3 (AGP 9) aynı isim alanını hata sayıyor; CI build'i bu yüzden düşüyordu ([googleads-mobile-unity #4212](https://github.com/googleads/googleads-mobile-unity/issues/4212)) |
| 2026-10-01 | Uygulama ikonu: **Güneş** varyantı (Pingi + raket, sarı zemin) | Krem varyant açık duvar kâğıtlarında kayboluyordu; sarı zemin her arka planda ve 48 px'de okunuyor, Play butonuyla aynı marka rengi |

## Asset takibi

Durumlar: `Brief → WIP → Review → Approved → Integrated`

| Asset | Tür | Durum | Not |
|---|---|---|---|
| Style frame A/B/C | Mockup | Approved | B seçildi |
| `ui_icon_*` (11 adet) | SVG ikon | Integrated | Rank ikonu Skorlar butonunda kullanılıyor |
| `spr_ball_idle/happy/sad` | SVG sprite | Integrated | Pingi ifadeleri |
| `spr_paddle`, `spr_bg_blob` | SVG sprite | Integrated | — |
| `fnt_fredoka_medium/semibold/bold` | Font + SDF asset | Integrated | Türkçe harfler eklendi, lisans: `Assets/_Game/Fonts/OFL_Fredoka.txt` |
| Pingi kostümleri (9 × 3 ifade) | SVG sprite | Integrated | `generate_skins.py` ile üretiliyor; 9. kostüm Astro (seri ödülü) |
| Raket kostümleri (6) | SVG sprite | Integrated | Şerit ve ahşap desenler clipPath ile |
| Temalar (4) | USS + veri | Integrated | Soft Pop, Mint, Sunset, Night |
| `ui_icon_wardrobe/lock/check` | SVG ikon | Integrated | — |
| `ui_icon_clock/flame/chevron_left/chevron_right` | SVG ikon | Integrated | Rush, seri ve mod seçici |
| `ui_icon_sound` | SVG ikon | Integrated | Ayarlar → Sound |
| Ses paleti (8 efekt) | SFX | Integrated | `generate_sfx.py` ile sentezlendi, lisans gerektirmez. `_Library` adayları kullanılmadı |
| Uygulama ikonu | Adaptive + round + legacy | Integrated | "Güneş" varyantı. `generate_app_icon.py` ile üretiliyor; kaynak SVG'ler `ArtSource/app_icon/`. Mağaza 512 px sürümü yayın hazırlığında |

## Lisans kaydı

| Varlık | Lisans | Kaynak | Not |
|---|---|---|---|
| Fredoka (değiştirilmiş) | SIL OFL 1.1 | github.com/google/fonts | Statik ağırlıklar + 5 Türkçe harf eklendi |
| Prinbles paketleri | Ticari kullanım serbest, yeniden dağıtım yasak | Satın alınmış | Repoda değil: `ArtSource/_licensed/` |
