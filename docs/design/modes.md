# Özellik: Oyun modları ve skor tablosu

*v1.1 · 2026-10-01 · Durum: **A aşaması uygulandı ve Editor'de test edildi · B aşaması çok oyunculu plan M4'te (UGS Leaderboards)***

Görsel öneri (v1 öncesi, 4 modlu taslak): [modes/index.html](modes/index.html)

## Kapı kararları (2026-09-30)
- **Modlar:** v1'de yalnızca **Classic + Rush**. Party ve Daily yedekte.
- **Skor tablosu:** Kişisel tablo şimdi. Dünya sıralaması B aşamasında. *2026-10-01 güncellemesi:* Google Play Games yerine UGS Leaderboards.
- **Mod kilitleri:** Önerildiği gibi. Rush baştan açık.
- **Seri ödülü:** 7 gün üst üste oynayana yeni kostüm **Astro** verilir. Daily modu olmadığı için seri, hangi mod olursa olsun "o gün en az bir oyun bitirmek" ile sayılır.

## Modlar

İki mod da aynı fizikle ve aynı tek başparmak kontrolüyle çalışır; her mod küçük bir kural katmanı ekler.

| Mod | Kural | Değerler (`mode_*.asset`) | Açılma |
|---|---|---|---|
| **Classic** | 1 can. Her vuruş 1 puan. Top her vuruşta hızlanır | — | Açık |
| **Rush** | 60 saniye; süre ilk vuruşla başlar. Top kaçınca 1 sn sonra yeniden gelir ve süreden 5 sn düşer. Topun raketin orta %25'ine denk gelmesi **Perfect** sayılır | Normal vuruş 1 · Perfect 2 · art arda 3+ Perfect 3 puan | Açık |

**Yedekte (v1 sonrası):** Party (çoklu top), Daily (günlük değiştiriciler), Zen, Duel.

**Kostüm kilitleriyle ilişki:** Skorla açılan kostümler **Classic** rekoruna bakar; Rush'ın skor ölçeği farklı.

### Tempo (2026-10-14)

Oynanış testinden gelen geri bildirim: oyun yavaş başlıyor ve topun gidip gelmesi fazla bekletiyor. Ölçüm: top yolu tek yön yaklaşık 8,1 birim; eski ayarlarla ilk gidiş-dönüş 3,2 sn, yatık açıda 6,8 sn; 20. vuruşa 56 saniyede varılıyordu.

**Sabit dikey hız:** topun dikey hızı açıdan bağımsızdır; açı yalnız yana hızı belirler. Yatık açıdaki uzun zikzaklar ortadan kalkar ve gidiş-dönüş süresi her vuruşta öngörülebilir olur.

| Değer (`Ball`) | Ayar | Anlamı |
|---|---|---|
| `SteadyPace` | açık | Kapatılınca eski model (`LaunchSpeed`, `MaxSpeed`, `SpeedIncreasePercent`) geri gelir; karşılaştırma için |
| `PaceStart` | 10 | Başlangıç dikey hızı: gidiş-dönüş 1,6 sn |
| `PaceMax` | 18 | En yüksek dikey hız: gidiş-dönüş 0,9 sn |
| `PaceIncreasePercent` | %1,3 | Her raket vuruşunda artış; 45. vuruşta, yaklaşık 56. saniyede en yüksek hıza ulaşılır |
| `MaxSideSpeed` | 5,5 | Yana hız sınırı; dikey hız artsa da sabit kalır |
| `MinSideSpeed` | 1,5 | Top dümdüz inip çıkmasın diye en az yana hız |

- Servis: yön ve açı aynı rastgele sayılardan üretilir (aynı tohum, aynı servis); yana hız sınırın %35–85'i arasındadır.
- Kapsam: tek kişilik modlar, Table Duel, Co-op Rally, Portal Duel, Rush Battle. Party Table (dört kenar) eski modelde kalır.
- Çevrimiçi protokol sürümü 3 oldu; eski sürümle eşleşme yapılmaz.
- Mantık `BallPace` içinde, testleri `BallPaceTests` ve `GameFlowTests`.

### Güçlendirmeler (Rush, 2026-10-15)

Ürün sahibinin kararı: güçlendirmeler **yalnız Rush'ta**; Classic saf beceri oyunu olarak kalır. Rush Battle (çevrimiçi) kendi saldırı kurallarıyla kalır, güçlendirme almaz.

