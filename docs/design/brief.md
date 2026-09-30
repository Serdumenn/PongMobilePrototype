# Creative Brief — Pingi Pongi

*v1 · 2026-09-30*

## Oyun tek cümlede
Topu tek parmakla havada tut: her raket vuruşu bir puan, top her vuruşta biraz daha hızlanır, düşürünce el biter.

## Platform ve format
- Android, dikey (portrait), tek el / tek başparmak
- Oturum süresi 30–90 saniye; ölümden sonra 1 saniyede yeniden başlama
- Gelir: geçiş reklamı (3 dakika aralıkla), UMP onay ekranı

## Hedef kitle
- 13+ genel casual oyuncu; bekleme anlarında, kısa oturumlarla oynayan
- Bilinçli olarak çocuklara yönelik değil: Families politikası ve reklam kısıtlarından kaçınmak için

## Deneyim hedefi
Üç kelime: **akıcı · tatmin edici · "bir el daha"**

Oyuncu her vuruşta küçük bir ödül hissetmeli (ses + titreşim + görsel tepki), kaybettiğinde suçu kendinde görmeli ve hemen tekrar denemek istemeli.

## Tasarım ilkeleri
1. **Oyun alanı kahramandır.** Oyun sırasında ekranda sadece skor ve pause kalır.
2. **Tek başparmak.** Kontrol alt yarıda; sık kullanılan butonlar başparmak bölgesinde.
3. **Her vuruş hissedilir.** Ses, titreşim ve görsel geri bildirim birlikte çalışır.
4. **Sıfır sürtünme.** Menüden oyuna 1 dokunuş, ölümden tekrar denemeye 1 dokunuş.
5. **Okunabilirlik önce.** Kontrast en az 4.5:1, dokunma hedefi en az 48dp.

## Kısıtlar
- Unity 6.3 LTS, UI Toolkit, Android API 36, 16 KB sayfa desteği
- AAB boyutu < 40 MB, 60 FPS sabit
- Tüm fontlar ve görseller ticari kullanıma ve yeniden dağıtıma uygun lisanslı (repo public)

## Kapsam dışı (v1)
Çok oyunculu mod, çevrimiçi hesap, sanal para birimi.

*2026-09-30 güncellemesi: kostüm koleksiyonu ve uygulama içi satın alma kapsama alındı; bkz. [cosmetics.md](cosmetics.md).*

## Açık sorular (Faz 3'te karara bağlanacak)
- Sıralama tablosu: Google Play Games entegrasyonu mu, yoksa yalnızca yerel en iyi skor mu?
- "Beğen" butonu: uygulama içi değerlendirme (In-App Review) mi, yoksa kaldırılsın mı?
- Ödüllü reklamla "devam et" seçeneği eklensin mi?
