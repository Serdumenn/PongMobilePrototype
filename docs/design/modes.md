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
| **Arcade** (2026-10-27, menüde ilk) | 1 can. Puanlar Rush gibi (Perfect 2, art arda 3+ Perfect 3). Sahada altınlar ve güçlendirmeler çıkar | Normal 1 · Perfect 2 · seri 3 puan | Açık |
| **Macera** (2026-10-31, menüde ikinci) | 3 dünyada 30 bölüm; her bölümün kendi hedefi var. 3 hak, kalan hak yıldız olur | Puanlar Arcade gibi · bölüm verisi `adventure_book.asset` | Açık; bölümler sırayla açılır |
| **Classic** | 1 can. Her vuruş 1 puan. Top her vuruşta hızlanır | — | Açık |
| **Rush** | 60 saniye; süre ilk vuruşla başlar. Top kaçınca 1 sn sonra yeniden gelir ve süreden 5 sn düşer. Topun raketin orta %25'ine denk gelmesi **Perfect** sayılır | Normal vuruş 1 · Perfect 2 · art arda 3+ Perfect 3 puan | Açık |

### Arcade, altın ve "devam et" (2026-10-27)

Oyun testinden gelen geri bildirim: oyun sadece "vur, skor yap" olarak kalırsa zayıf; altın, olaylar ve geri dönme sebebi gerekiyor. Ürün sahibi kararı: her şeyin olduğu yeni ana mod **Arcade**; Classic ve Rush saf kalır; tek para birimi **Altın**; reklamla iki katlama altına uygulanır, skora asla.

- **Arcade:** yeni oyuncular buradan başlar (mod listesinde ilk). Kurallar Rush puanlamasıyla Classic'in tek canı. Güçlendirmeler Arcade'de de çıkar. Dünya sıralaması `arcade` (UGS panelinde aynı ayarlarla oluşturulmalı).
- **Sahada altın:** ilk servisten 3 sn sonra, sonra 3,5–6 sn'de bir sahanın üst yarısında altın çıkar, 5 sn'de küçülerek kaybolur. Top değerse alınır: normal altın 5, büyük altın (%15) 15. Sıra ve yer koşunun tohumundan gelir (`GoldCoins`).
- **Koşu sonu altını:** toplanan altın + her 5 puan için 1 altın (`RunPurse`). Her modda kazanılır; Arcade'de sol üstte sayaç görünür.
- **2× altın:** oyun sonunda ödüllü reklamla o koşunun altını bir kez daha verilir. Skor ve sıralama değişmez.
- **Bir kez devam et:** Classic ve Arcade'de koşuyu bitiren kaçıştan sonra "Devam mı?" kartı çıkar (5 sn geri sayım). Reklam izlenirse skor korunur, top yeniden servis edilir. Koşu başına bir kez; Rush, Günlük, Hayalet ve Rush Battle'da yoktur. Reklam hazır değilse kart hiç çıkmaz.
- **Görev altını:** her tamamlanan günlük görev ayrıca 25 altın verir.
- Kod: `Assets/_Game/Scripts/Economy/` (`GoldCoins`, `CoinTrigger`, `RunPurse`), `SoloGameManager` (devam et), `ReviveScreen`; testler `GoldTests`, `ArcadePlayTests`.

### Arcade alan olayları (2026-10-31)

Ürün sahibi kararı: Arcade'de koşunun ortasında görev gibi olaylar çıkar. Her olayın hedefi, ilerlemesi ve ödülü vardır; skorun üstünde bir olay kartında görünür (ikon, ad, hedef, süre çubuğu, sayaç). Classic ve Rush'ta olay yoktur.

- **Zamanlama:** ilk olay ilk servisten 20 sn sonra, sonrakiler 30–40 sn arayla. Sıra koşunun tohumundan gelir, aynı olay üst üste gelmez (`EventSchedule`). Olay sürerken yeni güçlendirme çıkmaz.
- **Top Fırtınası (10 sn):** top 5'e bölünür, yelpaze gibi yukarı açılır. Fırtına boyunca kaçan top koşuyu bitirmez, sadece sayaç düşer. Hedef: en az 3 topu oyunda tutmak. Sonunda kalan her top +3 puan; başarı +25 altın. Ana top düştüyse kalan toplardan biri ana top olur, hiç kalmadıysa yeniden servis edilir.
- **Altın Yağmuru (9 sn):** 0,55 sn'de bir altın düşer. Hedef 6 altın; başarı +20 altın (toplanan altınların değerine ek).
- **Hedef Tahtası (12 sn):** sahanın üstünde yay biçiminde 5 hedef çıkar. Top değen hedef +3 puan; hepsi vurulursa +25 altın.
- **Dev Pingi (10 sn):** top 1,8 kat büyür, %20 hızlanır (yavaş top güçlendirmesi iptal olur). Her raket vuruşu +1 ek puan; hedef 8 vuruş, başarı +20 altın. Sonunda top eski boyuna döner.
- **Bitiş:** hedefe ulaşınca olay hemen biter, konfeti ve "Başardın! +N altın" yazısı çıkar; süre biterse "Süre doldu!" yazar, altın verilmez. Oyun biterse veya menüye dönülürse olay ödülsüz kapanır.
- **Görevler:** günlük görev havuzlarına "N Arcade olayı tamamla" (1/2/4) ve "N altın topla" (30/80/160) eklendi.
- Kod: `Assets/_Game/Scripts/Events/` (`ArcadeEvents`, `EventSchedule`, `TargetBoard`, `TargetTrigger`), `SoloGameManager.Sheltered` (fırtınada kaçışları affeder), HUD olay kartı; testler `EventScheduleTests`, `EventsPlayTests`.
- **Sonra:** süreli canlı olaylar (hafta sonu altın yağmuru gibi).

