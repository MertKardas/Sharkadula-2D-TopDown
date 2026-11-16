# UI regresyon kontrolleri

## Oyuncu servisleri ve SO — 16 Kasım 2026

`PlayerServiceChecks.Architecture` Play Mode gerektirmez; SO ve normal C# servis
sınıflarını, container factory bulunmamasını kontrol eder.
`PlayerServiceChecks.ConfigurationAndIsolation` Play Mode gerektirmez; geçici
SO ve ayrı servis gruplarıyla config etkisi, küçük analog giriş, can/para/eşya
izolasyonu, SO'nun değişmemesi, hatalı config/restore reddini kontrol eder.
`PlayerServiceChecks.InvalidConfigurationStartup` Play Mode gerektirir; izole
context ve geçici oyuncu ile bozuk SO'nun callback hataları üretmeden oyuncuyu
kapatıp sahne başlangıcını başarısız kılmasını kontrol eder. Sonuçlar PASS.
Son iki sınır önce FAIL, düzeltme sonrası PASS verdi.

Mevcut 11 oyuncu kontrolü, tüm build sahnelerinin referans kontrolü, ES3
StorageRoundTrip, legacy eşya/dinamik restore ve HUD/restart tekrar PASS verdi.
Envanter/can çağıranları yeni servis API'sine uyarlandı. Gerçek kayıt dosyalarına
yazılmadı. Player servisleri VContainer'a eklenmedi; controller kendi grubunu
oluşturur. Runtime verisi SO'ya yazılmaz. Sahne kaydının ilgisiz yeniden yazımları
ayıklanmış; eski component değerleri DefaultPlayerConfiguration'a aktarılmıştır.

Unity arka planda komut işlerken bir asset oluşturma komutu timeout verdi;
sonuç sonradan diske yazıldığı doğrulandı. Yeniden derleme/Play Mode sonrasındaki
bağlantı aralıkları beklendi; geçici auto-tick ayarı testlerden sonra kapatılır.
Klavye ve tam player build sınırları aşağıdaki önceki doğrulama notlarında sürer.

## Oyuncu düzenlemesi — 15 Kasım 2026

`PlayerRegressionChecks.cs`, diğer kontroller gibi `script run --file
AgentScripts/PlayerRegressionChecks.cs --entry PlayerRegressionChecks.Run`
ile bellekte derlenir; player'a NUnit/UnityEditor bağımlılığı eklemez.

`Run` Play Mode gerektirir, yeni Episode(01) oturumu başlatır ve on bir kontrolü
çalıştırır: mimari sorumluluklar, merkezi locomotion geçişleri, hasar sınırları/olay sayısı, hasar-ölüm durumları ve abonelikler,
envanter/para sınırları, hareket ve kaydedilen gerçek hız, kapalı restore,
kapalıyken ölüm, dört yön sunumu, collider bazında etkileşim, gerçek Animator
ölüm olayı ve Gameover. Sonunda oyun ölüm ekranındadır; Play Mode'dan çık.
`SceneReferences` ayrı olarak Play Mode dışında çalıştırılır: bütün build
sahnelerini gerektiğinde additive açar, eski serialize alanlarının yeni
property'lere doğru yüklendiğini kontrol eder ve açtığı sahneleri kaydetmeden
kapatır. Son çalıştırmada üç build sahnesi ve bulunan bir oyuncu PASS verdi.

Kullanıcı kayıt dosyalarına yazılmaz. Kontroller ayrı girişler olarak da çalışır;
oyuncu gerektiren girişlerden önce `OpenGame` çağrılır.

Ek `MovementInput` gerçek Input System tuş olaylarıyla yürüme/koşma/durma
geçişini dener. Arka plandaki Editor'ın sentetik klavye olayını almaması bu
kontrolü etkileyebilir; bu durumda hata mesajında key/input değerleri görülür.
F5 için mevcut `SaveRegressionChecks.QuickSaveInput` aynı ortam sınırına tabidir.
Bu iki kontrol fiziksel klavye ile Game View'da ayrıca doğrulanmalıdır.

