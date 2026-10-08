# Yayın kılavuzu (Google Play, yalnız Android)

Durum: 2026-10-06. Proje tarafı hazır. Aşağıdaki adımlar Play Console, Google Cloud, AdMob, Unity Cloud ve GitHub hesaplarında yapılır.

## Projede hazır olanlar

| Konu | Durum |
|---|---|
| Paket adı | `com.oceanforge.pingipongi`, sürüm 1.0.0, hedef API 36, en düşük API 25, IL2CPP, ARMv7 + ARM64 |
| İmzalı AAB | `.github/workflows/release-android.yml`: `v1.0.0` gibi bir etiket push edilince (ya da Actions'tan elle) testler koşar, imzalı `.aab` ve sembol dosyası üretilir. Sürüm kodu 1000 + çalıştırma numarası |
| Google Play Games girişi | Play Games eklentisi 2.3.0. Açılışta otomatik giriş; Unity hesabına bağlanır, yeni telefonda aynı hesaba dönülür. Ayarlar'da "Google Play Games" satırı ve kopyalanabilir Oyuncu ID'si var |
| İlerleme yedeği | Rekorlar, istatistikler, açılan kostümler, Pass ve Reklamsız durumu Unity Cloud Save'e yedeklenir ve yeni telefonda birleşir. Satın alımları ayrıca Google Play geri yükler |
| Reklam onayı (GDPR) | Google UMP: Avrupa'da ilk açılışta onay penceresi, Ayarlar'da "Privacy choices" |
| Web sitesi | Kişisel hesapta ayrı repo `serdumenn.github.io`, sayfalarda Ocean Forge adıyla: stüdyo sayfası, oyun sayfası, gizlilik politikası, hesap silme sayfası, `app-ads.txt`. Ayarlar'daki "Privacy policy" bağlantısı buraya gider |
| Lisans | Tescilli ("All rights reserved"); repo herkese açık ama izinsiz kullanım yok |
| Mağaza materyalleri | `store-listing.md` (8 dil), `store/store_icon_512.png`, `store/feature_graphic.png`, `store/screenshots/` (EN + TR, 7'şer adet) |
| Form cevapları | `data-safety.md` (Veri güvenliği, IARC, hedef kitle) |

## Hesap adımları

### 1. İletişim e-postası (tamam)
`fatihtas.contact@gmail.com`. Play Console'da ve gizlilik politikasında bu adres kullanılır.

### 2. Web sitesi (Ocean Forge)
1. GitHub Desktop → **File → Add local repository** → `Documents\GitHub\serdumenn.github.io`. "This directory does not appear to be a Git repository" uyarısında **create a repository** → Create repository.
2. İlk commit: `Initial site` → **Publish repository**. Ad `serdumenn.github.io` olarak kalır, Organization seçilmez (kişisel hesap), **Keep this code private** kapalı → Publish.
3. github.com/Serdumenn/serdumenn.github.io → Settings → Pages → **Deploy from a branch** → `main` / `(root)` → Save.

Birkaç dakika sonra adresler çalışır:

| Sayfa | Adres |
|---|---|
| Stüdyo | `https://serdumenn.github.io/` |
| Oyun | `https://serdumenn.github.io/pingi-pongi/` |
| Gizlilik politikası | `https://serdumenn.github.io/pingi-pongi/privacy/` |
| Hesap silme | `https://serdumenn.github.io/pingi-pongi/delete-account/` |
| app-ads.txt | `https://serdumenn.github.io/app-ads.txt` |

Oyun reposunun GitHub sayfasında **About** (sağdaki dişli): açıklama "A cheerful paddle game for Android by Ocean Forge.", Website `https://serdumenn.github.io/pingi-pongi/`. "Releases" ve "Packages" işaretli kalır, "Deployments" kaldırılabilir.

### 3. Yükleme anahtarı ve GitHub secret'ları (tamam)
Anahtar `keytool` ile oluşturulur (Unity'nin OpenJDK'sında vardır) ve repo dışında saklanır:

```
keytool -genkeypair -v -keystore upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

Base64'e çevirme (PowerShell):

```
[Convert]::ToBase64String([IO.File]::ReadAllBytes("upload.keystore")) | Set-Clipboard
```

GitHub → Settings → Secrets and variables → Actions:

| Ad | Değer |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | keystore'un base64 hâli |
| `ANDROID_KEYSTORE_PASS` | keystore şifresi |
| `ANDROID_KEYALIAS_NAME` | `upload` |
| `ANDROID_KEYALIAS_PASS` | anahtar şifresi |

`upload.keystore` repoya konmaz ve yedeklenir.

### 4. Play Console'da uygulama
1. Play Console → Create app → ad **Pingi Pongi**, varsayılan dil **English (United States)**, Game, Free.
2. App signing: "Play App Signing" açık kalır.
3. Test sürümü: GitHub → Actions → **Release Android (signed AAB)** → **Run workflow** → Version `1.0.0`. Üretim sürümü: `v1.0.0` etiketi push edilir. Actions bitince **PingiPongi-1.0.0-…** artifact'ından `.aab` indirilir.
4. Testing → **Internal testing** → yeni sürüm → `.aab` yüklenir, test kullanıcıları eklenir.
5. Setup → App integrity → App signing: **SHA-1** değerleri not edilir ("App signing key" ve "Upload key"). 5. adımda gerekir.

Kişisel geliştirici hesaplarında (13 Kasım 2023'ten sonra açılanlar) üretimden önce **en az 12 test kullanıcısıyla 14 günlük kapalı test** zorunludur: internal testten sonra Closed testing açılır, 12 kişi eklenir ve 14 gün beklenir.

### 5. Google Play Games Services (tamam, yayınlama hariç)
| Konu | Değer |
|---|---|
| Play Games projesi / APP_ID | `987043051187` |
| Google Cloud projesi | `pingi-pongi-511004` (sahibi Play Console hesabı) |
| OAuth onay ekranı | External, **In production**; ana sayfa ve gizlilik bağlantıları sitede; logo yok (doğrulama gerekmez) |
| Android kimlik bilgisi | `987043051187-t0hmh2pmv67foi7ciquaaettf9ps98fh.apps.googleusercontent.com`, App signing SHA-1 `6B:2C:47:54:88:6C:10:EA:23:4F:17:52:5E:92:2B:93:6E:E8:45:C1`, korsanlıkla mücadele açık |
| Oyun sunucusu kimlik bilgisi (Web) | `987043051187-1m1kb6go03pc8gm5connv0t599ua5bd8.apps.googleusercontent.com`. Client secret yalnızca indirilen JSON'da ve Unity Cloud'da durur, repoya girmez |

Unity tarafı: `Assets/GooglePlayGames/Resources/PlayGamesSettings.asset` (APP_ID + Web Client ID), `Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml` (APP_ID meta-data), `ProjectSettings/GooglePlayGameSettings.txt`. Bu dosyalar Unity'de **Window → Google Play Games → Setup → Android setup** penceresinin ürettiği içerikle aynıdır.

Kalan: Play Console → Play Oyun Hizmetleri → **İncele ve yayınla**. Yayınlanana kadar yalnızca Play Games test kullanıcıları giriş yapabilir.

Play Games girişi yalnızca Play'den kurulan sürümlerde çalışır (imza SHA-1'i Play'in anahtarı). CI'nın geliştirme APK'sında giriş başarısız olur, oyun anonim hesapla devam eder.

### 6. Unity Cloud panosu (tamam)
Unity Cloud → Pingi Pongi → Products → Player Authentication → Identity Providers → **Google Play Games**: oyun sunucusu Client ID + secret, durum **Enabled**.

"Put your data to work" (Developer Data) penceresi kabul edilmedi. Kabul edilirse Veri güvenliği formu ve gizlilik politikası güncellenmelidir.

### 7. AdMob
1. AdMob → Apps → Pingi Pongi. Yayıncı `pub-3792906065455391`, uygulama kimliği `ca-app-pub-3792906065455391~1899985859` (projede `GoogleMobileAdsSettings`). (tamam)
2. Ad units (tamam): **Rewarded - Unlock** `ca-app-pub-3792906065455391/7924262989`, **Interstitial - Between games** `ca-app-pub-3792906065455391/8179413854`. `Game.unity` içindeki `AdManager` alanlarında. Geliştiricinin telefonu AdMob → Ayarlar → **Test cihazları** listesinde olmalı; kendi reklamına tıklamak hesabı riske atar.
3. Privacy & messaging → **GDPR** mesajı oluşturulup yayınlanır (dil: otomatik). İsteğe bağlı: ABD eyaletleri mesajı.
4. Mağaza sayfası yayınlanınca AdMob'da uygulama mağazaya bağlanır. `app-ads.txt` sitede hazır; Play Console'da **Website** alanı `https://serdumenn.github.io` olunca AdMob bir gün içinde doğrular (Apps → View all apps → app-ads.txt sütunu).

### 8. Uygulama içi ürünler
Monetize → Products → In-app products. 15 ürün, hepsi tek seferlik. Fiyatlar `docs/design/cosmetics.md` tablosundan.

| Ürün kimliği | Ad | Önerilen fiyat |
|---|---|---|
| `pingi_pass` | Pingi Pass | $4.99 |
| `remove_ads` | Remove ads | $2.99 |
| `skin_minty`, `skin_grape`, `skin_sunny`, `paddle_coral`, `paddle_candy`, `theme_mint` | Minty, Grape, Sunny, Coral paddle, Candy paddle, Mint theme | $0.99 |
| `skin_panda`, `skin_kitty`, `paddle_wood`, `theme_sunset` | Panda, Kitty, Wood paddle, Sunset theme | $1.49 |
| `skin_froggy`, `paddle_rainbow`, `theme_night` | Froggy, Rainbow paddle, Night theme | $1.99 |

### 9. Mağaza sayfası ve formlar
- **Main store listing:** `store-listing.md` metinleri. Her dil Translations → Add translation ile eklenir.
- **Görseller:** ikon `store/store_icon_512.png`, tanıtım `store/feature_graphic.png`, telefon ekran görüntüleri `store/screenshots/` (Türkçe sayfaya `tr_*`, diğerlerine `en_*`).
- **Privacy policy URL:** `https://serdumenn.github.io/pingi-pongi/privacy/`
- **Website:** `https://serdumenn.github.io` (app-ads.txt için gerekli).
- **Data safety → Delete account URL:** `https://serdumenn.github.io/pingi-pongi/delete-account/`
- **App content:** Data safety, Content rating, Target audience, Ads, App access → `data-safety.md`.
- **Store settings:** Category Games → Arcade; iletişim e-postası 1. adımdaki adres, web sitesi yukarıdaki adres.

### 10. Yayından hemen önce
- Unity Cloud → Leaderboards → classic, rush, daily, coop panoları **Reset** edilir (geliştirme sırasındaki skorlar silinir).
- Internal testte kontrol:
  - Play Games girişi; Ayarlar'da "Connected" görünür.
  - Reklam onay penceresi (VPN ile AB'den denenebilir).
  - Bir satın alma (lisans test kullanıcısı ve test kartı).
  - Uygulama silinip yeniden kurulunca ilerlemenin geri gelmesi.
- Hepsi tamamsa Production → Create release → aynı `.aab` → Rollout.

## Sürüm ve commit düzeni
- `main` her zaman derlenebilir durumda tutulur; her push'ta testler ve geliştirme APK'sı koşar.
- Commit mesajı İngilizce, emir kipinde ve kısa: ilk satır en fazla ~60 karakter (`Add friend invite banner`, `Fix rush timer on resume`). Gerekirse boş bir satırdan sonra açıklama.
- Bir commit tek bir konuyu kapsar; ilgisiz değişiklikler ayrı commit'lere bölünür.
- Yayın: GitHub Desktop → **History** → yayınlanacak commit'e sağ tık → **Create Tag** → `v1.0.1` → **Push origin**. Etiket imzalı AAB'yi üretir; sürüm adı etiketten alınır, sürüm kodu kendiliğinden artar (Player Settings'e dokunmak gerekmez).
- GitHub → Releases → **Draft a new release** → aynı etiket → kısa sürüm notu ("What's new"). Play Console'daki "Release notes" ile aynı metin kullanılır.
