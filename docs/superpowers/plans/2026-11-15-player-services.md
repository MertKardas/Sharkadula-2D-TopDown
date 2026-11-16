# Oyuncu servisleri ve SO ayarları

Kullanıcı component davranışlarının servis mantığına taşınmasını ve gerekli
değerlerin ScriptableObject ile ayarlanmasını istedi. Tam uygulama yetkisi sürer.

## Sorumluluklar

- PlayerConfiguration yalnız başlangıç ayarlarını tutar: hareket/ivme, azami
  can, envanter slotları/başlangıç parası, input eşikleri, etkileşim yarıçapı,
  animasyon çarpanı ve AudioData referansları. Runtime bu asseti değiştirmez.
- PlayerMovementService, PlayerHealthService, PlayerInventoryService ve
  PlayerInteractionService saf C# sınıflarıdır; MonoBehaviour değillerdir.
- Kullanıcının düzeltmesi: yeni servisler VContainer'a kaydedilmez, factory
  kullanılmaz. Controller SO ile kendi PlayerServices grubunu oluşturur ve
  sahiplenir. Paylaşılan SO oyuncu durumunu paylaşmaz. Mevcut global servis
  bağlantıları proje düzeninde kalır.
- Unity adaptörleri yalnız Rigidbody, IDamageable/trigger ve Animator/ses
  bağlantılarını taşır. Runtime işi kalmayan InventoryComponent kaldırılır.
- UI, dünya nesneleri ve kayıt adaptörü doğrudan ilgili oyuncu servislerini
  kullanır. Controller giriş/durum/yaşam döngüsünü bağlamaya devam eder.

Mevcut sahne değerleri önce capture edilir. Configuration Unity ile oluşturulur
ve Episode oyuncusuna bağlanır. Sahne kaydının ilgisiz yeniden yazımları baseline
ile ayıklanır. Mevcut script GUID'leri ve kayıt DTO/state adları korunur;
envanter component'i kaldırılmadan önce tek sahne referansı giderilir.

## Sonuç — 16 Kasım 2026

- Mevcut sahne değerleri SO assetine aktarıldı ve oyuncuya bağlandı.
- Controller kendi dört servisini oluşturuyor; yeni VContainer kaydı veya
  factory yok. Envanter MonoBehaviour ve tek scene bağlantısı kaldırıldı.
- ConfigurationAndIsolation PASS; input deadzone alt sınırı önce RED, sonra
  GREEN. Runtime/SO izolasyonu, mevcut DTO ile restore ve config validasyonu
  doğrulandı.
- InvalidConfigurationStartup önce RED, sonra GREEN. Bozuk SO, oyuncuyu kapatır
  ve mevcut güvenli sahne başlangıcı reddine nedenini iletir.
- Oyuncu 11/11, sahne referansları, legacy/dinamik restore, ES3 StorageRoundTrip
  ve HUD/restart PASS. Bağımsız incelemede önemli yeni regresyon bulunmadı;
  küçük analog giriş sınırı inceleme sonrası testle giderildi.
- Editor derlemesi başarılı. Player C# derlemesi mevcut Input System
  Unity.Scripting/PreserveAttribute paket hatası nedeniyle başarısız; tam build
  veya klavye otomasyonu başarılı olarak raporlanmaz.

## Doğrulama sırası

1. Servis/config mimari testi ve paylaşılmayan runtime durum testi: RED → GREEN.
2. SO ve yerel servis grubu, servis çıkarımı, Unity adaptörleri ve bütün çağıranlar.
3. Sahne ayar aktarımı; eski component bağlarının giderilmesi, GUID doğrulaması.
4. Editor derlemesi; config etkisi/izolasyonu, mevcut oyuncu testleri, sahne,
   ES3/legacy ve UI restart kontrolleri; bağımsız inceleme ve belge güncellemesi.

Kayıttaki hareket ve can değerleri eski sözleşmedeki gibi yüklenir; SO'ya
yazılmaz. Yeni saldırı mekaniği, genel servis arayüzleri ve paket değişikliği yok.
