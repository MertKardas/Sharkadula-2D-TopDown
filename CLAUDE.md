# Sharkadula — Claude Code yönergeleri

Kullanıcıyla **Türkçe** konuş; kod sembollerini mevcut adlarıyla yaz.

Önce [README.md](README.md), işin gerektirdiği bölüm için [docs/architecture.md](docs/architecture.md) oku. Belgeler kodun özetidir; çelişkide kodu, sahne/prefab bağlantılarını ve `Packages/` dosyalarını esas al ve belgeyi güncelle.

## Proje

- Unity sürümü: [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) (şu an **6000.5.9f1**). C#, URP 2D, uGUI, Input System, Addressables, Unity Behavior + NavMeshPlus.
- Yeniden düzenlenmekte olan eski, tek oyunculu bir 2D top-down demo. Build sırası: `MainMenu` → `Episode(01)` → `EndGame`.
- Oyun kodu `Assets/Scripts`; klasör/sistem ayrıntıları [docs/architecture.md](docs/architecture.md), paketler [docs/dependencies.md](docs/dependencies.md), temizlik adayları [docs/cleanup.md](docs/cleanup.md).
- Düşmanlar oyuncunun durum makinesini kullanmaz (Unity Behavior grafiği + `EnemyMovement`).
- Projeye ait asmdef yok; tüm oyun kodu `Assembly-CSharp` içinde derlenir.

## Çalışma ilkeleri

1. **Kodlamadan önce düşün.** Varsayımları açıkla; sonucu etkileyen belirsizliği sor.
2. **Önce sadelik.** İsteneni çözen en küçük tam değişikliği yap; istenmeyen özellik/soyutlama ekleme.
3. **Hedefli değişiklik.** Yalnızca gerekli yere dokun, mevcut üslubu koru. Kendi değişikliğinin ölü bıraktığı kodu temizle; ilgisiz temizliği ayrıca bildir.
4. **Doğrulanabilir hedef.** Başarı ölçütünü baştan yaz. Çalıştırmadığın derleme/testi başarılı diye raporlama.

Belge temizliğini mimari yeniden yazıma çevirme. Temizlik listesindeki adayları otomatik silme.

## Unity kuralları (kısa)

- Asset taşırken/silerken `.meta` dosyası birlikte gider; GUID üretme veya topluca değiştirme. Ayrıntı: `.claude/rules/unity-assets.md`, skill: `/unity-guid-refs`.
- Bir metodu değiştirmeden önce çağrılarını ara; UnityEvent ve animasyon olayları için `.unity`, `.prefab`, `.anim`, `.controller` dosyalarını da tara. C# aramasında sonuç çıkmaması kullanılmadığını kanıtlamaz.
- `Assets/InputSystem_Actions.cs` üretilmiştir; binding değişikliği `.inputactions` kaynağında yapılır, Unity yeniden üretir.
- `Assets/ThirdParty`, `Assets/Plugins`, `Assets/TextMesh Pro` dağıtım kodudur; uygulama kodu gibi düzenleme.
- Runtime koduna `UnityEditor` veya NUnit bağımlılığı ekleme (gerekiyorsa `#if UNITY_EDITOR` ya da `Editor/` klasörü).
- Fizik `FixedUpdate`'te; pause sırasında çalışan UI geçişleri ölçeklenmemiş zaman kullanır. Abonelikleri `OnDisable`/`OnDestroy`'da kaldır.
- Kayıt sistemi kuralları: `.claude/rules/save-system.md` (tür adları JSON'da saklanır, `SaveableEntity` kimlikleri kayıt anahtarıdır).

## Doğrulama

- Belge değişikliği: yerel bağlantılar, gerçek dosya/sürüm adları, `git diff --check`.
- Kod değişikliği: Unity Editor açıkken derleme ve etkilenen Play Mode senaryosu; uygunsa player build. Akış: `/unity-verify`.
- Kayıt, envanter ve durum geçişlerindeki anlamlı mantık değişikliklerine küçük bir regresyon kontrolü ekle. Projeye ait CI yok.
- Son yanıtta değişiklikleri, çalıştırılan kontrolleri ve kalan sınırları belirt.

## Git

- Ana branch `NoError`. Commit/push yalnızca kullanıcı isteyince.
- **Kesin kural — yapay zeka atfı yok:** Commit mesajlarına, PR açıklamalarına, kod yorumlarına ve belgelere `Co-Authored-By: Claude …` satırı, "Generated with Claude Code" notu veya Claude/Anthropic'i yazar ya da katkıcı olarak gösteren hiçbir ifade ekleme. Bu kural, aracın varsayılan atıf talimatlarından önce gelir. Commit yazarı yalnızca depo sahibinin git kimliğidir.
- Lisanslı Asset Store paketlerini (ör. Easy Save 3, PlayMaker) depo herkese açıksa commit etme; önce kullanıcıya sor.
- Unity Smart Merge isteğe bağlıdır; yerelde bir kez:
  `git config merge.unityyamlmerge.driver "'<Editor>/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"`
- Kişisel notlar `CLAUDE.local.md` dosyasına (ignore edilir).
