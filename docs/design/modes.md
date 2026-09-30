# Özellik: Oyun modları ve skor tablosu

*v1 · 2026-09-30 · Durum: **A aşaması uygulandı ve Editor'de test edildi · B aşaması hesap bekliyor***

Görsel öneri (v1 öncesi, 4 modlu taslak): [modes/index.html](modes/index.html)

## Kapı kararları (2026-09-30)
- **Modlar:** v1'de yalnızca **Classic + Rush**. Party ve Daily yedekte.
- **Skor tablosu:** Kişisel tablo şimdi. Google Play Games dünya sıralaması hesaplar hazır olunca (B aşaması).
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
- **Dünya sekmesi:** Mod çipleri (Classic / Rush) ve "Coming soon with Google Play Games" durumu var; oyuncunun o moddaki rekoru gösterilir.

### Dünya (Google Play Games) — B aşaması
- **Tablolar:** 2 skor tablosu (Classic, Rush), her biri Bugün / Bu hafta / Tüm zamanlar. `GameModeDefinition.LeaderboardId` alanı hazır.
- **Gösterim:** Oyun içinde Soft Pop tasarımıyla (`LoadScores`): ilk 25 sıra ve oyuncunun kendi sırası. Google'ın hazır ekranı yedek olarak açılabilir.
- **Giriş:** Otomatik (Play Games v2). Giriş yapılmamışsa "Play Games ile bağlan" butonu çıkar.
- **Game Over:** Skor gönderilir ve sıra değişimi gösterilir.
- **Hileye karşı:** Play Console'da her tablo için izin verilen en düşük ve en yüksek skor tanımlanır, kurcalama koruması açılır.
- **Eklenti:** Google Play Games Plugin for Unity **v2.3.0**, PGS v2 kullanıyor. En düşük Android API 24; projemiz 25. İndirme için ayrıca izin istenecek.

### Neden Google Play Games?
Ücretsiz ve Android'de standart; oyuncu adı ve kimliği hazır geliyor; günlük ve haftalık sıfırlamayı Google yapıyor. Unity'nin bulut skor tablosu (UGS) şu an "sınırlı süre ücretsiz" ve uzun vadeli fiyatı belirsiz.

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
1. **A — Hesap gerektirmeyenler: ✅ tamam.**
   - İçerik: Classic + Rush, mod seçici, kişisel rekorlar ve istatistikler, seri ve Astro, Skorlar ekranı.
   - Test (Editor, 3 ekran oranı):
     - Rush puanlaması, doğru sayıda kaçırmadan sonra oyun sonu, yeni rekor
     - Skorlar ekranının iki sekmesi
     - Dolap'ta Astro kartı ve penceresi
     - 7. gün ödülü ve giydirme
     - Duraklatma akışları
2. **B — Google Play Games (hesap hazır olunca):** eklenti, giriş, skor gönderme, canlı sıralama.

## Senin yapman gerekenler (B aşaması için)
1. Play Console'da uygulamayı oluştur (`com.SERDUMEN.PingiPongi`).
2. **Release keystore** oluştur. Şifreyi sen belirlersin; ben şifre girmem.
3. Play Games Services projesini kur. SHA-1 parmak izleriyle (upload ve app signing) OAuth istemcisi oluştur.
4. 2 skor tablosunu (Classic, Rush) oluştur ve bana kaynak XML'ini ya da kimlikleri ver.
5. Test kullanıcılarını ekle.
