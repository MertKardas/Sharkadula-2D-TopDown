# UI Consistency Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans for inline execution.

**Goal:** Mevcut görünümü koruyarak UI ölçekleme, etiket, odak ve yaşam döngüsü farklarını düzelt.

**Architecture:** Mevcut controller ve UnityEvent girişlerini koru. GameUI'ın odak algoritmasını küçük ortak UISelection yardımcısına taşı; çıkış işlemini GameService'e devret.

**Tech Stack:** Unity 6000.5.9f1, uGUI, VContainer, PrimeTween.

**Spec:** ../specs/2026-11-15-ui-consistency-design.md

## Global Constraints

- Font, renk, sprite, asset GUID ve mevcut UnityEvent metot adlarını koru.
- Canvas: 1920×1080, Match Width Or Height = 1; görsel inceleme kararı spec içinde kayıtlıdır.
- Gerçek kayıt dosyalarına dokunma; yeni bağımlılık ekleme.

## Review Focus

- Kayıt oluşturulduktan/silindikten sonra yeniden açılan ana menü güncellenmeli.
- Gizli/devre dışı tercih edilen buton yerine kullanılabilir öğe seçilmeli.
- HUD'a dönüşte eski menü odağı temizlenmeli.
- Pause sırasında tween ve panel geri dönüşleri çalışmalı.
- Farklı en-boy oranlarında metin/butonlar ekran dışında kalmamalı.

## Task 1: Controller tutarlılığı

- [x] AgentScripts/UIConsistencyChecks.cs içinde yeniden açılma regresyonunu yaz ve FAIL gör.
- [x] UISelection.Select(GameObject panel, Selectable preferred = null) çıkar; UIManager, GameUI ve EndGameUI kullansın.
- [x] UIManager.OnEnable kayıt durumunu ve odağı yenilesin.
- [x] GameService.QuitGame ekle; mevcut üç QuitGame girişi bunu çağırsın. OpenURL hata yakalamasını eşleştir.
- [x] Yeniden açılma ve odak kontrollerini PASS doğrula.

## Task 2: Görsel ayarlar ve son doğrulama

- [x] Üç sahnenin Canvas ayarlarını ve buton etiketlerini Unity Editor üzerinden hedefli düzenle.
- [x] Mevcut RunAssets, Run, RunSceneFlow, RunPanels, RunRestartAndHud kontrollerini çalıştır.
- [x] 1280×720, 1024×768 ve 2560×1080 geniş ekran görünümünü kontrol et.
- [x] Console, git diff --check ve görev kapsamını kontrol et; sonuçları AgentScripts/README.md içine yaz.

Çalışma mevcut kirli NoError checkout'unda yapılır; önceki kullanıcı değişiklikleri korunur, commit yapılmaz.
