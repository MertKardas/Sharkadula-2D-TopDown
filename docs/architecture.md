# Mimari ve mevcut durum

İnceleme: **13 Kasım 2026**, `NoError` branchi, kaynak başlangıcı `9d2f16f`. İlk inceleme tarihi korunmuştur; 14 Kasım 2026 UI ve VContainer geçişleri aşağıya işlenmiştir. Doğrulama kapsamı [regresyon rehberinde](../AgentScripts/README.md) bulunur.

## Başlatma ve sahneler

Build listesi `MainMenu`, `Episode(01)`, `EndGame` sırasındadır. Ana menüde `UIManager`, yeni oyun için ilk sahneyi; devam etmek için `GameSaveService.QuickLoad()` sonrasında kayıtlı sahneyi açar.

`AppLifetimeScope`, VContainerSettings preload assetinin gösterdiği kalıcı prefabdır. GameService, InputService, AudioService, GameSaveService, WorldSaveValidator, ItemCatalog ve ISaveStorage uygulama boyunca yaşar. SceneSaveContext, DynamicObjectSpawner ve ItemDynamicObjectFactory her SceneLifetimeScope içinde yeniden oluşturulur. Global servisler sahne nesnesi veya child scope referansı saklamaz; oyun kodunda statik Instance veya service locator kullanılmaz.

Her build sahnesinde `SceneLifetimeScope` vardır; parent uygulama scope'udur. Sahne kökleri ve kapalı paneller ilk kullanımdan önce inject edilir. Sahne değişince child scope kapanır, global servisler korunur. Üç sahne Editor'dan bağımsız başlatılabilir. Global servisler sahne UI/oyuncu referanslarını saklamaz.

`AudioConfiguration` mevcut mixer/grup referanslarını tutar. `AudioService` ses kaynaklarını root altında oluşturur, havuz takibini VContainer `ITickable` ile yapar. Input, Addressables handle'ı, sesler ve sahne aboneliği sahipleri tarafından `IDisposable` ile temizlenir.

## Oyuncu ve durum makinesi

Oyuncunun mantığı normal C# servislerinde, Unity bağlantıları adaptörlerdedir:

```text
Player/
  PlayerController.cs              Giriş, servis sahipliği, durum/yaşam döngüsü
  Configuration/PlayerConfiguration.cs  SO başlangıç ayarları
  Services/                        Hareket, can, envanter, etkileşim ve PlayerServices
  Components/                      Rigidbody, IDamageable, trigger, sunum adaptörleri
  States/                          Locomotion, Idle, Move, Run, TakingDamage, Death
  Persistence/PlayerSaveAdapter.cs  DTO eşlemesi ve giriş konumu politikası
  Inventory/InventoryItem.cs        Kayıtla uyumlu eşya veri sınıfı
```

`PlayerController.Awake`, kendi `PlayerServices(configuration, catalog)` grubunu
oluşturur; her oyuncunun servisleri ayrıdır. Yeni servisler VContainer'a
kaydedilmez ve factory/service locator kullanılmaz. Mevcut global input, ses,
katalog ve kayıt servisleri projenin mevcut bağlantılarıyla alınır. Oyuncu
servisleri kapanıp açılınca yeniden oluşturulmaz; durumu korunur.

`PlayerConfiguration` hızları, ivmelenme/yavaşlamayı, input/idle eşiklerini,
azami canı, envanter slotu ve başlangıç parasını, etkileşim yarıçapını, animasyon
çarpanını ve AudioData referanslarını tutar. Aktif oyuncu
[DefaultPlayerConfiguration](../Assets/ScriptableObjects/Player/DefaultPlayerConfiguration.asset)
assetine bağlıdır. Yeni SO `Create > Sharkadula > Player Configuration` üzerinden
oluşturulup controller'a atanabilir. Eski sahnedeki değerler ve ses referansları
bu varsayılan assete aktarılmıştır. Runtime can, para, eşyalar ve kayıtla
üzerine yazılan hareket değerleri servislerdedir; paylaşılan SO değiştirilmez.
Bozuk veya eksik configuration oyuncuyu kapatır ve SceneSaveContext'e başlangıç
hatası bildirir; oyun input/zamanı açılmadan mevcut güvenli dönüş akışı çalışır.

- `PlayerMovementService`: hız/ivme ayarları ve hız hesabı. `MovementComponent`
  yalnız Rigidbody hızını servis sonucuna uygular. Input eşiğini controller
  uygular; fizik hesabında ikinci bir gizli deadzone yoktur.
- `PlayerHealthService`: can, hasar/ölüm olayları ve kayıt restorasyonu.
  `HealthComponent` collider üzerindeki `IDamageable` girişini servise aktarır.