Bu düzenlemenin son toplu çalıştırmasında on bir oyuncu kontrolü PASS verdi.
ES3 `StorageRoundTrip`, `DIRegressionChecks.RunLegacySaveAndDynamicItem` ve
`UIRegressionChecks.RunRestartAndHud` da geçti; gerçek kayıt dosyalarına
yazılmadı. Bağımsız incelemenin kapalı restore/kapalıyken ölüm bulguları önce
FAIL, düzeltme sonrası PASS verdi. Editor derlemesi başarılıdır.

`MovementInput` bir çalıştırmada geçti fakat sonraki denemelerde gönderilen
tuş input'a ulaşmadı; F5 testi sıfır yazım bildirdi. Input güncellemeleri
izlendiğinde F5 basışı input katmanında da görünmedi. Bu oturumda klavye/F5
otomasyonu başarılı sayılmaz. Denenen odak/sentetik cihaz fixture değişiklikleri
geri alındı; mevcut F5 testi ve oyun tuş davranışı değiştirilmedi.

Windows player C# derlemesi yeniden denendi; Input System paketindeki
`Unity.Scripting/PreserveAttribute` referans hataları nedeniyle
`Assembly-CSharp.dll` üretilemedi. Bu önceki kayıt çalışmasında da belgelenmiş
bir sınırdır. Tam executable build yapılmadı. Etkileşimde küçük kalan borç:
yok edilmiş collider girdileri sensör kapanana kadar tutulur, seçime katılmaz.

## ID tabanlı kayıt — 15 Kasım 2026

Güncel veri modeli tek `GameSaveData.Objects` tablosudur. Aşağıdaki eski tarihli
bölümler önceki geçişlerin geçmişini de içerir; genel veri kabı ve sahne
sözlükleri artık yoktur. `SaveRegressionChecks.Lifecycle` yerine yeni
`IdSaveRegressionChecks.Lifecycle` kullanılır.

`script run --file AgentScripts/<dosya>.cs --entry <Sınıf.Metot>` ile:

| Dosya / entry | Ön koşul / kontrol |
| --- | --- |
| `IdSaveRegressionChecks.Contract` | Play Mode gerekmez; tek tablo/entity, açık silinme ve collect sözleşmesi |
| `IdSaveRegressionChecks.DelayedPlayerPolicy` | Play Mode gerekmez; geç etkinleşen oyuncu için hedef context geçiş amacını korur |
| `IdSaveRegressionChecks.ReservedPlayerRecord` | Play Mode; ayrılmış oyuncu ID'sinde bilinen yanlış DTO reddedilir |
| `IdSaveRegressionChecks.LoadingGuard` | Hazır bir sahnede Play Mode; async yükleme sürerken eski context Collect/Save yapamaz |
| `IdSaveRegressionChecks.RemovedEntityReactivation` | Play Mode; Episode(01)'i açar, silinen nesnenin yeniden etkinleştirilememesini doğrular |
| `IdSaveRegressionChecks.InvalidRecords` | Play Mode; eksik/yetim dinamik kayıt, çifte sahiplik, geçersiz konum/tip ve başarısız yüklemede oturum koruması |
| `IdSaveSceneFixture.Setup` | Play Mode dışında; yalnız geçici `Assets/_IdSaveRegressionB.unity` kopyasını additive açıp değiştirir, test build listesine ekler; mevcut sahneleri kaydetmez |
| `IdSaveRegressionChecks.Lifecycle` | Setup sonrası Play Mode; bellek collect, kısmi/tam toplama, aynı kare silinme, Destroy sonrası hata/yeniden deneme, statik işaret, kapalı veri, başarısız geçiş, gerçek A-B-A, oyuncu giriş/ilerleme ve dosyadan restart; MainMenu'ye döner |
| `IdSaveSceneFixture.Cleanup` | Play Mode dışında; yalnız kendi geçici sahnesini ve build listesi girdisini kaldırır, global ID kontrolünü çalıştırır |
| `SaveRegressionChecks.StorageRoundTrip` | Play Mode gerekmez; yeni ES3 tablo, spawner ID listesi, statik silinme işareti, iç içe StateData, UTC, overwrite, yol ve v1 sürüm reddi |

Kayıt fixture'ları yalnız `Temp/IdSaveChecks/<kimlik>` veya mevcut izole test
dizinlerinde çalışır; kullanıcı `Saves`, `SavesV2`, `SavesV3` dosyalarına yazmaz.
Setup/Cleanup'ı Play Mode dışında, Lifecycle'ı ayrı bir test oturumunda çalıştır.
Editor yeniden başlatılırsa geçici build listesi girdisi kaybolabilir;
Cleanup ve Setup ile fixture'ı yeniden hazırla.

