# Kayıt sistemi

Kayıt verisi tek bir nesne tablosudur: `GameSaveData.Objects[UniqueId] -> EntitySaveData`. Kök ayrıca devam edilecek sahnenin adını tutar. Genel anahtarlı veri kabı, dünya/sahne sözlükleri ve ayrı dinamik durum sözlüğü yoktur.

## Sorumluluklar

| Parça | Görevi |
| --- | --- |
| `SaveableEntity` | Tek örnek kimliği, açık silinme flag'i, kendi durumunu başlangıçta yükleme |
| `ISavable` | StateType, bağımsız DTO Capture ve Restore |
| `SceneSaveContext` | Açık sahneyi collect etme, bekleyen kapalı nesne/silinme verisi ve başlangıç kontrolü |
| `DynamicObjectSpawner` | Sahneye ait dinamik ID üyeliği ve tekrar oluşturma |
| `GameSaveService` | Aday veriyi doğrulama, oturum sahipliği, hızlı kayıt/yükleme |
| `ISaveStorage` / `EasySaveStorage` | Dosya işlemleri ve metadata |
| `WorldSaveValidator` | Tek tablo, spawner üyeliği ve oyun verisi doğrulaması |

Oturum/storage uygulama scope'unda, context/spawner/factory sahne scope'unda yaşar. Global servis sahne nesnesi veya child scope referansı saklamaz.

## Collect ve Save

- `SceneSaveContext.Collect()`: açık sahnenin güncellemelerini belleğe aktarır; dosyaya yazmaz.
- `SceneSaveContext.QuickSave()`: sahneyi aday veriye collect eder, bütün oturumu dosyaya yazar ve başarıdan sonra oturumu değiştirir. F5 tek basış bu yolu kullanır.
- `SceneSaveContext.ChangeScene(sceneName)`: normal oynanış bölümü geçişinden önce input/zamanı durdurur, collect eder ve başarıda sahneyi açar. Hatalı collect kaynak sahnede bırakır.
- Yeni oyun, devam/restart, ana menü ve demo bitişi collect çağırmaz. Düşük seviyeli yükleme metoduna otomatik collect eklenmez.
- Yükleme sürerken dışarıdan Collect/QuickSave reddedilir; geçişin kendi collect adımı bu kilit içinde çalışır.

Yeni DTO'lar canlı modelle mutable liste/eleman paylaşmaz. Önceki sahnelerin verisi korunur; açık sahnede görülmeyen bütün ID'ler silinmez. Başarısız doğrulama/yazım önceki oturumu ve yeniden denemeye gereken silinme kararlarını değiştirmez.

## Tek entity ve dinamik nesneler

Aynı prefab sahneye yerleştirilebilir veya runtime oluşturulabilir; ikisi de tek `SaveableEntity` ve tek `ISavable` kullanır. Sahne kimliği sabittir. Dinamik örnek yeni kimlik alır; kayıttan oluşturulan örneğe önceki kimlik atanır. Prefab kimliği ile örnek kimliği farklıdır.

`DynamicObjectSpawner.Create(prefabId, position)` runtime üretim girişidir. Mevcut factory injection'ı Awake öncesinde, kimliği Start öncesinde kurar ve örneği context üzerinden aynı spawner'a kaydeder. Doğrudan factory çağrıları da takibe katılır. Runtime referanslar dosyaya veya global servise taşınmaz.

Spawner yalnız dinamik ID listesini `SpawnerSaveData` olarak aynı tabloda saklar. Sabit kayıt kimliği mevcut `SceneLifetimeScope.spawnerSaveId` alanındadır; yeni kontrolcü GameObject'i yoktur. Her dinamik nesnenin prefab/oluşturulma konumu ve durum DTO'su kendi `EntitySaveData` kaydındadır. Şu an factory yalnız katalogdaki DropObject prefablarını destekler.

## Silinme

Kalıcı kaldırmada `entity.MarkRemoved()` çağır; gerekirse sonra `Destroy(entity.gameObject)` kullan. Metot flag'i senkron işaretler, nesneyi kapatır ve ID/köken bilgisini context'te collect'e kadar korur. Yok edilme zamanlaması kayıt kararını etkilemez.

- Dinamik nesne: başarılı collect'te hem spawner üyeliği hem tam kayıt çıkar.
- Sahne nesnesi: kendi ID'sinde `IsRemoved` işareti kalır; yüklemede kapatılır ve yeniden etkinleştirilemez.
- Yalnız `SetActive(false)`: silinme değildir; veri korunur.
- `OnDisable` / `OnDestroy`: kalıcı silme bildirimi üretmez; sahne boşaltma kayıtları silmez.
- Bildirimsiz Destroy ile kaybolan takipli dinamik nesne collect hatasıdır.
- Tam toplanan eşya MarkRemoved kullanır; kısmi toplama miktarı kaydolur.

## Yükleme ve oyuncu

Spawner kendi ID listesini yükleyip örnekleri Start'tan önce oluşturur. Entity önce silinme işaretini, sonra kendi durum tipini kontrol eder ve bir kez Restore yapar. Kapalı nesne ilk etkinleşmesinde Restore yapar; başlamamış kapalı nesnenin bekleyen verisi yeniden collect'te korunur. Restore/oluşturma hatasında oynanış açılmadan güvenli ana menü dönüşü korunur.

Oyuncunun ortak ID'si `sharkadula.player` olarak Awake'te atanır. Normal bölüm geçişinde can/envanter/para ve hareket ayarları taşınır; hedef sahnenin giriş konumu, sıfır hız ve başlangıç durumu kullanılır. Geçiş amacı hedef context'te saklanır; geç etkinleşen oyuncuda da korunur. Dosyadan devam/restart tam konum/hız/state Restore eder.

## Kimlik, format ve sınırlar

ID'ler bütün build sahnelerinde benzersiz olmalıdır; ortak oyuncu ID'si tek istisnadır. `Tools > Save System > Validate Build Scene IDs` build sahnelerini denetler ve build öncesinde de çalışır. Kopyalanan nesnelerdeki ID'leri mevcut Fix Duplicate Scene IDs aracıyla düzelt; sahne/spawner kopyaları arasında global kontrolü de çalıştır. Kimlikleri topluca yeniden üretme, .meta GUID'lerini değiştirme.

Yeni kayıt format sürümü 2'dir: `Application.persistentDataPath/SavesV3/quickSave.es3`, aynı dosyada `metadata` ve `data` anahtarları. Eski `Saves` JSON ve `SavesV2` ES3 dosyaları korunur; okunmaz, dönüştürülmez veya üzerine yazılmaz. Legacy sağlayıcı aktif değildir.

Tek etkin oynanış sahnesi ve sahneyi yeniden açarak yükleme desteklenir. Additive oynanış, yerinde rollback, yeni slot/backup sistemi ve genel prefab kataloğu kapsam dışıdır. Geçişte yalnız belleğe collect edildiğinden F5 yapılmadan uygulama kapatılırsa ilerleme kalıcı değildir.

Tasarım: [ID tabanlı kayıt](../../../../docs/superpowers/specs/2026-11-15-id-based-save-design.md). Kontroller: [regresyon rehberi](../../../../AgentScripts/README.md).
