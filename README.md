# Sharkadula

Unity ile geliştirilmiş, vampir ve zindan temalı bir **2D top-down oyun demosu**. Eski proje yeniden düzenlenmek üzere inceleniyor; bu depo şu an mevcut uygulamanın başlangıç noktasıdır.

[![Eski demo videosu](intro/intro.png)](https://www.youtube.com/watch?v=s4vGGgpIS2c)

> Video eski sürümü gösterir. UI geçişi doğrulamalarının kapsamı [UI kontrol rehberinde](AgentScripts/README.md) açıklanır; bu kontroller tüm oynanışın hatasız olduğu anlamına gelmez.

## Projeyi açma

1. Unity Hub üzerinden **6000.5.9f1** Editor sürümünü kur. Hedef platforma göre build modülünü ekle.
2. Bu depoyu klonla ve kök klasörü Unity Hub'a proje olarak ekle.
   - **Depoda olmayan lisanslı paketler:** Asset Store lisansı nedeniyle **Easy Save 3** (`Assets/Plugins/Easy Save 3`) ve **PrimeTween** (`Assets/Plugins/PrimeTween`, `Packages/com.kyrylokuzyk.primetween.tgz`) git'e eklenmez. Projeyi açmadan önce ikisini Asset Store'dan kendi hesabınla içe aktar; PrimeTween kurulumu tgz dosyasını `Packages/` altına koyar. Bu paketler olmadan proje derlenmez.
3. Paketlerin ve assetlerin içe aktarılmasını bekle. İlk paket çözümlemesinde internet ve Git erişimi gerekir.
4. `Assets/Scenes/MainMenu.unity` sahnesini aç; Console hatalarını kontrol edip Play Mode'a geç.
5. Yeni oyun veya mevcut kayıtla devam et akışını kullan. Esc pause açar; ayarlarda bir önceki panele döner.

Editor sürümünün kaynağı [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), paketlerin kaynağı [manifest.json](Packages/manifest.json) ve [packages-lock.json](Packages/packages-lock.json) dosyalarıdır. Bu düzenleme sırasında paketler veya Unity sürümü yükseltilmedi.

## Mevcut kapsam

| Alan | Kodda bulunanlar |
| --- | --- |
| Oyuncu | Yürüme/koşma, dört yön animasyonları, can/hasar/ölüm durumları |
| Etkileşim | Sandık, anahtar toplama, kilitli kapı ve kapıdan demo bitişi |
| Envanter | Beş slot, yığınlanabilir eşya desteği, para ve değişiklik olayları |
| Kayıt | Değiştirilebilir `ISaveStorage`, Easy Save dosyası, metadata, hızlı kayıt ve nesnenin başlangıçta kendi durumunu yüklemesi |
| Düşman | Unity Behavior grafiği, NavMeshAgent ve NavMeshPlus ile hareket altyapısı |
| Ses | AudioSource havuzu, müzik, mixer grupları ve PlayerPrefs ses ayarları |
| Arayüz | Ana menü, HUD, envanter, pause/ayarlar/ölüm panelleri ve PrimeTween geçişleri |

**Tamamlanmamış:** Oyuncu saldırı işlemi ve kayıt slotu seçme paneli taslaktır. Kayıt modelleri birden fazla slotu destekleyecek yapıda olsa da mevcut kullanıcı akışı hızlı kayıt ve en son kayıtla devam et üzerine kuruludur.

Oyuncunun hız, ivme, azami can, envanter slotu/başlangıç parası, etkileşim
yarıçapı ve ses/animasyon ayarları
[DefaultPlayerConfiguration](Assets/ScriptableObjects/Player/DefaultPlayerConfiguration.asset)
assetinden düzenlenir. Her oyuncu kendi servis durumunu oluşturur; SO runtime
verisi saklamaz. Ayarlar yeni oyuncunun başlangıç değerleridir; devam/restart
sırasında eski kayıt sözleşmesindeki can ve hareket değerleri kayıttan yüklenir.

## Kontroller

| Tuş | İşlev |
| --- | --- |
| WASD / yön tuşları | Hareket |
| Sol Shift | Koşma |
| E | Yakındaki uygun nesneyle etkileşim |
| Esc | Oyun içi menü geçişleri |
| F5 | Tek basışta hızlı kayıt; basılı tutmak tekrar yazmaz |

Input assetinde saldırı ve başka hazır eylemler de bulunur; binding bulunması oyun davranışının tamamlandığı anlamına gelmez. Kaynak: [InputSystem_Actions.inputactions](Assets/InputSystem_Actions.inputactions).

## Sahne ve dosya düzeni

Build sırası: **MainMenu → Episode(01) → EndGame**. Liste [EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset) içinde tutulur.

```text
Assets/
  Scripts/                 Oyuncu, dünya nesneleri, sistemler ve UI kodu
  Data/                    EnemyData tanımı ve düşman ayarı
  ScriptableObjects/Items/ Eşya assetleri
  Prefabs/                 Dünya nesneleri ve UI prefabları
  Scenes/                  Menü, bölüm, bitiş; aydınlatma ve NavMesh verileri
  Animations/              Klipler ve animator controller dosyaları
  Audio/                   Sesler, AudioData assetleri ve mixer
  AddressableAssetsData/   Addressables grupları ve ayarları
  Plugins/, ThirdParty/    Harici araçlar ve onların örnekleri
  _Recovery/               Eski kurtarma sahneleri; aktif build listesinde yok
Packages/                  Paket manifesti ve kilit dosyası
ProjectSettings/           Unity proje ayarları
```

## Bilinen eksikler ve doğrulama

- Ölüm ekranındaki yeniden başlatma kayıtlı sahneyi yeniden yükler; kayıt yoksa ana menüye döner. Yeniden başlatma butonu kayıt metadata'sı varsa görünür.
- Pause/ayarlar geçişleri oyun zamanını durdurur; geçişler ölçeklenmemiş zamanla tamamlanır. Butonların ortak vurgu ve ses davranışı `MenuButtonUI` içindedir; işlemleri UnityEvent/controller metotlarına bağlıdır.
- Kayıt yükleme sahneyi yeniden açar; nesneler başlangıçlarında verilerini çeker. Aynı sahnede yerinde geri sarma ve additive sahne yüklemesi bu kayıt akışının kapsamında değildir.
- İlk sahne başlangıcında Restore hatası oluşursa input ve oyun zamanı açılmadan ana menüye dönülür. Envanterin Capture/Restore verisi canlı listeden bağımsız kopyalanır. Metadata erişim hataları başarılı boş kayıt listesi olarak sunulmaz.
- UI ve servis scope'ları için [çalıştırılabilir regresyon kontrolleri](AgentScripts/README.md) bulunur. Bunlar henüz otomatik CI veya Unity Test Runner akışına bağlı değildir. Harici paketlerin test/örnekleri oyun testleri değildir.

Ayrıntılar ve ele alınma sırası: [Mimari ve mevcut durum](docs/architecture.md).

Kayıt sisteminin sınıf sorumlulukları ve yeni bir nesne ekleme akışı [SaveSystem rehberinde](Assets/Scripts/Systems/SaveSystem/README.md) açıklanır. `SceneSaveContext` sahneyi, `GameSaveService` kayıt oturumunu, `ISaveStorage` dosya işlemlerini yönetir. Nesne kimliği tek `SaveableEntity` içindedir.

Yeni kayıtlar format sürümü 2 ile `Application.persistentDataPath/SavesV3/quickSave.es3` dosyasına yazılır; `metadata` ve `data` aynı dosyada ayrı anahtarlardır. Eski `Saves` JSON ve `SavesV2` ES3 dosyaları korunur; yeni sistem bunları okumaz veya dönüştürmez. Bu konum proje klasörü değildir. `GameSaveData.Objects`, bütün nesneleri tek ID tablosunda tutar; mevcut spawner yalnız dinamik ID üyeliğini aynı tabloya kaydeder. Sahne/dinamik nesneler aynı `SaveableEntity` kullanır; kalıcı silme `MarkRemoved` ile bildirilir.

Normal bölüm geçişinde `SceneSaveContext.ChangeScene` önce mevcut sahneyi belleğe collect eder; geri dönüldüğünde durumu korunur. Geçiş otomatik dosya kaydı değildir; F5 bütün oturumu diske yazar. Yeni oyun ve restart bırakılan sahneyi collect etmez. Oyuncu ortak ilerlemesini taşır; bölüm geçişinde hedef giriş konumu, dosyadan devam/restart'ta kayıtlı konum kullanılır.

Sahne nesneleri ve spawner kayıt kimlikleri bütün build sahnelerinde benzersiz olmalıdır; ortak oyuncu ID'si istisnadır. Prefab kopyaladıktan sonra `Tools > Save System > Fix Duplicate Scene IDs` komutunu çalıştırıp sahneyi kaydet. `Tools > Save System > Validate Build Scene IDs` sahneler arası kontrolü yapar; build öncesinde de çalışır. Prefab GUID'leri değiştirilmez.

## Geliştirme belgeleri

- [Proje yönergeleri](CLAUDE.md): Depoda yapay zeka ile çalışırken uygulanan tek talimat dosyası; ek kurallar ve skill'ler `.claude/` altında.
- [Mimari ve mevcut durum](docs/architecture.md): Sistem ilişkileri, kayıt akışı ve bilinen teknik borçlar.
- [Bağımlılık envanteri](docs/dependencies.md): Harici kütüphaneler, Unity paketleri ve kurulum durumu.
- [Gereksiz dosya adayları](docs/cleanup.md): Referans incelemesi, tutulan dosyalar ve temizlik önerileri.

Eski Copilot yönergeleri ve Memory Bank bu belgelere dönüştürüldü. Yeni özellikler veya hedef mimari kararlaştırıldıkça belgeler kodla birlikte güncellenmeli.

## Kullanılan araçlar ve içerikler

VContainer **1.19.0** servis oluşturma ve dependency injection için aktiftir. **AppLifetimeScope** uygulama boyunca yaşayan saf C# servislerini, **SceneLifetimeScope** her sahnenin bileşenlerine injection yapılmasını ve dinamik nesne spawner'ını yönetir. Eski singleton managerlar kaldırıldı. MessagePipe ve VContainer entegrasyonu **1.8.2** kurulu bağımlılıklardır. UniTask **2.5.11**, sahne başlangıcında nesnelerin `Start` aşamasını beklemek için kullanılır. Kurulum sınırları ve tam liste için [bağımlılık envanterine](docs/dependencies.md) bak.

Input System, URP 2D, Addressables, Unity Behavior, AI Navigation/NavMeshPlus, Easy Save 3, uGUI/TextMesh Pro, PrimeTween ve SerializedCollections kullanılıyor. Newtonsoft.Json eski alternatif kayıt sağlayıcısında kalır. LeanTween yalnız `AudioService` ses fade'lerinde kalır. UIEffect ve Rounded Corners paketleri de manifestte yer alıyor.

Eski README, karakterleri “Free Vampire 4-Direction Pixel Character Sprite Pack”, çevreyi “Dungeon Asset Pack” olarak belirtiyordu. Üçüncü taraf lisans ve atıf dosyalarını koru; içerikleri yeniden dağıtmadan önce ilgili lisansları doğrula. Depo kökünde projeye ait bir lisans dosyası bulunmuyor.
