# Yerelleştirme (Faz 6b) — Plan ve Kapı L

Hedef: oyunda 8 dil. Biri mutlaka Türkçe; diğerleri **en çok gelir getirecek** pazarlara göre seçilir. Kalite hedefi "kusursuz": her ekran her dilde taşmasız, doğru ve doğal.

## 1. Pazar verisi (2025)

Oyun yalnızca Google Play'de çıkacak. Gelirimiz reklam (ödüllü ve geçiş) ve uygulama içi satın alma. Bu yüzden ölçü, **Android'deki oyun geliri** ve **reklam fiyatı (eCPM)**.

| Pazar | Mobil oyun geliri | Android payı | Oyuncu başı gelir | Dil |
|---|---|---|---|---|
| ABD | 24,2 milyar $ | %37 | 151 $ | İngilizce |
| Japonya | 12 milyar $ | %36 | 209 $ | Japonca |
| Güney Kore | 4,9 milyar $ | %71 | 179 $ | Korece |
| Almanya | 2,4 milyar $ | %63 | 68 $ | Almanca |
| Birleşik Krallık | 2,4 milyar $ | %48 | 79 $ | İngilizce |
| Brezilya | 1,4 milyar $ | %83 | 15 $ | Portekizce |
| Tayvan | 1,4 milyar $ | %49 | 93 $ | Geleneksel Çince |
| Fransa | 1,35 milyar $ | %70 | 51 $ | Fransızca |
| Meksika | 1,25 milyar $ | %76 | 19 $ | İspanyolca |
| İtalya | 1,0 milyar $ | %65 | 40 $ | İtalyanca |
| Türkiye | 0,9 milyar $ | %74 | 21 $ | Türkçe (%71 büyüme) |
| İspanya | 0,57 milyar $ | %72 | 22 $ | İspanyolca |

Kaynak: Allcorrect, Mobile Game Market Index 2025 (Newzoo tabanlı). Google Play harcamasında ilk 5 sırayı ABD, Japonya, Güney Kore, Hindistan ve Brezilya alıyor (Sensor Tower, State of Mobile 2026). Android'de reklam fiyatı en yüksek pazarlar ABD, Kanada, Japonya, Güney Kore ve Avustralya (Tenjin × CAS, 2024).

**Elenenler:**
- **Rusça:** Google, Rusya'daki kullanıcılara AdMob reklamlarını ve Play ödemelerini durdurdu. Reklam ve satın alma gelirimiz orada sıfıra yakın olur.
- **Basitleştirilmiş Çince:** Çin anakarasında Google Play yok.
- **Hintçe ve Endonezce:** Oyuncu sayısı çok yüksek ama oyuncu başı gelir ve reklam fiyatı çok düşük.

## 2. Önerilen dil seti

Toplam 8 dil (İngilizce dahil):

| # | Dil | Neden |
|---|---|---|
| 1 | İngilizce (mevcut) | ABD, Birleşik Krallık, Kanada, Avustralya; varsayılan dil |
| 2 | Türkçe | Zorunlu; hızlı büyüyen, Android ağırlıklı pazar |
| 3 | Japonca | 2. büyük pazar; en yüksek oyuncu başı gelir |
| 4 | Korece | Android payı %71, Google Play'de 3. sırada |
| 5 | Almanca | Avrupa'nın en büyük pazarı (Avusturya ve İsviçre dahil) |
| 6 | İspanyolca (Latin Amerika) | Meksika ve İspanya ile birlikte geniş ve Android ağırlıklı kitle |
| 7 | Portekizce (Brezilya) | Android payı %83, Google Play'de 5. sırada |
| 8 | Fransızca | Fransa, Kanada (Quebec), Belçika, İsviçre |

