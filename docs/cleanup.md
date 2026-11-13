# Gereksiz dosya adayları

İnceleme: **13 Kasım 2026**. Bu bir silme listesi değil, referans kanıtlarıyla hazırlanmış temizlik raporudur. Bu düzenlemede oyun kodu, asset, sahne, prefab veya paket silinmedi. Üretilen `UnityUITest.slnx` yalnızca Git takibinden çıkarıldı; yerel dosya korundu.

## Yöntem ve sınırlar

- `Assets` altındaki metin biçimli sahne, prefab, animasyon, controller, asset, material, meta, JSON, input ve C# dosyaları GUID referansları için tarandı.
- C# akışları, build listesi ve paket dosyaları incelendi. Binary dosya, çalışma anında üretilen yol, reflection veya dış araç referansları için tam kullanılmama kanıtı değildir.
- Referans bulunmaması “silince sorun olmaz” garantisi vermez. Taşıma/silme sonrasında Unity import, Console, üç build sahnesi ve etkilenen akışlar kontrol edilmeli.
- Boyutlar `.meta` dosyaları dahil disk içerik toplamıdır; build boyutu tasarrufu değildir. Örnekler zaten player build'e girmiyor olabilir.

## Küçük ve güçlü adaylar

| Dosya | Bulgu | İşlem önerisi |
| --- | --- | --- |
| `Assets/Scripts/Healt.cs` ve `.meta` | Start/Update boş; GUID referansı bulunmadı. | İlk kod temizliğinde kaldırılabilir. |
| `Assets/TakeDamageBehaviour.cs` ve `.meta` | Animasyon state çıkışı boş; GUID referansı bulunmadı. | İlk kod temizliğinde kaldırılabilir. |
| `Assets/Behaviour.meta` | Karşılık gelen klasör yok; GUID referansı bulunmadı. | Yetim meta adayı; silmeden önce Unity'nin klasörü yeniden üretip üretmediğini kontrol et. |
| `Assets/AddressableAssetsData/Windows.meta` | Karşılık gelen klasör yok. | Addressables çıktısı/ayarları ile birlikte kontrol et; mevcut çıktı ignore kurallarını koru. |
| `UnityUITest.slnx` | Unity tarafından yeniden üretilebilen IDE çözümü; listelenen csproj dosyaları depoda yok. | Git takibinden çıkarıldı ve `*.slnx` ignore kuralı eklendi; yerel dosya korundu. `.vscode/settings.json` aynı yerel çözümü kullanmaya devam ediyor; Unity ile yeniden üretilebilir. |

## Boş ama bağlı dosyalar

Bu dosyaları yalnızca içerikleri boş diye silmek Missing Script bağlantıları bırakır.

| Dosya | Referans | Öneri |
| --- | --- | --- |
| `Assets/Scripts/Torch.cs` | `Assets/Prefabs/torch.prefab` | Boş bileşeni önce prefabdan kaldır; sonra script ve metasını kaldır. |
| `Assets/Scripts/UI/Main Menu UI/SavePanelUI.cs` | `MainMenu.unity` ve iki recovery sahnesi | Slot seçimi yapılmayacaksa önce sahne bileşenlerini kaldır. Yapılacaksa taslağı kullan. |
| `Assets/AttackAnimationState.cs` | `PlayerAnimationController.controller` | Animator state davranışını kaldırmadan scripti silme. |
| `Assets/Scripts/Player/AttackComponent.cs` | Oyuncu tarafından alan/başlatma üzerinden kullanılıyor | Bu bir özellik taslağı; saldırı kapsamına karar vermeden gereksiz sayma. |

## Toplu örnek ve kurtarma içeriği

