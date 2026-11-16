# Easy Save ve nesnenin kendi verisini yüklemesi

Tarih: 14 Kasım 2026. Durum: kullanıcı tarafından onaylandı ve uygulandı. Gerçek doğrulama sonuçları [regresyon rehberinde](../../../AgentScripts/README.md).

## Amaç ve kararlaştırılan sınırlar

- Dosya işlemleri, oyun verisinin toplanması ve nesneye uygulanmasından ayrılır.
- `GameSaveData` oyuna özel alan taşımayan, başka projeye aktarılabilir veri kabıdır.
- Nesne başlangıcında kendi kaydını kimliğiyle çeker; bir Restore olayı veya merkezi Restore dağıtımı kullanılmaz.
- Capture ve Restore aynı `ISavable` sözleşmesinde kalır.
- `SceneSaveLoader` yerine yalnızca dinamik nesnelerin oluşturulmasını yöneten `DynamicObjectSpawner` kullanılır.
- Easy Save aktif depolama sağlayıcısıdır. Mevcut JSON `SaveService` algoritmaları geliştirilmez.
- Kullanıcı kararı: eski JSON kayıtları korunur, yeni sistem yeni kayıtlarla başlar. Otomatik dönüştürme yapılmaz.
- Mevcut tek oyunculu, tek etkin oynanış sahnesi kapsamı korunur. Slot arayüzü, additive sahne desteği ve aynı sahnede yerinde geri sarma eklenmez.

## Değerlendirilen yaklaşımlar

1. Mevcut sahne yükleyicisine Easy Save eklemek: küçük disk değişikliği sağlar ama oyun nesnelerini merkezi Restore ile yönetmeye devam eder; kullanıcının istediği akışa uymaz.
2. Genel veri kabı, değiştirilebilir disk sağlayıcısı ve başlangıçta nesnenin kendi verisini çekmesi: seçilen yaklaşım. Mevcut Capture/Restore modellerini kullanır ve oyun bağımlılıklarını çekirdekten çıkarır.
3. Nesneleri doğrudan Easy Save Auto Save/slot bileşenlerine bağlamak: nesneleri sağlayıcıya bağlar; değiştirilebilirlik amacına uymaz.

Metadata için slot başına tek dosyada iki anahtar önerilir: `metadata` ve `data`. Ayrı metadata dosyası büyük kayıt listelerinde avantajlı olabilir, ancak bu demoda iki dosyanın tutarlılığını yönetmeyi gerektirecek bir ihtiyaç yoktur. Ayrı bir anahtar yüklemek bütün dosyanın hiç okunmadığı anlamına gelmez.

## Parçalar ve bağımlılıklar

### Genel veri ve depolama

- `GameSaveData`: `Dictionary<string, object>` tabanlı veri; küçük `Set<T>` / `TryGet<T>` erişimi. Sahne, oyuncu, Unity nesnesi, katalog veya ES3 bağımlılığı yoktur. Kayıt adı ve zaman gibi bilgiler burada tekrarlanmaz.
- `MetaData`: mevcut tür adı korunabilir. Kayıt kimliği, görünen ad, UTC kayıt zamanı, oyun sürümü ve format sürümü taşır. Constructor sahne veya `Application` okumaz; değerler çağıran tarafından verilir. Sahne bilgisi oyun modelindedir.
- `ISaveStorage`: mevcut `SaveService` metotlarının şekli esas alınır: `Save(GameSaveData, MetaData)`, `Load(string, out GameSaveData)`, `DeleteSave(string)`, `GetAllMetaData()`. Sonuçlar mevcut `Result` ile raporlanır; listelemede bozuk bir dosya diğer geçerli slotları gizlemez, hata sessizce yutulmaz.
- `EasySaveStorage`: ES3 çağrılarının tek sahibi; açık dosya yolu ve kendisine ait ES3 ayarları kullanır. Global varsayılan slot, hazır slot UI veya Auto Save kullanılmaz.
- `SaveService`: imzalar uyduğu ölçüde yalnızca `ISaveStorage` bildirimi eklenir; JSON/disk algoritması aynı kalır. Aktif DI kaydı Easy Save'dir. Eski formattaki JSON dosyalarının bu sözleşmeyle otomatik uyumlu olduğu ileri sürülmez.
- Yeni kayıt kökü eski `Saves` klasöründen ayrılır; öneri `Application.persistentDataPath/SavesV2`. Yeni dosyalar slot başına `.es3` kullanır. Eski kayıt kökü okunmaz, yazılmaz veya silinmez.
- Newtonsoft ayarları eski sağlayıcıya aittir; genel veri kabına veya yeni arayüze taşınmaz. Yeni sağlayıcı için gereksiz JSON dönüşümü yapılmaz.

