# Grid System Architecture

## Overview
The Grid System is the foundation of the game, designed with a strict separation between logical data and visual representation. This **Data-Driven Architecture** ensures that gameplay logic remains testable and decoupled from the Unity Engine's visual layer.

## Core Principles
1.  **Logic First:** The "truth" of the game state lives in pure C# classes (`HexData`, `Grid`). Unity `GameObjects` are merely visual puppets that reflect this state.
2.  **Reactive Views:** Visual components (`Hex` MonoBehaviour) listen for state changes on the data layer and update themselves. They do not drive logic.
3.  **Visual Authority:** Styling rules (colors, rims, highlighting) are centralized in the `HexStateVisualizer`, avoiding scattered visual logic.
4.  **Tool-Based Interaction:** Interaction is handled by dedicated tools (`SelectionTool`, `GridCreator`) rather than the core manager, keeping classes focused (Single Responsibility Principle).

## Detailed Structure

### 1. Logic Layer (Pure C#)
This layer manages the raw data and rules of the hex grid. It has no dependencies on `MonoBehaviour` visual properties.

*   **`HexData`**: The atomic unit of the grid.
    *   **Coordinates**: Stores Axial/Cube coordinates (`Q`, `R`, `S`).
    *   **Terrain & Elevation**: Stores gameplay properties like `TerrainType` and `Elevation`.
    *   **State Management**: Contains a `HashSet<HexState>` to track active abstract states (e.g., `Selected`, `Hovered`, `PathfindingCandidate`).
    *   **Events**: Exposes `OnStateChanged` events that the View layer subscribes to.
    *   **Serialization**: Marked `[Serializable]` for JSON persistence via `GridSaveData`.

*   **`Grid`**: The container logic.
    *   Manages the collection of `HexData`.
    *   Provides spatial queries (e.g., `GetHexAt(q, r)`).
    *   Handles grid boundaries and dimensions.

### 2. View Layer (Unity MonoBehaviours)
This layer handles the visual representation in the scene.

*   **`Hex` Component**:
    *   Attached to the instantiated Hex GameObjects.
    *   Acts as a **View Puppet**: It holds a reference to its corresponding `HexData`.
    *   **Synchronization**: On `AssignData`, it subscribes to `HexData.OnStateChanged`. When the data updates, the `Hex` component requests a visual refresh.
    *   **Editor Persistence**: Uses hidden serialized fields (`viewQ`, `viewR`, etc.) to survive Unity assembly reloads when the C# `HexData` object might be lost or reset in Edit Mode.

*   **`HexStateVisualizer`**:
    *   **The Styling Authority**: Defines how abstract states (`HexState`) translate to visual properties (Rim Color, Width, Pulsation).
    *   **Priority System**: Uses a list of `StateSetting` structs, each with a priority. When a hex has multiple states (e.g., `Hovered` AND `Selected`), the visualizer applies the settings of the highest-priority state.
    *   **Default Fallback**: Automatically falls back to a "Default" visual style if no specific states are active.
    *   **Editor Synchronization**: In Editor mode, changes to the "Default" settings immediately propagate to the shared Material, ensuring the scene looks correct even without active play mode overrides.

### 3. Management & Creation Layer
*   **`GridVisualizationManager`**:
    *   **Pure Visualizer**: Responsible for the physical layout (Mesh generation/instantiation) and coordinate math (`HexToWorld`).
    *   **Lifecycle**: It initializes the visual scene based on a `Grid` object passed to `VisualizeGrid`.
    *   **Resource Holder**: Holds references to shared Materials (`HexSurfaceMaterial`, `HexSideMaterial`) and Meshes.

*   **`GridCreator`**:
    *   **Generator**: Handles procedural generation logic (Perlin noise for terrain/elevation).
    *   **Persistence**: Manages Save/Load operations to JSON.
    *   **Lifecycle Control**: Contains the `ClearGrid` logic to reset the scene and data.

### 4. Interaction Layer
*   **`SelectionTool`**:
    *   A dedicated tool component that manipulates `HexData` states.
    *   Has methods like `SelectHex`, `HighlightHex`, `DeselectHex`.
    *   It **only** modifies the `HexData` states (adding/removing `HexState` enums). It does *not* touch materials directly. The View layer reacts to these state changes.

*   **`SelectionManager`**:
    *   Handles Input (Mouse clicks, Raycasting).
    *   Orchestrates the `SelectionTool`.

## Key Design Choices
*   **Lazy Initialization**: `HexData` properties (like the `States` HashSet) use lazy initialization to prevent NullReferenceExceptions during Unity's erratic serialization/reload cycles.
*   **Material Property Blocks**: Visual updates use `MaterialPropertyBlock` for performant, individual hex coloring without creating unique material instances.
*   **EditMode Testing**: The system is designed to be testable in EditMode, allowing for rapid iteration without entering Play Mode.
