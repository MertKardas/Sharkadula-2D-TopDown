# Bağımlılık envanteri

Güncelleme: **13 Kasım 2026**. Paketler `Packages/manifest.json` ve `packages-lock.json`, asset olarak gelen kütüphaneler kendi kaynak dosyalarından listelendi. Bu liste çalışma anında hepsinin kullanıldığını göstermez.

## Harici kod kütüphaneleri

| Kütüphane | Sürüm | Kaynak / konum | Amaç |
| --- | --- | --- | --- |
| VContainer | 1.19.0 | UPM: `jp.hadashikick.vcontainer` | Aktif uygulama/sahne dependency injection ve saf C# servis yaşam süreleri |
| MessagePipe | 1.8.2 | UPM: `com.cysharp.messagepipe` | Mesaj yayınlama/abonelik |
| MessagePipe.VContainer | 1.8.2 | UPM: `com.cysharp.messagepipe.vcontainer` | İki kütüphane arasındaki entegrasyon |
| UniTask | 2.5.11 | UPM: `com.cysharp.unitask` | Sahne başlangıcında nesnelerin Start aşamasını bekleme |
| Easy Save 3 | Asset sürümü doğrulanmadı | `Assets/Plugins/Easy Save 3` | Aktif `EasySaveStorage` sağlayıcısı; tek dosyada metadata ve oyun verisi |
| UIEffect | 5.11.8 | UPM: `com.coffee.ui-effect` | UI görsel efektleri |
| NavMeshPlus | 0.2.23 | UPM: `com.h8man.2d.navmeshplus` | 2D NavMesh desteği |
| UI Rounded Corners | 3.5.0 | UPM: `com.nobi.roundedcorners` | UI köşelerini yuvarlama |
| PrimeTween | 1.5.2 | UPM: `com.kyrylokuzyk.primetween`, yerel `Packages/com.kyrylokuzyk.primetween.tgz` | UI geçişleri ve animasyonları |
| LeanTween | 2.50 | `Assets/ThirdParty/LeanTween` | `AudioService` ses fade'leri; UI tarafından kullanılmıyor |
| SerializedCollections | Kaynakta doğrulanamadı | `Assets/Plugins/SerializedCollections` | Inspector'da serileştirilebilir sözlükler |

### Unity 6.5 compile compatibility

The project now targets **6000.5.9f1**. UIEffect **5.11.8** and NavMeshPlus commit `d615919ce2d1472e21ea1c761882cc02d7ba4469` are pinned to upstream fixes; Cinemachine is updated to **3.1.7**. Rounded Corners retains its previous lock hash.


Removed `com.unity.2d.enhancers`, which is unused by game code and brings AI Generators/Toolkit versions that fail to compile on Unity 6.5. Its AI generation tools are no longer installed. Package changes were resolved with UPM `Client.AddAndRemove` in a temporary project.

VContainer is integrated through AppLifetimeScope and SceneLifetimeScope; its pure C# services and scene injection have Play Mode regression coverage. MessagePipe remains a dependency only; UniTask coordinates scene startup after object initialization. Easy Save 3 is installed as an asset and used through ISaveStorage. Windows player C# assemblies compile; full IL2CPP executable verification remains blocked by the previously reported missing Windows SDK. Unity 6000.5.9f1 successfully compiled both the open project and a separate copy of all C# sources with the same packages.

## Unity'nin doğrudan paketleri

Aşağıdaki liste Unity'nin yerleşik `com.unity.modules.*` motor modüllerini içermez.

| Paket | Version |
| --- | --- |
| `com.unity.2d.animation` | 15.1.0 |
| `com.unity.2d.aseprite` | 5.0.3 |
| `com.unity.2d.psdimporter` | 14.0.3 |
| `com.unity.2d.sprite` | 1.0.0 |
| `com.unity.2d.spriteshape` | 15.0.3 |
| `com.unity.2d.tilemap` | 1.0.0 |
| `com.unity.2d.tilemap.extras` | 8.0.3 |
| `com.unity.addressables` | 2.9.1 |
| `com.unity.ai.navigation` | 2.0.14 |
| `com.unity.behavior` | 1.0.16 |
| `com.unity.cinemachine` | 3.1.7 |
| `com.unity.collab-proxy` | 2.13.6 |
| `com.unity.ide.rider` | 3.0.38 |
| `com.unity.ide.visualstudio` | 2.0.26 |
| `com.unity.inputsystem` | 1.20.0 |
| `com.unity.multiplayer.center` | 1.0.1 |
| `com.unity.pipeline` | 1.0.0 |
| `com.unity.render-pipelines.universal` | 17.5.0 |
| `com.unity.test-framework` | 1.7.0 |
| `com.unity.timeline` | 1.8.12 |
| `com.unity.ugui` | 2.5.0 |

## Kilit dosyasındaki dolaylı paketler

Bunlar diğer paketler tarafından getiriliyor. Özellikle **Newtonsoft.Json 3.2.2**, **NUnit 2.0.5** ve TextMesh Pro'nun uGUI ile gelen içeriği, harici kod kullanımı değerlendirilirken dikkate alınmalı. TextMesh Pro'nun ayrıca `Assets/TextMesh Pro` altında kaynak ve örnek içeriği var; bağımsız bir TMP UPM sürümü manifestte yok.

| Paket | Version |
| --- | --- |
| `com.unity.2d.common` | 14.0.1 |
| `com.unity.burst` | 1.8.30 |
| `com.unity.collections` | 6.5.0 |
| `com.unity.dt.app-ui` | 2.1.7 |
| `com.unity.ext.nunit` | 2.1.0 |
| `com.unity.mathematics` | 1.4.0 |
| `com.unity.nuget.mono-cecil` | 1.11.6 |
| `com.unity.nuget.newtonsoft-json` | 3.2.2 |
| `com.unity.profiling.core` | 1.0.3 |
| `com.unity.render-pipelines.core` | 17.5.0 |
| `com.unity.render-pipelines.universal-config` | 17.5.0 |
| `com.unity.scriptablebuildpipeline` | 2.6.1 |
| `com.unity.searcher` | 4.9.5 |
| `com.unity.settings-manager` | 2.1.1 |
| `com.unity.shadergraph` | 17.5.0 |
| `com.unity.splines` | 2.9.0 |
| `com.unity.test-framework.performance` | 3.5.0 |

## Kod kütüphanesi olmayan üçüncü taraf içerikler

- `Assets/ThirdParty/2D Pixel Dungeon Asset Pack`: çevre grafikleri.
- `Assets/ThirdParty/Enemy_Animations_Set`: düşman animasyon içeriği.
- `Assets/ThirdParty/free-vampire-4-direction-pixel-character-sprite-pack`: karakter grafikleri.

Bu envanter bir lisans incelemesi değildir; dağıtım öncesi ilgili lisans ve atıf dosyalarını kontrol et. Temizlik adayları için [cleanup.md](cleanup.md) dosyasına bak.

## Unity MCP connection

`com.unity.pipeline` **1.0.0** provides the local Editor connection for the Unity CLI and Codex MCP server. The existing global Codex registration runs `unity mcp`; no duplicate server entry is needed. Check the connection with `unity status --format json` after the Editor imports the package.

## Removed dependency: NaughtyAttributes

NaughtyAttributes, its Editor/tests/samples, and all game-code imports/attributes were removed. Serialized fields remain intact; scene-name fields now use ordinary string inputs. Runtime-only fields are no longer shown by the normal Inspector. The GitHub icon used by MainMenu and recovery scenes was moved to `Assets/Images/icon-github.png`, retaining GUID `005888ede18a58e4db8d069cfa3007cb`.
