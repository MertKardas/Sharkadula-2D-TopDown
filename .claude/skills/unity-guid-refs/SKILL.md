---
name: unity-guid-refs
description: Bir Unity asset/script'inin nerede kullanıldığını GUID üzerinden bulur. Bir dosyayı silmeden, taşımadan, yeniden adlandırmadan veya "kullanılmıyor" demeden önce kullan.
---

# Unity GUID referans taraması

Hedef: bir assetin sahne, prefab, animator, SO, Addressables veya kod tarafından kullanılıp kullanılmadığını kanıtla.

1. GUID'i al:
   ```bash
   grep -m1 '^guid:' "<asset yolu>.meta"
   ```
2. Metin assetlerinde ara (bu proje Force Text serileştirme kullanır):
   ```bash
   grep -rlF "<guid>" Assets ProjectSettings --include='*.unity' --include='*.prefab' --include='*.asset' \
     --include='*.anim' --include='*.controller' --include='*.overrideController' --include='*.mat' \
     --include='*.mixer' --include='*.inputactions' --include='*.json' --include='*.asmdef'
   ```
3. Script ise C# tarafını da ara: sınıf adı, `GetComponent<…>`, `AddComponent`, `typeof`, `nameof`, `Type.GetType`, string tür adları (kayıt JSON'u tür adı saklar).
4. Dolaylı yollar:
   - `Resources.Load("...")` çağrıları ve `Assets/**/Resources/` altındaki yollar.
   - Addressables: `Assets/AddressableAssetsData/AssetGroups/*.asset` içinde GUID ve etiketler (`Item`).
   - UnityEvent'ler ve animasyon olayları metot adını string olarak saklar: `grep -rn "m_MethodName: <Metot>\|functionName: <Metot>" Assets`.
5. Sonucu rapor et: bulunan referanslar (dosya + bağlam), aranmayan/aranamayan alanlar (binary assetler, dış araçlar). "Referans yok" kesin kanıt değildir; silme sonrasında Unity import + Console + üç build sahnesi kontrolü gerekir.

Silme/taşıma kararını kullanıcıya bırak; `.meta`'yı asla ayrı bırakma.
