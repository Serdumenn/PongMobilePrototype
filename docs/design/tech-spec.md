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
- Japonca ve Korece yedek fontlar (6b): her Fredoka ağırlığının yedek listesinde önce M PLUS Rounded 1c (medium → Medium, semibold → Bold, bold → ExtraBold), sonra Jua vardır. Kaynak TTF'ler `ArtSource/fonts/` içinde, kırpılmış kopyaları `Assets/_Game/Fonts/fnt_mplus_rounded_*_ja.ttf` ve `fnt_jua_ko.ttf`. Font asset'leri dinamiktir ve build'de dinamik veri temizlenir. Yeni metin eklenince sıra: `loc_strings.py` → çeviri → `subset_fonts.py` → Editor'de **Pingi → Rebuild Language Fonts**. Lisanslar (SIL OFL) fontların yanında.

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

- Online kodu (M2) ayrı assembly'ye taşınmadı; `Pingi.Runtime` içinde `Scripts/Online/` klasöründe durur.
- `Pingi.Runtime`, UGS paketlerine referans verir: `Unity.Services.Core`, `Unity.Services.Authentication`, `Unity.Services.Multiplayer`, `Unity.Netcode.Runtime`.

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

**Online katmanı** (M2, 2026-10-04):

| Sınıf | Görev |
|---|---|
| `OnlineService` | UGS başlatma ve anonim giriş; durumlar: Offline, Connecting, Ready, Failed. Giriş, Online sekmesi ilk açıldığında yapılır. Editor'de her Multiplayer Play Mode oyuncusu `Application.dataPath`'ten türeyen ayrı bir profille girer. `DeleteDataAsync` UGS hesabını siler |
| `OnlineLobby` | Multiplayer Services oturumu: kodla kurma ve katılma, hızlı eşleşme (mod ve protokol sürümü `String1`/`String2` indeksleriyle süzülür), hazır durumu, gecikme, 10 sn yeniden bağlanma, host ayrılması. Hataları `Failure` türlerine çevirir. Online mod listesi bu bileşendedir |
| `PlayerNames` | Oyuncu kimliğinden türetilen "Adjective Animal" adı (32 × 32). Aynı hesap hep aynı adı alır |
| `SessionCode`, `LobbyPlayer` | Kod biçimi; lobi oyuncusu ve "herkes hazır" kuralı |
| `NativeShare` | Android paylaşım menüsü (`ACTION_SEND`); Editor'de panoya kopyalar |
| `HubScreen` (Online sekmesi), `LobbyScreen` | Arayüz. Ayarlar'da "Online data → Delete" |

**Online maç katmanı** (M3, 2026-10-04):

| Sınıf | Görev |
|---|---|
| `MatchMessage` | Maç mesajları: Start, Handoff, Miss, Score, Reaction, Rematch. Sürüm baytıyla başlayan küçük ikili biçim (`ToBytes` / `TryParse`); bozuk ya da bilinmeyen veri reddedilir |
| `IMatchLink`, `NetcodeLink` | Taşıma katmanı. `NetcodeLink`, NGO adlı mesajlarını (`pingi.match`, ReliableSequenced) kullanır. Prefab ve NetworkObject yok. Testler sahte bir bağlantı kullanır |
| `OnlineMatchRules` | Host'taki kurallar: Portal Duel (5 sayı, kaybeden servis atar), online Co-op Rally (3 ortak can, skor = pas), terk etme |
| `OnlineMatchController` | Bir telefonun sahası: geri sayım, servis, portaldan çıkış ve giriş, kaçırma, skor, sonuç, rövanş, tepkiler, rakip kopunca 10 sn bekleme. `GameManager` nesnesinde |
| `PortalView` | Portal halkası animasyonu (`GameWorld/portal_ring`) |
| `OnlineHudScreen`, `OnlineResultScreen` | Maç ekranı ve sonuç kartı |