Dosya hataları `Result.Fail` olur. Slot adı tek dosya adı olarak doğrulanır; mutlak yol, yol ayırıcıları ve üst dizin geçişi kabul edilmez. Silme yalnızca doğrulanmış yeni slot dosyasını hedefler; kayıt kökünü recursive silmez.

`metadata` ve `data` bir `ES3File` içinde hazırlanıp tek `Sync` ile yazılır. Kurulu paketin geçici dosya/yerine koyma davranışı kullanılır; ayrıca bir JSON veya atomik dosya çerçevesi yazılmaz. Bunun güç kesintisinde mutlak veri kaybı garantisi olduğu iddia edilmez. Eksik anahtar, uyumsuz sürüm ve serileştirme hatası başarılı yükleme sayılmaz.

### Oyuna ait kayıt koordinasyonu

- `GameSaveService` oyuna aittir. `ISaveStorage` üzerinden QuickSave/QuickLoad, metadata, mevcut snapshot ve nesne verisine erişimi yönetir.
- Genel `GameSaveData` içinde tek sabit oyun anahtarıyla dünya snapshot'ı tutulur. Snapshot son sahneyi ve sahne kayıtlarını taşır. Bu anahtar oyun katmanında bir yerde tanımlanır.
- `SceneSaveData` oyun/Unity tarafında kalır; nesne durumları `UniqueId -> object` olarak tutulur. Dinamik oluşturulma kayıtları ayrı bir `UniqueId -> oluşturulma bilgisi` sözlüğündedir. Aynı durum verisi iki yerde tutulmaz; ayrıca `DynamicObjectIds` listesi gerekmez.
- Oluşturulma bilgisi prefab kimliği ve projede zaten kullanılan konumu taşır. Nesnenin kalıcı kimliği sözlük anahtarıdır. Prefab kimliği, sahnedeki örneğin kimliğinden farklıdır.
- `PlayerSaveData`, sandık, kapı, tuzak, düşman ve eşya modelleri ilgili oyun kodunda kalır. Bunların adları/namespace'leri gereksiz yere değiştirilmez.
- Yeni snapshot etkin sahnenin güncel durumunu temsil eder; diğer sahne kayıtları korunur. Başarısız yazım başarılı kayıt olayı üretmez. Mevcut `OnSaveAfter(Result)` bildirim şekli korunur.

### ISavable ve SaveableEntity

- `ISavable`: `UniqueId`, `CaptureState()`, `RestoreState(object)`. Dosya veya Easy Save erişimi yoktur.
- `SaveableEntity`: kalıcı kimliği ve ilk başlangıçta veriyi çekme davranışını ortaklaştırır. Uygun dar veri-okuma sözleşmesi DI ile verilir; nesne disk sağlayıcısını veya dünya snapshot'ının iç düzenini bilmez.
- Bir `SaveableEntity` için tek `ISavable` kuralı uygulanır. Eksik veya birden fazla hedef açık hata olur; sessizce ilk bileşen seçilmez. Bu görevde çok bileşenli kayıt protokolü eklenmez.
- Nesnenin `Awake` hazırlığı ve DI tamamlandıktan sonra `SaveableEntity.Start` Restore'u bir kez çalıştırır. Mevcut controller'ların varsayılan `Start` davranışından önce çalışması execution order ile açıkça belirlenir; sahne scope'unun -5000 sırası korunur.
- Kayıt yoksa nesne normal başlangıç durumunda kalır. Tipi bozuk veya uyumsuz kayıt, kayıt yokmuş gibi sessizce uygulanmaz.
- Başlangıçta kapalı nesne, ilk etkinleşmesinde Restore yapar. Tekrar açılıp kapanmak eski snapshot'ı yeniden uygulamaz.
- Capture yalnızca başlangıcı tamamlanmış nesnelerin runtime durumunu okur. Henüz başlamamış kapalı nesnelerin yüklenmiş durumları yeni kayıtta korunur; olmayan bir kayıt için henüz hazırlanmamış bileşende Capture çağrılmaz. Böyle bir nesne ilk açıldığında prefab/sahne başlangıcını kullanır.
- Sahne nesnelerinin kimlikleri düzenleme sırasında kalıcıdır. Yinelenen kimlikler kayıt öncesinde reddedilir; runtime'da sessiz kimlik değiştirerek kayıt bağlantısı koparılmaz. Editor'da kopyalama kimlik sorunu ayrıca hedefli biçimde giderilir; mevcut GUID ve sahne kimlikleri topluca yenilenmez.

