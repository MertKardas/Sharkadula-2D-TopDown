# ID tabanlı kayıt uygulama planı

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Bu oturumda uygulama ve doğrulama yapılır.

**Goal:** Tek ID tablosu, mevcut spawner üyeliği ve açık silinme flag'iyle bölüm durumunu korumak.

**Architecture:** SceneSaveContext sahneden aday değişiklik üretir; GameSaveService doğrulama/yazma başarılı olunca oturumu değiştirir. Mevcut spawner kendi ID listesini aynı tabloda saklar. Global servise sahne referansı taşınmaz.

**Tech Stack:** Unity 6000.5.9f1, C#, VContainer 1.19.0, Easy Save 3, UniTask.

**Spec:** [Tasarım](../specs/2026-11-15-id-based-save-design.md).

## Global Constraints

- Tek SaveableEntity, mevcut ISavable ve storage sınırı korunur; yeni bağımlılık yok.
- Kullanıcının uygulama onayı belgedeki teknik önerileri kapsar: yalnız belleğe collect, F5 dosya kaydı, hedef giriş konumu, ES3 format 2 / SavesV3.
- Çalışma ağacındaki önceki değişiklikler korunur; otomatik commit ve worktree oluşturulmaz. Mevcut dosyaların uygulanan sürümü HEAD'den farklıdır; temiz checkout esas alınamaz.
- Yeni sınıflar gerektiği kadar eklenir; kaldırılan assetlerin GUID/referansları kontrol edilir.

## Review Focus

- Destroy tamamlandıktan sonra silinme ID'sinin korunması; aynı kare testi.
- Başarısız Save ardından tekrar deneme; oturum ve silinme kararlarının korunması.
- Başlamamış kapalı nesne ve silinmiş statik işaretinin tekrar collect'te korunması.
- A-B-A ve diskten reload; oyuncu ortak ilerlemesi ile hedef giriş konumunun ayrılması.
- Global ID çakışması, yetim dinamik kayıt ve bildirimsiz Destroy'un sessiz veri kaybına dönüşmemesi.

## Görev 1: Veri modeli ve aday oturum

Dosyalar: Core/GameSaveData.cs, yeni Core/EntitySaveData.cs, yeni Unity/SpawnerSaveData.cs, Game/GameSaveService.cs, Game/WorldSaveValidator.cs, Storage/EasySaveStorage.cs, Core/MetaData.cs, AppLifetimeScope.cs; kullanılmaz WorldSaveData/SceneSaveData/DynamicObjectSaveData kaldırılır.

Arayüzler: GameSaveData.ResumeSceneName + Objects; EntitySaveData.IsRemoved/PrefabId/SpawnPosition/State; GameSaveService.GetRecord(id), internal Commit(sceneName, updates, removedIds, writeToDisk).

- [x] ID tablosu/silinme sözleşmesini reflection ile denetleyen test ekle; eski kodda FAIL gör.
- [x] Modeli, spawner üyeliği doğrulamasını ve aday oturumun başarıdan sonra devrini uygula.
- [x] Storage round-trip, bozuk veri, hata sonrası oturum koruma kontrollerini çalıştır.

## Görev 2: Entity, context ve spawner

Dosyalar: Unity/SaveableEntity.cs, SceneSaveContext.cs, DynamicObjectSpawner.cs; Items/ItemDynamicObjectFactory.cs, DropObject.cs; SceneLifetimeScope.cs.

Arayüzler: SaveableEntity.MarkRemoved(); SceneSaveContext.Collect(), QuickSave(), ChangeScene(name); spawner.Spawn/Create/Track ve sabit SaveId. Context pending silinmeleri sahiplenir. Factory doğrudan çağrıları da takibe katılır; constructor döngüsü oluşturulmaz.

- [x] Flag/Destroy ve kapalı nesne/ID üyeliği kontrollerini ekle.
- [x] Tek entity ile ID atama, pending silinmeler, inactive Restore ve spawner üyeliğini uygula.
- [x] Başarısız collect/save, aynı kare silinme, yeniden spawn ve statik işaret kontrollerini çalıştır.

## Görev 3: Geçiş, oyuncu ve Editor doğrulaması

