---
paths:
  - "Assets/**"
  - "ProjectSettings/**"
  - "Packages/**"
---

# Unity asset kuralları

- Her asset ve klasörün `.meta` dosyası GUID taşır; sahne, prefab ve SO bağlantıları bu GUID'e işaret eder.
- Taşıma/yeniden adlandırmada dosyayı ve `.meta`'yı birlikte taşı (`git mv` ikisine de). Yeni `.meta` yazma; Unity üretsin.
- Silmeden önce GUID'i tüm metin assetlerinde ara (`/unity-guid-refs`). Addressables grupları (`Assets/AddressableAssetsData`), `Resources/` yolları ve reflection/tür adı kullanımı da kontrol edilir.
- Boş ama bağlı bir scripti silmeden önce bileşeni sahne/prefab/controller'dan kaldır; aksi hâlde Missing Script kalır.
- `.unity`, `.prefab`, `.asset` dosyalarını elle düzenlemekten kaçın. Gerekirse küçük, hedefli düzenle ve Unity'de açıp yeniden kaydetmeyi öner.
- Klasör adlarındaki yazım hataları (ör. `Healt.cs`, `Skelaton.asset`) GUID ile bağlıdır; yeniden adlandırma ayrı, bilinçli bir iş olmalı.
- `Packages/manifest.json` ile `packages-lock.json` birlikte değişir; paket ekleme/kaldırma Unity Package Manager üzerinden yapılır.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/` ve üretilen `*.csproj`/`*.sln` dosyalarını düzenleme veya commit etme.