Bu uygulamada yukarıdaki kontroller geçti. İncelemedeki geç oyuncu, yanlış oyuncu
tipi, yeniden etkinleşme ve yükleme sırasındaki Save bulguları düzeltme öncesi
FAIL, düzeltme sonrası PASS verdi. Setup'ın gerçek sahneleri yeniden kaydetmesi
kaldırıldı; mevcut sahnelerde yalnız Editor'ın atadığı spawner ID alanı tutuldu.

Uyarlanan StorageRoundTrip, metadata hataları, DI geçersiz yükleme/legacy eşya/
yaşam süreleri, iki envanter izolasyonu, Restore hatasında güvenli dönüş,
F5 tek yazım ve UI Run/RunAssets/RunSceneFlow/RunStartupPause/RunPanels/
RunRestartAndHud kontrolleri PASS verdi. F5 testi Game View odağıyla çalıştırılır;
arka plandaki Esc testi için geçici IgnoreFocus ve AllDeviceInputAlwaysGoesToGameView
yönlendirmesi kullanıldı ve sonunda önceki varsayılanlara geri alındı. Input asseti
değiştirilmedi. Console'daki iki Restore hata logu kasıtlı bozuk fixture'a aittir;
bunlar dışında yeni oyun hatası görülmedi.

Editor komut kuyruğu bir yeniden derlemede yanıt vermeyi bıraktı. Kullanıcının
kaydedilmemiş değişiklik olmadığını onaylamasından sonra Editor yeniden açıldı;
doğrulama yeni oturumda sürdürüldü. Oyun kaynaklarına bu araç sorununa yönelik
bir workaround eklenmedi.

Editor derlemesi başarılıdır. Windows player C# derlemesi denendi fakat
Input System, Addressables ve UIEffect paketlerinde `Unity.Scripting` /
`PreserveAttribute` referansı bulunamadığından Assembly-CSharp üretilmedi.
Bu nedenle player derlemesi veya tam executable build başarılı sayılmaz;
paket kaynakları bu kayıt düzenlemesi için değiştirilmedi. Eski tarihli
66 assembly başarıları önceki doğrulama oturumlarına aittir.

## UI tutarlılığı — 15 Kasım 2026

`AgentScripts/UIConsistencyChecks.cs`, aynı `script_run` yöntemiyle çalışır:

| Entry | Ön koşul / kontrol |
| --- | --- |
| `UIConsistencyChecks.StartupFocus` | MainMenu, Play Mode. İzole menü kopyasıyla EventSystem UIManager'dan sonra etkinleştiğinde ilk odağın Start aşamasında kurulması. |
| `UIConsistencyChecks.MainMenuRefresh` | MainMenu, Play Mode. Yeniden açılışta kayıt ekleme/silme sonrası Resume durumu, ilk odak ve kullanılamayan buton yerine seçim. İzole `Temp/UIConsistency/<kimlik>` kaydı kullanır; gerçek servis geri yüklenir. |
| `UIConsistencyChecks.Focus` | Play Mode. Ortak odak seçiminin kapalı/devre dışı öğeleri atlaması ve eski seçimi temizlemesi. |
| `UIConsistencyChecks.ButtonLabelsFit` | Play Mode, metinli butonu olan görünür menü. Render edilen metin butonun yatay sınırlarını aşmamalı. |
| `UIConsistencyChecks.ControlsFitScreen` | Play Mode, görünür menü. Button/Slider gibi Selectable öğeler ekran içinde kalmalı. |

Game View boyutunu değiştirerek son iki kontrolü farklı en-boy oranlarında
tekrarla; panel geçişi tamamlandıktan sonra çalıştır. Bu kontroller başlık,
etiket çakışması veya tüm olası ekran boyutları için görsel incelemenin yerini tutmaz.

- MainMenuRefresh, düzeltme öncesi FAIL, sonrası PASS. Ortak odak kontrolü PASS.
- Son incelemedeki başlangıç sırası bulgusu StartupFocus ile önce FAIL,
  Start/OnEnable ayrımı sonrası PASS. Tekrar incelemede engelleyici bulgu yok.