- `PlayerInventoryService`: slot/yığın, para, snapshot ve katalogla restore.
  Eski `InventoryComponent` ve sahne bağı kaldırılmıştır. UI, sandık, kapı ve
  kayıt adaptörü oyuncunun servisini kullanır. Eski büyük yığın/fazla slot
  kayıtları yüklemede korunur; yeni eklemeler güncel slot sınırına uyar.
- `PlayerInteractionService`: collider menzil takibi ve en yakın geçerli hedefin
  seçimi. `InteractionComponent` trigger olaylarını iletir, SO yarıçapını fizik
  collider'ına uygular ve kapanırken servis menzil bayraklarını temizler.
- `PlayerVisualComponent`: Animator/SpriteRenderer, ses ve animasyon olayları;
  sunum ayarlarını SO'dan okur. `AttackComponent` bağlı özellik taslağıdır.

Controller her `Update` içinde Move/Run girişini bir kere okur ve durumlara
`MoveInput`/`RunRequested` sunar. Fizik `FixedUpdate` üzerinden yürür. Interact,
sağlık ve ölüm animasyonu abonelikleri OnEnable/OnDisable sınırında yönetilir;
etkileşim ve F5 yalnız locomotion sırasında işlenir.

```text
PlayerController
  LocomotionState  → Idle/Move/Run seçiminin tek sahibi
    IdleState
    MoveState      → ortak hareket/sunum yaşam döngüsü
    RunState       → MoveState'in koşu uzmanlaşması
  TakingDamageState
  DeathState
```

Durumlar global Input/Game/Save/Audio servislerini almaz; controller'ın oyuncu
servislerini ve Unity adaptörlerini kullanır. `PlayerSaveAdapter` DTO eşlemesini
ve bölüm geçişindeki giriş konumu politikasını taşır. Controller tek `ISavable`
girişini ve `sharkadula.player` kimliğini korur. Kapalıyken yüklenen durum
etkinleşmeye ertelenir ve capture sırasında korunur; kapalıyken ölüm açılışta
uzlaştırılır. Capture gerçek Rigidbody hızını alır.

SO yeni oyuncunun başlangıç değeridir. Devam/restart sırasında mevcut
`PlayerSaveData` sözleşmesindeki can, azami can, yürüyüş hızı, ivme ve yavaşlama
kayıttan yüklenir; SO'ya geri yazılmaz. Slot sayısı ve koşu hızı config'ten gelir.
Kayıt DTO/state/InventoryItem tür adları ve kalan Unity script GUID'leri
korunmuştur. Bileşen referansları private setter'lı property'lerdir;
`FormerlySerializedAs` önceki sahne alanlarını okuyabilir.

`StateMachine<T>` ve `HierarchicalState<T>` mevcut iç içe StateData sözleşmesini
korur. Güncel servis/oyuncu testleri, sahne referansları ve klavye/player derleme
sınırları [kontrol rehberinde](../AgentScripts/README.md) açıklanır.

## Dünya ve düşmanlar

`InteractableBase`, menzil ve `CanInteract` ortak davranışını sağlar. Sandık bir kez para verir; anahtar `DropObject` üzerinden envantere eklenir; kapı anahtarı tüketip kilidi açar. Açılmış kapıyla sonraki etkileşim `GameService.EndGame()` üzerinden bitiş sahnesine geçer.

Tuzak hasarı `IDamageable` üzerinden verir, collider ve ses zamanlaması animasyon olaylarıyla yürür. Bu olaylar C# çağrı aramasında görünmeyebilir.

`EnemyController`, `EnemyData` ayarlarını Unity Behavior blackboard'una yazar. `EnemyMovement`, 2D düzleme uyarlanmış NavMeshAgent'ı kullanır. Düşmanların karar sistemi oyuncunun durum makinesinden ayrıdır. Düşman ölümü hareketi ve grafiği durdurur; loot/ölüm sunumu henüz tamamlanmış değildir.

## Kayıt ve yükleme

15 Kasım 2026 ID tabanlı kayıt düzenlemesi, [tasarım belgesindeki](superpowers/specs/2026-11-15-id-based-save-design.md) genel kabı ve sahne sözlüklerini kaldırır. Ayrıntılı kullanım [SaveSystem rehberinde](../Assets/Scripts/Systems/SaveSystem/README.md).

```text
GameSaveData
  ResumeSceneName
  Objects[ID] -> EntitySaveData (durum, silinme işareti, varsa prefab/oluşturulma konumu)
  Objects[spawnerID].State -> SpawnerSaveData (yalnız dinamik ID listesi)

Bölüm geçişi -> SceneSaveContext.ChangeScene -> input/zaman kilidi -> Collect
  -> aday tablo -> WorldSaveValidator -> bellekte oturumu değiştir -> hedef sahne
F5 -> aynı toplama -> ISaveStorage.Save -> başarılı yazımdan sonra oturumu değiştir
Devam/restart -> diskteki tabloyu doğrula -> kayıtlı sahneyi aç -> nesneler kendi ID'sinden Restore
```