### DynamicObjectSpawner

- Sahne kapsamındadır, yalnızca ilgili sahnenin kayıtlı dinamik örneklerini oluşturur. Capture ve toplu Restore yapmaz.
- `IDynamicObjectFactory`, prefab kimliğini çözme ve DI ile oluşturma sınırıdır. Bu projenin uygulaması `ItemCatalog` ve VContainer kullanır; genel spawner `DropObject` veya `ItemData` bilmez.
- Yeni runtime örnek yeni kimlik alır. Kayıttan oluşturulan örneğe kayıtlı kimlik, `Start` çalışmadan önce atanır. Prefab kimliği örnek üzerinde saklanır; sonraki Capture aynı oluşturulma kaydını üretir.
- Oluşturulan nesne etkin oyun sahnesine aittir; uygulama scope'unun DontDestroyOnLoad alanına taşınmaz. Mevcut açık parent argümanı davranışı korunur.
- Aynı kimlik zaten sahnede varsa tekrar oluşturulmaz. Oluşturma listesi önceden doğrulanır; bilinmeyen prefabın sessizce atlanması başarılı yükleme sayılmaz.
- Toplanmış eşyanın sıfır miktarlı durumu korunur. Restore sonucu etkisizleşen nesne sonraki kayıtlardan yanlışlıkla düşürülmez.
- Yok edilmiş bir runtime dinamik nesne, sahnenin sonraki snapshot'ındaki oluşturulma listesinden çıkar. Henüz başlamamış, kayıttan oluşturulmuş kapalı nesnenin oluşturulma kaydı ise korunur. Sahne çıkışının bütün dünyayı silinmiş saymasına izin verilmez.
- `DropObject.InitializeAsDynamic`, oluşturulma/kimlik sorumluluğu yeni bileşene taşındığında gereksiz hâle gelirse yalnızca ilgili çağrılarıyla kaldırılır. Eşya durumunu kurma ve Restore davranışı korunur.

## Yükleme ve yaşam döngüsü

1. Dosya belleğe okunur; henüz mevcut snapshot değiştirilmez.
2. Format, dünya snapshot'ı, sahne, nesne durumlarının tipleri, oyuncu envanteri ve dinamik prefab/konum bilgileri doğrulanır. Hata mevcut snapshot'ı değiştirmez.
3. Doğrulanmış snapshot mevcut veri olarak atanır; kayıtlı sahne yeniden açılır.
4. Sahne scope'u DI yapar. Spawner, nesnelerin `Start` aşamasından önce dinamik nesneleri oluşturur ve kimliklerini atar.
5. Başlayan her `SaveableEntity` kendi durumunu çeker ve uygular.
6. İlk aktif nesnelerin Restore işlemleri bittikten sonra mevcut oyun başlangıç bildirimi ve oynanış etkinleştirmesi yapılır. Restore için olay eklenmez. HUD'un `OnEnable` / `Start` sırası da test edilir; bir kare beklemek tek başına bütün kapalı nesnelerin Restore olduğunun garantisi sayılmaz.

Mevcut `GameService.sceneLoaded -> DistributeSaveData` çağrısı kalkar. Devam et ve yeniden başlat sahneyi yeniden yükler. Oyun sırasında yalnız Load çağırıp zaten başlamış nesneleri yeniden Restore etmek desteklenmez. Yalnız sahne nesnelerine göre kurgulanmış bu yaşam döngüsü, ileride kalıcı oyuncu eklenirse ayrıca ele alınmalıdır.

## Kapsamın ulaşacağı yerler