- 1024×768 pause menüsünde metin buton dışına taştığında ButtonLabelsFit FAIL;
  referans panel ölçüleri korunduktan sonra PASS. Ana menü sosyal butonlarında
  ekran dışı kalma yakalandı; Canvas'a bağlandıktan sonra sınır kontrolü PASS.
- 1024×768, 1280×720 ve 2560×1080 Game View boyutlarında görünüm incelendi.
  Ana menü, pause/ayarlar ve bitiş görüntüleri `Temp/UIConsistency` altındadır.
- RunAssets, Run, RunSceneFlow, RunStartupPause, RunPanels, RunRestartAndHud PASS.
- Editor derlemesi ve Windows player C# derlemesi başarılı; 66 assembly üretildi.
  Tam executable build yapılmadı.
- Oyun kaynaklı yeni Console hatası görülmedi. Geçici çözünürlük aracının
  temizleme aşamasındaki iki indeks hatası araçta giderildi; eklenen geçici
  çözünürlükler kaldırıldı ve önceki Game View seçimi geri yüklendi.
- Sahne kaydının ürettiği ilgisiz URP/biçim/sıralama değişiklikleri ayıklandı;
  önceki kullanıcı değişiklikleri ve asset GUID'leri korundu.

`UIRegressionChecks.cs`, Unity MCP `script_run` ile bellekte derlenip çalıştırılır.
`Assets` dışında olduğundan player'a test veya NUnit bağımlılığı eklemez.
Bu kontroller CI/Unity Test Runner testleri değildir.

Editor'da MainMenu sahnesini aç, Play Mode'a gir ve sırayla:

Play Mode'a geçişte domain reload tamamlanana kadar bekle; Editor hazır ve
`Application.isPlaying == true` olduktan sonra kontrolleri başlat.

| `file` | `entry` | Ön koşul / sonuç |
| --- | --- | --- |
| `AgentScripts/UIRegressionChecks.cs` | `UIRegressionChecks.Run` | Herhangi bir açık oyun sahnesi; unscaled fade/raycast, buton deselect/disable |
| Aynı | `UIRegressionChecks.RunSceneFlow` | MainMenu; yeni oyun, bildirimler, fade/highlight, EndGame ve ana menü dönüşü |
| Aynı | `UIRegressionChecks.RunStartupPause` | Play Mode; Episode(01) açar, BeginScene/CompleteScene arasında gerçek Esc girdisinin pause paneli açmamasını ve sonrasında normal pause/resume'u doğrular; MainMenu'ye döner |

Ardından ana menüde Yeni Oyun ile Episode(01)'i açıp sırayla:

| `entry` | Kontrol |
| --- | --- |
| `UIRegressionChecks.RunPanels` | Gerçek Esc inputu, pause/input, ayarlar/geri, tekrarlanan istekler, abonelikler ve geçişte ölüm; ölüm panelinde bırakır |
| `UIRegressionChecks.RunRestartAndHud` | Kayıttan restart, tekrarlı restart, HUD/envanter yenileme ve kayıtsız dönüş; MainMenu'de bırakır |

`script_run` için `timeout: 30` yeterlidir. Sonuç `PASS: ...` döner; aksi hâlde
anlamlı assertion mesajıyla exception oluşur. Play Mode'un arka planda kare
ilerlettiği kontrol edilir. Testler geçici oyun durumunu değiştirir; normal
oynanış kaydı açıkken değil, ayrı bir Play Mode oturumunda çalıştır.

Restart testi gerçek kayıt dizinine yazmaz. SaveManager servisini geçici olarak
`Temp/UIRegressionSaves/<benzersiz kimlik>` altında çalışan bir servisle değiştirir;
`finally` içinde eski servisi/veriyi geri yükler ve kendi test dizinini temizler.
Kayıtsız restart testinin `No saved games found` uyarısı beklenen çıktıdır. Fixture artık `ISaveStorage` alanına izole bir `EasySaveStorage` verir; gerçek kullanıcı kayıtları kullanılmaz. Ölümden restart fixture'ı yaşayan oyuncu ve Locomotion/Idle durumunu saklar.

