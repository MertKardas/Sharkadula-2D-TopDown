# VContainer ile servis mimarisi

## Kullanıcı kararı ve amaç

Kullanıcı, yalnız mevcut manager bileşenlerini container'a kaydetmek yerine
uygun managerların saf C# servislere dönüştürüldüğü kapsamlı yaklaşımı seçti.
Amaç servislerin oluşturulmasını, bağımlılıklarını ve yaşam süresini VContainer'ın
yönetmesi; oyun kodundaki statik `.Instance` erişimlerinin kaldırılmasıdır.
Bu tasarım kullanıcı tarafından onaylandı ve 14 Kasım 2026'da uygulandı.

## Mevcut durum

Unity 6000.5.9f1 ve VContainer 1.19.0 kurulu. GameManager, InputManager,
AudioManager, SaveManager ve ItemDataBase Singleton<T> tabanından gelir.
SaveManager, SaveService ve SceneSaveLoader'ı kendisi oluşturur. UI, oyuncu
durumları ve dünya nesneleri singletonlara erişir. InventoryItem JSON
constructor'ı bile ItemDataBase.Instance kullanır. Önceki PrimeTween/UI
değişiklikleri çalışma alanında korunacaktır.

## Değerlendirilen yaklaşımlar

1. Mevcut MonoBehaviour managerları koruyup injection eklemek: daha az asset
   değişikliği, fakat sistem mantığı Unity bileşen yaşam döngüsüne bağlı kalır.
2. Saf C# servisler ve açık uygulama/sahne scope'ları: kullanıcının seçimi.
   Daha fazla çağrı ve asset değişir; bağımlılıklar ve kapanış sorumlulukları
   servislerin constructor'larında ve IDisposable sözleşmelerinde görünür olur.

## Servis sınırları

| Mevcut | Hedef | Sorumluluk / yaşam süresi |
| --- | --- | --- |
| GameManager | GameService | Oyun durumu, pause, ölüm ve sahne geçişleri; uygulama boyunca tek instance |
| InputManager | InputService | Mevcut InputSystem_Actions, okuma/abonelik ve action-map açma/kapatma; uygulama boyunca tek instance, IDisposable |
| AudioManager | AudioService | Mevcut ses havuzu, müzik, mixer/PlayerPrefs ayarları; uygulama boyunca tek instance, ITickable ve IDisposable |
| SaveManager | GameSaveService | Mevcut kayıt koordinasyonu, CurrentGameSaveData ve bildirim olayları; uygulama boyunca tek instance |
| ItemDataBase | ItemCatalog | Addressables Item etiketiyle yüklenen ItemData kataloğu ve handle sahipliği; uygulama boyunca tek instance, IDisposable |
| SaveService | SaveService | Disk/JSON işlemleri; constructor injection ile oluşturulur, davranışı korunur |
| SceneSaveLoader | SceneSaveLoader | Sahne kayıt toplama/dağıtma ve dinamik eşya oluşturma; katalog ve oluşturma bağımlılıkları inject edilir |

Saf C# burada MonoBehaviour'dan türememek anlamındadır; AudioService ve
GameService gibi Unity'ye özgü servisler AudioSource, Time ve SceneManager
API'lerini kullanabilir. Sırf her sınıfın bir interface'i olsun diye yeni
arayüzler, genel service locator, event bus veya servis sarmalayıcıları eklenmez.

## Oluşturma, ayarlar ve başlangıç sırası

- Kalıcı bir AppLifetimeScope prefabı ve VContainerSettings preload asseti
  oluşturulur. Servisler yalnız burada kaydedilir. Ek bir bootstrap sahnesi
  veya build sahnesi sırası değişikliği gerekmez.
- Her güncel build sahnesine SceneLifetimeScope eklenir. Parent uygulama
  scope'udur; MainMenu, Episode(01) ve EndGame doğrudan Editor'dan da başlatılır.
