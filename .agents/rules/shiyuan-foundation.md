# com.shiyuan.foundation Coding Rules

When developing Unity features in this repository:

1. **Framework Usage**:
   - Utilize `Shiyuan.Foundation.Core` (`SingletonMonoBehaviour<T>`, `StateMachine<TState>`, `DebugLogger`) for infrastructure.
   - Use `Shiyuan.Foundation.Addressables.AddressablePrefabLoader` for Addressable prefab instantiations and always call `Dispose()` in `OnDestroy()`.
   - Use `Shiyuan.Foundation.LocalStorage.LocalStorageService` for encrypted local data persistence and check `result.IsSuccess`.
   - Use `Shiyuan.Foundation.Localization.LocalizationManager` and `LocalizedTMPText` for UI strings.
   - Implement scene transitions and scene-specific logic through `Shiyuan.Foundation.Scenes` (`SceneManagerBase`, `SceneStateMachineBase`, `SceneLifecycleBase`).
   - Use `Shiyuan.Foundation.Notifications.LocalNotificationService` for local push notifications.
   - Use `Shiyuan.Foundation.Effects.TouchEffectPlayer` for touch/tap effects.
   - Use `Shiyuan.Foundation.AppTrackingTransparency.AppTrackingTransparencyManager` for iOS ATT flows.

2. **Async & Memory Safety**:
   - Always propagate `CancellationToken` through asynchronous calls.
   - Strictly manage resource cleanup on `OnDestroy()` or scene unloads.
