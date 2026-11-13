# Mimari ve mevcut durum

İnceleme: **13 Kasım 2026**, `NoError` branchi, kaynak başlangıcı `9d2f16f`. Bu belge mevcut kodu anlatır; yeni mimari taahhüdü veya Unity'de test edilmiş sonuç değildir.

## Başlatma ve sahneler

Build listesi `MainMenu`, `Episode(01)`, `EndGame` sırasındadır. Ana menüde `UIManager`, yeni oyun için ilk sahneyi; devam etmek için `SaveManager.QuickLoad()` sonrasında kayıtlı sahneyi açar.

`GameManager`, `InputManager`, `AudioManager`, `SaveManager` ve `ItemDataBase`, `MyUtility.Singleton<T>` kullanır. Bu nesneler sahneler arasında korunur. Bağımlılıkları Inspector üzerinden verilen managerlar için otomatik oluşturulan boş bir GameObject'in yeterli olduğunu varsayma.

## Oyuncu ve durum makinesi

`PlayerController.Awake()` üst durumları oluşturur ve bileşenlere `Initialize(this)` çağırır. `Update`/`FixedUpdate`, mevcut duruma aktarılır.

```text
PlayerController
  LocomotionState
    IdleState
    MoveState
    RunState
  TakingDamageState
  DeathState
```

- `InputManager`: üretilmiş Input System sınıfı üzerinden eylem okuma ve abonelik.
- `MovementComponent`: Rigidbody2D hızını yürüyüş/koşu hedefine yaklaştırma.
- `HealthComponent`: can, hasar ve ölüm olayları.
- `InteractionComponent`: trigger menzilindeki etkileşilebilir nesneler arasından en yakın uygunu seçme.
- `InventoryComponent`: slotlar, yığınlar, para ve UI olayları.
- `PlayerVisualComponent`: yön/animasyon parametreleri, sesler ve animasyon bitiş olayları.
- `AttackComponent`: yalnızca başlangıç iskeleti; saldırı işlemi henüz yok.

`StateMachine<T>` durumları türle saklar; `HierarchicalState<T>` alt makineyi yönlendirir. Kayıt sırasında üst ve alt durum tür adları tutulur. Türleri yeniden adlandırmak eski kayıtları etkiler.

## Dünya ve düşmanlar

`InteractableBase`, menzil ve `CanInteract` ortak davranışını sağlar. Sandık bir kez para verir; anahtar `DropObject` üzerinden envantere eklenir; kapı anahtarı tüketip kilidi açar. Açılmış kapıyla sonraki etkileşim `GameManager.EndGame()` üzerinden bitiş sahnesine geçer.

Tuzak hasarı `IDamageable` üzerinden verir, collider ve ses zamanlaması animasyon olaylarıyla yürür. Bu olaylar C# çağrı aramasında görünmeyebilir.

`EnemyController`, `EnemyData` ayarlarını Unity Behavior blackboard'una yazar. `EnemyMovement`, 2D düzleme uyarlanmış NavMeshAgent'ı kullanır. Düşmanların karar sistemi oyuncunun durum makinesinden ayrıdır. Düşman ölümü hareketi ve grafiği durdurur; loot/ölüm sunumu henüz tamamlanmış değildir.

## Kayıt ve yükleme

```text
QuickSave
  SceneSaveLoader.CollectSceneData
    ISavable.CaptureState
  GameSaveData.SceneData[sahne]
  SaveService.Save
    savedata.json + metadata.meta

Ana menüden devam et
  QuickLoad → LoadGame → CurrentGameSaveData
  Kayıtlı sahneyi yükle
  sceneLoaded → DistributeSaveData
    Dinamik eşyaları oluştur → ISavable.RestoreState
```

`SaveService` disk ve JSON işlemlerini, `SceneSaveLoader` nesne bulma ve durum dağıtımını yapar. Oyuncu, kapı, sandık, tuzak, düşman ve yerdeki eşyalar `ISavable` uygular. `SaveableEntity.UniqueId`, sahne verisi sözlüğünün anahtarıdır.

