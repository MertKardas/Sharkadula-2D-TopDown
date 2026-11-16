# Kayıt incelemesi düzeltmeleri

Kullanıcı dört bulgunun düzeltilmesini onayladı. Uygulama mevcut Editor/çalışma ağacında yapılır; eski kullanıcı kayıtları kullanılmaz.

- [x] Capture ve LoadInventory sınırlarında envanter liste/eleman kopyası. GetInventory'nin UI için mevcut davranışını koru. Capture/Restore izolasyon regresyonları önce FAIL, sonra PASS olmalı.
- [x] Dünya doğrulamasını WorldSaveValidator'a taşı; GameSaveService yalnız koordinasyonu yürütsün. Mevcut doğrulama senaryoları geçmeli.
- [x] Nesnelerin kendi Start Restore akışını koru. Başlangıç hatasını kaydet; GameService başarılı başlangıç doğrulamasından önce zaman/input açmasın. Başarısız sahne başlangıcı MainMenu'ye dönsün; gameplay OnGameStarted bildirilmesin.
- [x] Metadata listeleme ISaveStorage üzerinde Result + out liste dönsün. Eksik klasör başarılı boş liste, okunamayan/geçersiz kök hata, tek bozuk slot diğerlerini engellememeli. Eski SaveService algoritmasını değiştirmeden ince arayüz uyarlaması yap.
- [x] DI, UI ve AgentScripts çağrılarını uyumlu güncelle; Editor/Play Mode regresyonları ve player C# derlemesini çalıştır.
- [x] Belgeleri, sonuçları ve bağımsız kod incelemesini tamamla; git diff --check.