- SceneLifetimeScope, sahne bileşenlerini ve başlangıçta kapalı UI panellerini
  ilk Awake/OnEnable kullanımlarından önce inject eder. PlayerController,
  durum makinesini servisler verildikten sonra kurar. State sınıfları gerekli
  servisleri constructor parametresiyle alır; container'a kendileri erişmez.
- AudioConfiguration ScriptableObject'i eski AudioManager üzerindeki gerçek
  mixer ve grup referanslarından oluşturulur. Ses kaynakları uygulama scope'una
  ait Transform altında tutulur; temizlik ITickable ile yürür. Ses ayar anahtarları,
  ses içeriği ve mevcut fade davranışı korunur.
- ItemCatalog kayıt okuma ve eşya tüketiminden önce hazır olur. İlk geçişte
  mevcut senkron yükleme korunur; başarısız yükleme sessizce geçilmez. Root
  kapanırken Addressables handle'ı yalnız sahibi tarafından bırakılır.
- SceneLoaded koordinasyonu GameService'de tek bir akış olur: yeni sahnenin
  bileşenleri hazır olduğunda kayıt dağıtılır, oyun/input durumu ayarlanır ve
  oyun başlangıç olayı yayınlanır. Rastgele iki anonim aboneliğin sırasına
  güvenilmez. Kalıcı servisler geçişlerde yeniden oluşturulmaz.
- IDisposable ile InputSystem_Actions, sceneLoaded abonelikleri, aktif sesler,
  ses tween'leri ve havuz kaynakları kapatılır. Sahne/UI OnDisable abonelik
  kaldırmaları, root dispose sırası nedeniyle hata üretmemelidir.

## UI ve dünya nesneleri

UI, PlayerVisualComponent, kapı/sandık/tuzak, ortam/müzik bileşenleri ve DropObject
gibi tüketiciler [Inject] metotlarıyla servis alır. Input eylemi binding'leri,
mevcut MonoBehaviour/animasyon olay metotları ve görsel tasarım korunur.
Buton başına sınıf eklenmez; ortak MenuButtonUI ve controller UnityEvent
bağlantıları devam eder.

GameService GameUI aramaz ve sahne UI nesnesini kalıcı alanda saklamaz.
Bitiş olayını oyun içi UI dinler; mevcut PrimeTween fade tamamlandığında
GameService'e sahne geçişini tamamlama isteği verir. Uygun UI dinleyicisi
yoksa oyun servisinin tanımlı doğrudan geçişi kullanılır. Tekrarlanan
ölüm/bitiş/yeniden başlatma istekleri birden fazla sahne yüklemez.

Dinamik DropObject prefabları VContainer IObjectResolver.Instantiate üzerinden
oluşturulur; injection ilk OnEnable'dan önce tamamlanır. Sadece veri gösteren,
servis kullanmayan envanter slotları için sıradan Instantiate yeterlidir.
Container kullanımı composition scope'ları ve injection destekli nesne
oluşturma noktalarıyla sınırlıdır; oyun koduna Resolve çağrıları dağıtılmaz.

## Kayıt uyumluluğu

- PlayerSaveData, InventoryItem, StateData ve diğer kayıt türlerinin adları,
  namespace/assembly kimlikleri, JSON alanları ve SaveableEntity kimlikleri
  değişmez. Oyuncu state sınıfları yeniden adlandırılmaz.
- InventoryItem JSON constructor'ı yalnız item kimliğini ve miktarını okur;
  statik katalog veya container çağrısı yapmaz. DropObject.Collect ve
  InventoryComponent.AddItem içindeki runtime oluşturma çağrıları mevcut
  InventoryItem(ItemData, int) overload'una taşınır. InventoryComponent.LoadInventory, yüklenen
  kayıt girdilerini inject edilmiş katalog üzerinden ItemData ile bağlar.
- Katalog/hatalı item kimliği durumu açıkça ele alınır; referans çözümlemesi
  tamamlanmadan HUD ve oyun kullanımı başlatılmaz.
- Gerçek kullanıcı kayıtları değiştirilmez. Uyumluluk ve restart kontrolleri
  proje içindeki geçici test dizininde eski şemalı örnek kayıtlarla yapılır.
