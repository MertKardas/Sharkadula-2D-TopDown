# Oyuncu scriptlerinin düzenlenmesi

## Tasarım ve kapsam

Kullanıcı kapsamlı düzenleme ve uygulama kararları için yetki verdi. Mevcut
MonoBehaviour bileşenleri ve hiyerarşik durumlar korunur. Yeni bir karakter
çatısı kurmak yerine sorumluluklar ve yaşam döngüsü düzeltilir; yalnız biçimsel
temizlik yapmak ise mevcut hasar/fizik/abonelik sorunlarını bırakır.

- PlayerController: bileşen başlangıcı, sağlık olaylarından durum seçimi ve kayıt.
- HealthComponent: can değişikliği ve olaylar; durum makinesine bağımlılık yok.
- MovementComponent: yürüyüş/koşu için tek hız hesaplama yolu.
- Durumlar: sunum, giriş ve FixedUpdate üzerinden hareket; temiz abonelikler.
- InteractionComponent: en yakın geçerli hedef, kapanırken menzil temizliği.
- InventoryComponent: mevcut yığın/slot akışı, geçersiz miktarlara karşı koruma.
- PlayerVisualComponent: mevcut dört yön davranışı ve animasyon olayları korunur.

Kayıt DTO ve durum tür adları, serialize edilmiş alanlar ve asset GUID'leri
korunur. Saldırı mekaniği, yeni paket, genel state-machine değişikliği ve dünya/UI
yeniden tasarımı kapsam dışıdır. Önceden değiştirilmiş çalışma ağacı korunur;
başlangıç oyuncu kaynakları Temp/PlayerRefactorBaseline altında karşılaştırılır.

## Uygulama ve doğrulama

1. AgentScripts/PlayerRegressionChecks.cs ile geçersiz hasar, ölüm sırasında
   tek geçiş, disable/enable abonelikleri, fizik aşaması ve envanter sınırlarını
   önce mevcut kodda çalıştır. Beklenti: ilgili kusurlar FAIL vermeli.
2. Controller/health/state sorumluluklarını ve fizik aşamasını düzenle.
3. Hareket tekrarını, etkileşim seçimini ve envanter girişlerini sadeleştir;
   çağıranları ve animasyon/sahne referanslarını kontrol et.
4. Editor yeniden derlemesi, oyuncu regresyonları, mevcut kayıt/DTO/DI ve UI
   kontrolleri; uygun olduğunda Windows player C# derlemesi. Gerçek kullanıcı
   kayıtlarına yazma. Tam executable build ayrı bir doğrulamadır.
5. Mimari ve test rehberini güncelle, başlangıç kopyasıyla kapsam karşılaştır,
   git diff --check çalıştır ve kalan sınırları raporla.

## Özellikle incelenecekler

Hasar animasyonu sırasında ölüm; kapalı oyuncuda giriş aboneliği; tekrarlanan
enable/disable; doğrudan fizik hızının kayda yansıması; sıfır/negatif miktarlar;
yok edilmiş veya kapanmış etkileşim hedefleri; eski StateData restore yolu.

## Sonuç kaydı

- Oyuncu refactor'u tamamlandı; son `PlayerRegressionChecks.Run`: 9/9 PASS.
- Bağımsız incelemedeki kapalı restore ve kapalıyken ölüm: ayrı testlerde
  RED → GREEN, ardından toplu 9/9 PASS.
- Karar: eski kayıtlardaki büyük yığın/fazla slot miktarları restore sırasında
  korunur. Aksi karar eski ilerlemeyi yüklenemez yapardı; yeni eklemeler mevcut
  slot/yığın kurallarına tabidir. LegacySaveAndDynamicItem PASS.
- ES3 StorageRoundTrip ve HUD/restart kontrolleri PASS. Editor derlemesi PASS.
- Klavye otomasyonu tutarsız: bir MovementInput PASS sonrasında tuş olayları
  input'a ulaşmadı, F5 0 yazım verdi. Başarılı sayılmadı; oyun input koduna
  workaround eklenmedi ve mevcut F5 fixture'ı başlangıç hâline döndürüldü.
- Player C# derlemesi mevcut Input System Unity.Scripting/PreserveAttribute
  paket hatasında durdu. Paket değişikliği ve tam executable build yapılmadı.
- Küçük ertelenen borç: yok edilmiş interaction collider girdileri OnDisable'a
  kadar tutulur. Seçime girmezler; çok uzun yaşayan sensörler biriktirirse
  temizleme eklenmeli. Kodda shortcut yorumu bulunur.
- Önceden değiştirilmiş çalışma ağacı nedeniyle commit/merge yapılmadı.