`GameSaveService`, uygulama scope'unda saf veri oturumunu ve storage'ı yönetir. Global servis sahne taramaz, scene context/GameObject referansı saklamaz. `SceneSaveContext` güncellemeleri üretir; diğer bölümlerin kayıtları korunur. Collect dosyaya yazmaz. Başarısız collect/yazım önceki oturumu ve bekleyen silinme kararlarını korur. Normal bölüm geçişi collect hatasında başlamaz; input/zaman geri açılır. Yeni oyun, yükleme/restart, menü ve demo bitişi bırakılan sahneyi collect etmez. Async yükleme sırasında eski context'ten dış Collect/F5 reddedilir.

Tek `SaveableEntity`, sahne ve dinamik nesnelerin kimliğini ve kendi verisini yüklemesini yönetir; tek ISavable hedefi ve açık StateType kuralı korunur. `MarkRemoved`, flag'i yok etmeden önce bildirir; ID/köken context'te başarılı collect'e kadar kalır. Dinamik kaydı ve üyeliği silinir; statik nesnenin silinme işareti saklanır ve yeniden etkinleştirilmesi engellenir. Yalnız kapatma ve sahne boşaltma silinme değildir. Tam toplanan eşya ortak silinme yolunu kullanır. Bildirimsiz Destroy ile kaybolan takipli dinamik nesne collect hatasıdır.

Mevcut `DynamicObjectSpawner` sahne scope'unda kendi örnek referanslarını tutar; dosyaya yalnız ID üyeliğini aynı tabloda yazar. Sabit spawner ID'si mevcut `SceneLifetimeScope` üzerinde serialize edilir; yeni kontrolcü bileşeni yoktur. `ItemDynamicObjectFactory`, injection'ı Awake öncesinde, kimliği Start öncesinde atar ve aynı spawner takibine katılır. Factory halen ItemCatalog'daki DropObject prefablarıyla sınırlıdır. Nesneler hedef sahneye aittir; app scope'unda tutulmaz.

Oyuncu ortak `sharkadula.player` kimliğini kullanır. Normal geçişte ortak ilerleme taşınır, hedef sahnenin giriş konumu/sıfır hız/başlangıç durumu uygulanır; dosyadan devam/restart tam Restore yapar. Geçiş amacı hedef context boyunca korunur; başlangıçta kapalı oyuncuda kaybolmaz. Diğer nesnelerde başlangıçta kapalı verinin korunması, tek seferlik Restore, envanter DTO izolasyonu ve Restore hatasında oyunu açmadan menüye dönüş sürer.

`WorldSaveValidator`, tek tablodaki DTO'ları, ayrılmış oyuncu ID'sini, prefab/konumları, dinamik kayıt-üyelik eşleşmesini ve oyun kurallarını doğrular. Sahne ve spawner ID'leri bütün build sahnelerinde benzersizdir; ortak oyuncu ID'si istisnadır. Mevcut Editor aracındaki Validate Build Scene IDs komutu build öncesinde de çalışır.

`EasySaveStorage`, format sürümü 2 ile `SavesV3/quickSave.es3` içinde metadata/data anahtarlarını tek ES3File/Sync ile yazar. UTC metadata, yol doğrulaması, bozuk tek slotun diğerlerini gizlememesi ve StateData ES3 uzantısı korunur. Eski `Saves` JSON ve `SavesV2` ES3 dosyaları okunmaz, dönüştürülmez veya üzerine yazılmaz. Legacy sağlayıcı aktif değildir. Additive oynanış, yerinde Restore, otomatik dosya kaydı ve genel migration altyapısı kapsam dışıdır.

## UI ve ses

UI **uGUI** kullanır. `GameUI`, oyun içi panel durumunu ve Esc girişini tek başına yönetir. Esc oyun sırasında pause açar, pause sırasında oyuna, ayarlarda pause'a döner; ölüm ve bitişte normal menü geçişi yapmaz. Pause açılırken `GameService.PauseGame`, HUD'a dönüş tamamlanınca `UnpauseGame` çağrılır.

`UITransition`, PrimeTween fade ayarlarını taşıyan durumsuz bir ScriptableObject'tir. Çalışan `Sequence` `GameUI` tarafından tutulur; hızlı tekrar girişleri geçiş tamamlanana kadar yok sayılır, ölüm önceki geçişi iptal eder. Panelin aktifliği, alpha, `interactable` ve `blocksRaycasts` birlikte yönetilir. `GameUI`, GameService.OnGameEnded olayındaki fade tamamlanınca CompleteEndGame çağırır; global servis UI aramaz. Ortak `MenuButtonUI` fare/klavye odağı, ölçek ve ses davranışını sağlar; butona özel script sınıfları yoktur. Buton işlemleri panel/controller metotlarına UnityEvent ile bağlanır.

