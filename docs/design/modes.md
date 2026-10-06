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