`FindAllSavables()` etkin olmayan nesneleri de toplar; toplanmış eşyanın sıfır miktarlı durumunu korumak açısından bu davranış önemlidir. Dinamik eşya kayıtları prefabı `ItemDataBase` üzerinden bulup yeniden üretir. Mevcut yükleyici genel bir dinamik nesne sistemi değil, `DropObject` bileşenini varsayan bir eşya yükleyicisidir.

`ItemDataBase`, Addressables `Item` etiketini senkron tamamlanma bekleyerek yükler. Başlatma sırası, yükleme hatası ve handle yaşam döngüsü yeniden düzenlemede kontrol edilmelidir.

## UI ve ses

UI mevcut durumda **uGUI** kullanır. `GameUI` oyun içi panelleri bağlar; `UITransition` bir ScriptableObject olarak LeanTween fade geçişlerini tanımlar. Menü açılması ile oyunun durması ayrı işlemlerdir; bunların bağlanması eksiktir.

`AudioManager`, efektler için `ObjectPool<AudioSource>`, müzik için ayrı bir kaynak tutar. `AudioData` klip seçimi, ses/pitch değişimi ve uzamsal ayarları taşır. Ses ayarları Audio Mixer'a uygulanır ve PlayerPrefs'te tutulur. Havuz kullanılması tüm sistemin tahsissiz olduğunu veya aktif ses sayısının 64 ile sınırlandığını kanıtlamaz.

## Öncelikli teknik işler

Bunlar statik incelemede görülen durumlardır; bu belge düzenlemesinde düzeltilmedi.

| Öncelik | Konum | Mevcut sorun | İlk doğrulama / küçük çözüm |
| --- | --- | --- | --- |
| 1 | `GameManager.RestartGame` | QuickLoad başarılıysa yalnızca veri belleğe alınır; sahne tekrar yüklenmez. | Kayıt al, öl, yeniden başlat. Başarılı yüklemede kayıtlı sahneyi aç. |
| 1 | `GameUI.OnCancel`, `PauseMenu` | Panel geçişi pause/unpause metotlarını çağırmaz. | Hareket ederken Esc aç/kapat; zamanı ve player inputunu mevcut manager metotlarıyla yönet. |
| 1 | `LocomotionState.Update` | F5 basılı kaldığı her karede kayıt yazılır. | Tuşu basılı tutunca yalnızca bir kayıt oluşmasını doğrula; tek basış kullan. |
| 1 | `SaveService.Save` | Mevcut kayıt doğrudan üzerine yazılır; yarım yazım önceki kaydı bozabilir. | Geçici dosyaya yazıp değiştir; hata halinde eski kaydın okunabildiğini test et. |
| 2 | `SaveableEntity.OnValidate` | Kimlik yalnızca boşsa üretilir; kopyalanan nesneler aynı kimliği taşıyabilir. | Prefabı çoğaltıp kayıt anahtarlarını kontrol et; sahne içinde benzersizliği doğrula. |
| 2 | Runtime script importları | `ItemData`/`DropShadow` UnityEditor, `TakingDamageState`/`MenuButtonUI` NUnit import ediyor. | Gerçek player build ile doğrula; gereksiz importları kaldır veya editor kullanımını koşullu tut. |
| 2 | `SaveManager.Awake` | `sceneLoaded` anonim aboneliği OnDestroy'da kaldırılmıyor; yinelenen manager riski var. | Menü–bölüm geçişlerini tekrarla; abonelik ve tek instance yaşam döngüsünü doğrula. |
| 2 | Test altyapısı | Projeye ait otomatik test/CI akışı yok. | Önce kayıt round-trip, dolu envanter ve ölümden dönüş regresyonlarını kapsa. |

## Yeniden düzenleme sınırı

Önce mevcut demoyu açılabilir ve temel akışları doğrulanabilir duruma getir. Sonra dosya temizliği, bağımlılık sadeleştirme ve özellik/mimari değişikliklerini ayrı, küçük değişiklikler olarak uygula. Bu belgede klasör taşıma, paket kaldırma veya kayıt formatı değiştirme kararı verilmiş değildir.

Dosya adayları ve referans bulguları için [temizlik raporuna](cleanup.md), çalışma kuralları için [AGENTS.md](../AGENTS.md) dosyasına bak.