HUD ve envanter yeniden açılınca modelden yenilenir; animasyonlar kendi bileşenleri kapanınca durdurulur. UI tween'leri ölçeklenmemiş zaman kullanır. Sahne/prefab easing değerleri PrimeTween enum'larına anlamları korunarak çevrilmiştir. Yeniden başlatma başarılı kayıttaki sahneyi yeniden yükler; kayıt yoksa ana menüye döner. UI regresyonlarının çalıştırılması [kontrol rehberinde](../AgentScripts/README.md) açıklanır.

Üç sahnenin Canvas ölçekleme kuralı 1920×1080 referansı ve yüksekliğe göre
ölçeklemedir. Pause/ayarlar panelleri referans ölçülerini korur; ana menü alt
öğeleri geniş arka plan yerine Canvas'a bağlıdır. `UISelection`, controller'ların
odak seçimini paylaşır; etkin/kullanılabilir tercih yoksa ilk uygun öğeyi seçer.
Ana menü her etkinleşmede kayıt kullanılabilirliğini yeniler. Mevcut UI
`QuitGame` girişleri uygulamadan çıkış için `GameService.QuitGame` çağırır.

`AudioService`, efektler için `ObjectPool<AudioSource>`, müzik için ayrı bir kaynak tutar. `AudioData` klip seçimi, ses/pitch değişimi ve uzamsal ayarları taşır. Ses ayarları Audio Mixer'a uygulanır ve PlayerPrefs'te tutulur. Havuz kullanılması tüm sistemin tahsissiz olduğunu veya aktif ses sayısının 64 ile sınırlandığını kanıtlamaz.

## Öncelikli teknik işler

Bunlar statik incelemede görülen durumlardır; bu belge düzenlemesinde düzeltilmedi.

| Öncelik | Konum | Mevcut sorun | İlk doğrulama / küçük çözüm |
| --- | --- | --- | --- |
| 1 | Eski alternatif `SaveService.Save` | JSON algoritması doğrudan dosyaya yazar; aktif Easy Save akışı bunu kullanmaz. | Eski sağlayıcıda geliştirme yapılmayacak; yeniden seçilirse bu sınır dikkate alınmalı. |
| 2 | Sahne kopyalarının kimlikleri | Kopyalanan prefablar aynı kayıt kimliğini taşıyabilir; runtime kayıt bunu reddeder. Episode(01)'de dört kopyanın kimliği ayrıldı. | Kopyalama sonrası `Tools > Save System > Fix Duplicate Scene IDs` çalıştırıp sahneyi kaydet. |
| 2 | Test altyapısı | UI için elle tetiklenen MCP regresyon kontrolleri var; otomatik test/CI akışı yok. | Kontrolleri otomatikleştir; daha geniş kayıt ve dolu envanter senaryolarını kapsa. |

## Yeniden düzenleme sınırı

Temizlik ve mimari değişikliklerini ayrı, küçük kapsamlarla uygula. UI ve DI geçişlerinden sonra 14 Kasım 2026 Easy Save tasarımı uygulandı; 15 Kasım 2026 [ID tabanlı sadeleştirme](superpowers/specs/2026-11-15-id-based-save-design.md) bu yapının veri modelini değiştirdi. Bu kayıt geçişi dışında paket yükseltme veya dosya temizliği yapılmadı.

Dosya adayları ve referans bulguları için [temizlik raporuna](cleanup.md), çalışma kuralları için [AGENTS.md](../AGENTS.md) dosyasına bak.

## Kaynak klasör düzeni

- `Assets/Scripts/Enemy`: düşman controller ve hareket kodu.
- `Assets/Scripts/Items`: eşya tanımı, katalog ve DropObject.
- `Assets/Scripts/World`: tuzak/çevre/görsel davranışlar; `Interactables` altında kapı ve sandık.
- `Assets/Scripts/UI`: `MainMenu`, `InGame`, `Inventory`, `EndGame`, `Common`.
- `Assets/Scripts/Systems`: `Audio`, `Input`, `SaveSystem`, `StateMachine`, `DependencyInjection`.
- `Player`, `Interfaces` ve kayıt modellerinin mevcut ayrımı korunur.

Önceki klasör taşımaları mevcut meta GUID kimlikleriyle yapılmıştır. Easy Save geçişi yeni kayıt şeması kullanır; eski JSON kayıtlarıyla otomatik uyumluluk yoktur. Mevcut oyun DTO ve state tür adları korunur.
