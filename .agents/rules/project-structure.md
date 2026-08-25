# Project Directory Structure & Coding Guidelines

## 1. Directory Structure (`Assets/Runner/`)

All project-specific assets and source code are located under `Assets/Runner/`.

```text
Assets/Runner/
├── Editor/             # Editor-only scripts and tools (Runner.Editor.asmdef)
├── Prefabs/            # Prefab assets (including Addressable prefabs)
├── Scenes/             # Unity scene files (Boot.unity, Title.unity, Home.unity, Game.unity, etc.)
├── Settings/           # ScriptableObject settings and asset configurations
└── Scripts/            # Runtime C# scripts
    ├── Camera/         # Camera management and Cinemachine binding (Runner.Camera.asmdef)
    ├── Gameplay/       # Core game mechanics, player/enemy logic, states (Runner.Gameplay.asmdef)
    │   ├── Characters/ # Character controllers, status, visuals, components
    │   ├── States/     # In-game StateMachine states (GamePlayingState, GameOverState, etc.)
    │   └── UI/         # Gameplay-specific UI (e.g. FloatingHpBar)
    ├── Input/          # Player input handling & Input System binding (Runner.Input.asmdef)
    ├── Scenes/         # Scene-level lifecycles & state machines (Runner.Scenes.asmdef)
    │   ├── Boot/
    │   ├── Title/
    │   ├── Home/
    │   └── Game/
    └── UI/             # Shared / Menu UI components (Runner.UI.asmdef)
```

---

## 2. Assembly Definitions (`.asmdef`) & Dependency Rules

Each feature domain under `Scripts/` has its own Assembly Definition. Dependencies must flow top-down:

```text
Runner.Scenes ──► Runner.Gameplay ──► Runner.Input
              ──► Runner.UI
              ──► Runner.Camera
              ──► Shiyuan.Foundation.*
```

- **`Runner.Input`**: Low-level input layer. Depends on `Unity.InputSystem`.
- **`Runner.Camera`**: Camera controller and player tracker. Depends on `Unity.Cinemachine`.
- **`Runner.UI`**: Reusable UI components. Depends on `Shiyuan.Foundation.Core`, `Unity.TextMeshPro`.
- **`Runner.Gameplay`**: Game mechanics, character logic, and gameplay states. Depends on `Runner.Input`, `Shiyuan.Foundation.Core`, `Shiyuan.Foundation.Addressables`, etc.
- **`Runner.Scenes`**: Top-level scene orchestration, lifecycle, and scene state machines. References `Runner.Gameplay`, `Runner.UI`, `Runner.Input`, `Shiyuan.Foundation.Scenes`.
- **`Runner.Editor`**: Editor scripts, setup tools, and inspectors.

> [!IMPORTANT]
> **No Circular References**: Never introduce dependencies from lower layers (e.g., `Runner.Input` or `Runner.Gameplay`) to higher layers (e.g., `Runner.Scenes`).

---

## 3. C# Coding Conventions

- **Namespace**:
  - Always use `Runner` as the root namespace.
  - Match sub-namespaces to the directory hierarchy (e.g., `Runner.Gameplay.Characters.Player`, `Runner.Scenes.Game`).
- **Naming Conventions**:
  - **Classes / Structs / Interfaces / Enums / Methods / Properties**: `PascalCase`
  - **Interface Names**: Prefix with `I` (e.g., `IGameContext`)
  - **Private / Protected Fields**: `_camelCase` (e.g., `_playerController`, `_moveSpeed`)
  - **Constants / Static Readonly**: `k_CamelCase` or `UPPER_SNAKE_CASE`
  - **Serialized Fields**: Use `[SerializeField] private Type _fieldName;`
- **Async & Lifecycle**:
  - Use `CancellationToken` for all asynchronous operations (e.g., scene transitions, Addressables loading).
  - Always clean up event subscriptions and loaders in `OnDestroy()` or `Dispose()`.