Sahne/prefab GUID'leri değişmedi. UI sahnelerindeki eski butona özel eksik
script bağlantıları ortak MenuButtonUI/controller UnityEvent bağlantılarına
çevrildi. Gizli kayıt adı/slot paneli hâlâ bir taslaktır; slot özelliği eklenmedi.

Play Mode dışında `UIRegressionChecks.RunAssets` üç build sahnesindeki eksik
scriptleri, 18 ortak butonu, click hedeflerini, AudioData/easing referanslarını
ve EndGame başlangıç odağını kontrol eder. Açık sahnede kaydedilmemiş değişiklik
varken bu kontrolü çalıştırma; sahneleri açıp önceki sahneye döner.

## Son doğrulama — 14 Kasım 2026

- Unity 6000.5.9f1 Editor derlemesi başarılı.
- Beş kontrol grubu çalıştırıldı: RunAssets, Run, RunSceneFlow, RunPanels,
  RunRestartAndHud. Tamamı PASS; son Play Mode oturumunda yeni hata yok.
- Pause ve ayarlar ekranları 1280×720 Game View görüntülerinde incelendi.
- Bağımsız kod incelemesinde önemli açık bulgu kalmadı.
- Windows x64 IL2CPP build'i player C# ve üç sahnenin derlemesini geçti;
  C++ toolchain aşamasında Windows SDK 10.0.19041.0+ eksikliği nedeniyle
  başarısız oldu. Player build ve çalıştırılabilir oyun doğrulanmış sayılmaz.
- Gerçek kayıt dosyaları değiştirilmedi. Test çıktıları ve görseller
  ignore edilen Temp/UIVerification altında; kalıcı test verisi oluşturulmadı.

## VContainer servis kontrolleri

UI kontrolleri artık servisleri test tarafındaki sahne resolver'ından alır;
oyun kodunda `.Instance` veya service locator kullanılmaz. Testte oluşturulan
servis tüketen UI nesneleri etkinleştirilmeden önce inject edilir.

`AgentScripts/DIRegressionChecks.cs` için `script_run` entry noktaları:

| Entry | Ön koşul / kontrol |
| --- | --- |
| `DIRegressionChecks.JsonConstructorIsDataOnly` | Play Mode gerekmez; InventoryItem constructor'ı ID/miktar saklar, global servis çağırmaz |
| `DIRegressionChecks.CheckScene` | Herhangi bir build sahnesinde Play Mode; tek root, child scene scope, saf C# servisler ve kapalı nesnelerde injection |
| `DIRegressionChecks.RunLifetimes` | MainMenu'de Play Mode; üç sahne boyunca aynı global servisler, eski child scope'un kapanması, AudioService.Tick havuz temizliği; MainMenu'ye döner |
| `DIRegressionChecks.RunLegacySaveAndDynamicItem` | MainMenu'de Play Mode; eski InventoryItem JSON constructor'ı, yeni genel veri kabının JSON round-trip'i, katalog bağlama, spawner ile tekrarsız oluşturma, nesnenin başlangıçta kendi Restore'u ve sahne sahipliği; eski oyun dosyası migration testi değildir; MainMenu'ye döner |
| `DIRegressionChecks.RunInvalidSaveRegression` | Play Mode; bilinmeyen item/bozuk sahne kaydı başarısız olur ve mevcut yüklenmiş veriyi değiştirmez; yalnız Temp altında dosya kullanır |

DI kontrolleri için `timeout: 30` kullan. `CheckScene` ayrıca Episode(01) ve
EndGame doğrudan açılıp ayrı Play Mode oturumlarında çalıştırıldı; giriş ve
kapanışta yeni Console hatası görülmedi. VContainer 1.19.0'ın yeni scope script
şablonuyla mevcut Configure gövdeleri değiştirilmesin diye projeye ait
VContainerSettings assetinde DisableScriptModifier açık tutulur.

Windows hedefi için PlayerBuildInterface.CompilePlayerScripts ile 66 assembly,
Assembly-CSharp dahil, üretildi. Bu yalnız player C# derlemesidir; eksik Windows
SDK nedeniyle tam IL2CPP executable build'i hâlâ doğrulanmış değildir.
DI sırasında üretilen geçici dosyalar ve başlangıç yedekleri Temp/DIWork altındadır.

## Easy Save regresyonları — 14 Kasım 2026