- **Akış:** `GameUI.PumpOnline` her karede bakar. Lobi dolu ve relay bağlıysa maçı `Waiting` durumunda açar. Host, herkes hazır olunca `HostStart` ile `Start` mesajını gönderir; iki taraf aynı geri sayımla başlar.
- **Topun geçişi:** Top üst kaleye girince `Ball.ExitPoint` ve `LastVelocity` ile `Handoff` gönderilir. Alıcı, x'i ve yönü aynalayarak topu `Ball.Enter` ile portalın 0,6 birim altından aynı hızla bırakır.
- **Ses:** `AudioManager`, online maçın geri sayımını ve raket vuruşlarını (rallide yükselen perde) dinler; portal ve tepki sesleri doğrudan çalınır.
- **Rush Battle (M4a):** `OnlineRushController` (GameManager) ve `RushBattleRules`. Koşunun kendisi `SoloGameManager.StartBattleRun` ile tek kişilik Rush'tır (`BattleRun`, `SoloScoreManager.Unranked`). Saldırı etkileri: `Paddle.SetLengthScale`, `Ball.SetSpeedBoost`; sis yalnız arayüzde. Host, misafir mesajlarını (`RushScore`, `RushFinal`, `Attack`, `Shield`, tepki, rövanş) diğer misafirlere iletir. Ekranlar: `RushBattleScreen` (tek kişilik HUD'un üstünde), `RushResultScreen`; tepki çubuğu `ReactionView` ile paylaşılır.
- **Günlük ve sıralamalar (M4b):** `DailyChallenge` (UTC gün anahtarı, tohum, kalan süre, cihazdaki günün en iyisi) ve `OnlineScores` (Services nesnesinde). Günlük koşu `SoloGameManager.StartDailyRun` ile sıralamaya sayılmayan Rush'tır (`DailyRun`); servis yönü ayrı `Ball.ServeRng` ile tohumlanır, böylece sekme rastgeleliği servisleri bozmaz. Sıralama ayarları `Assets/_Game/Services/Leaderboards/*.lb` dosyalarındadır ve Editor'deki Deployment penceresinden yüklenir; seçili ortam `ProjectSettings/Packages/com.unity.services.core/Settings.json` içinde (production). Gönderim adı ve Co-op eşi skorun metadata'sında gider. Classic/Rush rekoru yalnızca hesabı olan oyuncu için gönderilir; günlük koşu ve Dünya sekmesi gerekirse girişi kendisi yapar. Testlerde `OnlineService.Disabled` ağ çağrılarını kapatır.
- **Canlı Düello (M5):** `Core/Live/Fix.cs`, `LiveSim.cs`, `LiveSession.cs` saf C# (Unity fiziği yok, kayan nokta yok). `LiveDuel` (GameManager) girdi, çizim ve sesleri yönetir; `OnlineMatchController` canlı modda ona devreder. `IMatchLink.SendLive` / `LiveReceived` hızlı kanal (`pingi.live`, `NetworkDelivery.UnreliableSequenced`). Paket: tür, maç tohumu, tik, onay, ilk girdi tiki, girdiler (en fazla 32 × 2 bayt), "öndeyim" farkı, son özet tiki ve özeti. Sabitler: 60 Hz, 4 alt adım, girdi gecikmesi 2, en fazla 15 tik geri sarma, 128 tiklik halka, 30 tikte bir özet. Saha düzeni `FieldTopology.Live`. Altın test özeti `LiveSimTests.GoldenChecksum`; simülasyon kuralı değişirse bilerek güncellenmeli.
- **Hayalet (M4c):** `GhostRun` (Core) kayıt biçimi: küçük-endian ikili, sürüm 1; zaman santisaniye (`ushort`), konum sahaya oranlı ×10000 (`short`), raket 0–255 bayt. `GhostRecorder` ve `GhostRace` GameManager'da, `OnlineGhosts` Services'te. Sunucu: Cloud Code JS `ghost_share` ve `ghost_get` (`Assets/_Game/Services/CloudCode/`), Cloud Save özel alanı `ghost-KOD` (yalnız sunucu). Parametreler hem scriptte (`module.exports.params`) hem `.js.meta` içinde tanımlıdır; paket, JS projesi kurulmadan scriptteki tanımı okumaz. İstemci yanıtı `JsonUtility` ile okur (IL2CPP kırpmasına dayanıklı). Yükleme: Editor'de **Pingi → Deploy Ghost Scripts** (`Scripts/Editor/CloudCodeDeploy.cs`); paketin Deploy butonu alan yeniden yüklemesinden sonra script adlarını kaybettiği için çalışmaz.
- **Yerelleştirme (6b):** Paket yok, kendi küçük sistemimiz.
  - **Tablo:** `Assets/_Game/Localization/Resources/loc_strings.txt` (TSV). Sütunlar: `key`, `en`, `note`, `tr`, `ja`, `ko`, `de`, `es`, `pt`, `fr`. Anahtar İngilizce metnin kendisidir. Aynı İngilizce metin iki farklı anlamda geçiyorsa sonuna `#ek` konur (ör. `{0}-day streak#other`). Ek yalnızca anahtarın sonundaki, harfle başlayan `#kelime`dir; metnin içindeki `Rank #{1}` gibi işaretler ek sayılmaz. `note` sütunu çevirmen için bağlam notudur.
  - **`Loc` (Core):** `T(anahtar, değişkenler)`, `Plural(tekil, çoğul, sayı)`, `SetLanguage(kod)`, `Changed` olayı, `Culture` (es-MX, pt-BR…). Tarih ve süre biçimleri de anahtardır: `{0:MMM d}`, `{0} h`, `{0}h {1}m`. Çeviride olmayan anahtar İngilizceye düşer. Seçilen dil PlayerPrefs `Language` anahtarında. Kayıt yoksa cihaz dili kullanılır, desteklenmiyorsa İngilizce.
  - **Sahte dil `qps`:** Metinleri yaklaşık %40 uzatır ve aksanlı harflerle doldurur. Ayarlar'da görünmez; taşma denetimi için `Loc.SetLanguage("qps", false)` ile açılır.
  - **UXML metinleri:** `UiLocalizer`, `GameUI` açılırken tablodaki metinleri yakalar ve dil değişince yeniden yazar. Koddan yazılan metinler her zaman `Loc.T` ile alınır. Mod adları ve kostüm adları `GameModeDefinition.Title`/`Blurb` ve `CosmeticItem.Title` üzerinden çevrilir. Oyuncu adları İngilizce kalır.
  - **Sığdırma:** `TextFit` (UI) tek satırlık metin kutusuna ya da satırdaki ikonla birlikte sığmazsa font boyutunu en fazla %40 küçültür. Kaydırmalı (wrap) metinlere ve `text-overflow: ellipsis` olan adlara dokunmaz. `fit-group` sınıfı olan kapsayıcıdaki metinler (sekmeler) aynı boyutu kullanır; `fit-skip` sınıfı bir dalı dışarıda bırakır. Metin değişince önce normal boyuta döner, sonra yeniden sığdırılır.
  - **Araçlar (`ArtSource/tools/`):** `loc_strings.py` UXML'deki, koddaki `Loc.T`/`Loc.Plural` çağrılarındaki ve mod/kostüm dosyalarındaki metinleri tabloya ekler; `--check` eksik ya da artık anahtar varsa hata verir. Çevrilmeyecek metinler (adlar, örnek değerler) `Localization/loc_ignore.txt` içinde. `loc_merge.py` çeviri gruplarını tabloya işler; Japoncada tam genişlik `！？：`, Fransızcada noktalama öncesi bölünmez boşluk uygular. `subset_fonts.py` Japonca ve Korece fontları tablodaki harflere kırpar.
  - **Testler:** `LocTests` (ekrandaki her metin tabloda, her çeviride aynı değişkenler, çoğul kuralları, İngilizceye düşme, sahte dil) ve `TextFitPlayTests`. EditMode ve PlayMode testleri İngilizceye sabitlenir.