### Raket konumu ve nişan (2026-10-27)

- **Raket yukarıda:** oyunda raket ekranın altından %19 yukarıda durur (eskiden %10); başparmak raketin altında kalır ve raket görünür. Menüde raket eski yerinde (0,9 birim aşağıda) durur, oyun başlarken yerine geçer; menü düzeni değişmez.
- **Parmak şeridi:** oyun sırasında en altta silik bir şerit ve sürükleme ikonu başparmağın yerini gösterir.
- **Kavisli raket:** topun rakete değdiği yer yönü belirler. Sağ uçtan vuruş sağa, sol uçtan sola gider; ortadan vuruş düze yakın çıkar (Perfect bölgesi). Yan hız = vuruş noktası × 0,6 (en çok yan hız sınırı) + gelen açının %25'i (`BallPace.Aim`). Raket görselleri bunu anlatmak için hafif öne kavislidir. Party Table eski fizikte kalır; Canlı Düello kendi simülasyonunu kullanır, henüz değişmedi. Çevrimiçi protokol sürümü 4 oldu.

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

## Macera (2026-10-31)

Ürün sahibinin kararları: can/enerji sistemi yok (istediğin kadar dene); ilk sürümde 3 dünya × 10 bölüm.

- **Akış:** menüde Macera kartı (harita ikonu, "33/90 yıldız"). Oyna'ya basınca harita açılır: üstte dünya sekmeleri (Çayır, Kumsal, Gece), dünya kartı (ad, bölüm sayısı, dünyadaki yıldızlar) ve zikzak bir yol üzerinde 10 bölüm düğmesi. Bitenler dünya renginde ve altında yıldızlar, sıradaki mercan, kilitliler gri; 10. bölüm büyük ve taçlı. Düğmeye dokununca bölüm kartı çıkar: hedef, özellik etiketleri (süre, hızlı top, dev top, engeller, hareketli hedefler), en iyi yıldız ve ilk geçiş ödülü; Oyna ile başlar.
- **Hak ve yıldız:** her bölümde 3 hak var; top kaçarsa 1 hak gider ve top yeniden gelir. Hedefe ulaşınca kalan hak sayısı yıldız olur (3/2/1). Haklar biterse bölüm kaybedilir; reklamla bir kez "devam et" teklifi çıkar (1 hak verir). Süreli bölümde süre biterse bölüm kaybedilir (Dayan bölümünde ise kazanılır).
- **Hedefler:** N vuruş, N Perfect, tüm hedefleri vur (Hedef Tahtası), N altın kap (bölüm boyunca altın düşer), N saniye dayan. Puanlar Arcade gibi sayılır; olaylar ve güçlendirmeler Macera'da çıkmaz.
- **Dünyalar:** Çayır temel hedefleri öğretir. Kumsal **engelleri** (tampon) getirir: top sahadaki yuvarlak tamponlardan seker, bazıları sağa sola kayar. Gece **hareketli hedefler** ve daha hızlı top getirir. Bazı bölümlerde top dev olur.
- **Açılma:** bölümler sırayla açılır (önceki bölümden en az 1 yıldız). Bir dünyanın ilk bölümü önceki dünyanın son bölümü bitince açılır.
- **Ödül:** ilk geçişte bölümün altını (20'den 200'e; patron bölümleri daha çok), her kazanılan bölüm +25 XP (Pingi Yolu). Koşu altını (skor/5 + toplanan altın) her zamanki gibi eklenir. Tekrar oynamak yıldız artırabilir, altını tekrar vermez.
- **Sonuç kartı:** "Bölüm tamam!" ya da "Az kaldı!", yıldızlar tek tek dolar, altın satırı; düğmeler harita, tekrar ve sonraki bölüm (kaybedince tekrar oyna). Geçişlerde ara reklam kuralı geçerli.
- **Kayıt:** bölüm yıldızları `cosmetics.json` içinde (`LevelStars`) tutulur, buluta kaydedilir; iki cihaz birleşirken her bölüm için büyük olan alınır.
- **Değerler:** tüm bölümler `Assets/_Game/Data/adventure_book.asset` içinde (hedef, sayı, süre, hız, top boyu, altın, hedef ve tampon yerleri, kayma, hedef boyu).
- **Denge (2026-10-31, bot testi):** 30 bölüm, ortalama ve kusursuz oynayan bir botla oynatıldı; hepsi geçilebilir. Hedefler ilk sürümde çok çabuk vuruluyordu (3-9 sn), bu yüzden hedef boyu Kumsal'da %85, Gece'de %68, son bölümde %62 yapıldı; 3-6 ve 3-9'a 30 sn süre eklendi. Bot artık hedef bölümlerini 5-13 sn'de bitiriyor ve ara sıra hak kaybediyor; gerçek oyuncu için kabaca 2-3 katı. Yerler sahaya göre normalize: x −1..1 (sol..sağ), y 0..1 (sahanın ortasının biraz altı..olay kartının altı).
- Kod: `Assets/_Game/Scripts/Adventure/` (`AdventureBook`, `AdventureMap`, `AdventureRules`, `AdventureRun`, `FieldObstacles`, `ObstacleBody`), `Gameplay/FieldSpots`, `SoloGameManager.StartAdventureRun`, `AdventureScreen`, `AdventureResultScreen`, HUD hak çipi; testler `AdventureTests`, `AdventurePlayTests`.
- **Sonra:** yeni dünyalar güncellemelerle eklenir (geri dönme sebebi).