`AgentScripts/SaveRegressionChecks.cs`, aynı `script_run` yöntemiyle çalışır. Yeni bağımlılık veya player'a NUnit/UnityEditor referansı eklemez.

| Entry | Ön koşul / kontrol |
| --- | --- |
| `SaveRegressionChecks.DataContract` | Play Mode gerekmez; genel kabın kaldırılması, doğrudan ID tablosu ve statik silinme işareti |
| `SaveRegressionChecks.StorageRoundTrip` | Play Mode gerekmez; gerçek ES3 dosyasından primitive, oyuncu, envanter, iç içe StateData ve dinamik kayıt okuma; UTC metadata, overwrite, geçersiz yol, uyumsuz sürüm, bozuk dosya ve izole silme |
| `IdSaveRegressionChecks.Lifecycle` | Güncel entry; önce üstteki IdSaveSceneFixture.Setup çalıştırılır. Play Mode'da collect/silinme ve gerçek A-B-A/dosya reload akışını kontrol eder. |
| `SaveRegressionChecks.QuickSaveInput` | Play Mode ve klavye; yeni Episode(01) açar, F5 basılı tutulurken tek başarılı kayıt yazıldığını doğrular |
| `SaveRegressionChecks.CompilePlayerScripts` | Play Mode dışında; Windows x64 player C# assembly'lerini Temp/EasySavePlayerScripts altına derler. Tam executable build değildir. `timeout: 90` kullan. |

Kayıt testleri yalnız `Temp/EasySaveChecks/<benzersiz kimlik>` altında yazar; gerçek `Saves` ve `SavesV2` dizinlerini değiştirmez. Test klasörleri sınır kontrolü sonrası finally içinde temizlenir. Bozuk kayıt listeleme uyarısı beklenen çıktıdır.

Bu geçişin doğrulaması:

- DataContract ilk çalıştırmada eski oyun alanları nedeniyle FAIL, uygulama sonrası PASS.
- StorageRoundTrip, UTC Kind kaybı ve iç içe StateData alanının atlanmasını yakaladı; sağlayıcı düzeltmeleri sonrası PASS.
- Lifecycle, sahnedeki eski duplicate ID'leri yakaladı. Episode(01)'de yalnız dört prefab örneğinin kayıt kimlikleri ayrıldı; asset GUID'leri korunur. Yanlış tipli dinamik veri regresyonu da önce FAIL, factory doğrulaması sonrası PASS.
- Lifecycle, QuickSaveInput, DI RunInvalidSaveRegression, RunLegacySaveAndDynamicItem, RunLifetimes ve UI RunSceneFlow, RunPanels, RunRestartAndHud PASS. UI restart testi, panel testinin bıraktığı ölüm ekranından da çalıştırıldı.
- Unity 6000.5.9f1 Console derlemesi ve Windows player C# derlemesi PASS; 66 assembly üretildi. Son Play Mode Console kontrolünde yeni hata yok; kayıtsız restart uyarısı beklenir.
- Bağımsız kod incelemesindeki tek P2 dinamik durum tipi doğrulamasıydı; regresyonla giderildi ve tekrar incelemede yeni P1/P2 bulunmadı.
- Tam IL2CPP executable build bu geçişte çalıştırılmadı; önceki Windows SDK sınırı için ayrıca kurulum/build doğrulaması gerekir.
- Statik sahne nesnesinde ID/tip uyumsuzluğu nesne başlarken anlaşılır; yükleme bütün dünya için atomik bir işlem olarak garanti edilmez. Başlangıçta kapalı nesne ilk etkinleşmesinde Restore yapar.

## Kayıt incelemesi düzeltmeleri — 14 Kasım 2026

`AgentScripts/SaveReviewRegressionChecks.cs` entry noktaları:

| Entry | Ön koşul / kontrol |
| --- | --- |
| `InventoryCaptureIsolation` | Play Mode; yeni Episode(01) açar. Capture sonrası eşya tüketme ve slot silmenin önceki snapshot'ı değiştirmemesi |
| `InventoryRestoreIsolation` | Play Mode; yeni Episode(01) açar. LoadInventory'nin kaynak listeyi/elemanı değiştirmemesi, asset'i yalnız canlı kopyaya bağlaması ve kaynak veride sonradan yapılan değişikliklerin canlı envanteri etkilememesi |
| `MetadataErrors` | Play Mode gerekmez. Eksik dizin ile dosya olan kök/üst bileşen ayrımı; Easy Save ve eski sağlayıcı adaptörü; QuickLoad/LoadAllMetadata hata yayılımı |
| `RestoreFailureStopsStartup` | Play Mode; yanlış tipli statik tuzak kaydını izole ES3 dosyasından yükler. Restore hatasında gameplay başlangıç bildirimi verilmeden kullanılabilir MainMenu'ye dönülmesi |

