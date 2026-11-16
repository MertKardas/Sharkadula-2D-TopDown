# UI PrimeTween Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans; implement in this session.

**Goal:** Görünümü koruyarak tutarlı UI ve PrimeTween geçişleri.
**Architecture:** GameUI panel durumunu ve Esc girişini yönetir. Mevcut panel
ve ortak MenuButtonUI bileşenleri korunur; butona özel sınıf eklenmez.
**Tech Stack:** Unity 6000.5.9f1, uGUI, PrimeTween 1.5.2.
**Spec:** ../specs/2026-11-14-ui-primetween-design.md

## Global Constraints

- Mevcut sahne/prefab GUID'leri, görsel tasarım ve UnityEvent bağlantıları korunur.
- AudioManager fade'leri kapsam dışı; LeanTween dağıtımı silinmez.
- Kullanıcının mevcut dosya değişiklikleri korunur.
- Karar: açık Unity Editor ve kurulmuş paketler bu checkout'ta olduğundan yerinde
  uygulanır; yeni worktree, otomatik commit veya paket yükseltmesi yapılmaz.

## Review Focus

- Hızlı Esc/tekrarlı click: tek panel, çakışmayan geçiş.
- Geçiş sırasında ölüm: önceki panelin callback'i ölüm panelini geri kapatamaz.
- Pause altında fade: zaman dururken UI tamamlanır.
- Panel kapatıp açma: normal ölçek, güncel HUD/envanter, tek abonelik.
- Başarılı kayıttan restart: sahne tekrar yüklenir, Playing durumu geri gelir.

### Task 1: Regresyon ve geçiş

Files: AgentScripts/UIRegressionChecks.cs, Assets/Scripts/UI/Common/UITransition.cs,
Assets/Scripts/UI/Common/IPanelTransition.cs, Assets/Scripts/UI/Common/MenuButtonUI.cs.
Interface: Sequence Transition(GameObject from, GameObject to).

- [x] Eski kodda transition raycast ve buton deselect regresyonlarını çalıştır.
- [x] PrimeTween geçişi, kendi tween'ini iptal etme ve ortak buton davranışını uygula.
- [x] Aynı kontrolleri çalıştır; beklenen: geçen kontroller.

### Task 2: Panel durumları ve bağlı UI

Files: GameUI, PauseMenu, SettingsMenu, DeathMenuUI, StatsPanelUI,
GameSaveTextUI, InventoryUI, FadeInScene, SquareHighlight, GameManager, UIManager.
Interfaces: GameUI.OpenPauseMenu/ResumeGame/OpenSettings/ReturnToPauseMenu;
Tween FadeOutScene(); GameManager çağrısı OnComplete kullanır.

- [x] Esc/pause durumları, geçişte ölüm ve tekrar enable senaryolarını kontrol et.
- [x] Tek Esc sahibi, gerçek pause, güvenli panel state ve UI animasyonlarını uygula.
- [x] RestartGame başarılı kayıt sonrası kayıtlı sahneyi yeniden açsın.
- [x] Derleme ve Play Mode regresyonlarını çalıştır; beklenen: doğru zaman/input/panel.

### Task 3: Serialized bağlantılar ve son kontrol

Files: UITransition asseti, Door prefabı ve gerekli UI sahne/prefab bağlantıları;
README.md, docs/architecture.md ve docs/dependencies.md ilgili UI açıklamaları.

- [x] Easing değerlerini anlamlarına göre Editor üzerinden dönüştür.
- [x] UnityEvent hedeflerini, eksik scriptleri, buton click sesini ve odağı doğrula.
- [x] MainMenu → Episode(01) → EndGame akışını, ölüm/restart ve HUD'ı kontrol et.
- [x] Uygunsa player build al; git diff --check çalıştır; bağımsız son kod incelemesi yap.
- [x] Çalışan/çalıştırılamayan doğrulamaları ve kalan riskleri raporla.

## Sonuç

Beş MCP regresyon kontrol grubu PASS; Editor derlemesi başarılı.
1280×720 pause/ayarlar görsel kontrolü tamamlandı. Bağımsız son incelemede
önemli açık bulgu kalmadı. Windows IL2CPP build denemesi C#/sahnelerden sonra
eksik Windows SDK nedeniyle tamamlanamadı; başarılı build olarak sayılmadı.
Ayrıntılı sınırlar AgentScripts/README.md içinde.
