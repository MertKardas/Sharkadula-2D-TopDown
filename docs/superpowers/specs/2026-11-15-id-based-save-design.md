# ID tabanlı kayıt ve sahne geçişinde veri toplama

Tarih: 15 Kasım 2026. Durum: kullanıcı onayıyla uygulandı. Editor ve Play Mode doğrulaması yapıldı; Windows player C# derlemesi paketlerdeki Unity.Scripting/PreserveAttribute referans hatası nedeniyle tamamlanamadı. Ayrıntılar [regresyon rehberinde](../../../AgentScripts/README.md).

İlk bağımsız tasarım incelemesinde üç ürün kararı dışında planlamayı engelleyen ek belirsizlik bulunmadı. Bu kararlar uygulama onayıyla sonuçlandırıldı; aşağıda ilk öneriler ve uygulama doğrulaması birlikte tutulur.

Bu belge, [önceki Easy Save tasarımındaki](2026-11-14-easy-save-design.md) genel veri kabı ve sahnelere göre veri bölme kararlarının yerine uygulanan yapıyı anlatır. Önceki tartışmadaki kararlar ve uygulamada benimsenen teknik öneriler aşağıda korunmuştur.

## Amaç ve kabul edilen kararlar

- Genel amaçlı `GameSaveData.Entries` / `WorldKey` katmanını kaldırmak.
- Nesne durumlarını sahne sözlüklerine bölmek yerine tek `ID -> kayıt` tablosunda tutmak.
- Bölümler arasında geçişten önce mevcut sahneyi collect etmek; geri dönüldüğünde o bölümün durumunu korumak.
- Dinamik nesnelerin durum ve oluşturulma verilerini ayrı sözlüklerde tutmamak.
- Mevcut spawn eden yapının sahneye ait dinamik ID'leri yönetmesi; yeni bir sahne kontrolcüsü GameObject'i eklememek.
- Sahneye yerleştirilen ve çalışma anında oluşturulan nesnelerde aynı `SaveableEntity` sınıfını kullanmak. Aynı prefab iki şekilde de kullanılabilmeli.
- Silinme kararını nesnenin Unity tarafından yok edilmesini bekleyerek değil, açık bir flag ile belirtmek.
- Dinamik nesne silindiğinde üyeliğini ve kaydını kaldırmak; sahne nesnesinin silinmiş olduğunu ise kayıtta korumak.

## Mevcut kodun tekrar incelemesi

- `SaveableEntity` zaten tektir. `PrefabId` çalışma anında `InitializeDynamic` ile atanır; `IsDynamic` buradan türetilir. Ayrı bir dinamik entity sınıfı gerekmez.
- `GameSaveService` şu anda genel veri kabından `WorldSaveData` çıkarır; dünya `Scenes` içinde `SceneSaveData`, sahne de `SaveObjects` ve `DynamicObjects` taşır.
- `SceneSaveContext.Capture` sahneyi tarar; yalnız hızlı kayıt yolu bu veriyi oturuma aktarır. `GameService.LoadScene` öncesinde collect yoktur.
- `DynamicObjectSpawner` sahne scope'unda saf C# başlangıç servisidir. Şu anda kalıcı ID'si ve oluşturduğu örnekler için sürekli runtime takibi yoktur.
- `ItemDynamicObjectFactory` injection ve kimlik atamasını `Start` öncesinde yapar. Mevcut somut prefab çözümü yalnız katalogdaki `DropObject` prefablarını destekler.
- `DropObject` toplandığında `Destroy` yerine miktarı sıfırlar ve nesneyi kapatır. Mevcut kayıt bu durumu korur.
- `GameService` yeni oyun, devam, restart, menü ve bitiş geçişlerini aynı düşük seviyeli yükleme metodunda toplar. Bunların tamamına koşulsuz collect eklemek yanlış olur.
- Build listesinde yalnız bir oynanış bölümü vardır: `MainMenu`, `Episode(01)`, `EndGame`. Gerçek A-B-A doğrulaması izole ikinci bölüm fixture'ı gerektirir.

