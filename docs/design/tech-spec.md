# Teknik Şartname

*v1 · 2026-09-30 — Faz 0'dan itibaren tüm yeni asset'ler için geçerli*

## Ekran ve ölçek

| Konu | Standart |
|---|---|
| Referans çözünürlük | 1080×1920, dikey |
| UI ölçekleme | **Expand**: 1080×1920 referans alan her cihazda tamamen sığar (telefonda genişliğe, tablette yüksekliğe göre) |
| Test oranları | 13 cihazlık Device Simulator matrisi (aşağıda) |
| Güvenli alan | Tüm etkileşimli öğeler Safe Area içinde (`.safe` kapsayıcıları, `GameUI.ApplySafeArea`) |
| İçerik sütunu | Geniş ekranlarda (tablet, katlanabilir) UI içeriği ortada en fazla **1080** referans px genişlikte kalır; arka plan tam ekran |
| Dokunma hedefi | En az 48dp ≈ 130 referans px; hedefler arası en az 8dp |
| Boşluk ölçeği | 8 px grid: 8 / 16 / 24 / 32 / 48 / 64 / 96 |
| Dünya ölçeği | Kamera ortho size 5 → 10 birim = 1920 px → **192 PPU** (tüm oyun sprite'ları) |

### Cihaz uyumluluğu

| Konu | Karar |
|---|---|
| Yön | Yalnızca dikey. `appCategory = game` olduğu için Android 16 büyük ekranlarda da dikey kilit korunur |
| En-boy | Android `Custom`, en fazla **3.0**. Bugünkü en uzun telefon ekranı 2.77 (katlanabilir dış ekran); siyah bant yok |
| Tam ekran | Açık: durum ve gezinme çubukları gizli. Çentik alanına arka plan çizilir (`Render outside safe area`) |
| Oyun alanı | Tavan ve yan duvarlar **Safe Area** sınırında (`ResponsiveWalls.Field`). Top çentiğin altına girmez; raket bu alanla sınırlı. Zemin (kaçırma çizgisi) ekranın altında |
| Sabit saha | Saha genişliği en fazla **9:16** (10 birim yükseklikte 5.625 birim). Telefonlarda saha ekranı doldurur. Tablet ve katlanabilirlerde saha ortada kalır; iki yanında beyaz kenar çizgisi (`railLeft`, `railRight`) görünür. Böylece zorluk her cihazda aynı olur ve dünya sıralaması adil kalır. UI içerik sütunu da aynı genişlikte hizalanır |
| Çözünürlük değişimi | Katlanabilir açma/kapama, bölünmüş ekran ya da çözünürlük ayarı değişince güvenli alan, içerik sütunu ve saha kendiliğinden yeniden hesaplanır. Güvenli alan hesabı ekran oranıyla yapılır; panel ölçeğinin güncellenme zamanından etkilenmez |

**Test matrisi** (Device Simulator, her ekran: menü, skorlar, dolap, ayarlar, oyun sonu, ödül, HUD, duraklatma):
- Katlanabilir dış ekran 960×2658
- Z Flip3
- Huawei P40 Pro (hap çentik)
- Pixel 5 (delik kamera)
- Note20 Ultra
- iPhone 12 Pro Max (çentik + ana ekran çubuğu)
- Galaxy S10e
- Pixel 3 (18:9)
- Galaxy S5 Mini (720p)
- Moto E (540×960)
- Nvidia Shield (16:10 tablet)
- iPad Pro 11
- iPad (4:3)

Otomatik denetim kontrolleri: ekran dışına taşan öğe, taşan ya da kesilen yazı, Safe Area dışındaki buton, üst üste binen buton, menü butonunun top ya da raketle çakışması, tavanın Safe Area tepesinde olması.

## Görseller

| Tür | Kaynak | Unity'ye giren | Import |
|---|---|---|---|
| UI ikonları | SVG, 24 birimlik grid, tek çizgi kalınlığı | SVG (UI Toolkit Vector Image) | Preset: `SVG_UI_Icon` |
| UI bitmap (gerekirse) | SVG/Figma | PNG, ekrandaki boyutun 1×'i | Preset: `Texture_UI` — mipmap kapalı, ASTC 4×4 |
| Oyun sprite'ları | SVG, @2x çizim (viewBox = piksel boyutu) | SVG → Textured Sprite, **384 PPU** (= 192 × 2) | Preset: `SVG_Sprite`; PNG gerekirse `Texture_Sprite` |
| Uygulama ikonu | SVG (`ArtSource/app_icon/`, `generate_app_icon.py`) | Adaptive: ön ve arka katman 432×432 (ön katman 66dp güvenli dairede). Round ve legacy 512×512 (72dp görünür alan). Mağaza 512×512 | Sıkıştırma yok, mipmap yok, NPOT ölçekleme yok |

Yasak: ekranda 150 px görünen bir öğe için 2000 px kaynak; `scale` ile küçültülmüş UI; placeholder görselin canlı ekranda kalması.

**SVG sprite kuralı:** Textured Sprite'larda dünya boyutunu doku boyutu belirler (`TextureSize / PPU`). Bu yüzden `TextureSize`, SVG'nin viewBox genişliğine eşit olmalıdır: top kostümleri 200, raketler 430. Preset bu alanı bilerek dışarıda bırakır.

**SVG preset'lerinin uygulanması:** SVG preset'leri Preset Manager'a **kaydedilmez**. Unity 6.3, SVG importer türünü proje açılışında çözemiyor; kayıt her açılışta bozuluyor ve yeni SVG'ler yanlış türde (GameObject) import ediliyor. Klasör → preset eşlemesi `Scripts/Editor/SvgImportPresets.cs` içinde tanımlı ve yalnızca ilk importta uygulanıyor:
- `Art/UI/Icons/**` → `SVG_UI_Icon`
- `Art/Sprites/**` → `SVG_Sprite`

Texture ve ses preset'leri Preset Manager'da kalır; orada sorunsuz çalışıyor.

## Tipografi
- En fazla 2 aile: 1 başlık + 1 metin
- Lisans: SIL OFL veya eşdeğeri (ticari kullanım ve yeniden dağıtım serbest)
- Font atlasında Türkçe karakterler: `ÇĞİÖŞÜçğıöşü`

## Ses

| Tür | Kaynak | Unity import |
|---|---|---|
| Efekt (SFX) | WAV, mono, 44.1 kHz, kısa kuyruk | ADPCM, Decompress On Load — Preset: `Audio_SFX` |
| Müzik | WAV/OGG, stereo | Vorbis, Streaming — Preset: `Audio_Music` |
| Seviye | Efektler ve müzik birbirine göre normalize | AudioMixer grupları: `Music`, `SFX` |

## Adlandırma
Küçük harf, alt çizgi, kategori öneki. Boşluk, Türkçe karakter, sürüm ya da tarih yok.

| Önek | Kullanım | Örnek |
|---|---|---|
| `ui_icon_` | UI ikonu | `ui_icon_play.svg` |
| `ui_` | Diğer UI görselleri | `ui_panel_card.png` |
| `spr_` | Oyun sprite'ı | `spr_ball.png` |
| `sfx_` | Ses efekti | `sfx_paddle_hit.wav` |
| `mus_` | Müzik | `mus_menu_loop.ogg` |
| `fnt_` | Font | `fnt_display.ttf` |
| `app_icon_` | Uygulama ikonu katmanları | `app_icon_fg.png` |

C# script'leri, sahneler ve prefab'lar PascalCase kalır (Unity sınıf adı eşleşmesi gerektirir): `SoloBall.cs`, `Game.unity`, `GameOverScreen.prefab`.

## Klasör yapısı

```
Assets/
  _Game/
    Art/Sprites/        oyun sprite'ları
    Art/UI/Icons/       SVG ikonlar
    Art/UI/_Legacy/     eski uGUI görselleri — UI Toolkit geçişinde silinecek
    Audio/SFX/          seçilmiş efektler
    Audio/SFX/_Library/ aday efektler — ses paleti seçiminden sonra temizlenecek
    Audio/Music/
    Audio/Mixers/
    Fonts/
    Prefabs/
    Scenes/
    Data/               ScriptableObject verileri (kostümler, modlar)
    Scripts/Core | Gameplay | UI | Services | Cosmetics   Pingi.Runtime.asmdef
    Scripts/Editor/     yalnızca Editor'de çalışan araçlar (import kuralları) · Pingi.Editor.asmdef
    Scripts/Tests/EditMode | PlayMode   otomatik testler · Pingi.Tests.*.asmdef
    Settings/Presets/
    UI/                 UXML, USS, PanelSettings, tema
  ThirdParty/           yolu sabit olmayan 3. parti içerik (örn. TextMesh Pro)
  GoogleMobileAds/, ExternalDependencyManager/, Plugins/Android/
                        SDK'lar sabit yol beklediği için yerinde kalır
ArtSource/              Unity dışı kaynak dosyalar (SVG, Figma export)
ArtSource/_licensed/    yeniden dağıtılamayan paketler — gitignore'da
docs/design/            tasarım dokümanları
```

Taşımalar ve yeniden adlandırmalar **Unity içinden** yapılır; böylece GUID'ler ve referanslar korunur.

## Kod yapısı ve testler

**Assembly'ler** (2026-10-01):

| Assembly | Klasör | Not |
|---|---|---|
| `Pingi.Runtime` | `Scripts/` | Oyun kodu. Input System, Unity IAP ve UGUI'ye referans verir; GMA DLL'leri otomatik bağlanır |
| `Pingi.Editor` | `Scripts/Editor/` | Yalnız Editor |
| `Pingi.Tests.EditMode` | `Scripts/Tests/EditMode/` | Kurallar, kayıtlar, envanter, veri bütünlüğü, rastgelelik |
| `Pingi.Tests.PlayMode` | `Scripts/Tests/PlayMode/` | `Game` sahnesinde uçtan uca akış, gerçek dokunuşla ralli |
| `Pingi.Multiplayer` | `Scripts/Multiplayer/` | M2'de, online paketlerle birlikte gelecek |

**Maç çekirdeği** (M0 refaktörü; tek oyunculu davranış testlerle aynı tutuldu):

| Sınıf | Görev |
|---|---|
| `FieldSide` | Taraf (Bottom, Top, Left, Right), içe dönük yön, servis yönü dönüşümü |
| `Participant` | Oyuncu: taraf, raket, vuruş serisi, en uzun seri, Perfect serisi |
| `MatchRules` → `ClassicRules`, `RushRules` | Puanlama, kaçırma sonucu, süre. Eski `ModeRunner` |
| `MatchRandom` | Tohumlu, platformdan bağımsız rastgelelik (mulberry32). Servis yönü ve sekme sapması bundan gelir; `SoloGameManager.SetSeedForNextRun` ile sabitlenebilir |
| `Ball` | Fizik ve servis. Oyun yöneticisini tanımaz; `PaddleStruck(raket, sapma)` ve `GoalEntered(kale)` olaylarını yayınlar. Eski `SoloBall` |
| `Paddle` + `IPaddleController` | Taraf ve eksen bilgisi olan raket; girdi kontrolcüden gelir. Eski `RacketController` |
| `TouchZoneController` | Tarafın kenarındaki ekran şeridine düşen dokunuşu raket hedefine çevirir |
| `Goal` | Kale; tarafını bilir. `bottom` etiketinin yerini aldı |
| `FieldLayout` | Güvenli alan içindeki saha, duvarlar ve raylar. Eski `ResponsiveWalls` |
| `SaveLocation` | Kayıt dosyalarının kökü; testler geçici klasöre yönlendirir |
| `LocalMatchController` | Yerel maç akışı (M1). Katılımcılar, raket klonları, toplar, geri sayım, servis, sonuç ve kayıt. Tek oyunculu `SoloGameManager` maç sırasında menü durumunda bekler ve dünyayı paylaşırlar |
| `TableDuelRules`, `CoopRallyRules`, `PartyRules` | Yerel modların kuralları; skor ve can `Participant` üzerinde |
| `FieldTopology` (Solo, TopBottom, FourSides) | `FieldLayout.SetTopology` kaleleri açar ya da kapatır ve sahayı yeniden kurar. Parti sahası güvenli alanın kısa kenarına sığan karedir |

Yeniden adlandırılan betiklerin `.meta` GUID'leri korundu; sahne bağlantıları `FormerlySerializedAs` ile taşındı.

**Testleri çalıştırma:** Unity'de *Window → General → Test Runner*. CI'da her push'ta önce testler (`game-ci/unity-test-runner@v4.3.2`), sonra APK build'i çalışır; test düşerse build başlamaz. v4.4.0 yeni "game-ci CLI" sarmalayıcısına geçtiği için ilk koşuda Unity'yi hiç başlatmadan düştü (2026-10-02); bu yüzden aynı düzeltmeleri eski, kararlı mimaride taşıyan v4.3.2 kullanılıyor. Unity'nin test günlükleri CI loguna ve `test-results` artifact'ına yazılır. PlayMode testleri kullanıcı verisine dokunmaz: kayıt dosyaları geçici klasöre gider, PlayerPrefs test öncesi diske yedeklenir ve sonra geri yüklenir. Yarıda kesilen bir koşudan kalan yedek bir sonraki testte otomatik geri yüklenir.

## Versiyon kontrol
- Git LFS: png, jpg, psd, tga, wav, mp3, ogg, ttf, otf, fbx, mp4
- SVG, UXML, USS, `.unity`, `.prefab`, `.asset` metin olarak kalır (LFS'e girmez)
- Yeniden dağıtılamayan lisanslı içerik repoya girmez

## Asset "Tamamlandı" (Definition of Done)
- [ ] Adlandırma ve klasör kuralına uygun
- [ ] Doğru preset uygulanmış; boyut ekrandaki kullanıma uygun
- [ ] Kaynak dosyası `ArtSource/` altında
- [ ] Lisansı kayıtlı (3. parti ise)
- [ ] Sahnede/ekranda kullanılıyor; kullanılmayan kopya yok
- [ ] 3 test oranında ekran görüntüsüyle doğrulandı