| Güçlendirme | Etki | Süre |
|---|---|---|
| Dev raket | Raket 1,5 kat uzar | 8 sn |
| Yavaş top | Top hızı %72'ye iner | 6 sn |
| Çift puan | Her vuruş iki kat puan (Perfect dahil) | 8 sn |
| Kalkan | Raketin altında bir duvar; bir kaçışı geri çevirir, süre cezası olmaz | Kullanılana ya da 12 sn dolana kadar |

- **Jeton:** topun ilk servisinden 8 sn sonra ilk jeton çıkar, sonra 10–14 sn'de bir. Sahanın üst yarısında belirir, 7 sn bekler (son 1,5 sn yanıp söner). Top değince alınır; raket alamaz.
- **Adalet:** sıra, yer ve aralık koşunun tohumundan gelir (`PowerSchedule`, `MatchRandom`). Aynı tohumlu koşularda (Günlük) herkes aynı jetonları görür. Aynı güçlendirme üst üste gelmez.
- **Görünüm:** renkli jeton + beyaz ikon; alınca beyaz halka, şerit ("Çift puan!") ve `sfx_power` (kalkanda `sfx_shield`). Etkin güçlendirmeler skorun altında, süresi azalan çubuklu çiplerde görünür.
- **Süre:** etkiler oyun zamanıyla azalır; pause'da durur. Koşu bitince ya da menüye dönünce hepsi kapanır.
- **Sıralama:** Rush puanları güçlendirmelerle yükselir; dünya sıralaması yayından önce zaten sıfırlanacak.
- Kod: `Assets/_Game/Scripts/Gameplay/Power/` (`PowerUps`, `PowerSchedule`, `PowerToken`, `ShieldWall`); testler `PowerScheduleTests`, `PowerPlayTests`.

## Günlük görevler (2026-10-15)

Ürün sahibinin kararı: görev ödülü **görev yıldızı**.

- **Her gün 3 görev**, herkese aynı (tarihten üretilir, `MissionBook`): biri kolay, biri orta, biri zor havuzdan. Aynı türden iki görev çıkmaz. Rush henüz açılmamışsa Rush'a özel görevler (Rush puanı, Perfect, güçlendirme) yerine diğerleri gelir.
- **Görev türleri:** tek Klasik oyununda N vuruş · tek Rush oyununda N puan · N Perfect · toplam N vuruş · üst üste N vuruş · N oyun · N kıl payı kurtarış · N güçlendirme.
- **Ödül:** her görev 1 yıldız, üçü birden +1 bonus. Yıldızlar birikir: 15'te Starry raket, 40'ta Stella top, 80'de Aurora raket ([cosmetics.md](cosmetics.md)).
- **Arayüz:** menüde yıldız düğmesi ve "2/3" rozeti; düğme görev kartını açar (görevler, ilerleme çubukları, sıradaki ödül ve toplam yıldız). Görev tamamlanınca bildirim ve ses; yeni ödül oyun sonunda ödül penceresiyle gelir.
- **Kayıt:** günlük ilerleme `missions.json` (cihazda), yıldızlar `cosmetics.json` (bulutta). Gün, cihazın yerel tarihine göre döner.
- Kod: `Assets/_Game/Scripts/Missions/` (`MissionBook`, `MissionLog`, `DailyMissions`), `MissionsScreen`; testler `MissionTests`, `MissionsPlayTests`.

### Rush HUD'u
- Üstte süre çubuğu. Son 10 saniyede mercan rengine döner. Altında `m:ss` sayacı.
- Perfect vuruşta ekranın ortasında "Perfect! +N" yazısı çıkar (520 ms).
- Skorun altında seri rozeti durur ("×N perfect"). 2 Perfect'ten itibaren görünür.

## Skor tablosu

### Kişisel (cihazda, hesap gerekmez) — uygulandı
- **Kayıt yeri:** `records.json` (`persistentDataPath`), atomik yazılır. Rekorlar PlayerPrefs'te: Classic `bestScore` (eski kayıt olduğu gibi korunur), Rush `bestScore_rush`.
- **Benim sekmesi:**
  - Oyun sayısı, toplam vuruş, en uzun seri, toplam oyun süresi
  - Mod başına rekor ve tarihi (Bugün / Dün / gün ay). Kayıt sisteminden önce kırılmış rekorlar "earlier" olarak görünür.
