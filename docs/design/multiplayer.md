# Özellik: Çok oyunculu modlar — entegrasyon planı

*v1 · 2026-10-01 · Durum: **Kapı M0 geçildi — M1 başladı***

Fikir panosu (mod taslakları ve karşılaştırma): [Pingi Pongi Çok Oyunculu](https://claude.ai/artifact/F8swRbu67w4WJ8JchhwABS)

Karar: ürün sahibi yedi modun hepsini istiyor. Bu doküman, mevcut projeyi bozmadan hepsini hangi sırayla, hangi mimariyle ve hangi kontrol noktalarıyla ekleyeceğimizi tanımlar.

| Mod | Nerede | Tür | Faz |
|---|---|---|---|
| Masa Düellosu | Aynı telefon | 2 kişi, rakip | M1 |
| Ortak Ralli | Aynı telefon ve online | 2 kişi, takım | M1 + M3 |
| Parti Masası | Aynı tablet | 3–4 kişi, herkes kendine | M1 |
| Pas Düellosu | Online | 2 kişi, rakip | M3 |
| Rush Kapışması | Online | 2–4 kişi, rakip | M4 |
| Hayalet Meydan Okuma + Günün Meydan Okuması | Sırayla (async) | Arkadaşlar / herkes | M4 |
| Canlı Düello | Online, gerçek zamanlı | 2 kişi, rakip | M5 |

---

## 1. Mevcut durum analizi

### 1.1 Tek oyuncu varsayımının gömülü olduğu yerler

| Dosya | Bugün | Çok oyunculu için sorun |
|---|---|---|
| `Core/SoloGameManager.cs` | Tek top, tek raket, tek `ModeRunner`, tek skor; durum makinesi Menu/Playing/Paused/GameOver | Katılımcı kavramı yok; birden çok raket, top ve skor taşıyamıyor |
| `Gameplay/SoloBall.cs` | Tek `Paddle` referansı; raket vuruşunu "bu raket mi" diye kontrol ediyor; `bottom` etiketli tetik = kaçırma; servis tek raketin dokunuşuyla | Hangi raketin vurduğunu ve hangi kalenin (alt/üst/sol/sağ) kaçırdığını bilmeli |
| `Gameplay/RacketController.cs` | Ekranın alt yarısı = giriş bölgesi; yalnız yatay; sabit `baseY` | Taraf (alt/üst/sol/sağ), eksen ve giriş kaynağı (dokunma bölgesi, ağ, hayalet, simülasyon) parametrik olmalı |
| `Gameplay/ResponsiveWalls.cs` | Tavan = duvar, zemin = kaçırma, sabit 9:16 saha | Topolojiye göre kale ve duvar dizilimi; Parti'de tam ekran saha |
| `Cosmetics/SkinApplier.cs` | Kostümü tek top ve tek rakete uygular | Her katılımcının kendi kostümü |
| `UI/GameUI.cs` + ekranlar | Tek HUD, tek oyun sonu | Merkez (hub), lobi, ters çevrilmiş üst HUD, rakip çubuğu, sonuç ekranı |
| `Core/RecordsService.cs` | Tek oyunculu koşu istatistikleri | Maç istatistikleri (galibiyet, yenilgi, en uzun ortak ralli) ayrı tutulmalı |
| `Cosmetics/CosmeticsService.cs` | Oyun sonunda Classic rekoruna göre kilit açar | Çok oyunculu maçlar skor kilidi açmamalı; seriye sayılmalı |
| `Services/AudioManager.cs` | Tek seri sayacı | Katılımcı başına perde |

### 1.2 Teknik notlar
- **Rastgelelik:** `SoloBall` servis yönünü ve duvar sekmesini Unity'nin genel üretecinden alıyor. Hayalet ve Günün Meydan Okuması için tohumlanabilir (seeded) bir üretece geçmeli.
- **Fizik:** 50 Hz sabit adım, sürekli çarpışma algılama. Physics2D cihazlar arasında bit düzeyinde aynı sonucu garanti etmez. Bu yüzden Canlı Düello için deterministik bir simülasyon gerekir (Bölüm 4.7).
- **Etiketler:** `bottom` etiketi kaleyi belirliyor; yerine `Goal` bileşeni ve taraf bilgisi gelecek.
- **Unity Cloud:** `cloudProjectId` boş; proje bir Unity Cloud projesine bağlı değil. Online fazların ilk koşulu bu (hesap işi, sende).
- **Paket uyumu (6000.3.25f1):**
  - Multiplayer Services 2.3.3
  - Netcode for GameObjects 2.13.3
  - Transport 2.7.4
  - Authentication 3.8.0
  - Cloud Save 3.4.1
  - Cloud Code 2.10.4
  - Leaderboards 2.3.4
  - Multiplayer Play Mode 2.0.2
- **Test altyapısı yok:** Oyun kodu `Assembly-CSharp` içinde. Unity test assembly'leri `Assembly-CSharp`'a referans veremediği için önce oyun kodu bir assembly definition'a taşınmalı.

---

## 2. Hedef mimari

```mermaid
flowchart TB
  subgraph Flow[Oyun akışı]
    GM[GameFlow<br/>Menu · Hub · Lobby · Match · Results]
  end
  subgraph Match[Maç]
    MC[MatchController]
    RULES[MatchRules<br/>Solo · Duel · Coop · Party · Portal · RushBattle · Ghost · LiveDuel]
    P[(Participants<br/>taraf · skor/can · kostüm · kontrolcü)]
    FL[FieldLayout<br/>Duvarlar · Kaleler · Saha boyutu]
  end
  subgraph World[Dünya]
    BALL[Ball ×N]
    PAD[Paddle ×N]
    GOAL[Goal ×N]
  end
  subgraph Control[Kontrolcüler]
    TOUCH[TouchZoneController]
    NET[NetworkController]
    GHOST[ReplayController]
    SIM[DeterministicSim]
  end
  subgraph Online[Online katman]
    AUTH[Kimlik · UGS Auth]
    SESS[Oturum · Multiplayer Services]
    MSG[MatchNet · NGO mesajları]
    CLOUD[Cloud Save · Cloud Code · Leaderboards]
  end
  GM --> MC
  MC --> RULES
  MC --> P
  MC --> FL
  FL --> GOAL
  P --> PAD
  MC --> BALL
  PAD --> TOUCH & NET & GHOST & SIM
  MSG --> NET
  SESS --> MSG
  AUTH --> SESS & CLOUD
```

### 2.1 Çekirdek kavramlar
- **Participant:** Taraf (Bottom/Top/Left/Right), takım, skor ve can, kostüm (top, raket), kontrolcü türü (Yerel dokunma, Ağ, Hayalet, Simülasyon).
- **MatchRules:** Mevcut `ModeRunner`'ın genelleşmiş hâli. Vuruş puanı, kaçırma sonucu, kazanma koşulu, saat ve ek top kuralları. Classic ve Rush da bu yapıya geçer; davranışları bit düzeyinde aynı kalır ve testlerle korunur.
- **FieldLayout:** Topoloji (TekSaha, ÜstAlt, DörtKenar, Portal) → duvar, kale ve saha boyutu. `ResponsiveWalls`'un yerini alır; güvenli alan ve sabit saha kuralları korunur.
- **Ball / Paddle / Goal:** `SoloBall` → `Ball`. Hangi raketin vurduğunu ve hangi kalenin kaçırdığını olayla bildirir. `RacketController` → `Paddle` (taraf ve eksen). `Goal` kaleyi tanımlar.
- **Kontrolcüler:**
  - `TouchZoneController`: ekranın bir bölgesine giren parmağı o oyuncuya bağlar; çoklu dokunuşta parmak kimliğiyle takip eder.
  - `NetworkController`: raketi ağdan gelen pozisyonla oynatır.
  - `ReplayController`: hayalet kaydını oynatır.
  - `DeterministicSim`: yalnız Canlı Düello.
- **GameFlow:** `SoloGameManager`'ın durum makinesi, Hub, Lobby ve Results durumlarıyla genişler. Tek oyunculu akış aynı kalır.

### 2.2 Klasör ve assembly yapısı
```
Assets/_Game/Scripts/
  Core/          GameFlow, Match, Participants, Rules, Records, Seeds
  Gameplay/      Ball, Paddle, Goal, FieldLayout, Replay
  Multiplayer/   Online: Auth, Sessions, MatchNet, Handoff, Rollback  (Pingi.Multiplayer.asmdef)
  UI/            Ekranlar
  Editor/
  Tests/EditMode, Tests/PlayMode
```
Assembly'ler: `Pingi.Runtime`, `Pingi.Multiplayer` (yalnız online paketlere bağımlı), `Pingi.Editor`, `Pingi.Tests.EditMode`, `Pingi.Tests.PlayMode`. Böylece testler mümkün olur ve online kod çevrimdışı koddan ayrı kalır.

### 2.3 Online servis seçimi (öneri: tek çatı UGS)

| İhtiyaç | Öneri | Not |
|---|---|---|
| Oyuncu kimliği | UGS Authentication, anonim | İsteğe bağlı Google Play Games bağlama yayın fazında |
| Eşleşme | Multiplayer Services Sessions: arkadaş kodu + hızlı eşleşme | Oturum özelliklerinde mod ve protokol sürümü; eski sürümler eşleşmez |
| Bağlantı | Relay + Netcode for GameObjects (host-client) | İlk 50 ortalama eşzamanlı kullanıcı ücretsiz |
| Mesajlar | NGO özel mesajları (`MatchNet`) | Topun el değiştirmesi, skor, saldırı, tepki, rövanş |
| Hayalet kayıtları | Cloud Save + Cloud Code | 6 haneli meydan okuma kodu sunucuda üretilir |
| Sıralamalar | UGS Leaderboards (günlük/haftalık/tüm zamanlar) | Play Games planının yerine; tek çatı, sohbetsiz |
| Testler | Multiplayer Play Mode | Tek bilgisayarda en fazla 4 oyuncu (ana Editor + 3 sanal oyuncu) |

Oyuncu adları serbest metin değil, üretilmiş adlar olacak ("Neşeli Penguen" gibi). Sohbet yok; 4 hazır tepki var. Böylece moderasyon gerekmez ve yaş derecelendirmesi sade kalır.

---

## 3. Fazlar

Her fazın sonunda bir onay kapısı var. Kapıdan geçmeyen iş bir sonraki faza taşınmaz. Süreler, tek geliştirici ve yapay zekâ yönetimi varsayımıyla kaba tahmindir.

### M0 — Zemin (2–3 hafta) · çevrimdışı, hesap gerektirmez
1. **Test altyapısı:** Oyun kodunu assembly definition'lara taşı. EditMode ve PlayMode test projeleri; CI'da build'den önce test adımı (`game-ci/unity-test-runner`).
2. **Koruma testleri:** Bugünkü davranış için testler. Classic ve Rush puanlama, seri, envanter kuralları, rekorlar, kostüm uygulama ve oyun akışı. Refaktör bu testler yeşilken yapılır.
3. **Çekirdek refaktör:** Participant, MatchRules, FieldLayout, Ball/Paddle/Goal ve kontrolcüler. Tek oyunculu modlar yeni yapı üzerinde, birebir aynı davranışla çalışır.
4. **Tohumlu rastgelelik:** Maç başına `MatchRandom` (servis yönü, sekme sapması).
5. **Tasarım kapısı:** Hub, lobi, ters HUD, sonuç ekranı ve parti düzeni için tel çerçeve ve stil kareleri (`docs/design/multiplayer/index.html`).
6. **Açık iş:** Dolap bildiriminin konumunu düzelt.

**M0 durumu (2026-10-01):**

| Adım | Durum |
|---|---|
| 1 · Test altyapısı | ✅ Kodda 4 assembly var. Testler: EditMode 74, PlayMode 10. CI'da `test` işi build'den önce çalışıyor; CI'daki ilk koşu senin bir sonraki push'unla doğrulanacak |
| 2 · Koruma testleri | ✅ Kapsam: Classic ve Rush puanlama, süre ve ceza, rekorlar, seri (gece yarısı, yıl dönümü, saat geri), envanter ve Pass kuralları, katalog verisi, kostüm giydirme ve kalıcılık, skor ve seri ödülleri, duraklatma. Ralli testinde raket topu gerçek dokunuşla takip ediyor ve hız artışı ölçülüyor |
| 3 · Çekirdek refaktör | ✅ `Ball`, `Paddle` + `IPaddleController` + `TouchZoneController`, `Goal`, `FieldLayout`, `MatchRules`, `Participant`. Testler refaktörden sonra da yeşil |
| 4 · Tohumlu rastgelelik | ✅ `MatchRandom` (mulberry32). Aynı tohum aynı servisi veriyor (PlayMode testi) |
| 5 · Tasarım kapısı | ✅ [multiplayer/index.html](multiplayer/index.html) **onaylandı (2026-10-01)**, önerilen seçeneklerle |
| 6 · Açık iş | ✅ Dolap bildirimi üste taşındı. "Reklam hazır değil" ve "mağaza hazır değil" yolları Editor'de görsel olarak doğrulandı |

**M1'e bırakılanlar:** Parmağın başladığı bölgeye kilitlenmesi (4.1), `Participant`'ta skor ve can, `FieldLayout`'ta ÜstAlt ve DörtKenar topolojileri. Bunları ilk kullanan modlar M1'de geliyor; şimdiden yazmak kullanılmayan kod olurdu.

**Kapı M0:**
- Tüm testler yeşil, 13 cihaz düzen denetimi temiz.
- Tek oyunculu oyun cihazda öncekiyle aynı hissettiriyor.
- Ekran tasarımları onaylı.

### M1 — Aynı cihaz (3–4 hafta) · çevrimdışı
- **Masa Düellosu:**
  - Topoloji ve giriş: ÜstAlt topoloji; ekranın üst yarısı üstteki, alt yarısı alttaki oyuncunun.
  - Kurallar: 5 sayıya ulaşan kazanır; her sayıdan sonra servisi kaybeden atar.
  - Arayüz: üst HUD 180° dönük.
- **Ortak Ralli (yerel):** Aynı düzen, takım kuralları; 3 ortak can, skor toplam pas.
- **Parti Masası (tablet):**
  - DörtKenar topoloji; sol ve sağ raketler dikey.
  - Can sistemi var; elenen oyuncunun kenarı duvara dönüşür; 15 sayıda ikinci top gelir.
  - Tam ekran saha; 600dp altındaki cihazlarda gizli.
- **Kostümler:** Cihaz sahibinin açtığı kostümler her oyuncuya seçtirilir.
- **Kayıt:** Maç istatistikleri ayrı tutulur; maçlar günlük seriye sayılır, skor kilidi açmaz.
- **Arayüz ve ses:** Menüde "Arkadaşla oyna" → Hub: Bu cihazda / Online / Meydan okuma. Yeni sesler: geri sayım, sayı, galibiyet, beraberlik.

**M1 iş dökümü (2026-10-01):**

| Adım | İçerik |
|---|---|
| M1.1 Kurallar ve veri | Üç mod varlığı (Table Duel, Co-op Rally, Party Table). `Participant`'a skor, can, elenme ve görünüm eklenir. `TableDuelRules`, `CoopRallyRules`, `PartyRules` yazılır; EditMode testleri |
| M1.2 Dünya | `FieldLayout` topolojileri: Tek, ÜstAlt, DörtKenar. Kenarlar duvar ya da kale olarak açılıp kapanır; parti için kare saha ve köşe blokları. Raket kurulumu (taraf, konum, açı, renk) ve parmak kilidi. Top her raketten servis edebilir, ortadan otomatik servis yapabilir; ikinci top |
| M1.3 Maç yöneticisi | `LocalMatchController`: kurulum → geri sayım → oyun → sayı → sonuç → rövanş; duraklatma. Kayıtlar: maç sayısı, en uzun ortak ralli, günlük seri. Seri ödülü ve mevcut reklam kuralı |
| M1.4 Arayüz | Menüde Together kartı, merkez (3 sekme; Online ve Challenges M2'ye kadar "yakında"), görünüm seçimi, üç modun HUD'u, geri sayım, maç duraklatma, sonuç ekranı |
| M1.5 Asset | Yeni ikonlar, oyuncu raketleri (P1–P4 renkleri), sesler (geri sayım, sayı, galibiyet) |
| M1.6 Test ve QA | Kural testleri. PlayMode testleri: iki parmakla düello ve parmak kilidi, ortak can, partide elenme ve ikinci top. Tek oyunculu testler yeşil kalır. 13 cihaz denetimi yeni ekranlarla; tablette parti |

**M1 durumu (2026-10-01):**

| Adım | Durum |
|---|---|
| M1.1 Kurallar ve veri | ✅ `TableDuelRules`, `CoopRallyRules`, `PartyRules`. Üç mod varlığı. Maç kayıtları: maç sayısı, en uzun ortak ralli, günlük seri |
| M1.2 Dünya | ✅ `FieldLayout` üç topoloji. Kale ↔ duvar geçişi (`Goal.SetOpen`). Parti: kare saha, köşe blokları, kapalı kenar çubuğu. Düello: orta çizgi. Raket taraf/açı/renk kurulumu, parmak kilidi. Her raketten servis, otomatik servis, ikinci top |
| M1.3 Maç yöneticisi | ✅ `LocalMatchController`: kurulum → 3-2-1 → oyun → sonuç → rövanş, duraklatma, çıkış. Seri ödülü ve reklam kuralı maçlarda da geçerli |
| M1.4 Arayüz | ✅ Together kartı, merkez (Online ve Challenges "yakında"), görünüm seçimi (düello yarıları, partide dört kenar), maç HUD'u, geri sayım, sonuç |
| M1.5 Asset | ✅ İkonlar (`people`, `heart`), dört oyuncu raketi, dört ses |
| M1.6 Test ve QA | ✅ EditMode 83, PlayMode 18 test. Yeni ekranlarla 13 cihaz denetimi: 87 ekran durumu (menü kartı, merkez, düello kurulum/HUD/sonuç, ortak ralli HUD'u, tabletlerde parti kurulum/HUD/sonuç). Bulunan 3 sorun düzeltildi; yeniden denetimde sorun çıkmadı |

**Denetimde düzeltilenler:**
- **Together kartı:** Açıklama 16:9 ekranlarda iki satıra iniyor ve Play butonunu raketin üstüne itiyordu. Açıklama kısaltıldı: "Play with your friends".
- **Parti etiketleri:** Tabletlerde sol ve sağ oyuncu etiketleri ekrandan taşıyordu. Kort payı 0,55'ten 0,8 birime çıktı.
- **Ortak ralli HUD'u:** Pas sayısı servis noktasındaki topun üstüne biniyordu; düellodaki skorlar gibi sola alındı.
- **Koruma:** Tek oyunculu oyun sürerken maç açılamaz.
- **Skorlar (2026-10-03):** "Me" sekmesine "With friends" paneli eklendi: oynanan maç sayısı ve en uzun ortak ralli.

**Tasarım ayrıntıları (onaylı tasarımla uyumlu, uygulamada netleşti):**
- **Raketler:** Oyuncu renginde (P1 mercan, P2 turkuaz, P3 güneş, P4 üzüm). Kimliği renk ve konum birlikte verir.
- **Kostüm:** Her oyuncunun seçtiği kostümü top taşır; topa en son kim vurduysa onun kostümüne bürünür.
- **Servis:**
  - Düello ve ortak ralli: sayıyı kaybeden, dokunarak servis atar.
  - Parti: top 1 sn sonra ortadan, rastgele bir oyuncuya doğru kendiliğinden çıkar.
- **Ortak ralli hızı:** Top her vuruşta tek oyunculudaki gibi hızlanır; Perfect vuruş topu %6 yavaşlatır, ama servis hızının altına düşürmez.

**Kapı M1:** İki kişiyle tek telefonda, dört kişiyle tablette cihaz testi; küçük telefonda ergonomi kontrolü.

### M2 — Online altyapı (2–3 hafta) · **senin hesap adımın gerekli**
- **Senden:** Unity Cloud projesi oluştur ve bağla (Unity Dashboard).
- **Altyapı:** Paketleri kur:
  - Authentication
  - Multiplayer Services
  - NGO
  - Transport
  - Cloud Save
  - Cloud Code
  - Leaderboards
  - Multiplayer Play Mode
- **Kimlik:** Anonim giriş; üretilmiş oyuncu adı; profilde kostüm.
- **Oturum:** Kod ile oluştur ve katıl; hızlı eşleşme; protokol sürümü kontrolü.
- **Dayanıklılık:**
  - Uygulama arka plana atılınca ve bağlantı koparsa: 10 sn yeniden bağlanma süresi, sonra maç biter.
  - İnternet yoksa online kartları nazikçe kapat.
- **Lobi ekranı:** Oyuncular, kostümleri, hazır durumu, geri sayım.
- **Gizlilik:** Ayarlar'a "Online verilerimi sil" (UGS hesabını siler). Data Safety notları.

**Kapı M2:** Multiplayer Play Mode'da iki sanal oyuncu kodla eşleşip lobide buluşuyor; iki telefonla Wi-Fi ve mobil veri testi.

### M3 — Pas Düellosu ve online Ortak Ralli (3–4 hafta)
- **Topun el değiştirmesi (bkz. 4.4):** Top tavandaki portaldan çıkınca sahibi `BallHandoff` mesajı gönderir. Alıcı, portal animasyonu sırasında topu kendi sahasında başlatır; animasyon gecikmeyi gizler.
- **Host otoritesi:** Skor, kazanma ve rövanş host'ta. Her cihaz yalnızca topu kendi sahasındayken fiziği yürütür.
- **Etkileşim:** 4 hazır tepki ve rövanş isteği.
- **Kostüm:** Top rakibin sahasına geçince onun kostümünü giyer.

**Kapı M3:**
- Ağ simülatöründe 200 ms gecikme ve %5 paket kaybıyla oynanabilirlik.
- İki telefonda 20 maçlık test.

### M4 — Rush Kapışması ve meydan okumalar (4–5 hafta)
- **Rush Kapışması (2–4 kişi):**
  - Herkes kendi sahasında oynar; ağdan yalnızca skor ve saldırı olayları gider.
  - Art arda 3 Perfect, en yüksek skorlu rakibe saldırı gönderir: mini raket, hızlı top, sis.
  - Saldırı gelirken Perfect vurmak kalkan sayılır.
- **Hayalet Meydan Okuma:**
  - Tohumlu Rush koşusunun olay kaydı (sekme ve vuruş anları; yaklaşık 1–2 KB).
  - Cloud Code 6 haneli kod üretir; arkadaş kodu girip senin hayaletine karşı oynar.
  - Sonuç ekranında karşılaştırma.
- **Günün Meydan Okuması:**
  - Tohum tarihten türetilir (UTC); herkes aynı servis ve sekme dizisiyle oynar.
  - UGS günlük sıralaması.
- **Skorlar ekranı:** "Dünya" sekmesi UGS Leaderboards ile canlanır (Classic, Rush, Günlük, Ortak Ralli çiftleri).
- **Hile azaltma:** Sunucu tarafında makul skor üst sınırları; hayalet kaydı ile skor tutarlılık kontrolü.

**Kapı M4:** Multiplayer Play Mode'da 4 oyunculu Rush Kapışması; kod ile meydan okuma uçtan uca; günlük sıralama sıfırlanıyor.

### M5 — Canlı Düello (6–8 hafta)
- **Deterministik simülasyon:** Kendi küçük, sabit noktalı (fixed-point) Pong simülasyonumuz. Mevcut top kurallarının birebir kopyası: hız artışı, en düşük dikey açı, sekme sapması.
- **Geri sarma (rollback):**
  - 60 Hz; ağdan yalnızca raket girdileri gider.
  - Geç gelen girdi tahmin edilir, yanlışsa geri sarılıp yeniden hesaplanır.
  - Kopma tespiti için periyodik durum özeti (checksum).
- **Görsel:** Simülasyon durumundan yumuşatılmış çizim; mevcut kostümler ve efektler.
- **Alternatif:** Photon Quantum. Hazır deterministik motor, ama oyun mantığının ona göre yeniden yazılması ve ikinci bir servis gerekir.
- **Testler:** Determinizm (aynı girdiler → aynı özet, binlerce maç), geri sarma doğruluğu, 150 ms gecikmede his.

**Kapı M5:** İki telefonda 150 ms gecikmede akıcı oyun; kopma testi; adalet (iki taraf aynı sonucu görüyor).

**Toplam:** yaklaşık 20–27 hafta. Her faz kendi başına yayınlanabilir.

---

## 4. Modların teknik tasarımı

### 4.1 Masa Düellosu
- **Saha:** ÜstAlt topoloji. Alttaki oyuncu bugünkü raket; üstteki ters yönde. Kaleler alt ve üst.
- **Giriş:** Ekran yatay çizgiyle ikiye bölünür. Her parmak başladığı bölgeye kilitlenir; diğer oyuncunun alanına kaysa bile kendi raketini sürer.
- **Kurallar:**
  - Kaçıran rakibe sayı verir; 5'e ulaşan kazanır (ayarlanabilir).
  - Top hızı her vuruşta artar ve her sayıda sıfırlanır.
- **HUD:** Skorlar orta çizginin iki yanında; üstteki 180° dönük. Duraklat butonu ortada.

### 4.2 Ortak Ralli
- **Yerel:** Masa Düellosu düzeni.
- **Online:** Pas Düellosu düzeni.
- **Kurallar:** 3 ortak can; skor = toplam pas. Her 10 pasta hız artar; Perfect topu biraz yavaşlatır.
- **Sıralama:** "En uzun ralli" çift sıralaması (online).

### 4.3 Parti Masası
- **Saha:** DörtKenar. Kare saha tabletin kısa kenarına sığdırılır, köşeler duvardır.
- **Kurallar:** 3–4 oyuncu, her birine 3 can. Elenen oyuncunun kenarı duvar olur. 15 sayıda ikinci top gelir; son kalan kazanır.
- **Giriş:** Her oyuncunun dokunma bölgesi kendi kenarına bitişik bir şerit.

### 4.4 Pas Düellosu (topun el değiştirmesi)
```mermaid
sequenceDiagram
  participant A as Telefon A (top burada)
  participant H as Host (skor)
  participant B as Telefon B
  A->>A: Fizik A'da çalışır
  A->>B: BallHandoff {seq, x, yön, hız, ralli, kostüm}
  Note over B: Portal animasyonu 0,5 sn<br/>gecikmeyi gizler
  B->>B: Top B'nin sahasına iner
  B-->>H: Miss {seq}
  H->>A: ScoreUpdate {A:1, B:0, servis: B}
  H->>B: ScoreUpdate
```
- Saha boyutları her iki tarafta normalize edilir (9:16). Giriş x'i aynalanır (rakibin solu benim sağım).
- Aynı mesaj iki kez gelirse `seq` ile tekilleştirilir. Kaybolan el değiştirmede gönderen 1 sn sonra tekrar yollar.
- Uygulama arka plana geçerse 10 sn bekleme; dönmezse rakip kazanır.

### 4.5 Rush Kapışması
- Herkes kendi `RushRules`'unu çalıştırır. Saniyede 4 kez skor yayını ve saldırı olayları gider.
- Saat host'tan başlar (ortak başlangıç anı). Bitişte skorlar host'ta karşılaştırılır.
- Saldırı hedefi lideri dengeler: saldırı en yüksek skorlu rakibe gider.

### 4.6 Hayalet ve Günün Meydan Okuması
- **Kayıt biçimi** (sürümlü, sıkıştırılmış, ~1–2 KB):
  - Başlık: sürüm, mod, tohum, süre, son skor, kostüm.
  - Olay listesi: sekme (ms, x, y, hız vektörü), vuruş (ms, perfect), kaçırma (ms).
  - Toplar çarpışmalar arasında düz gittiği için olay kaydı yeterli.
- **Oynatma:** Hayalet top ve skor çizgisi olaylar arasında ara değerlenir. Fizik yeniden çalıştırılmaz, cihaz farkı önemsizdir.
- **Kod:** Cloud Code, karışık karakterler olmadan 6 haneli bir kod üretir ve kaydı Cloud Save'e yazar. 7 gün geçerli.
- **Günlük tohum:** `hash(UTC tarih)`. Herkes aynı servis dizisini görür.

### 4.7 Canlı Düello (deterministik simülasyon + geri sarma)
- **Sabit nokta matematiği** (Q16.16): saha 5.625×10 birim; top dairesi; raket kapsülü.
- **Tik:** 60 Hz. Girdi = raketin hedef x'i (8 bit), son 5 tikle birlikte (kayba karşı) güvenilmez-sıralı kanaldan gider.
- **Geri sarma:** 15 tiklik durum halkası. Tahmin = son bilinen girdi. Düzeltme gelince geri sar ve yeniden hesapla.
- **Kopma tespiti:** Her 30 tikte durum özeti karşılaştırılır.
- **Testler:** Aynı girdi dizisi → aynı özet (EditMode, 10.000 rastgele maç); sabit nokta ile mevcut Physics2D davranışının görsel karşılaştırması.

---

## 5. Kalite ve test stratejisi

| Katman | Araç | Kapsam |
|---|---|---|
| EditMode | Unity Test Framework | Kurallar, kayıt ve sayaçlar, hayalet kodlayıcı, el değiştirme eşlemesi, deterministik simülasyon |
| PlayMode | Input System test düzeneği | Yerel düello akışı, çoklu dokunuş bölgeleri, oyun sonu ve rövanş |
| Online (Editor) | Multiplayer Play Mode | 2–4 sanal oyuncuyla eşleşme, el değiştirme, kopma |
| Ağ koşulları | Transport ağ simülatörü | 50 / 150 / 300 ms gecikme, %1–5 kayıp |
| Düzen | Mevcut 13 cihazlık denetim sürücüsü | Yeni ekranlar (hub, lobi, sonuç, dönük HUD) |
| Cihaz | CI APK'ları (artan sürüm) | 2 telefon + 1 tablet, her kapıda |
| CI | `unity-test-runner` + `unity-builder` | Her push'ta test, sonra build |

**Kural:** Tek oyunculu oyunun davranışı M0'dan sonra testlerle kilitlenir. Her faz bu testleri yeşil bırakmadan kapanmaz.

---

## 6. Etki: oyuna ve diğer sistemlere

| Sistem | Değişiklik |
|---|---|
| Menü | "Arkadaşla oyna" kartı → Hub |
| Skorlar | Dünya sekmesi UGS Leaderboards; yeni "Maçlar" bölümü (galibiyet/yenilgi, en uzun ortak ralli) |
| Kostümler | Rakip kostümleri görünür; yeni satın alma yok |
| Reklamlar | Online maç sırasında ve hemen sonrasında reklam yok; yerel maç sonunda mevcut geçiş kuralı (3 dk bekleme) |
| Seri | Biten her maç günlük seriye sayılır |
| Skor kilitleri | Yalnız tek oyunculu Classic rekoru (değişmez) |
| Ses | Yeni efektler aynı sentez betiğinden: portal, saldırı, tepki, geri sayım, galibiyet, yenilgi |
| Gizlilik | Data Safety: oyuncu kimliği, oyun etkileşimi. "Online verilerimi sil" |
| APK boyutu | NGO + servisler yaklaşık +3–5 MB |

---

## 7. Riskler

| Risk | Olasılık | Etki | Önlem |
|---|---|---|---|
| Refaktörde tek oyunculu oyunun bozulması | Orta | Yüksek | M0'da önce testler, sonra refaktör; cihaz kontrolü |
| Küçük telefonda iki kişilik ergonomi | Orta | Orta | Bölge sınırı ve top hızı ayarı; M1 kapısında test |
| Online maliyet | Düşük | Orta | Ücretsiz kotalar; panelde alarm |
| Hile (online skorlar) | Orta | Düşük–Orta | Host otoritesi, sunucuda skor sınırı, hayalet tutarlılığı |
| Canlı Düello'nun karmaşıklığı | Yüksek | Orta | En sona bırakıldı; Photon Quantum yedek planı |
| Hesap adımlarının gecikmesi | Orta | Orta | M0 ve M1 hesapsız ilerler |
| UGS kesintisi | Düşük | Düşük | Online kartları gizle; çevrimdışı modlar etkilenmez |

---

## 8. Senden gerekenler

| Ne zaman | Adım |
|---|---|
| Şimdi | Bu planı ve aşağıdaki kararları onayla |
| M0 sonu | Ekran tasarımlarını onayla |
| M2 başı | Unity Dashboard'da proje oluştur, Unity Editor'den projeye bağla. Ücretsiz kota için ödeme bilgisi istenirse sen girersin |
| Her kapı | İki telefon (ve varsa tablet) ile cihaz testi |

## 9. Kapı M0 kararları
- [x] Faz sırası M0 → M5 (2026-10-01)
- [x] Online servis: **UGS tek çatı** (2026-10-01)
- [x] Sıralamalar: **UGS Leaderboards**, Play Games sıralama planının yerine (2026-10-01)
- [x] Canlı Düello: **kendi deterministik simülasyonumuz** (2026-10-01)
- [x] Oyuncu adları: üretilmiş adlar, sohbet yok, 4 hazır tepki (2026-10-01)

## 10. Kapı M0 tasarım kararları (onay 2026-10-01)
- [x] Menü girişi: mod seçicide üçüncü kart **Together**; Play merkezi açar
- [x] İngilizce mod adları: Table Duel, Co-op Rally, Party Table, Portal Duel, Rush Battle, Ghost Challenge, Daily Challenge, Live Duel
- [x] Table Duel hedefi: 5 sayı
- [x] Oyuncu adları: üretilmiş "Adjective Animal", İngilizce
- [x] Tepkiler: başparmak, şaşkın, gülen yüz, ateş
- [x] Party Table telefonda kilitli görünür ("Tablet only")
