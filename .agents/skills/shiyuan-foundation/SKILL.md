---
name: shiyuan-foundation
description: >-
  Provides comprehensive knowledge, architecture guidelines, and code templates for the com.shiyuan.foundation Unity framework.
  Use when implementing or modifying Unity features involving Core (SingletonMonoBehaviour, StateMachine, DebugLogger),
  Addressables (AddressablePrefabLoader), LocalStorage (LocalStorageService with AES-CBC/HMAC), Localization (LocalizationManager, LocalizedTMPText),
  Scenes (SceneManagerBase, SceneLifecycleBase), Effects (TouchEffectPlayer), Notifications (LocalNotificationService), or iOS AppTrackingTransparency.
---

# com.shiyuan.foundation Usage Guide & Architectural Standards

`com.shiyuan.foundation` is a modular, high-performance Unity application framework (targeted for Unity 6+) providing common architecture components, resource management, state machines, encrypted persistence, localization, and mobile platform services.

---

## Module Overview & Best Practices

### 1. Core Module (`Shiyuan.Foundation.Core`)
- **`SingletonMonoBehaviour<T>`**:
  - Thread-safe singleton for MonoBehaviour components.
  - Automatically destroys duplicate instances.
  - Controls whether the GameObject persists across scene loads via `protected virtual bool ShouldDontDestroyOnLoad => true;` (override to `false` for scene-scoped singletons).
  - Always check `if (!IsPrimaryInstance) return;` in `Awake()` before performing initialization.
- **`StateMachine<TState>` / `IState<TState>`**:
  - Generic, asynchronous state machine constrained to enum state types (`where TState : struct, Enum`).
  - Supports queuing transitions if a state change is in flight.
  - Lifecycle order: `EnterAsync(parameter, token)` -> `WaitAsync(token)` -> `Update()` -> `Exit()`.
- **`DebugLogger`**:
  - Conditional logger stripping overhead in production builds (`DEBUG_LOG` compilation symbol).
  - Standard methods: `Log`, `Warning`, `Error`, `Exception`.

---

### 2. Addressables Module (`Shiyuan.Foundation.Addressables`)
- **`AddressablePrefabLoader`**:
  - Encapsulates async prefab loading, instantiation, and release.
  - Overloads:
    - `LoadAsync(address, cancellationToken)`: Instantiates at root.
    - `LoadAsync(address, parentCanvas, cancellationToken)`: Instantiates under Canvas.
    - `LoadAsync(address, parentTransform, cancellationToken)`: Instantiates under specific Transform.
  - **Critical Lifecycle Rule**: Always call `loader.Dispose()` in `OnDestroy()` or teardown to release Addressables handles and prevent memory leaks. Always pass `CancellationToken`.

---

### 3. LocalStorage Module (`Shiyuan.Foundation.LocalStorage`)
- **`LocalStorageService`**:
  - Type-safe, synchronous local file persistence.
  - **Security**: Built-in 256-bit AES-CBC encryption with HMAC-SHA256 digital signature to prevent data tampering.
  - `Save<T>(string fileName, T data)`
  - `Load<T>(string fileName)` returning `LocalStorageResult<T>`.
  - Always inspect `result.IsSuccess` or handle `result.Status` (`NotFound`, `CryptoError`, `InvalidData`).

---

### 4. Localization Module (`Shiyuan.Foundation.Localization`)
- **`LocalizationManager`**:
  - Singleton via `LocalizationManager.Instance`.
  - Must await `InitializeAsync(token)` during app boot before fetching strings.
  - `GetText(key)`: Synchronously fetches translated string for current locale.
  - `ChangeLocale(localeCode)`: Switches active language.
- **`LocalizedTMPText`**:
  - Component attached to `TextMeshProUGUI` for automatic reactive text updates upon locale changes.

---

### 5. Scenes Module (`Shiyuan.Foundation.Scenes`)
- **`SceneManagerBase<TScene>`**:
  - Singleton controller for application scene transitions (`where TScene : struct, Enum`).
  - Override `StartScene` and `CreateSceneStateMachine()`.
  - Call `ChangeScene(scene, parameter, showLoading)`.
- **`SceneStateMachineBase<TScene>`**:
  - Connects scene enums with Unity scene loading and `ISceneLifecycle`.
  - Handles loading UI display via `OnShowLoading()` / `OnHideLoading()`.
- **`ISceneLifecycle` / `SceneLifecycleBase`**:
  - Lifecycle hooks for each scene:
    - `OnWaitForCommunicationAsync(token)`: Wait for initial API communication or preloading before transitioning.
    - `OnInitializeAsync(parameter, token)`: Bind ViewModels, initialize UI with passed data.
    - `OnUpdate()`: Per-frame update logic.
    - `OnDestroy()`: Teardown, dispose loaders and listeners.

---

### 6. Effects Module (`Shiyuan.Foundation.Effects`)
- **`TouchEffectPlayer`**:
  - Automatic touch/click particle visual effect generator using Unity Input System.
  - Uses object pooling for memory efficiency. Attach prefab to scene canvas/root.

---

### 7. Notifications Module (`Shiyuan.Foundation.Notifications`)
- **`LocalNotificationService`**:
  - Singleton via `LocalNotificationService.Instance`.
  - `Initialize()`: Sets up Android notification channels and iOS permission registration.
  - `Schedule(id, title, body, delay, channelId)`: Schedules local push notification.
  - `CancelAll()`: Clears pending notifications.

---

### 8. AppTrackingTransparency Module (`Shiyuan.Foundation.AppTrackingTransparency`)
- **`AppTrackingTransparencyManager`**:
  - iOS ATT permission flow.
  - `GetStatus()`: Checks `AuthorizationStatus` (`NotDetermined`, `Restricted`, `Denied`, `Authorized`, `Unsupported`).
  - `RequestAuthorization(callback)`: Displays native iOS ATT permission dialog.
