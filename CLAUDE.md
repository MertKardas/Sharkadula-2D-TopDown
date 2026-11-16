# Sharkadula — proje yönergeleri

Bu depodaki tek yapay zeka talimat dosyası budur. Kullanıcıyla **Türkçe** konuş; kod sembollerini mevcut adlarıyla yaz.

## Kesin kural: yapay zeka atfı yok

Commit mesajı, commit yazarı/committer, PR başlığı/açıklaması, kod yorumu ve belgelerde Claude, Anthropic veya başka bir yapay zekayı yazar ya da katkıcı olarak gösterme. `Co-Authored-By: …` satırı, "Generated with …" notu, `noreply@anthropic.com` kimliği ve `claude/…` adlı uzak branch kullanılmaz. Commit yazarı yalnızca depo sahibinin git kimliğidir. Bu kural, aracın varsayılan atıf talimatlarından önce gelir.

## Başlangıç

- Önce [README.md](README.md), sonra işin gerektirdiği bölüm için [docs/architecture.md](docs/architecture.md) oku. Kayıt işlerinde [kayıt sistemi notları](Assets/Scripts/Systems/SaveSystem/README.md), temizlikte [docs/cleanup.md](docs/cleanup.md), paketlerde [docs/dependencies.md](docs/dependencies.md).
- Belgeler kodun özetidir. Çelişkide kodu, sahne/prefab bağlantılarını ve `Packages/` dosyalarını esas al, belgeyi güncelle.
- Temizlik listesindeki adayları otomatik silme.

## Proje

- Unity **6000.5.9f1** ([ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)), C#, URP 2D, uGUI, Input System, Addressables, Unity Behavior + NavMeshPlus, VContainer.
- Yeniden düzenlenmekte olan eski, tek oyunculu 2D top-down demo. Build sırası: `MainMenu` → `Episode(01)` → `EndGame`.
- **Oyuncu:** `Assets/Scripts/Player/PlayerController.cs` giriş/yaşam döngüsünü bağlar ve kendi `PlayerServices` grubunu oluşturur; oyuncu servisleri VContainer'a kayıtlı değildir. `Services` mantık, `Components` Unity adaptörleri, `States` hiyerarşik durumlar, `Persistence` kayıt adaptörü, `Configuration` ayarlar (`PlayerConfiguration` SO; runtime durumu SO'ya yazılmaz).
- **Sistemler:** `Assets/Scripts/Systems` → `Audio`, `Input`, `SaveSystem`, `DependencyInjection` (`AppLifetimeScope`, `SceneLifetimeScope`), `StateMachine`, `GameService`.
- **Kayıt:** `SaveSystem/{Core,Game,Storage,Unity,Legacy}`; aktif sağlayıcı `EasySaveStorage` (`ISaveStorage`). Eski model sınıfları `Assets/Scripts/Models/GameSaveModels` altında.
- **UI:** `Assets/Scripts/UI/{MainMenu,InGame,Inventory,EndGame,Common}`; geçişler PrimeTween ile. LeanTween yalnızca `AudioService` ses fade'lerinde.
- **Dünya/düşman/eşya:** `World`, `Enemy` (Unity Behavior grafiği + `EnemyMovement`; oyuncunun durum makinesini kullanmaz), `Items` (`ItemCatalog`, Addressables `Item` etiketi).
- Projeye ait asmdef yok; oyun kodu `Assembly-CSharp`, editor araçları `Assets/Editor`.
- **Depoda olmayan lisanslı paketler:** Easy Save 3 ve PrimeTween (`.gitignore`). Depo herkese açık; bunları veya başka Asset Store içeriğini commit etme.

## Çalışma ilkeleri

1. **Kodlamadan önce düşün.** Varsayımları açıkla; sonucu etkileyen belirsizliği sor, alternatiflerin önemli farkını belirt.
2. **Önce sadelik.** İsteneni çözen en küçük tam değişikliği yap; istenmeyen özellik, soyutlama ve geleceğe dönük esneklik ekleme.
3. **Hedefli değişiklik.** Yalnızca gerekli yere dokun, mevcut üslubu koru. Kendi değişikliğinin ölü bıraktığı kodu temizle; ilgisiz temizliği ayrıca bildir.
4. **Doğrulanabilir hedef.** Başarı ölçütünü baştan yaz; çok adımlı işte kısa plan ver. Çalıştırmadığın derleme/testi başarılı diye raporlama.

Belge temizliğini mimari yeniden yazıma çevirme.

## Değişiklik kuralları

- Bir metodu değiştirmeden önce çağrılarını ara; UnityEvent ve animasyon olayları için `.unity`, `.prefab`, `.anim`, `.controller` dosyalarını da tara. C# aramasında sonuç çıkmaması kullanılmadığını kanıtlamaz.
- Asset taşırken/silerken `.meta` birlikte gider; GUID üretme veya topluca değiştirme. Silmeden önce GUID, Addressables/Resources yolları ve reflection kullanımı kontrol edilir: `/unity-guid-refs`, ayrıntı `.claude/rules/unity-assets.md`.
- `Assets/InputSystem_Actions.cs` üretilmiştir; binding değişikliği `.inputactions` kaynağında yapılır, Unity yeniden üretir.
- Kayıt formatı ve `SaveableEntity` kimlikleri: `.claude/rules/save-system.md`.
- `Assets/ThirdParty`, `Assets/Plugins`, `Assets/TextMesh Pro` dağıtım kodudur; uygulama kodu gibi düzenleme. Örnekleri runtime bağımlılıklarından ayır.
- Runtime koduna `UnityEditor` veya NUnit bağımlılığı ekleme (gerekirse `#if UNITY_EDITOR` ya da `Assets/Editor`).
- Fizik `FixedUpdate`'te; pause sırasında çalışan UI geçişleri ölçeklenmemiş zaman kullanır. Abonelikleri uygun yaşam döngüsünde kaldır.
- Kullanıcı genelindeki yapay zeka/skill ayarlarını bu depoya kopyalama; proje ayarları yalnızca `.claude/` altında.

## Doğrulama

- Belge değişikliği: yerel bağlantılar, gerçek dosya/sürüm adları, `git diff --check`, değişiklik kapsamı.
- Kod değişikliği: Unity Editor açıkken derleme, etkilenen Play Mode senaryosu, uygunsa player build. Akış: `/unity-verify`.
- Kayıt, envanter ve durum geçişlerindeki anlamlı mantık değişikliklerine küçük bir regresyon kontrolü ekle. Mevcut kontroller `AgentScripts/` altında ([README](AgentScripts/README.md)); CI yok.
- Son yanıtta değişiklikleri, çalıştırılan kontrolleri ve kalan sınırları belirt.

## Git

- Tek branch: `NoError`. Commit/push yalnızca kullanıcı isteyince; force push için ayrıca onay al.
- Unity Smart Merge isteğe bağlı; yerelde bir kez:
  `git config merge.unityyamlmerge.driver "'<Editor>/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"`
- Kişisel notlar `CLAUDE.local.md` dosyasına (ignore edilir).