Sıradaki aday **Geleneksel Çince** (Tayvan ve Hong Kong; yüksek gelir, Android'de Fransızcaya yakın). "8 dil" İngilizceden ayrı sayılıyorsa 9. dil bu olur.

## 3. Teknik yol

| Seçenek | Artı | Eksi |
|---|---|---|
| **A · Kendi küçük sistemimiz (öneri)** | Paket indirmek gerekmez. Dil tabloları düz dosya; çevirmene CSV olarak verilir. Kendi testlerimizle tamamen denetlenir. CI derlemesi değişmez | Çoğul ve tarih kurallarını biz yazarız (8 dil için kısa ve iyi bilinen kurallar) |
| B · Unity Localization 1.5.13 | Resmî araç, tablo düzenleyici, sahte dil (pseudo-locale) | Addressables 1.25'i de getirir. Derlemeye ek adım ekler, CI riski ve proje karmaşıklığı artar |

Seçenek A'nın parçaları:
- **Dil tabloları:** Her dil için bir anahtar → metin tablosu. İngilizce kaynak dil.
- **Ekrana uygulama:** UXML'deki metinlere anahtar verilir, ekranlar dil değişince kendini yeniler. Kod içindeki metinler `Loc.Get("anahtar", değişkenler)` ile alınır.
- **Biçimler:** Çoğul ("1 point" / "2 points"), tarih ("Oct 5" → "5 Eki"), süre ("6 h", "45 min") ve sayılar dile göre.
- **Dil seçimi:** İlk açılışta cihaz dilinden otomatik. Ayarlar'da "Language" satırı var; değişince hemen uygulanır.
- **Sahte dil testi:** Metinleri %40 uzatan ve aksanlı harflerle dolduran bir test dili. Taşma ve çevrilmemiş metin kalmasın diye 13 cihaz denetimi bununla yapılır.
- **Otomatik testler:**
  - Her dilde her anahtar var.
  - Değişkenler (`{0}`, `{name}`) her çeviride aynı.
  - Kullanılan her harf fontta var.
  - Hiçbir metin uzunluk bütçesini aşmıyor.

## 4. Fontlar

- **Fredoka:** Latin dillerinin tümünü ve Türkçeyi kapsıyor; doğrulandı. Almanca, Fransızca, İspanyolca, Portekizce ve İtalyanca harflerinin hepsi var.
- **Japonca:** M PLUS Rounded 1c (Google Fonts, SIL OFL). Yuvarlak hatlı, Fredoka ile aynı karakter.
- **Korece:** Jua (Google Fonts, SIL OFL). Yuvarlak, oyunsu.
- **Yükleme şekli:** İki font da Fredoka'nın yedek fontu olarak eklenir; o dillerde harf Fredoka'da yoksa onlardan gelir. Uygulama boyutu büyümesin diye fontlar yalnızca tablolarımızda geçen harflere kırpılır (fontTools ile; dil başına yaklaşık 100 KB).

## 5. Çeviri ve kalite

- **İlk çeviriler:** Proje içinde yapılır. Her metnin nerede göründüğünü anlatan bağlam notları ve bir terim sözlüğü kullanılır (Rush, Daily Challenge, Ghost, Pingi Pass, kostüm adları vb.).
- **Türkçe:** Geliştiricinin gözden geçirmesi yeterli.
- **Diğer diller:** Yayından önce ana dili konuşan birinin okuması önerilir. Toplam yaklaşık 1.500–2.000 kelime; serbest çevirmenle dil başına birkaç saatlik iş. Yayın ertelendiği için bu karar yayın öncesine bırakılabilir.
- **Mağaza:** Mağaza sayfası metinleri ve ekran görüntüleri yayın fazında, aynı dillerle yapılır.

## 6. Kapı L kararları (onay 2026-10-05)

| Konu | Karar |
|---|---|
| Dil seti | 8 dil, İngilizce dahil: İngilizce, Türkçe, Japonca, Korece, Almanca, İspanyolca (Latin Amerika), Portekizce (Brezilya), Fransızca |
| Teknik yol | A · kendi küçük sistemimiz (paket yok, CI değişmez) |
| Fontlar | M PLUS Rounded 1c (Japonca) ve Jua (Korece) indirilir, kullanılan harflere kırpılır |
| Oyuncu adları | İngilizce kalır; herkes aynı adı görür |

## Kararlardan önceki sorular

1. Dil seti: 8 dil İngilizce dahil mi, hariç mi?
2. Teknik yol: A (kendi sistemimiz) mı, B (Unity Localization + Addressables) mı?
3. Japonca ve Korece fontların indirilmesi (M PLUS Rounded 1c, Jua; Google Fonts, SIL OFL).
4. Oyuncu adları ("Witty Penguin"): İngilizce kalsın mı, her telefonda kendi dilinde mi görünsün? Adlar rakiplerin telefonunda ve sıralamalarda da görünüyor; İngilizce kalırsa herkes aynı adı görür.

## 7. İş sırası (onaydan sonra)

1. **Altyapı:** dil tabloları, `Loc`, dil seçimi, Ayarlar satırı, sahte dil, testler.
2. **Metinleri taşıma:** UXML ve koddaki ~300 metin anahtara taşınır. Görünüm değişmez; İngilizce aynı kalır.
3. **Fontlar:** Japonca ve Korece yedek fontlar ve kırpma aracı.
4. **Çeviriler:** 7 dil, sözlük ve bağlam notlarıyla.
5. **Denetim:** her dilde 13 cihaz, kritik ekranların görüntüleri ve uzun metin düzeltmeleri.
6. **Dokümanlar ve onay.**

## 8. Durum (2026-10-05)

| Adım | Durum |
|---|---|
| 1. Altyapı | Bitti: `Loc`, `UiLocalizer`, Ayarlar dil satırı ve penceresi, sahte dil `qps` |
| 2. Metinleri taşıma | Bitti: 320 metin |
| 3. Fontlar | Bitti: M PLUS Rounded 1c ve Jua yedek font; satır ölçüleri Fredoka'ya eşitlendi |
| 4. Çeviriler | Bitti: 7 dil, eksik yok. Yayından önce ana dili konuşan biri okumalı |
| 5. Denetim | Bitti. 8 dil × 25 ekran, 16:9 telefon ve 4:3 tablet (400 durum), sorunlu ekranlarda ikinci tur (128 durum): taşma yok. Sahte dil (`qps`) 13 cihazda denendi |
| 6. Dokümanlar | Bitti: tech-spec, design-system, README lisans kaydı ve bu dosya |

**Denetimde bulunup düzeltilenler:**
- Tek satırlık yazılar kutuya sığmıyordu → `TextFit` eklendi (en fazla %40 küçültür, sekmeler birlikte küçülür).
- 16:9'da menü 24–53 px taşıyordu: Japonca satırlar %23 daha yüksekti ve 4 dilde Rush açıklaması 2 satıra çıkıyordu → yedek fontların satır ölçüleri Fredoka'ya eşitlendi, açıklama tek satır yapıldı, ES/PT/DE/JA açıklamaları kısaltıldı.
- Uzun buton yazıları kenara dayanıyordu → butonlara 36 px iç boşluk.
- Skorlar satırları DE/ES/FR'de taşıyordu (boş tarih hücresi sığdırmayı engelliyordu) → düzeltildi.
- "Today's best {0} · Rank #{1}" metnindeki `#` not işareti sanılıyordu; İngilizcede sıra numarası görünmüyordu → not kuralı yalnız sondaki `#kelime` oldu, test eklendi.

**Testler:** EditMode 138, PlayMode 45; hepsi geçiyor. Yeni: harf kapsamı (her dilin her harfi bir fontta), yedek font satır ölçüleri, `TextFitPlayTests` (5).

**Kalan:** Yayından önce 6 dilin ana dili konuşan biri tarafından okunması; Türkçe geliştirici tarafından gözden geçirilir. Sıradaki aday dil Geleneksel Çince.