- Kayıt dosyalarının atomik yazımı, F5 davranışının değiştirilmesi, slot sistemi
  ve yeni kayıt formatı bu dönüşümün kapsamı değildir.

## Asset ve kaynak geçişi

Eski managerların Inspector ayarları alınmadan kaynakları dönüştürülmez.
Sahne/prefab üzerindeki eski manager bileşenleri ve referansları Editor API'leriyle
güncellenir; eksik script bırakılmaz. Kaynak GUID'lerinin tüm sahne/prefab
referansları taranır; güncel üç sahne dışındaki bağımlı assetlerde de gerekli
referans düzeltmeleri yapılır. Tarihi recovery sahnelerine yeni özellik eklenmez.
Kullanılmayan Singleton<T> altyapısı ancak referans kontrolünden sonra kaldırılır.
Mevcut asset GUID'leri, UI bağlantıları ve kullanıcının önceki dosya değişiklikleri
korunur. Üçüncü taraf paket kaynakları düzenlenmez; paket yükseltmesi yapılmaz.

## Doğrulama ve kabul ölçütleri

1. Oyun kaynaklarında servis singleton .Instance kullanımı ve servislerin kendi
   başına bağımlılık oluşturması kalmaz. Root kayıtları servisleri tek instance
   olarak sağlar; üç sahne arasında instance kimlikleri korunur.
2. Üç build sahnesi ayrı ayrı başlatılır; injection, başlangıç sırası, eksik
   referans/script ve çift abonelik kontrolleri geçer. Kapalı paneller açılırken
   veya dinamik eşya oluşturulurken null servis bulunmaz.
3. Input okuma, pause/ayarlar/geri, buton odak/ses davranışı, HUD/envanter,
   etkileşim ve MainMenu → Episode(01) → EndGame dönüşü kontrol edilir.
4. Eski şemalı kayıt okuma, ItemData bağlama, dinamik eşya restore, başarılı
   restart ve kayıtsız dönüş küçük regresyonlarla doğrulanır. Kayıt tür adları
   ve SaveableEntity kimlikleri karşılaştırılır.
5. Önceki AgentScripts/UIRegressionChecks.cs yeni servisleri kullanacak şekilde
   güncellenir; root bir kez başlatma/dispose ve DI kontrolleri eklenir.
6. Unity Editor derlemesi, Console ve git diff --check kontrol edilir. Windows
   IL2CPP player build'i daha önce eksik Windows SDK nedeniyle tamamlanamadı;
   ortam değişmediyse bu sınır açıkça raporlanır, başarılı build iddia edilmez.

Küçük API/klasör tercihleri ve test yardımcılarının ayrıntıları uygulayıcıya
bırakılmıştır; yukarıdaki servis sınırları ve gözlenen davranış korunur.

Bağımsız tasarım incelemesinde planı engelleyen açık karar kalmadı. İncelemenin
belirttiği mevcut string constructor çağrıları da yukarıdaki geçişe dahil edildi.

## Doğrulanan VContainer kaynakları

- https://vcontainer.hadashikick.jp/scoping/project-root-lifetimescope
- https://vcontainer.hadashikick.jp/integrations/entrypoint
- https://vcontainer.hadashikick.jp/resolving/gameobject-injection
- Yerel doğrulama: Library/PackageCache/jp.hadashikick.vcontainer@dcf16cda6a65/Runtime

## Uygulama sonucu

Saf servisler, global/scene scope assetleri ve tüm tüketiciler taşındı. Dinamik
eşyaların root resolver ile kalıcı sahneye taşınmasını önlemek için açık null
parent overload'u kullanılır. Regresyonlar miktar, sahne sahipliği, sonraki kayda
katılım ve sahne çıkışında yok edilmeyi doğrular. Bağımsız incelemenin bu bulgusu
kapatıldı; önemli açık bulgu kalmadı. Windows player C# derlemesinde 66 assembly
üretildi. Tam IL2CPP executable eksik SDK nedeniyle doğrulanmış sayılmaz.
