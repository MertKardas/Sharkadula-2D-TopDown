---
paths:
  - "Assets/Scripts/Systems/SaveSystem/**"
  - "Assets/Scripts/Models/**"
  - "Assets/Scripts/**/*Save*.cs"
  - "Assets/Scripts/**/Persistence/**"
---

# Kayıt sistemi kuralları

Ayrıntılı tasarım: [SaveSystem/README.md](../../Assets/Scripts/Systems/SaveSystem/README.md).

- Kayıt tek tablodur: `GameSaveData.Objects[UniqueId] -> EntitySaveData`. Aktif sağlayıcı `EasySaveStorage`; format sürümü 2, `SavesV3/quickSave.es3`. Format veya DTO değişikliğinde sürüm ve eski kayıt davranışını bilinçli ele al ve kullanıcıya söyle.
- Eski `Saves` (JSON, `TypeNameHandling.Auto`) ve `SavesV2` dosyaları okunmaz, dönüştürülmez, üzerine yazılmaz. `Legacy/` sağlayıcıdaki sınıf/namespace adları tür adı olarak JSON'da saklıdır; yeniden adlandırma eski dosyaları bozar.
- `SaveableEntity.UniqueId` kayıt anahtarıdır ve tüm build sahnelerinde benzersiz olmalıdır (tek istisna `sharkadula.player`). Kopyalanan nesnelerden sonra `Tools > Save System > Validate Build Scene IDs` çalıştır; kimlikleri topluca yeniden üretme.
- Kalıcı silme yalnızca `entity.MarkRemoved()` ile bildirilir; `SetActive(false)`, `OnDisable` veya `OnDestroy` silme sayılmaz.
- Runtime nesne üretimi `DynamicObjectSpawner.Create(prefabId, position)` üzerinden yapılır.
- Yeni DTO'lar canlı modelle mutable liste/eleman paylaşmaz.
- Gerçek kullanıcı kayıtlarına (`Application.persistentDataPath`) test sırasında yazma; geçici dizin kullan veya önce yedek al.
- Kayıt mantığı değişikliğinde en az bir round-trip regresyon kontrolü ekle (kaydet → yükle → durum eşit).