## Değerlendirilen yaklaşımlar

| Yaklaşım | Sonuç |
| --- | --- |
| Sahne başına nesne ve dinamik veri sözlüklerini sürdürmek | Mevcut yapı; kullanıcının kaldırmak istediği iç içe veri sahipliğini korur. |
| Tek tablo; dinamik kayıtların her birine sahne bilgisi yazmak | Sahne üyeliği nesne kayıtlarına dağılır; spawner sahneye göre bütün tabloyu filtreler. Seçilmedi. |
| Tek tablo; mevcut spawner'ın yalnız ID üyeliğini kaydetmesi | Konuşmada seçilen yön. Nesne verisi tek yerde, hangi nesnelerin oluşturulacağı mevcut spawner'da kalır. |

Flag yerine `null` kontrolüyle silinme çıkarmak değerlendirildi ve reddedildi. Silinme ile collect aynı karede gerçekleşebilir; ayrıca sahne boşaltılırken nesnelerin yok olması oyun içi silinme değildir.

## Önerilen veri modeli

Sınıf adları uygulama sırasında mevcut üslupla kesinleştirilebilir. Aşağıdaki şekil tasarım sözleşmesidir:

```text
GameSaveData
  ResumeSceneName
  Objects: Dictionary<string, EntitySaveData>

EntitySaveData
  IsRemoved
  PrefabId                 // yalnız tekrar oluşturulacak runtime örnekte dolu
  SpawnPosition            // yalnız runtime örneği oluşturmak için
  State                    // PlayerSaveData, DropObjectSaveData, SpawnerSaveData ...

SpawnerSaveData
  DynamicIds: List<string>
```

- `GameSaveData` artık oyuna ait doğrudan kayıt köküdür. `WorldSaveData`, `WorldKey` ve `Set/TryGet` katmanı kalkar.
- `SceneSaveData` ve ayrı `DynamicObjectSaveData` oluşturulma sözlüğü kalkar. Prefab/oluşturulma konumu ilgili nesnenin kaydında bulunur.
- Her nesnenin durum DTO'su korunur; bütün tipleri tek büyük DTO'ya dönüştürmeyiz. `State` mevcut polimorfik Capture/Restore sınırını sürdürür.
- `SpawnPosition` oluşturma bilgisidir; oyuncu/düşman gibi mevcut DTO'larda bulunan normal konum alanlarını topluca yeniden tasarlama gerekçesi değildir.
- `SpawnerSaveData` aynı `Objects` tablosunda spawner'ın sabit ID'si altında saklanır. ID listesi durum, prefab veya konum kopyası içermez.
- Sahne adı yalnız devam edilecek sahneyi açmak için kökte bulunur; nesne verisine ulaşmak için sahne anahtarı kullanılmaz.
- Silinmiş statik nesnenin kaydı asgari `IsRemoved = true` işaretidir; durum verisi zorunlu değildir. Silinen dinamik nesne için kalıcı işaret bırakılmaz, kayıt tamamen çıkarılır.

### Kimlikler ve mevcut spawner

- Nesne ID'leri artık bütün oyun oturumunda benzersiz olmalıdır. Yalnız açık sahne içinde duplicate kontrolü yeterli değildir. Sahne kopyalarında ve build sahneleri arasında doğrulama gerekir; mevcut kimlikler topluca yenilenmez.
- Sahne nesnesinin ID'si düzenleme sırasında sabittir. Runtime oluşturulan her örneğe yeni ID, kayıttan oluşturulana aynı kayıtlı ID verilir.
- Prefab assetinin ID'si runtime örneğin ID'si olarak kullanılamaz. Prefab kimliği ile örnek kimliği ayrımı korunur.
- Teknik öneri: sahneye ait spawner kayıt ID'si mevcut `SceneLifetimeScope` üzerinde serialize edilen sabit bir alanla sağlanır ve servise verilir. Yeni GameObject, yeni kontrolcü MonoBehaviour veya sahne adına bağlı nesne sözlüğü gerekmez. ID üretme/duplicate kontrolü mevcut Editor aracına genişletilebilir.
- Spawner runtime'da kendi oluşturduğu `ID -> SaveableEntity` referanslarını takip eder; dosyaya yalnız ID listesi yazılır. Referanslar sahne scope'u kapanınca bırakılır, global servise taşınmaz.
- Factory'den yapılan bütün dinamik üretimler mevcut spawner takibine alınmalıdır. Mevcut doğrudan factory çağrıları ve test fixture'ları bu kurala uyarlanır; ikinci bağımsız üretim yolu bırakılmaz.
- Mevcut item factory sınırı korunur. Tek entity kullanılması bütün prefab türlerinin şimdiden spawn edilebildiği anlamına gelmez; düşman/kapı için genel prefab kataloğu bu işin kapsamında değildir.