| Klasör | Dosya sayısı | Yaklaşık boyut | Referans sonucu / karar |
| --- | --- | --- | --- |
| `Assets/_Recovery` | 14 | 1,83 MiB | Yedi eski sahne ve metaları. Build listesinde yok; dış GUID referansı bulunmadı. Elle açılan kurtarma kopyaları olabilir; gerekirse proje dışına arşivle. |
| `Assets/ThirdParty/LeanTween/Examples` | 279 | 4,79 MiB | LeanTween Testing sahnelerinden referanslar var; Testing ile birlikte ele al. |
| `Assets/ThirdParty/LeanTween/Testing` | 88 | 0,93 MiB | Klasör dışından GUID referansı bulunmadı. Oyuna ait testler değil. |
| `Assets/Plugins/SerializedCollections/Sample` | 10 | 0,02 MiB | Dış GUID referansı bulunmadı; runtime/editor kütüphanesini tut. |
| `Assets/TextMesh Pro/Examples & Extras` | 284 | 6,03 MiB | Dış GUID referansı bulunmadı; kullanılan fontları ve gerekli TMP kaynaklarını tut. |
| `Assets/ThirdParty/NaughtyAttributes/Scripts/Test` | 74 | 0,06 MiB | `Samples/DemoScene` içeriği buraya bağlı; örnek sahne ve test assembly bağlantılarıyla birlikte ele al. |

Bu aday gruplar toplam **749 dosya**, yaklaşık **13,66 MiB**. Klasör metaları ve NaughtyAttributes'ın ek Samples içeriği bu toplamda yok. Hiçbiri bu turda kaldırılmadı.

## Şimdilik tutulacaklar

- `Packages/manifest.json` ve `packages-lock.json`: paket çözümlemesi için birlikte tutulmalı. Bir paketin uygulama C# kodunda görünmemesi sahne/asset kullanımını dışlamaz.
- `Assets/InputSystem_Actions.cs` ve `.inputactions`: üretilmiş sarmalayıcı şu anda runtime kodundan kullanılıyor; kaynak asset ile birlikte gerekli.
- `ProjectSettings/`, URP ayarları, NavMesh ve LightingData: sahneye bağlı oldukları için topluca silinmemeli.
- Prefab ve sahne `.meta` dosyaları: GUID bağlantılarının parçası.
- `.vscode` ve `.vsconfig`: geliştirme ortamı ayarları; tek başına çöp dosya sayılmaz.
- Üçüncü taraf LICENSE/README dosyaları: atıf ve dağıtım şartları için korunmalı.
- `intro/`: README demo görseli burada; belge referansı var.

## Bu turda yapılan belge temizliği

| Önce | Sonra |
| --- | --- |
| `.github/copilot-instructions.md` | Codex'in proje yönergeleri için kökte `AGENTS.md`; yanlış klasör/özellik iddiaları düzeltildi. |
| `MEMORY_BANK.md` | Tekrarlayan ve eski içerik yerine `docs/architecture.md` ve bu rapor. Eski sürüm Git geçmişinde mevcut. |
| `README.md` | Gerçek Editor sürümü, açılış adımları, mevcut/taslak özellik ayrımı ve belge bağlantıları. |

Codex yönerge dosyası için kaynak: [OpenAI AGENTS.md rehberi](https://learn.chatgpt.com/docs/agent-configuration/agents-md).

## Temizlik sonrası kontrol

1. Unity ile yeniden import et; Console ve Missing Script durumlarını kontrol et.
2. MainMenu'den yeni oyun aç; hareket, anahtar toplama, sandık ve kapıyı dene.
3. Kayıt alıp ana menüden devam et; toplanmış eşya ve açılmış sandık durumunu kontrol et.
4. Pause ve ölümden yeniden başlatma senaryolarını bilinen sorunlarıyla birlikte doğrula.
5. Player build al; runtime bağımlılıklarını ve çıkarılan örneklere kalan referansları kontrol et.

## Git ignore düzenlemesi

Unity önbellek/build kuralları korundu. Üretilen `*.slnx` dosyaları, Rider önbelleği, işletim sistemi dosyaları ve yeni recovery çıktıları ignore edildi. Önceden takip edilen recovery sahneleri hâlâ Git içinde; ignore kuralı onları silmez veya takipten çıkarmaz. `Assets`, `Packages`, `ProjectSettings`, `.vscode` ayarları ve Codex belgeleri topluca ignore edilmedi.
