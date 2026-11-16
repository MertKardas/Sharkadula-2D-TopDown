---
paths:
  - "Assets/Scripts/Systems/SaveSystem/**"
  - "Assets/Scripts/Models/**"
  - "Assets/Scripts/**/*Save*.cs"
  - "Assets/Scripts/**/Persistence/**"
---

# Kayıt sistemi kuralları

- JSON, `TypeNameHandling.Auto` ile tür adlarını saklar. Kayıt modeli sınıfı, namespace'i veya assembly'si değişirse eski kayıtlar okunamayabilir: uyumluluk (binder, göç kodu veya bilinçli kırılma kararı) ele alınmalı ve kullanıcıya söylenmeli.
- Oyuncu durum makinesinin üst/alt durum tür adları da kayda yazılır; durum sınıflarını yeniden adlandırmak kayıt formatını değiştirir.
- `SaveableEntity.UniqueId` sahne verisinin anahtarıdır. Prefab kopyası veya çoğaltılmış nesnede kimliğin benzersiz olduğunu doğrula.
- `FindAllSavables()` etkin olmayan nesneleri de toplar; toplanmış eşyanın durumu buna dayanır.
- Gerçek kullanıcı kayıtlarına (`Application.persistentDataPath/Saves`) test sırasında yazma; geçici dizin kullan veya önce yedek al.
- Kayıt mantığı değişikliğinde en az bir round-trip regresyon kontrolü ekle (kaydet → yükle → durum eşit).
