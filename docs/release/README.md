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
| Web sitesi | Ayrı repo `oceanforge-games.github.io` (Ocean Forge Games organizasyonu): stüdyo sayfası, oyun sayfası, gizlilik politikası, hesap silme sayfası, `app-ads.txt`. Ayarlar'daki "Privacy policy" bağlantısı buraya gider |
| Lisans | Tescilli ("All rights reserved"); repo herkese açık ama izinsiz kullanım yok |
| Mağaza materyalleri | `store-listing.md` (8 dil), `store/store_icon_512.png`, `store/feature_graphic.png`, `store/screenshots/` (EN + TR, 7'şer adet) |
| Form cevapları | `data-safety.md` (Veri güvenliği, IARC, hedef kitle) |

## Hesap adımları

### 1. İletişim e-postası (tamam)
`fatihtas.contact@gmail.com`. Play Console'da ve gizlilik politikasında bu adres kullanılır.

### 2. Web sitesi (Ocean Forge Games)
1. GitHub → sağ üst **+** → **New organization** → **Free**. Ad: `oceanforge-games`, iletişim e-postası 1. adımdaki adres, "My personal account".
2. GitHub Desktop → **File → Add local repository** → `Documents\GitHub\oceanforge-games.github.io`. "This directory does not appear to be a Git repository" uyarısında **create a repository** → Create repository.
3. İlk commit: `Initial site` → **Publish repository** → Organization: `oceanforge-games`, **Keep this code private** kapalı → Publish.
4. github.com/oceanforge-games/oceanforge-games.github.io → Settings → Pages → **Deploy from a branch** → `main` / `(root)` → Save.

Birkaç dakika sonra adresler çalışır:

| Sayfa | Adres |
|---|---|
| Stüdyo | `https://oceanforge-games.github.io/` |
| Oyun | `https://oceanforge-games.github.io/pingi-pongi/` |
| Gizlilik politikası | `https://oceanforge-games.github.io/pingi-pongi/privacy/` |
| Hesap silme | `https://oceanforge-games.github.io/pingi-pongi/delete-account/` |
| app-ads.txt | `https://oceanforge-games.github.io/app-ads.txt` |

Oyun reposunun GitHub sayfasında **About** (sağdaki dişli): açıklama "A cheerful paddle game for Android by Ocean Forge.", Website `https://oceanforge-games.github.io/pingi-pongi/`. "Releases" ve "Packages" işaretli kalır, "Deployments" kaldırılabilir.

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
3. GitHub'da `v1.0.0` etiketi oluşturulup push edilir. Actions bitince **PingiPongi-1.0.0-…** artifact'ından `.aab` indirilir.
4. Testing → **Internal testing** → yeni sürüm → `.aab` yüklenir, test kullanıcıları eklenir.
5. Setup → App integrity → App signing: **SHA-1** değerleri not edilir ("App signing key" ve "Upload key"). 5. adımda gerekir.

Kişisel geliştirici hesaplarında (13 Kasım 2023'ten sonra açılanlar) üretimden önce **en az 12 test kullanıcısıyla 14 günlük kapalı test** zorunludur: internal testten sonra Closed testing açılır, 12 kişi eklenir ve 14 gün beklenir.

### 5. Google Play Games Services
1. Play Console → Play Games Services → Setup and management → Configuration → **Create new Play Games Services project**.
2. Credentials:
   - **Android** kimlik bilgisi: paket adı `com.oceanforge.pingipongi`, SHA-1 = App signing key SHA-1. Upload key SHA-1 için ikinci bir Android kimlik bilgisi eklenir.
   - **Game server** kimlik bilgisi: Google Cloud'da "Web application" türünde OAuth istemcisi oluşturulup bağlanır. **Client ID** ve **Client secret** alınır.
3. Testers sekmesine test hesapları eklenir, yapılandırma **Publish** edilir.
4. Configuration sayfasındaki **Get resources** düğmesinden Android XML'i alınır.
5. Unity'de **Window → Google Play Games → Setup → Android setup**: Android XML'i ve Web Client ID girilir, **Setup** çalıştırılır.

### 6. Unity Cloud panosu
Unity Cloud → Pingi Pongi projesi → Authentication → Identity providers → **Add → Google Play Games** → 5. adımdaki Web **Client ID** ve **Client secret** → Save. Bu yapılmadan Play Games girişi Unity hesabına bağlanamaz (oyun anonim hesapla çalışmaya devam eder).

### 7. AdMob
1. AdMob → Apps → Pingi Pongi (uygulama kimliği projede: `ca-app-pub-6371794166256775~9435282405`).
2. Ad units: **Rewarded** ve **Interstitial** birimleri oluşturulur, kimlikleri `GameManager` sahnesindeki `AdManager` alanlarına girilir. Şu an test birimleri kullanılıyor.
3. Privacy & messaging → **GDPR** mesajı oluşturulup yayınlanır (dil: otomatik). İsteğe bağlı: ABD eyaletleri mesajı.
4. Mağaza sayfası yayınlanınca AdMob'da uygulama mağazaya bağlanır. `app-ads.txt` sitede hazır; Play Console'da **Website** alanı `https://oceanforge-games.github.io` olunca AdMob bir gün içinde doğrular (Apps → View all apps → app-ads.txt sütunu).

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
- **Privacy policy URL:** `https://oceanforge-games.github.io/pingi-pongi/privacy/`
- **Website:** `https://oceanforge-games.github.io` (app-ads.txt için gerekli).
- **Data safety → Delete account URL:** `https://oceanforge-games.github.io/pingi-pongi/delete-account/`
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