## Collect, Save ve sahne geçişi

`Collect` açık sahnenin durumunu bellekteki oturuma aktarır. `Save`, aynı toplama sonucuyla bütün oturumu dosyaya yazar. Collect başarı bildirimi dosyaya kayıt başarı bildirimi değildir.

1. Geçiş hedefi doğrulanır; tekrar geçiş, input ve simülasyon geçici olarak engellenir.
2. Sahne scope'undaki mevcut `SceneSaveContext`, kapalı nesneleri de kapsayarak nesne durumlarını, silinme kararlarını ve spawner ID üyeliğini toplar.
3. Mevcut oturumdan yeni aday tablo hazırlanır. Bu sahnenin güncellemeleri ve açık silinmeler uygulanır; diğer sahnelerin kayıtları korunur. Yalnız mevcut sahnede görülmedi diye genel tablodan kayıt silinmez.
4. Aday veri doğrulanır. Hata varsa oturum ve bekleyen silinmeler korunur; bölüm geçişi başlamaz, önceki input/zaman durumu geri kurulur.
5. Yalnız collect işleminde aday oturuma devredilir. F5 işleminde önce dosyaya yazılır, başarılı yazımdan sonra oturuma devredilir. Hatalı yazım collect'in bekleyen silinme bilgisini tüketmez.
6. Başarılı collect sonrasında sahne yüklenir. Yeni sahne başarıyla hazır olduğunda devam edilecek sahne bilgisi hedef sahne olur. Yükleme/başlangıç hatası başarılı geçiş sayılmaz; mevcut güvenli menü dönüşü korunur.

Collect için yeni genel bir servis eklenmez. Mevcut sahne context'i veri üretir, `GameSaveService` yalnız veriyi alır. Geçişi başlatan sahne scope'lu çağrı context ile collect yapıp `GameService` üzerinden yüklemeyi sürdürür. Global `GameService` child scope/context referansı saklamaz veya global servis araması yapmaz. Düşük seviyeli `LoadScene` metoduna koşulsuz collect eklenmez.

| İşlem | Collect davranışı |
| --- | --- |
| Normal bölümden bölüme geçiş | Kaynak sahne boşaltılmadan önce collect; hata geçişi durdurur. |
| F5 | Mevcut sahneyi aday tabloya collect et, bütün oturumu dosyaya yaz. |
| Yeni oyun | Eski oturumu bırak; mevcut sahneyi yeni oyuna collect etme. |
| Devam et / kayıttan yükle | Dosyayı doğrula ve oturumu değiştir; bırakılan sahneyi yüklenen verinin üzerine collect etme. |
| Ölümden restart | Son başarılı dosyayı yeniden yükle; ölü oyuncuyu collect etme. |
| Ana menü / uygulamadan çıkış | Öneri: mevcut davranış gibi kaydedilmemiş ilerlemeyi bırak; otomatik kayıt ekleme. |
| Demo bitişi | Öneri: yeni bölüm sayma; mevcut bitiş davranışını koru. |

DTO'lar canlı modelle paylaşılmaz. Aday sözlük hazırlanırken mevcut kayıtların mutable durumları yerinde değiştirilmez; pending veri de salt okunur kabul edilir. Genel deep-copy çerçevesi eklenmez. Oturum bellekte collect ile ilerleyebildiğinden `HasLoadedGame` ve son kayıt sahnesi gibi API'ler gözden geçirilir; bellekte oturum bulunması diskte kayıt bulunmasıyla karıştırılmaz.

