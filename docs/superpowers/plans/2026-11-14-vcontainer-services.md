# VContainer Services Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans in this session.

**Goal:** Global saf C# servisleri VContainer ile oluşturup bütün oyun tüketicilerini inject etmek.
**Architecture:** AppLifetimeScope kalıcı servisleri, SceneLifetimeScope sahne injection'ını yönetir.
**Tech Stack:** Unity 6000.5.9f1, VContainer 1.19.0, mevcut Input System/Addressables/PrimeTween.
**Spec:** ../specs/2026-11-14-vcontainer-services-design.md (kullanıcı onayladı).

## Global Constraints

- Kayıt modelleri, state tür adları, SaveableEntity kimlikleri ve UI görünümü korunur.
- Paket ve önceki UI değişiklikleri korunur; worktree/commit/paket yükseltmesi yapılmaz.
- Açık Unity Editor bu checkout'ta olduğundan yerinde uygulanır.
- GameService globaldir; sahne UI'sini saklamaz. SceneSaveLoader durumsuz global servistir.

## Review Focus

- Her build sahnesini doğrudan açma: root bir kez oluşur, OnEnable'dan önce injection.
- Menü/bölüm döngüsü: aynı global servisler, sahneye ait nesneler sızmaz.
- JSON deserialize: singleton oluşturulmaz; eski InventoryItem türü yüklenir ve katalogla bağlanır.
- Dinamik prefab: injection ilk aktif yaşam döngüsünden önce tamamlanır.
- Kapanış/dispose: input, Addressables ve ses callback'leri eski servislere erişmez.

### Task 1: Eski asset ayarlarını koru, regresyonu oluştur

Files: AgentScripts/DIRegressionChecks.cs; geçici Editor migration scripti.

- [x] JSON constructor bağımlılığını başarısız kontrolle doğrula.
- [x] AudioManager mixer/grup ayarlarını ve sahne SaveableEntity kimliklerini yedekle.
- [x] Editor ile eski manager bileşenlerini kaldır; yeni config asseti oluştur.

### Task 2: Saf servisler ve injection

Files: Systems altındaki managerlar -> GameService, InputService, AudioService,
GameSaveService; ItemDataBase -> ItemCatalog; SceneSaveLoader; InventoryComponent;
PlayerController/States, UI ve dünya tüketicileri.

- [x] Constructor injection, IDisposable ve AudioService.ITickable uygula.
- [x] Bütün .Instance tüketimlerini inject edilmiş bağımlılıklarla değiştir.
- [x] InventoryItem JSON constructor'ını veri olarak tut; LoadInventory'de katalogla bağla.
- [x] GameService sahne başlangıcını/kayıt restore'unu koordine etsin; UI bitiş olayını dinlesin.

### Task 3: Scope assetleri ve sahne entegrasyonu

Files: Systems/DependencyInjection/AppLifetimeScope.cs, SceneLifetimeScope.cs,
AudioConfiguration; root prefabı ve VContainerSettings asseti; üç build sahnesi.

- [x] Root global kayıtlarını kur; scene scope parent ve inactive injection'ını ayarla.
- [x] Dinamik eşya üretimini resolver.Instantiate'a geçir.
- [x] Eski script GUID referanslarını ve scene/prefab UnityEvent hedeflerini tara.
- [x] Unity derlemesi ve DI/JSON regresyonlarını çalıştır.

### Task 4: Oyun akışları, belgeler ve son inceleme

Files: AgentScripts/UIRegressionChecks.cs ve README; proje README/mimari/bağımlılıklar.

- [x] Önceki UI kontrollerini yeni servis/scope erişimine güncelle.
- [x] Üç sahne doğrudan açılışını, global servis kimliklerini ve dinamik injection'ı test et.
- [x] Eski şema kayıt, restart, HUD/envanter ve UI sahne akışını doğrula.
- [x] Bağımsız son kod incelemesi ve git diff --check; eksik SDK build sınırını raporla.

## Sonuç

Editor ve Windows player C# derlemesi geçti (66 assembly). UI ve DI
regresyonları geçti; üç sahne doğrudan açıldı, global instance kimlikleri
korundu. Root resolver dinamik dünya nesnelerini açık null parent ile
etkin sahnede üretir. EndGame butonu eski manager yerine scene controller
UnityEvent hedefini kullanır. Kayıt model/state türleri, meta GUID
kimlikleri ve sahne kayıt anahtarları korundu. Gerçek kayıt dosyaları
değiştirilmedi. Tam IL2CPP executable build eksik SDK nedeniyle yapılmış
sayılmaz. Ayrıntılar AgentScripts/README.md içinde.