Dosyalar: GameService.cs, PlayerController.cs, Editor/SaveableEntityEditor.cs; gerekli sahne ID alanları.

Arayüzler: GameService.ChangeScene(name, Func<Result> collect) geçici callback kullanır, saklamaz; normal geçişte giriş pozisyonu ve sıfır hız; Load/Restart tam Restore. Player ID ortak rezervli kimliktir.

- [x] Normal geçiş ve restart ayrımını, global kimlikleri ve A-B-A senaryosunu test et.
- [x] Giriş input/zaman kilidi, aday collect, sahne yükleme ve hatada geri dönüşü uygula.
- [x] Spawner ID'lerini mevcut scope üzerinde serialize et; build öncesi global kimlik doğrulaması ekle.
- [x] A-B-A, dosyadan reload, oyuncu ve menü/restart akışlarını doğrula.

## Görev 4: Fixture'lar, belgeler ve son inceleme

Dosyalar: AgentScripts/SaveRegressionChecks.cs, SaveReviewRegressionChecks.cs, SaveArchitectureChecks.cs, DIRegressionChecks.cs, UIRegressionChecks.cs; README.md, docs/architecture.md, SaveSystem/README.md, AgentScripts/README.md.

- [x] Eski model fixture'larını yeni tabloya uyarla; anlamsız genel veri kabı testini değiştir.
- [x] Editor derlemesi ve ilgili Play Mode regresyonlarını çalıştır; player C# derlemesini dene (başarısız, aşağıdaki sınır).
- [x] Bağımsız kod incelemesi; anlamlı bulguları regresyonla düzelt.
- [x] Belge bağlantıları, diff kapsamı ve git diff --check; sonuç/sınırları kaydet.

## Yürütme kaydı

- Başlangıç: Unity CLI 1.0.0 güncel; Editor 6000.5.9f1 hazır. CLI sandbox dışında erişilebiliyor.
- Ruling: Kullanıcı mevcut proje üzerinde uygulama istedi; 136 önceden değiştirilmiş tracked dosya ve yeni runtime kaynakları var. Yerinde çalışılır; HEAD tabanlı worktree mevcut kaynakları taşımaz.
- Ruling: Yeni uygulama/entegrasyon bu oturumda yapılacak; plan için tekrar onay istenmeyecek. Kullanıcı tasarımı ve uygulamayı açıkça onayladı.
- Görev 1–3: Editor derlemesi ve Contract, StorageRoundTrip, InvalidRecords, Lifecycle PASS. Gerçek A-B-A fixture'ı ve diske kayıt/restart gerçek engine içinde çalıştı.
- Final review: bağımsız salt okunur inceleme yapıldı. DelayedPlayerPolicy, ReservedPlayerRecord, RemovedEntityReactivation ve LoadingGuard önce FAIL, düzeltme sonrası PASS. Lifecycle bu korumaları ve başarısız geçiş geri dönüşünü de doğrular.
- Final review: fixture'ın gerçek sahneleri yeniden kaydetmesi kaldırıldı; yalnız additive geçici B kopyası kaydedilir. Tek seferlik sahne ID değişimindeki ilgisiz serialization farkları ayıklandı; her build sahnesinde yalnız bir spawnerSaveId eklendi.
- Görev 4: MetadataErrors; DI invalid/legacy-item/lifetimes; iki envanter izolasyonu; RestoreFailureStopsStartup; F5 tek kayıt; UI Run/RunAssets/RunSceneFlow/RunStartupPause/RunPanels/RunRestartAndHud PASS. Geçici B sahnesi ve build girdisi kaldırıldı, input test ayarları geri alındı.
- Sınır: Windows player C# derlemesi denendi, paketlerde Unity.Scripting/PreserveAttribute referansı bulunamadığından başarısız. Tam executable build yapılmadı; üçüncü taraf veya Editor kurulumu değiştirilmedi.
- Araç kurtarma: Editor kuyruğu bir recompile sırasında takıldı. Kullanıcı kaydedilmemiş düzenleme olmadığını doğrulayıp zorla yeniden başlatmayı onayladı; doğrulama yeni oturumda sürdürüldü. Unity restart Temp altındaki ilk baseline/scratch dosyalarını temizledi; kalıcı ilerleme kaydı bu plandadır.