## Silinme flag'i ve nesne yaşam süresi

Tek `SaveableEntity`, dışarıdan doğrudan değiştirilmeyen `IsRemoved` ve ortak bir silinme metodu taşır. İşaretleme, yok etmeden önce senkron olarak yapılır; tekrarlanan işaretleme ek yan etki üretmez.

İşaretleme anında sahne context'ine nesnenin ID'si ve sahne/dinamik kökeniyle silinme kararı aktarılır. Bu küçük runtime bilgisi başarılı collect'e kadar korunur. Flag'in yalnız MonoBehaviour üzerinde kalması yeterli değildir: Unity nesnesi collect'ten önce tamamen yok edilmiş olabilir. Ayrı kalıcı silinen-ID listesi oluşturulmaz; spawner ve context aynı silinme kararını kullanır.

| Nesne | Başarılı collect sonucu | Yeniden yükleme |
| --- | --- | --- |
| Silinmemiş dinamik | ID spawner listesinde, tam kayıt genel tabloda kalır. | Aynı prefab ve örnek ID'siyle oluşturulur. |
| Silinmiş dinamik | ID üyelikten ve nesne tablosundan çıkarılır. | Oluşturulmaz. |
| Silinmiş sahne nesnesi | `IsRemoved` işareti kendi ID kaydında korunur. | Durum Restore edilmeden oynanışa katılması engellenir. |
| Yalnız kapalı nesne | Silinmiş sayılmaz; durumu veya bekleyen verisi korunur. | Mevcut tek seferlik Restore kuralı sürer. |

- Silinme flag'i ilk kontrol edilir; silinmiş nesnede `CaptureState` çağrılmaz. Hiç kaydedilmeden oluşturulup silinen dinamik nesne dosyada iz bırakmaz.
- Silinmiş statik nesnenin işareti sonraki collect'lerde de korunur; sahnede bulunmaması işareti tablodan çıkarmak için gerekçe değildir.
- `OnDestroy` veya `OnDisable` otomatik kalıcı silinme sinyali değildir. Sahne kapanışı, pool veya geçici kapatma kaydı silmez.
- Açık silinme bildirilmeden takip edilen dinamik referans kaybolursa collect hata verir; zamanlamaya bağlı sessiz silme çıkarımı yapılmaz.
- Teknik öneri: eşyanın miktarı sıfıra düştüğünde ortak silinme yolu kullanılır; kısmen toplanan eşya normal kaydolur. Böylece sıfır miktarlı dinamik eşya tekrar oluşturulmaz. Statik eşya için işaret korunur. Bu davranış mevcut toplama testlerine açıkça eklenmelidir.
- İşaretlenmiş statik nesne Awake hazırlığından geçmiş olabilir; buna rağmen Start tabanlı oynanış/Restore başlamadan engellenmelidir. Kapalı bir statik nesnenin silinme kaydı da ilk etkinleşme öncesinde veya sırasında uygulanır.

## Sahneye dönüş ve yükleme sırası

1. Dosyadan yüklemede önce tüm veri doğrulanır; yalnız bölüm geçişinde bellekteki oturum kullanılır.
2. Sahne scope'u hazırlanır; input ve zaman kapalıdır.
3. Spawner kendi sabit ID'siyle ID listesini çeker. Kayıt yoksa ilk ziyaret kabul edilip boş liste kullanılır.
4. Listedeki her ID'nin tam kaydı, prefabı ve konumu doğrulanır. Eksik, silinmiş, yinelenen veya başka spawner tarafından sahiplenilmiş dinamik kayıt başarısız hazırlıktır.
5. Spawner factory ile nesneleri `Start` öncesinde oluşturur, injection ve kayıtlı ID atanır. Aynı ID yanlış köken/tipte bir sahne nesnesinde bulunuyorsa sessizce atlanmaz; hata olur.
6. `SaveableEntity` önce silinme işaretini denetler, sonra kendi ID'sinden tek seferlik Restore yapar.
7. Context başlangıcı doğrular; yalnız başarıda oyun açılır. Kapalı nesnelerin bekleyen verisini koruyan mevcut davranış sürer.