- **Seri paneli:** 7 daire var; 7. dairede Astro görünür. Altındaki not durumu söyler: "yarın gel", "bugün oyna" ya da "açıldı".
- **Dünya sekmesi:** Mod çipleri (Classic / Rush) ve "Coming soon" durumu var; oyuncunun o moddaki rekoru gösterilir. Metin M4'te UGS'ye göre güncellenecek.

### Dünya (UGS Leaderboards) — B aşaması
> **Karar 2026-10-01:** Play Games planının yerine **Unity Gaming Services Leaderboards** kullanılacak. Çok oyunculu modlarla aynı kimlik ve panel paylaşılır ([multiplayer.md](multiplayer.md)). B aşaması, çok oyunculu planın **M4** fazında yapılacak.

- **Tablolar:** Classic ve Rush; her biri Bugün / Bu hafta / Tüm zamanlar (UGS sıfırlama planlarıyla). Günün Meydan Okuması için ayrıca günlük tablo eklenecek. `GameModeDefinition.LeaderboardId` alanı hazır.
- **Gösterim:** Oyun içinde Soft Pop tasarımıyla. İlk 25 sıra ve oyuncunun kendi sırası gösterilir.
- **Kimlik:** UGS anonim giriş, otomatik; hesap ekranı yok. Oyuncu adı üretilmiş bir addır (sohbet yok). Google Play Games bağlama, yayın fazında isteğe bağlı.
- **Game Over:** Skor gönderilir ve sıra değişimi gösterilir.
- **Hileye karşı:** Skor Cloud Code üzerinden gönderilir. Mod başına üst sınır ve süreye göre makul skor kontrolü yapılır.

### Neden UGS Leaderboards?
Çok oyunculu modlar zaten UGS (kimlik, oturum, Relay) kullanacak. Sıralamaları aynı çatıda tutmak tek kimlik, tek panel ve Play Console'a bağlı olmayan bir kurulum demek. Günlük ve çift tabloları da kolayca açılabiliyor. Fiyatlandırma, ücretsiz kotayla izlenecek (multiplayer.md, riskler).

## Teknik yapı (uygulanan)

| Parça | Sorumluluk |
|---|---|
| `GameModeDefinition` (ScriptableObject) | Kimlik, ad, açıklama, rekor anahtarı, skor tablosu kimliği, açılma koşulu, Rush değerleri |
| `ModeRunner` → `ClassicRunner`, `RushRunner` | Puanlama, Perfect/seri, süre, kaçırma cezası, oyun sonu kararı |
| `SoloGameManager` | Aktif mod ve seçimi (`SelectedMode` pref'i), runner'ı çalıştırma, Rush'ta yeniden servis |
| `SoloScoreManager` | Moda göre rekor anahtarı, puan ekleme, rekor sıfırlama |
| `PlayerRecords` + `RecordsService` | İstatistikler, mod başına rekor tarihi, günlük seri |
| `CosmeticsService` | Oyun sonunda Classic rekoruna göre skor ödülleri, seriye göre Astro |
| UI | `MenuScreen` mod seçici (oklar, kaydırma, noktalar), `HudScreen` Rush göstergeleri, `ScoresScreen` (Benim / Dünya), `GameOverScreen` mod adlı başlık |

## Aşamalar
1. **A — Hesap gerektirmeyenler: tamam.**
   - İçerik: Classic + Rush, mod seçici, kişisel rekorlar ve istatistikler, seri ve Astro, Skorlar ekranı.
   - Test (Editor, 3 ekran oranı):
     - Rush puanlaması, doğru sayıda kaçırmadan sonra oyun sonu, yeni rekor
     - Skorlar ekranının iki sekmesi
     - Dolap'ta Astro kartı ve penceresi
     - 7. gün ödülü ve giydirme
     - Duraklatma akışları
2. **B — UGS Leaderboards (çok oyunculu plan M4):** anonim giriş, Cloud Code ile skor gönderme, canlı sıralama.

## Gerekenler (B aşaması için)
1. Unity Cloud'da proje oluşturulup Editor'e bağlanır (multiplayer.md, M2). Ücretsizdir; kart bilgisi gerekmez.
2. Panelde Leaderboards servisi açılır; tablolar proje içinde tanımlanır ve kimlikleri koda girilir.
