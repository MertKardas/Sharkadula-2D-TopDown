# Sharkadula

Unity ile geliştirilmiş, vampir ve zindan temalı bir **2D top-down oyun demosu**. Eski proje yeniden düzenlenmek üzere inceleniyor; bu depo şu an mevcut uygulamanın başlangıç noktasıdır.

[![Eski demo videosu](intro/intro.png)](https://www.youtube.com/watch?v=s4vGGgpIS2c)

> Video eski sürümü gösterir. Aşağıdaki özellikler kodda bulunuyor; mevcut checkout için Unity derlemesi ve oynanış doğrulaması yapılmış değildir.

## Projeyi açma

1. Unity Hub üzerinden **6000.3.3f1** Editor sürümünü kur. Hedef platforma göre build modülünü ekle.
2. Bu depoyu klonla ve kök klasörü Unity Hub'a proje olarak ekle.
3. Paketlerin ve assetlerin içe aktarılmasını bekle. İlk paket çözümlemesinde internet ve Git erişimi gerekir.
4. `Assets/Scenes/MainMenu.unity` sahnesini aç; Console hatalarını kontrol edip Play Mode'a geç.
5. Yeni oyun veya mevcut kayıtla devam et akışını kullan. Yeniden başlatma ve pause için aşağıdaki bilinen eksikleri dikkate al.

Editor sürümünün kaynağı [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), paketlerin kaynağı [manifest.json](Packages/manifest.json) ve [packages-lock.json](Packages/packages-lock.json) dosyalarıdır. Bu düzenleme sırasında paketler veya Unity sürümü yükseltilmedi.

## Mevcut kapsam

| Alan | Kodda bulunanlar |
| --- | --- |
| Oyuncu | Yürüme/koşma, dört yön animasyonları, can/hasar/ölüm durumları |
| Etkileşim | Sandık, anahtar toplama, kilitli kapı ve kapıdan demo bitişi |
| Envanter | Beş slot, yığınlanabilir eşya desteği, para ve değişiklik olayları |
| Kayıt | JSON dosyası, metadata, hızlı kayıt, sahne bazlı nesne durumları ve dinamik eşya yükleme |
| Düşman | Unity Behavior grafiği, NavMeshAgent ve NavMeshPlus ile hareket altyapısı |
| Ses | AudioSource havuzu, müzik, mixer grupları ve PlayerPrefs ses ayarları |
| Arayüz | Ana menü, HUD, envanter, pause/ayarlar/ölüm panelleri ve LeanTween geçişleri |

**Tamamlanmamış:** Oyuncu saldırı işlemi ve kayıt slotu seçme paneli taslaktır. Kayıt modelleri birden fazla slotu destekleyecek yapıda olsa da mevcut kullanıcı akışı hızlı kayıt ve en son kayıtla devam et üzerine kuruludur.

## Kontroller

| Tuş | İşlev |
| --- | --- |
| WASD / yön tuşları | Hareket |
| Sol Shift | Koşma |
| E | Yakındaki uygun nesneyle etkileşim |
| Esc | Oyun içi menü geçişleri |
| F5 | Hızlı kayıt; şu anda basılı tutulduğunda her karede tekrar yazar |

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

- Ölüm ekranındaki yeniden başlatma, başarılı kayıt okumasından sonra sahneyi yeniden açmıyor.
- Pause panelinin açılması/kapanması `GameManager.PauseGame/UnpauseGame` metotlarına bağlı değil.
- Hızlı kayıt tek basış yerine basılı tutma kontrolü kullanıyor; dosyalar doğrudan mevcut kaydın üzerine yazılıyor.
- Oyun koduna ait otomatik test veya CI build akışı bulunamadı. Harici paketlerin test/örnekleri oyun testleri değildir.

Ayrıntılar ve ele alınma sırası: [Mimari ve mevcut durum](docs/architecture.md).

Kayıtlar `Application.persistentDataPath` altındaki `Saves/quickSave` dizinine yazılır; `savedata.json` ve `metadata.meta` oluşturulur. Bu konum proje klasörü değildir. İşletim sistemindeki gerçek yolu Unity üzerinden kontrol et; eski kayıtlarla çalışırken yedek al.

## Geliştirme belgeleri

- [Codex yönergeleri](AGENTS.md): Depoda çalışırken uygulanacak kısa kurallar.
- [Mimari ve mevcut durum](docs/architecture.md): Sistem ilişkileri, kayıt akışı ve bilinen teknik borçlar.
- [Gereksiz dosya adayları](docs/cleanup.md): Referans incelemesi, tutulan dosyalar ve temizlik önerileri.

Eski Copilot yönergeleri ve Memory Bank bu belgelere dönüştürüldü. Yeni özellikler veya hedef mimari kararlaştırıldıkça belgeler kodla birlikte güncellenmeli.

## Kullanılan araçlar ve içerikler

Input System, URP 2D, Addressables, Unity Behavior, AI Navigation/NavMeshPlus, Newtonsoft.Json, uGUI/TextMesh Pro, LeanTween, NaughtyAttributes ve SerializedCollections kullanılıyor. UIEffect ve Rounded Corners paketleri de manifestte yer alıyor.

Eski README, karakterleri “Free Vampire 4-Direction Pixel Character Sprite Pack”, çevreyi “Dungeon Asset Pack” olarak belirtiyordu. Üçüncü taraf lisans ve atıf dosyalarını koru; içerikleri yeniden dağıtmadan önce ilgili lisansları doğrula. Depo kökünde projeye ait bir lisans dosyası bulunmuyor.
