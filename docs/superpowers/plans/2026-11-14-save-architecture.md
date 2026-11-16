# Kayıt mimarisi uygulama planı

Spec: [Tasarım](../specs/2026-11-14-save-architecture.md). Kullanıcı doğrudan uygulama yetkisi verdi; mevcut Editor/çalışma ağacında inline uygulanır.

- [x] Mevcut çağrıları, olay bağlarını, silinecek tiplerin GUID referanslarını kontrol et; yeni sözleşme kontrolünü önce FAIL gör.
- [x] SceneSaveContext'i kur; Capture, veri okuma ve başlangıç kontrolünü buraya taşı. GameSaveService'i yalnız oturum/depolama koordinasyonuyla sınırla.
- [x] SaveableEntity'de kimlik/prefab kimliğini birleştir; ISavable'a StateType ekle, yinelenen UniqueId ve eski bileşen/arayüzü kaldır.
- [x] Dinamik factory'yi sahne scope'una taşı; ItemCatalog prefab çözümlemesini ortaklaştır. Root validator'dan canlı sahne/factory bağımlılığını kaldır.
- [x] GameService BeginScene/CompleteScene akışını bağla; oyuncu F5 ve sahne DI çağrılarını uyumlu güncelle.
- [x] Çalışan dosyaları .meta kimlikleriyle anlamlı klasörlere taşı; test fixture'larını yeni API'ye uyarla.
- [x] Editor, ES3 uyumluluğu, kimlik, bekleyen nesne, envanter, başarısız Restore, F5, restart/HUD ve player C# regresyonlarını çalıştır.
- [x] Bağımsız kod incelemesi, belgeler, bağlantılar, git diff --check ve son kapsam kontrolünü tamamla.