Kayıt doğrulaması spawner üyeliği ile dinamik kayıtların bire bir eşleşmesini, geçerli ID/tip/prefab/konumları ve mevcut oyuncu/envanter kurallarını kapsar. Statik silinme işaretinde durum verisi aranmaz. Runtime kaynaklar yalnız sahne scope'unda yaşar.

## Uygulama onayıyla benimsenen kararlar

Kullanıcının tasarımı uygulama onayı sonrasında bu üç konuda aşağıdaki öneriler esas alındı. Otomatik kayıt ve migration eklenmedi; oyuncu normal bölüm geçişinde hedef giriş konumunu kullanır.

1. **Geçişte otomatik dosya kaydı:** Kullanıcı collect istedi; bunun diske otomatik yazılması kararlaştırılmadı. Önerim geçişte yalnız belleğe collect, F5'te dosyaya Save. Bu durumda kayıt yapılmadan uygulama kapatılırsa collect edilen ilerleme kaybolur.
2. **Oyuncunun bölümler arası verisi ve konumu:** Oyuncu verisinde envanter/can ile konum/hız/state aynı DTO'dadır. Ortak oyuncu ID'siyle hepsini körlemesine Restore etmek A konumunu B'ye taşır; her sahneye farklı ID vermek ortak envanteri geriye sarabilir. Önerim ortak oyuncu ID'si; devam/restart'ta tam Restore, normal bölüm geçişinde ortak ilerlemenin taşınması ve hedef sahnenin giriş konumu, sıfır hız, normal başlangıç durumu. Dönüşte bölümde bırakılan konumun mu, kapı girişinin mi kullanılacağı ayrıca seçilmelidir. Bu karar yalnız DTO değişikliği değildir; Restore amacının açık taşınmasını gerektirir.
3. **Mevcut ES3 dosyaları:** Eski tasarımda verilen JSON kayıtlarını dönüştürmeme kararı yeni ES3 geçişine otomatik olarak uygulanmaz. Önerim format sürümü 2 ve ayrı `SavesV3` kökü; `Saves` ve `SavesV2` korunur. ES3 v1 migration istenirse ayrı, açık kapsam olur. Uyumluluk kararı verilmeden v1 dosyanın üzerine yeni format yazılmaz.

## Kapsam, korunacaklar ve değişecek yerler

- Veri: `GameSaveData`, `WorldSaveData`, `SceneSaveData`, `DynamicObjectSaveData`; tek nesne kaydı ve spawner ID durumu.
- Runtime: `SaveableEntity`, `SceneSaveContext`, `GameSaveService`, `WorldSaveValidator`, `DynamicObjectSpawner`, `ItemDynamicObjectFactory` ve doğrudan factory çağrıları.
- Entegrasyon: `SceneLifetimeScope`, sağlayıcı kökü/sürümü için `AppLifetimeScope`, bölüm geçişi ile `GameService` sınırı, eşya toplama/silinme çağrıları ve oyuncunun seçilecek geçiş politikası.
- Editor: spawner kimliği, build sahneleri arasında nesne/spawner ID doğrulaması; mevcut `.meta` ve kimlikler korunur. Yeni serialization alanları dışında sahne/prefab toplu değişikliği yapılmaz.
- Testler: `SaveRegressionChecks`, `SaveReviewRegressionChecks`, `SaveArchitectureChecks`, `DIRegressionChecks`, `UIRegressionChecks` içindeki eski veri modeli ve factory fixture'ları. Mimari kontroller yeni sözleşmeyi doğrular; kaldırılan genel kabı zorunlu tutmaz.
- Belgeler: uygulama tamamlandıktan sonra kök README, SaveSystem README, mimari ve regresyon rehberi güncellenir. Eski tasarım tarihsel kayıt olarak kalır.

