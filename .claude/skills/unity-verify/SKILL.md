---
name: unity-verify
description: Bu Unity projesinde kod/asset değişikliğinden sonra derleme, Console ve Play Mode doğrulamasını yürütür. C# veya sahne/prefab değiştirdikten sonra, "derleniyor mu", "test et", "doğrula" denildiğinde kullan.
---

# Unity doğrulama akışı

Çalıştırmadığın adımı başarılı diye raporlama. Her adımın sonucunu (PASS/FAIL/çalıştırılamadı) yaz.

## 1. Statik ön kontrol (Editor gerekmez)

```bash
git diff --check
git status --short
```

- Yeni/taşınan her asset için `.meta` var mı? Silinen asset'in `.meta`'sı da silinmiş mi?
- Runtime koduna `using UnityEditor` / `using NUnit` eklenmedi mi?
  ```bash
  grep -rn "using UnityEditor\|using NUnit" Assets/Scripts --include=*.cs | grep -v "/Editor/"
  ```

## 2. Editor bağlantısı

Unity CLI (`unity`) kurulu ve `com.unity.pipeline` paketi projede. Editor bu proje ile açık olmalı.

```bash
unity status --format json
```

Bağlı Editor yoksa kullanıcıdan projeyi açmasını iste; Editor'ü kendiliğinden kapatma/yeniden başlatma. Komut ayrıntıları için `unity:unity-cli` skill'ini yükle.

## 3. Derleme

```bash
unity recompile
```

Hata varsa dosya:satır ile raporla ve düzelt; uyarıları değişiklikle ilgiliyse belirt.

## 4. Davranış kontrolü

- Editor'deki mevcut komutları görmek için: `unity cmd` (argümansız listeler).
- Etkilenen senaryoyu tanımla (ör. "MainMenu → Yeni oyun → anahtar al → kapı"). Otomatik çalıştırılamıyorsa kullanıcıya adım adım Play Mode kontrol listesi ver.
- Kayıt/envanter/durum geçişi değiştiyse regresyon kontrolü çalıştır veya ekle. Gerçek kayıt dosyalarına yazma.

## 5. Build (gerektiğinde)

Runtime bağımlılığı, `#if UNITY_EDITOR` veya paket değişikliğinde player build gerekir: `unity build --help` ile hedefi seç; uzun sürer, arka planda çalıştır.

## Rapor

Değişiklikler · çalıştırılan kontroller ve sonuçları · çalıştırılamayanlar ve neden.
