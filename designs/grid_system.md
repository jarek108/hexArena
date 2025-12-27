# Grid System Architecture

## Overview
The Grid System is the foundation of the game, designed with a strict separation between logical data and visual representation. This **Data-Driven Architecture** ensures that gameplay logic remains testable and decoupled from the Unity Engine's visual layer.

## Core Principles
1.  **Logic First:** The "truth" of the game state lives in pure C# classes (`HexData`, `Grid`). Unity `GameObjects` are merely visual puppets that reflect this state.
2.  **Reactive Views:** Visual components (`Hex` MonoBehaviour) listen for state changes on the data layer and update themselves.
3.  **Visual Authority:** Styling rules (colors, rims, highlighting) are centralized in the `GridVisualizationManager`.
4.  **Priority-Based Visibility:** Visuals are gated by a `priority > 0` check. Hidden states use negative priorities to preserve data without rendering.

## Detailed Structure

### 1. Logic Layer (Pure C#)
*   **`HexData`**: Atomic unit of the grid. Stores coordinates, terrain, and a `HashSet<string>` of active states.
*   **`Grid`**: Logical container providing spatial queries.

### 2. View Layer (Unity MonoBehaviours)
*   **`Hex` Component**: Syncs GameObject transform with `HexData` elevation and requests visual refreshes.
*   **`GridVisualizationManager`**:
    *   **Styling Authority**: Maps state strings to visual properties (Rim Color, Width, Pulse).
    *   **Visual Resolution**: Iterates active states and picks the one with the highest **positive** priority.
    *   **Visibility Toggle**: Tools can "hide" a state by flipping its priority sign (e.g., `15` -> `-15`).
    *   **Manual Ordering**: The Inspector provides **Up/Down** buttons to organize state settings manually in the data set.
    *   **Default Fallback**: Hardcoded visuals (priority 0) used when no positive-priority states are active.

### 3. Creation Layer
*   **`GridCreator`**: Procedural generation and persistence (JSON).
*   **`GameMaster`**: Houses the active **`Ruleset`**, which dictates movement costs and state transitions during gameplay.

### 4. Interaction Layer
*   **Tools**: Modify `HexData` states.
*   **Rulesets**: Dictate "side-effects" of movement (e.g., entering a hex adds a "ZoC" state to neighbors).

## Key Design Choices
*   **String-Based States**: Abstracted state management allows Rulesets to define game-specific states (e.g., `"ZoC_0_123"`) without modifying core grid code.
*   **Magnitude-Sign Toggling**: Preserves priority data while allowing binary visibility control.
*   **Lazy Initialization**: Prevents NullReferenceExceptions during Unity serialization cycles.