- SaveSystem modelleri, `ISavable`, `SaveableEntity`, `GameSaveService`, `SceneSaveLoader` yerine spawner/factory.
- `AppLifetimeScope` sağlayıcı kaydı ve `SceneLifetimeScope` sahne kapsamındaki spawner başlangıcı.
- `GameService` yükleme/başlangıç sırası; menü, ölüm ekranı, kayıt bildirimi ve oyuncu kayıt çağrıları.
- `DropObject` ve gerekli prefab kimlik bileşeni; oyuncu, düşman, kapı, sandık ve tuzak başlangıç/Restore uyumu.
- `LocomotionState` F5 kontrolü tek basışa çevrilir; yeni sağlayıcıya her kare yazılmasını sürdürmeyiz.
- Eski somut `SaveService` ve merkezi `SceneSaveLoader` kullanan AgentScripts fixture'ları yeni sözleşmeye/yaşam döngüsüne uyarlanır.
- README, mimari ve regresyon rehberi uygulanan durumla güncellenir. Unity asset taşımalarında mevcut `.meta` kimlikleri korunur.
- Kullanıcının mevcut çalışma ağacındaki ilgisiz değişikliklere dokunulmaz; toplu commit yapılmaz.

## Doğrulama ve kabul ölçütleri

- Unity Console derlemesi; player dosyalarına UnityEditor/NUnit eklenmemesi.
- Genel veri kabının farklı veri tiplerini ve eksik anahtarı işlemesi; Easy Save diske yaz/oku ile gerçek tiplerin korunması. Özellikle `object` içindeki PlayerSaveData, InventoryItem, StateData ve dinamik durumlar doğrulanır; Newtonsoft attribute'larının ES3 tarafından uygulandığı varsayılmaz.
- Yeni oyun, F5 tek basış, ana menüden devam et, ölümden restart, HUD/envanter ve durum makinesi geri yükleme.
- Dinamik eşyanın miktarı, konumu, kimliği, injection'ı, aktif sahne sahipliği; tekrar spawn'da kopya oluşmaması ve sahneden çıkınca yok olması.
- Toplanmış eşyanın geri gelmemesi; yok edilen runtime nesnenin sonraki kayıtta yeniden doğmaması.
- Kapalı nesneyi Restore öncesi yeniden kaydetme ve sonra etkinleştirme; önceki verinin korunması ve Restore'un yalnız bir kez olması.
- Yinelenen kimlik, geçersiz slot yolu, bozuk/eksik dosya, uyumsuz format, bilinmeyen eşya/prefab ve eksik durum hataları. Başarısız yüklemede mevcut snapshot korunur.
- Regresyonlar gerçek kullanıcı kayıtlarına yazmaz; yalnız göreve ait izole geçici klasör kullanır. Eski `Saves` klasörü testte değiştirilmez.
- İlgili Play Mode kontrolleri ve uygun olduğunda player derlemesi. Mevcut belgede belirtilen Windows SDK sınırı devam ediyorsa tam build yapılmış sayılmaz.
- Belge bağlantıları, kapsam kontrolü ve `git diff --check`.

## Uygulayıcıya bırakılan küçük seçimler

Yeni Unity bileşeninin adı, genel dünya snapshot'ının sınıf adı, dar okuma arayüzünün adı ve test metotlarının adları mevcut üslupla seçilebilir. Yeni bağımlılık, servis locator, global singleton, genel migration framework'ü veya otomatik kayıt özelliği eklenmez.

## İncelenen kaynaklar

- [Mevcut mimari](../../architecture.md)
- [Mevcut regresyon rehberi](../../../AgentScripts/README.md)
- [Easy Save temel kullanım](https://docs.moodkie.com/easy-save-3/getting-started/)
- [Easy Save tür desteği](https://docs.moodkie.com/easy-save-3/es3-supported-types/)
- [Easy Save kayıt slotları](https://docs.moodkie.com/easy-save-3/es3-guides/creating-save-slots/)
- Kurulu paketin `ES3SlotManager`, `ES3File`, `ES3IO`, `ES3Reflection` ve reflected type kaynakları. API ayrıntılarında kurulu kod esas alındı; Context7 CLI sorgusu sonuç vermediği için resmi kaynaklar web üzerinden kontrol edildi.