- **Editor'de relay testi:** `Temp/claude/RelayBot.cs` aynı süreçte ikinci bir UGS örneğiyle katılır. Kendi `INetworkHandler`'ı ikinci bir NetworkManager açıp `NetworkConfiguration.RelayServerData` ile istemci olarak bağlanır; mesajlara bir bot gibi cevap verir.

- **NetworkManager:** Sahnede durmaz. İlk online isteğinde kodla oluşturulur (`UnityTransport`, sahne yönetimi kapalı), böylece tek oyunculu oyun ve testler etkilenmez. Sahne yönetimi kapalı olmalı; açık kalırsa bağlanan istemci host'un sahnesini yeniden yükler.
- **Online modlar:** `Data/Modes/mode_portal_duel`, `mode_coop_online`, `mode_rush_battle`, `mode_live_duel`. Bunlar `GameModeDefinition` dosyalarıdır ve `Online` alanı işaretlidir; `ComingSoon` işaretli olanlar listede "Soon" görünür.
- **Oda kodları:** UGS üretir. 6 karakterdir, ama her harf geçerli değildir (ör. `Z`). Bu yüzden istemci yalnız biçimi denetler (6 harf ya da rakam), geçerliliğe sunucu karar verir.
- **Canlı test:** İkinci oyuncu Editor'de ayrı bir UGS örneğiyle (`UnityServices.CreateServices`) ve ağı başlatmayan bir `INetworkHandler` ile taklit edilir. Relay istemci bağlantısı yalnız iki ayrı süreçte (Multiplayer Play Mode ya da iki cihaz) denenebilir.

Yeniden adlandırılan betiklerin `.meta` GUID'leri korundu; sahne bağlantıları `FormerlySerializedAs` ile taşındı.

**Testleri çalıştırma:** Unity'de *Window → General → Test Runner*. CI'da her push'ta önce testler (`game-ci/unity-test-runner@v4.3.2`), sonra APK build'i çalışır; test düşerse build başlamaz. v4.4.0 yeni "game-ci CLI" sarmalayıcısına geçtiği için ilk koşuda Unity'yi hiç başlatmadan düştü (2026-10-02); bu yüzden aynı düzeltmeleri eski, kararlı mimaride taşıyan v4.3.2 kullanılıyor. Unity'nin test günlükleri CI loguna ve `test-results` artifact'ına yazılır. CI'da Unity odaksız (batchmode) çalışır ve Input System odak yokken eklenen dokunmatik ekranı devre dışı bırakır. Bu yüzden PlayMode testleri kendi süreleri boyunca `backgroundBehavior = IgnoreFocus` kullanır, sonra eski değere döner. Batchmode koşusu yerelde de tekrarlanabilir: `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode` (Editor kapalıyken). PlayMode testleri kullanıcı verisine dokunmaz: kayıt dosyaları geçici klasöre gider, PlayerPrefs test öncesi diske yedeklenir ve sonra geri yüklenir. Yarıda kesilen bir koşudan kalan yedek bir sonraki testte otomatik geri yüklenir.

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
