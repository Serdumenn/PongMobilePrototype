# Yayın listesi (Google Play, yalnız Android)

Durum: 2026-10-05. Projede yapılması gerekenler bitti. Aşağıdaki adımlar senin hesaplarında yapılır; ben şifre giremem ve hesaplara giriş yapamam. 🔁 işaretli adımlardan sonra bana bilgi ver, projede ayarlarını ben yaparım.

## Projede hazır olanlar

| Konu | Durum |
|---|---|
| Paket adı | `com.SERDUMEN.PingiPongi`, sürüm 1.0.0, hedef API 36, en düşük API 25, IL2CPP, ARMv7 + ARM64 |
| İmzalı AAB | `.github/workflows/release-android.yml`: `v1.0.0` gibi bir etiket push edince (ya da Actions'tan elle) testler koşar, imzalı `.aab` ve sembol dosyası üretilir. Sürüm kodu 1000 + çalıştırma numarası |
| Google Play Games girişi | Play Games eklentisi 2.3.0. Açılışta otomatik giriş yapılır. Unity hesabına bağlanır; yeni telefonda aynı hesaba dönülür. Ayarlar'da "Google Play Games" satırı ve kopyalanabilir **Oyuncu ID** var |
| İlerleme yedeği | Rekorlar, istatistikler, açılan kostümler, Pass/Reklamsız durumu Unity Cloud Save'e yedeklenir. Yeni telefonda birleşir; satın alımları ayrıca Google Play geri yükler |
| Reklam onayı (GDPR) | Google UMP: Avrupa'da ilk açılışta onay penceresi, Ayarlar'da "Privacy choices" |
| Gizlilik politikası | `docs/privacy/index.html` (İngilizce + Türkçe). Ayarlar'da "Privacy policy" bağlantısı var |
| Mağaza materyalleri | `store-listing.md` (8 dil), `store/store_icon_512.png`, `store/feature_graphic.png`, `store/screenshots/` (EN + TR, 7'şer adet) |
| Form cevapları | `data-safety.md` (Veri güvenliği, IARC, hedef kitle) |

## Senin adımların (sırayla)

### 1. İletişim e-postası 🔁
Play'de herkese görünecek bir destek e-postası seç; kişisel adresin olmak zorunda değil. Bana yaz; gizlilik politikasındaki `[CONTACT_EMAIL]` alanlarını dolduracağım.

### 2. Gizlilik politikasını yayınla
GitHub → repo → Settings → Pages → **Deploy from a branch** → `main` / `/docs` → Save. Birkaç dakika sonra adres çalışır:
`https://serdumenn.github.io/PongMobilePrototype/privacy/`

### 3. Yükleme anahtarı (upload key) ve GitHub secret'ları
Bilgisayarında bir kez çalıştır (JDK'daki `keytool`; Unity'nin OpenJDK'sı da olur). Şifreyi sen belirle ve sakla:

```
keytool -genkeypair -v -keystore upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

Sonra keystore'u base64'e çevir (PowerShell):

```
[Convert]::ToBase64String([IO.File]::ReadAllBytes("upload.keystore")) | Set-Content upload.b64
```

GitHub → Settings → Secrets and variables → Actions → New repository secret:

| Ad | Değer |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | `upload.b64` dosyasının içeriği |
| `ANDROID_KEYSTORE_PASS` | keystore şifresi |
| `ANDROID_KEYALIAS_NAME` | `upload` |
| `ANDROID_KEYALIAS_PASS` | anahtar şifresi |

`upload.keystore` dosyasını **repoya koyma**. Kaybetme; yedekle.

### 4. Play Console'da uygulama
1. Play Console → Create app → ad **Pingi Pongi**, varsayılan dil **English (United States)**, Game, Free.
2. App signing: Google'ın önerdiği "Play App Signing" açık kalsın.
3. GitHub'da `v1.0.0` etiketi oluştur ve push et. Actions bitince **PingiPongi-1.0.0-…** artifact'ından `.aab` dosyasını indir.
4. Testing → **Internal testing** → yeni sürüm → `.aab` dosyasını yükle, kendini test kullanıcısı ekle.
5. Setup → App integrity → App signing: **SHA-1** değerlerini not al (hem "App signing key" hem "Upload key"). 5. adımda lazım.

> Kişisel geliştirici hesabı 13 Kasım 2023'ten sonra açıldıysa Google, üretime çıkmadan önce **en az 12 test kullanıcısıyla 14 gün kapalı test** ister. O durumda internal testten sonra Closed testing aç, 12 kişiyi ekle ve 14 gün beklet.

### 5. Google Play Games Services 🔁
1. Play Console → Play Games Services → Setup and management → Configuration → **Create new Play Games Services project** (yeni Google Cloud projesi oluşturabilir).
2. Credentials:
   - **Android** kimlik bilgisi: paket adı `com.SERDUMEN.PingiPongi`, SHA-1 = App signing key SHA-1. Upload key SHA-1 için ikinci bir Android kimlik bilgisi ekle.
   - **Game server** kimlik bilgisi: Google Cloud'da "Web application" türünde OAuth istemcisi oluştur ve bağla. **Client ID** ve **Client secret** değerlerini al.
3. Testers sekmesine kendini ekle, sonra yapılandırmayı **Publish** et.
4. Configuration sayfasındaki **Get resources** düğmesinden Android XML'ini kopyala.
5. **Bana gönder:** Android XML'i ve Web Client ID. Unity'deki Play Games kurulumunu ben yaparım.

### 6. Unity Cloud panosu
Unity Cloud → Pingi Pongi projesi → Authentication → Identity providers → **Add → Google Play Games** → 5. adımdaki Web **Client ID** ve **Client secret** değerlerini gir → Save. Bu yapılmadan Play Games girişi Unity hesabına bağlanamaz (oyun yine anonim hesapla çalışır).

### 7. AdMob 🔁
1. AdMob → Apps → Pingi Pongi (uygulama kimliği projede zaten var: `ca-app-pub-6371794166256775~9435282405`).
2. Ad units: **Rewarded** ve **Interstitial** birimi oluştur. **Bana iki birim kimliğini gönder**; şu an test birimleri kullanılıyor ve bunlar gelir getirmez.
3. Privacy & messaging → **GDPR** mesajı oluştur ve yayınla (dil: otomatik). İstersen ABD eyaletleri mesajını da aç.
4. Play mağaza sayfası yayınlanınca AdMob'da uygulamayı mağazaya bağla. app-ads.txt bir web sitesi gerektirir; şimdilik atlanabilir.

### 8. Uygulama içi ürünler
Monetize → Products → In-app products. 15 ürün oluştur, hepsi tek seferlik. Fiyatlar `docs/design/cosmetics.md` tablosundan.

| Ürün kimliği | Ad | Önerilen fiyat |
|---|---|---|
| `pingi_pass` | Pingi Pass | $4.99 |
| `remove_ads` | Remove ads | $2.99 |
| `skin_minty`, `skin_grape`, `skin_sunny`, `paddle_coral`, `paddle_candy`, `theme_mint` | Minty, Grape, Sunny, Coral paddle, Candy paddle, Mint theme | $0.99 |
| `skin_panda`, `skin_kitty`, `paddle_wood`, `theme_sunset` | Panda, Kitty, Wood paddle, Sunset theme | $1.49 |
| `skin_froggy`, `paddle_rainbow`, `theme_night` | Froggy, Rainbow paddle, Night theme | $1.99 |

### 9. Mağaza sayfası ve formlar
- **Main store listing:** `store-listing.md` metinleri. Her dili Translations → Add translation ile ekle.
- **Görseller:** ikon `store/store_icon_512.png`, tanıtım `store/feature_graphic.png`, telefon ekran görüntüleri `store/screenshots/` (Türkçe sayfaya `tr_*`, diğerlerine `en_*`).
- **Privacy policy URL:** `https://serdumenn.github.io/PongMobilePrototype/privacy/`
- **App content:** Data safety, Content rating, Target audience, Ads, App access → `data-safety.md` cevapları.
- **Store settings:** Category Games → Arcade; iletişim e-postası 1. adımdaki adres.

### 10. Yayından hemen önce
- Unity Cloud → Leaderboards → classic, rush, daily, coop panolarını **Reset** et (geliştirme sırasında Editor hesabıyla gelen skorlar silinsin).
- Internal testte kontrol et:
  - Play Games girişi; Ayarlar'da "Connected" görünmeli.
  - Reklam onay penceresi (VPN ile AB'den denenebilir).
  - Bir satın alma: test kartıyla; lisans test kullanıcısı olarak ekle.
  - Uygulamayı silip yeniden kurunca ilerlemenin geri gelmesi.
- Hepsi tamamsa Production → Create release → aynı `.aab` → Rollout.

## Benim yapacaklarım (bilgiler gelince)
- Play Games XML'i ve Web Client ID ile Unity kurulumunu yapmak: `GooglePlayGamesManifest.androidlib` ve `GameInfo` dosyaları.
- Gerçek AdMob birim kimliklerini sahneye girmek.
- Gizlilik politikasına e-postayı yazmak.
- Yeni etiketle yayın derlemesini başlatmana yardım etmek.