`ISaveStorage`, Easy Save dosya erişim sınırı, `Result`, metadata, `ISavable`, StateData serileştirme uzantısı, envanter izolasyonu ve başarısız Restore'da güvenli dönüş korunur. Legacy kaldırma, boş slot paneli temizliği, state reflection sadeleştirmesi, yeni slot/backup sistemi, genel prefab kataloğu, additive sahneler, pool sistemi, yerinde rollback ve yeni bağımlılıklar bu işin kapsamında değildir. Legacy sağlayıcı yalnız değişen kayıt sözleşmesini derlenebilir tutacak ölçüde uyarlanabilir; ayrı modernizasyon yapılmaz.

## Kabul ölçütleri ve uygulamada yapılacak doğrulama

1. Tek `SaveableEntity`; aynı prefab sahnede statik ve runtime dinamik olarak çalışır. Runtime örnek ID'leri benzersiz, reload ID'leri sabittir.
2. ES3 round-trip tek tabloyu, spawner ID listesini, polimorfik StateData'yı, statik silinme işaretini ve metadata UTC bilgisini korur.
3. İzole A-B-A senaryosu: açılmış sandık, bırakılmış/taşınmış dinamik eşya ve diğer bölüm kayıtları korunur. Ardından F5, dosyadan reload ve A-B-A tekrarında da sonuç korunur.
4. Aynı karede işaretle, `Destroy`, collect: dinamik ID ve kayıt çıkar; statik silinme işareti kalır. Yok edilme tamamlandıktan sonra collect de aynı sonucu verir.
5. Kısmi toplama miktarı korunur; tam toplanan dinamik geri gelmez; statik toplanan eşya yeniden etkinleşmez. Yalnız kapatma silinme değildir.
6. Collect/yazım hatasında önceki oturum ve yeniden denemeye gereken silinme bilgisi korunur. Başarısız collect bölüm geçişini başlatmaz. Sahne kapanışı silinme üretmez.
7. Kapalı ve henüz başlamamış nesne verisi kaybolmaz; statik silinme işareti sonraki collect'te düşmez; Restore bir kez çalışır.
8. Eksik dinamik kayıt/prefab, yetim dinamik kayıt, çakışan global ID veya yanlış üyelik açık hatadır; bozuk veriyle oyun açılmaz.
9. Yeni oyun, devam/restart, F5 tek basış, menü/ölüm/HUD ve seçilecek oyuncu geçiş politikası ayrı senaryolarla doğrulanır. Restart collect edilmiş ölüm durumunu kullanmaz.
10. Testler yalnız izole geçici kayıt ve fixture kullanır; gerçek `Saves`, `SavesV2` dosyalarını değiştirmez. Unity Console, etkilenen Play Mode akışları ve uygun player derlemesi çalıştırılır; çalıştırılmayan kontrol başarılı sayılmaz.

Uygulama sonucunda Editor derlemesi, ES3 round-trip, flag/Destroy zamanlaması, başarısız Save yeniden denemesi, kapalı veri, statik silinme işareti, gerçek A-B-A ve dosyadan reload, oyuncu geçişi, metadata/DI/envanter ve ilgili UI kontrolleri çalıştırıldı. Windows player C# derlemesi tamamlanamadı; tam executable build yapılmadı. Bağımsız incelemede bulunan dört runtime durumu FAIL/PASS regresyonlarıyla kapatıldı; fixture yalnız kendi geçici sahnesini değiştirir.

## Yerel kaynaklar

- [Mimari ve mevcut durum](../../architecture.md)
- [SaveSystem rehberi](../../../Assets/Scripts/Systems/SaveSystem/README.md)
- [Regresyon rehberi](../../../AgentScripts/README.md)
- [SaveableEntity](../../../Assets/Scripts/Systems/SaveSystem/Unity/SaveableEntity.cs)
- [SceneSaveContext](../../../Assets/Scripts/Systems/SaveSystem/Unity/SceneSaveContext.cs)
- [DynamicObjectSpawner](../../../Assets/Scripts/Systems/SaveSystem/Unity/DynamicObjectSpawner.cs)
- [GameService](../../../Assets/Scripts/Systems/GameService.cs)
