# Oyuncu mimarisinin düzenlenmesi

Kullanıcının düzeltmesi: yalnız hata düzeltmesi yeterli değil; ana karakter
scriptleri yapısal olarak düzenlenecek. Önceki tam uygulama yetkisi sürüyor.

## Seçilen düzen

Mevcut Unity bileşenleri ve kayıt türleri korunur. Yeni genel karakter çatısı
veya ScriptableObject durum sistemi kurmak yerine mevcut sınırlar netleştirilir:

- PlayerController tek giriş okuma ve yaşam döngüsü/composition noktasıdır.
  Bileşen referansları dışarıdan yalnız okunabilir. Unity eski alan adlarını
  FormerlySerializedAs ile yükler. Mevcut çağıranların erişim adları korunur.
- PlayerSaveAdapter kayıt DTO eşlemesini ve giriş konumu politikasını taşır;
  controller ISavable girişini tutar, yeni sahne bileşeni gerektirmez.
- Durumlar servis almaz. LocomotionState Idle/Move/Run seçimini tek yerde yapar.
  RunState, MoveState'in ortak hareket/sunum yaşam döngüsünü paylaşır.
- Hareket yalnız Rigidbody2D alır, sunum kendi referanslarını kurar. Health,
  Inventory ve Attack'ın boş Initialize metotları ve IComponent kaldırılır.
- Altı MonoBehaviour Components altında, kayıt adaptörü Persistence altında,
  InventoryItem veri sınıfı Inventory altında ayrı dosyada bulunur.

Meta dosyaları taşınan scriptlerle birlikte taşınır; GUID'ler değişmez.
PlayerSaveData, StateData, InventoryItem ve kayıtlı state adları değişmez.
Yeni saldırı işlevi, paket yükseltmesi ve kayıt formatı değişikliği kapsam dışıdır.

## Adımlar

1. Mimari sözleşme ve deterministik locomotion geçiş testlerini ekle; mevcut
   kodda eksik mimarinin FAIL verdiğini gör.
2. Kayıt eşlemesini ayır, giriş ve servis aboneliklerini controller'da topla;
   durumları ve bileşen başlangıcını yeni sınırlara uyarla.
3. MonoBehaviour dosyalarını metalarıyla taşı, modeli ayır, sahne referanslarını
   ve eski serialized adlarının okunmasını kontrol et.
4. Editor derlemesi; 9 mevcut oyuncu regresyonu, yeni mimari/locomotion ve
   sahne referansları, ES3/legacy kayıt ve UI restart kontrolleri.
5. Bağımsız kod incelemesi, belge/path güncellemeleri, diff/bağlantı kontrolü.

Başlangıç kopyası Temp/PlayerArchitectureBaseline/Player. Önceki kullanıcı
değişiklikleri ve ilk turdaki doğrulanmış düzeltmeler korunur. Bilinen klavye
otomasyon sınırı ve player paket derleme sorunu başarılı gösterilmez.

## Doğrulanan sonuç

- Mimari sözleşme ve merkezi locomotion testleri önce RED, sonra GREEN.
- Son PlayerRegressionChecks.Run: 11/11 PASS. Gerçek Animator ölüm olayı dahil.
- SceneReferences: üç build sahnesi, bir oyuncu, eski serialize alanlarının
  yeni property'lere yüklenmesi PASS; sahneler kaydedilmedi.
- ES3 StorageRoundTrip, legacy InventoryItem/dinamik restore ve HUD/restart PASS.
- Editor derlemesi PASS; altı component'in meta içerikleri birebir korundu.
- Bağımsız inceleme önemli yeni regresyon bulmadı. Klavye otomasyonu ve player
  paket derleme sınırı önceki oturumdaki gibi açık bırakıldı; bunlar bu yapısal
  değişikliğin başarılı kontrolleri arasında sayılmaz.
- Yeni saldırı mekaniği veya genel karakter framework'ü eklenmedi. Mevcut
  çalışma ağacı korunarak commit/merge yapılmadan teslim edildi.
