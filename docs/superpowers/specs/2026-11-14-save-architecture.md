# Kayıt mimarisinin bütün olarak düzenlenmesi

Kullanıcı kararları bana bırakarak baştan sona düzenlemeyi onayladı. Mevcut oyun kapsamı, nesnenin kendi başlangıcında Restore yapması, değiştirilebilir Easy Save sağlayıcısı ve eski JSON dosyalarının korunması sürer. Kayıt DTO adları ve ES3 v1 şeması değiştirilmez.

## Sınırlar

- **Core:** GameSaveData, MetaData, Result ve ISaveStorage. Unity veya oyun nesnesi bilmez.
- **Storage:** EasySaveStorage ve dosya yolu kontrolü. ES3UserType_StateData, bu oyunun ES3 serileştirme uzantısıdır.
- **Unity:** SceneSaveContext, SaveableEntity, ISavable ve DynamicObjectSpawner. Nesne kimliği, sahne veri alışverişi ve nesne yaşam döngüsü burada bulunur.
- **Game:** GameSaveService, WorldSaveData ve WorldSaveValidator. Genel kayıt kabındaki oyun anahtarı, dünya kuralları, metadata ve hızlı kayıt politikası bu oyuna aittir.
- **Legacy:** Eski JSON sağlayıcısı ve ayarları algoritmaları korunarak ayrı tutulur.

GameSaveService sahne taramaz, sceneLoaded dinlemez veya sahne scope'unu saklamaz. Yalnız internal SaveScene(sceneName, snapshot) üzerinden context'in yeni ürettiği verinin sahipliğini alır; doğrulayıp diske yazar. Context.Capture da internal'dır; üretim komutu QuickSave olup snapshot referansını dışarıya vermez. Mevcut kayıt verisi yalnız başarılı yükleme/kaydetme ile değişir ve dışarıya değiştirilebilir snapshot olarak açılmaz. Clear, HasLoadedGame ve son sahne bilgisi açık uygulama API'leridir. Restore uygulamaları verilen DTO'yu değiştirmez veya canlı model olarak saklamaz.

SceneSaveContext sahne scope'unda yaşar. Root servislerin sahne referansı tutmasına gerek bırakmadan Capture, kimlik kontrolü, bekleyen kapalı nesne durumunun korunması ve nesnelerin durum sorgularını yönetir. Sahne nesnelerini gerektiğinde sahnenin kökleri altından toplar; kayıt sistemi için sürekli bir registry veya global tarama eklenmez. Sahne kapsamındaki spawner, context'teki oluşturulma kayıtlarını kullanır.

ISavable yalnız StateType, CaptureState ve RestoreState taşır. Kimlik ve isteğe bağlı dinamik prefab kimliği tek SaveableEntity'dedir. GetSavable tek hedefi kontrol edip önbelleğe alır; Restore türü StateType ile denetlenir. ISaveDataReader ve DynamicSaveableEntity kaldırılır. Capture/Restore oyun bileşenlerinde kalır.

ItemDynamicObjectFactory sahne resolver'ı ile oluşturur; kimlik bir kez atanır, nesne hedef sahneye aittir ve Start öncesinde inject edilir. ItemCatalog, doğrulanmış dünya eşyası prefabını çözer. WorldSaveValidator yalnız kayıt verisini doğrular; canlı hiyerarşi ve hazırlık kontrolü SceneSaveContext'tedir.

## Akış

1. Menü GameSaveService üzerinden dosyayı doğrulayıp okur; GameService kayıtlı sahneyi açar.
2. SceneSaveContext.Initialize, GameService.BeginScene ile zamanı/input'u kapatır; spawner dinamik örnekleri hazırlar.
3. SaveableEntity.Start kendi context'inden durumunu çekip Restore yapar. Kapalı nesne ilk etkinleşmesinde aynı işlemi bir kez yapar.
4. Context başlangıç aşamasının sonunda aktif nesnelerin hazır olduğunu kontrol eder; GameService.CompleteScene ile sonucu bildirir. Restore olayı yoktur. Başarısızlıkta ana menüye dönülür.
5. F5 context.QuickSave üzerinden Capture üretir; GameSaveService.SaveScene doğrulayıp depolar. Sonuç mevcut OnSaveAfter bildirimiyle UI'a gider.

## Korunan garantiler

Envanter veri kopyaları, metadata hata/boş ayrımı, duplicate ID reddi, dinamik oluşturmanın tekrarsız olması, toplanmış ve yok edilmiş eşya davranışları korunur. Schema/type adları değişmediği için mevcut ES3 v1 kayıtları okunur. JSON migration, additive sahneler, yerinde geri sarma, çok bileşenli ISavable protokolü ve tam atomik dünya Restore'u eklenmez.

Dosyalar Core/Storage/Unity/Game/Legacy klasörlerine mevcut .meta dosyalarıyla taşınır. Oyun modelleri mevcut GameSaveModels altında kalabilir. Yeni arayüz yalnız gerçek değiştirilebilir sınırlar içindir; context ve koordinatör için ek arayüz eklenmez.