Bu kontrollerin her biri düzeltme öncesi FAIL, düzeltme sonrası PASS verdi. RestoreFailureStopsStartup sırasında `Could not initialize saveable` ve `Could not start the loaded scene` hata logları kasıtlı bozuk fixture'ın beklenen çıktılarıdır. Metadata testindeki üst bileşenin dosya olduğu senaryo bağımsız incelemede bulundu; ayrı FAIL/PASS döngüsüyle kapatıldı.

Testler `Temp/SaveReviewChecks/<benzersiz kimlik>` altında çalışır ve sınır kontrolü sonrası finally içinde temizlenir; gerçek kullanıcı kayıtlarına dokunmaz. Sahne beklemeleri yalnız dosya adına değil tamamlanmış başlangıca göre yapılır. UI kayıtsız dönüş kontrolü de menünün zamanının açılmasını bekler.

Düzeltmeler sonrası StorageRoundTrip, Lifecycle, QuickSaveInput, DI RunInvalidSaveRegression/RunLegacySaveAndDynamicItem/RunLifetimes ve UI RunSceneFlow/RunPanels/RunRestartAndHud tekrar PASS verdi. Editor derlemesi ve 66 assembly'lik Windows player C# derlemesi başarılı. Sonraki tam executable build çalıştırılmadı. Bağımsız tekrar incelemede açık önemli bulgu kalmadı.

## Bütün kayıt mimarisi — 14 Kasım 2026

Güncel sorumluluklar [SaveSystem rehberinde](../Assets/Scripts/Systems/SaveSystem/README.md). Önceki bölümler yapılan geçişlerin geçmişini tutar; güncel kodda ISaveDataReader, DynamicSaveableEntity, public CurrentGameSaveData veya merkezi sahne Restore'u yoktur.

- `AgentScripts/SaveArchitectureChecks.cs` / `SaveArchitectureChecks.Contract`: kimliğin tek sahibi, açık StateType, kapalı oturum verisi, yalnız context'ten snapshot devri ve kaldırılan katmanları kontrol eder. Önce FAIL, uygulama sonrası PASS.
- DI RunLifetimes artık context ve factory'nin sahne değişince yeniden oluştuğunu da doğrular; global servis kimlikleri korunur.
- Eski fixture'lar session verisini yalnız test tarafındaki reflection yardımcılarıyla yerleştirir/okur. Bu yetki üretim API'sine eklenmemiştir. Capture da yalnız test helper'ıyla çağrılır; üretim komutu context.QuickSave'dir.
- ES3 DTO tür/alan adları ve metadata format sürümü korunmuştur. StorageRoundTrip ve metadata hata testleri PASS; kullanıcı kayıt dosyaları testlerde kullanılmamıştır.
- Lifecycle, iki envanter izolasyonu, başarısız statik Restore dönüşü, DI invalid/legacy-item/lifetime, F5 tek kayıt ve UI scene-flow/panels/restart-HUD kontrolleri PASS.
- Windows player C# derlemesi 66 assembly üretti. Bu bir executable build değildir.
- Bağımsız inceleme, child injection sırası, disposal ve dinamik nesne akışlarını değerlendirdi. Snapshot sahipliği sınırı public API'den kapatıldı; generic deep-copy katmanı eklenmedi.

Başlangıç/pause regresyonu önce FAIL, düzeltme sonrası PASS verdi. `GameService.PauseGame`, başlangıç sürerken false döner; GameUI yalnız kabul edilen istek için panel açar. RunStartupPause, RunSceneFlow, QuickSaveInput, RunPanels ve RunRestartAndHud tekrar PASS verdi. Çağrılmayan HandleSaveButton kaldırıldı; aktif F5 girişi LocomotionState.Update içinde kalır.
