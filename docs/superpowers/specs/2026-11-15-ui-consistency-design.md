# UI tutarlılığı

Kullanıcının seçimi: mevcut görünümü koru, kod ve görsel farkları düzelt.

Mevcut uGUI sahneleri, Bungee fontu, renkler, sprite'lar, buton efektleri ve
UnityEvent metot adları korunur. Yeni ekran veya prefab sistemi kurulmaz.

- Üç Canvas 1920×1080 referansıyla, yüksekliğe göre (Match Width Or Height = 1) ölçeklenir.
- Pause/ayarlar panelleri referans çözünürlükteki ölçüleriyle merkezden sabitlenir; ana menü alt öğeleri arka plan yerine Canvas'a bağlanır.
- Aynı eylem her ekranda aynı metni kullanır; buton etiketlerindeki satır sonları temizlenir.
- Odak tercihi etkin ve kullanılabilir bir öğeye düşer; menü yeniden açıldığında yenilenir.
- Ana menünün kayıt kullanılabilirliği her açılışta güncellenir.
- Çıkış işleminin tek sahibi mevcut GameService olur; UI metotları delegasyon yapar.
- Sosyal bağlantılarda mevcut hata yakalama davranışı ana menüde de korunur.

Alternatif: ortak prefab ailesiyle tüm UI'ı birleştirmek. Kullanıcı mevcut
görünümü koruyan hedefli düzeltmeyi seçti; kapsamlı prefab dönüşümü yapılmaz.

Doğrulama: önce ana menü yeniden açılma hatasını regresyonla göster;
ardından UI asset/panel/sahne akışı kontrolleri, Console ve 16:9 / 4:3 / geniş
ekran görüntüleri. Gerçek kullanıcı kayıtlarına dokunma. Tam player build
bu UI revizyonunun doğrulama kapsamında değildir.

Görsel inceleme kararı: 0.5 dengesi ve eski stretch paneller 4:3'te yazıları
taşırdı. Panel ölçülerini koruyup ana menünün mevcut yüksekliğe göre
ölçekleme kuralını paylaşmak 4:3 ve geniş ekranda bu sorunu giderdi.