## Pingi Yolu: seviye ve ödüller (2026-10-31)

Ürün sahibinin kararı: sezon ve ücretli yol yok; tek, bitmeyen bir yol. Pingi seviyesi ile yol aynı şeydir: her seviye yolda bir basamak, her basamağın bir ödülü vardır.

- **XP:** tek kişilik her koşuda skor kadar XP (Classic, Arcade, Rush, Günlük); başarılan Arcade olayı +15; her günlük görev +20, üçü birden +30; biten her çok oyunculu maç (yerel ya da çevrimiçi) +15; kazanılan her Macera bölümü +25.
- **Eğri:** 1'den 2'ye 60 XP, her seviye 10 XP daha fazla ister (`60 + 10 × (seviye − 1)`); 50. seviyeden sonra her seviye 550 XP'de sabit kalır. 50. seviye yaklaşık 14.700 XP'dir.
- **Ödüller:** her seviye altın verir (`30 + 5 × seviye`, 50'den sonra 280). Her 5. seviye ayrıca 2 görev yıldızı verir. 10, 25 ve 40. seviyelerde yıldız yerine yola özel kostüm gelir: **Kuyruklu Yıldız** raketi (10), **Şampiyon** Pingi (25), **Okyanus** teması (40). Bu kostümler satılmaz, altınla açılmaz, Pingi Pass'e dahil değildir.
- **Arayüz:** menüde sol üstte seviye rozeti (numara ve XP çubuğu); dokununca Pingi Yolu kartı açılır: seviye, XP çubuğu ve 2'den en az 50'ye kadar basamaklar (geçilenler işaretli, sıradaki çerçeveli, kostüm basamakları sarı). Oyun sonunda XP satırı çıkar: "+N XP" ya da seviye atlanınca "Seviye N! +M altın"; çubuk dolarak ilerler. Yeni kostüm, ödül penceresiyle gelir. Koşu dışında seviye atlanırsa (ör. maç sonu) bildirim çıkar.
- **Eski oyunculara hediye:** sistem gelmeden önce oynamış oyuncular ilk açılışta bir kez geçmişlerine göre XP alır: toplam vuruş + her maç için 15, en fazla 9. seviyeye kadar (Kuyruklu Yıldız raketi yine oynayarak kazanılır). Verildiği `cosmetics.json` içinde (`XpSeeded`) işaretlenir ve buluta gider; başka telefonda tekrar verilmez.
- **Kayıt:** toplam XP ve ödülü verilmiş son seviye `cosmetics.json` içinde (`Xp`, `RoadLevel`) tutulur, buluta kaydedilir; iki cihaz birleşirken ikisi de büyük olandan alınır. Ödül bir seviye için yalnızca bir kez verilir.
- **Değerler:** `Assets/_Game/Data/road_pingi.asset` (eğri, altın, yıldız); XP miktarları `PlayerLevel` bileşeninde.
- Kod: `Assets/_Game/Scripts/Progress/` (`PingiRoad`, `RoadMath`, `PlayerLevel`), `RoadScreen`, `GameOverScreen` XP satırı, `MenuScreen` seviye rozeti; testler `PingiRoadTests`, `LevelPlayTests`.

## Günlük görevler (2026-10-15)

Ürün sahibinin kararı: görev ödülü **görev yıldızı**.

- **Her gün 3 görev**, herkese aynı (tarihten üretilir, `MissionBook`): biri kolay, biri orta, biri zor havuzdan. Aynı türden iki görev çıkmaz. Rush henüz açılmamışsa Rush'a özel görevler (Rush puanı, Perfect, güçlendirme) yerine diğerleri gelir.
- **Görev türleri:** tek Klasik oyununda N vuruş · tek Rush oyununda N puan · N Perfect · toplam N vuruş · üst üste N vuruş · N oyun · N kıl payı kurtarış · N güçlendirme · N Arcade olayı · N altın.
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
