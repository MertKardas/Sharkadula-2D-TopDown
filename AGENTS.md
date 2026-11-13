# Codex proje yönergeleri

## Başlangıç

- Kullanıcıyla Türkçe konuş. Kod sembollerini mevcut adlarıyla kullan.
- Önce [README.md](README.md), sonra işin gerektirdiği bölüm için [mimari rehberi](docs/architecture.md) oku.
- Temizlik işlerinde [dosya incelemesini](docs/cleanup.md) kullan; listedeki adayları otomatik silme.
- Belgeler güncel kodun özeti; çelişkide kodu, sahne/prefab bağlantılarını ve paket dosyalarını kontrol et.

## Proje

- Unity **6000.3.3f1**, C#, URP 2D ve uGUI. Bu bir yeniden düzenlenmekte olan eski tek oyunculu demodur.
- Build sahneleri sırasıyla `MainMenu`, `Episode(01)`, `EndGame`.
- Oyuncu: `Assets/Scripts/Player/PlayerController.cs`, `IComponent` bileşenleri ve `Player/States` altındaki hiyerarşik durumlar.
- Sistemler: `Assets/Scripts/Systems`; kayıt modelleri `Assets/Scripts/Models/GameSaveModels` ve `Systems/SaveSystem/Models` altında.
- UI: `Assets/Scripts/UI`. `In Gmae Menu UI` klasör adı şu anda böyle; taşırken `.meta` kimliğini koru.
- Düşmanlar `EnemyController`, `EnemyMovement`, Unity Behavior ve NavMeshPlus kullanır; oyuncunun durum makinesini kullanmaz.
- Eşyalar `ItemData`, `ItemDataBase` ve Addressables `Item` etiketiyle yüklenir.

## Değişiklik kuralları

- İstenen işi mevcut bileşenlerle ve en küçük tam değişiklikle çöz. Belge temizliğini oyun mimarisini yeniden yazma işine dönüştürme.
- Bir metodu değiştirmeden önce çağrılarını ara. UnityEvent ve animasyon olayları için `.unity`, `.prefab`, `.anim`, `.controller` dosyalarını da tara.
- Bir Unity assetini taşırken `.meta` dosyasını birlikte taşı; GUID üretme veya topluca değiştirme.
- C# aramasında sonuç çıkmaması bir scriptin kullanılmadığını kanıtlamaz. Silmeden önce `.meta` GUID referanslarını, Addressables/Resources yollarını ve reflection kullanımını kontrol et.
- `Assets/InputSystem_Actions.cs` üretilmiştir. Binding değişikliklerini `.inputactions` kaynağında yapıp Unity ile yeniden üret.
- Kayıt sistemi tür adlarını JSON içinde saklar (`TypeNameHandling.Auto`); kayıt sınıfı/namespace değişikliklerinde eski kayıt uyumluluğunu ele al.
- `SaveableEntity` kimlikleri kayıt anahtarıdır. Prefab kopyalarında benzersizliği doğrula.
- Abonelikleri uygun yaşam döngüsünde kaldır. Fizik hareketini `FixedUpdate` üzerinden yürüt; pause sırasında çalışacak UI geçişlerinde ölçeklenmemiş zaman kullan.
- `Assets/ThirdParty`, `Assets/Plugins` ve TextMesh Pro dağıtımlarını uygulama kodu gibi düzenleme. Örnek dosyaları ile runtime bağımlılıklarını ayır.
- Projeye ait olmayan kullanıcı genelindeki Codex/skill ayarlarını bu depoya kopyalama.

## Doğrulama

- Belge değişikliği: yerel bağlantılar, gerçek dosya/sürüm adları, `git diff --check` ve değişiklik kapsamı.
- Oyun kodu değişikliği: Unity Console derlemesi, etkilenen Play Mode senaryosu ve uygun olduğunda player build.
- Kayıt, envanter ve durum geçişlerindeki anlamlı mantık değişikliklerine küçük bir regresyon testi ekle. Mevcut projeye ait otomatik test veya CI doğrulama akışı henüz yok.
- Editor dışında derlenen dosyalara `UnityEditor` veya NUnit bağımlılığı ekleme.
- Çalıştırmadığın testi/derlemeyi başarılı diye raporlama. Son yanıtta değişiklikleri, kontrolleri ve kalan sınırları belirt.
