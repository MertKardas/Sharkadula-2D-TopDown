# UI tutarlılığı ve PrimeTween geçişi

## Amaç ve sınırlar

Mevcut uGUI görünümü, içerikler ve yerleşim korunacak. Kullanıcının istediği
iyileştirme; güvenilir panel yönetimi, tutarlı giriş odağı ve LeanTween yerine
PrimeTween kullanılmasıdır. Gerekli sınıf değişiklikleri yapılabilir.

Yeni ekran, kayıt slotu sistemi, DI dönüşümü veya genel UI framework'ü eklenmeyecek.
Sahne/prefab GUID'leri ve mevcut UnityEvent bağlantıları korunacak. Kullanıcının
mevcut paket ve proje ayarı değişiklikleri korunacak.

## Yaklaşımlar

1. Yalnız tween çağrılarını değiştirmek: küçük, ancak mevcut Esc ve pause
   tutarsızlıklarını çözmediğinden yetersiz.
2. Mevcut bileşenleri düzeltmek, panel durumunu GameUI içinde toplamak:
   önerilen yaklaşım. Görünümü ve Inspector bağlantılarını korurken davranışın
   tek yerden yönetilmesini sağlar.
3. Yeni panel taban sınıfları ve genel gezinme sistemi kurmak: mevcut üç oyun
   içi panel için gereksiz ek kapsam; önerilmiyor.

İkinci yaklaşım kullanıcı tarafından 14 Kasım 2026'da onaylandı ve uygulandı.

## Önerilen davranış

- GameUI oyun içindeki Esc girişini tek başına yönetir. Oyun sırasında Esc
  pause açar; pause sırasında oyuna döner; ayarlarda pause ekranına döner.
  Ölüm ve bitiş sırasında Esc oyun ekranını açmaz.
- Pause açılırken oyuncu girdisi ve oyun zamanı mevcut GameManager metotlarıyla
  durdurulur. Ayarlara girip çıkarken pause korunur; devam etmede geri açılır.
  Oynanış sahnesi yüklenince CurrentGameState da Playing yapılır; eski ölüm
  durumu yeni sahnenin Esc yönetimini engellemez.
- Geçişler sırasında yinelenen istekler birbiriyle yarışmaz. Ölüm ve sahne
  bitişi normal menü geçişine üstün gelir. Hedef panel, arka plan, alpha,
  interactable ve blocksRaycasts her geçiş sonunda birlikte tutarlıdır.
- UI animasyonları PrimeTween 1.5.2 kullanır; pause sırasında çalışan
  animasyonlar ölçeklenmemiş zamanla yürür. Bileşen kapanınca kendi animasyonu
  ve gecikmiş callback'leri iptal edilir.
- Butonlar fare ve klavye seçiminde aynı vurgu davranışını kullanır. Seçim
  kaybında ve panel kapanışında ölçek sıfırlanır; çocuk butonlar birbirlerinin
  seçimini temizlemez. Yinelenen ses/click abonelikleri giderilir.
- HUD can/para, envanter ve kayıt bildirimi yeniden açılınca güncel durumu
  gösterir. UI olay abonelikleri kendi yaşam döngülerinde kaldırılır.
- Sahne fade ve SquareHighlight da geçirilir. GameManager içindeki fade
  çağrısı yeni dönüş tipine uyarlanır. Ölüm ekranının başarılı kayıt yükledikten
  sonra yeniden başlamayan akışı mevcut kayıt modeli korunarak düzeltilir.
- LeanTween ile PrimeTween easing enum sayıları eşdeğer varsayılmaz;
  StandardTransition asseti ve Door prefabındaki değerler anlamlarına göre
  dönüştürülür. Asset düzenlemeleri bağlı Editor üzerinden yapılır.

## Etkilenecek alanlar

Assets/Scripts/UI altındaki paneller, geçiş ve buton bileşenleri;
Assets/Scripts/UI/Common/FadeInScene.cs, Assets/Scripts/World/SquareHighlight.cs ve bunların
GameManager çağrıları; ilgili UITransition asseti, sahne/prefab bağlantıları.
AudioManager içindeki ses fade'leri bu UI geçişinin dışında kalır;
bu nedenle LeanTween dağıtımı bu çalışmada silinmez.
Geçiş testinde ses fade'inin ilk kez sahne kapanırken LeanTween yöneticisini
oluşturduğu görüldü. Bu hatayı önlemek için AudioManager yöneticiyi Awake'ta
başlatır; SceneMusic kapanışta singleton oluşturmaz ve uygulama çıkışında
fade başlatmaz. Ses fade algoritmaları değiştirilmez.

## Doğrulama

Panel durumu değişiklikleri için küçük bir regresyon kontrolü eklenecek;
runtime dosyalarına NUnit veya koşulsuz UnityEditor bağımlılığı eklenmeyecek.
Unity derlemesi ve Play Mode'da MainMenu → Episode(01), Esc aç/kapat,
pause → ayarlar → geri, hızlı yinelenen giriş, ölüm → yeniden başlat,
HUD/envanter güncellemesi ve EndGame dönüşü kontrol edilecek.
Uygun platform desteği varsa player build alınacak; çalıştırılamayan kontrol
açıkça raporlanacak. git diff --check ve değişiklik kapsamı incelenecek.

Başlangıç Console kayıtlarında URP test dosyaları için eksik meta hataları var;
bunlar yeni UI hatalarından ayrı takip edilecek. Sıfır hata iddiası yalnız
çalıştırılan senaryoların sonuçlarıyla sınırlandırılacak.
