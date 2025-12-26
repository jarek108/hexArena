# Unit System Architecture

## Overview
The Unit System manages dynamic game entities ("Units") that occupy the grid. It bridges the gap between static data definitions (Prototypes) and active scene objects, employing a separation between gameplay state and visual representation.

## Core Principles
1.  **Data-Driven Prototypes:** Unit attributes (Name, Base Stats) are defined in ScriptableObjects (`UnitSet`, `UnitSchema`), allowing designers to balance the game without touching prefabs or code.
2.  **View Abstraction:** The `Unit` logic class handles gameplay state (Position, Stats), while `UnitVisualization` handles rendering (Animations, Models). This allows swapping 2D/3D representations without affecting logic.
3.  **Grid Integration:** Units are spatially aware. They link strictly to `HexData` and synchronize their `Transform` position to the grid's layout.

## Architecture Structure

### 1. Data Definition Layer (ScriptableObjects)
This layer defines "what a unit is" before it enters the scene.

*   **`UnitSchema`**:
    *   Defines the *structure* of unit statistics (e.g., "HP", "Movement", "Damage").
    *   Ensures consistent stat IDs across different unit sets.
*   **`UnitType`**:
    *   The "Prototype" or "Template".
    *   Contains the Unit's Name and a list of base `UnitStatValue`s defined by the schema.
    *   Pure C# class (Serializable).
*   **`UnitSet`**:
    *   A collection (Library) of `UnitType`s.
    *   Acts as a faction or database (e.g., "Goblins", "Space Marines").

### 2. Logic & State Layer (MonoBehaviour)
*   **`Unit`**:
    *   The active agent in the scene.
    *   **State**: Holds runtime data: `CurrentHex` (Position), `Stats` (Dictionary), and `TeamId`.
    *   **Initialization**: Hydrates itself from a `UnitType` prototype, copying base stats into its runtime dictionary.
    *   **Grid Link**: `SetHex(Hex)` updates the unit's logical position and snaps its transform to the Hex's world coordinates.

### 3. View Layer (MonoBehaviour)
*   **`UnitVisualization` (Abstract)**:
    *   The interface for the visual puppet.
    *   Defines abstract hooks for gameplay events: `OnStartMoving`, `OnAttack`, `OnTakeDamage`, `OnDie`.
*   **`SimpleUnitVisualization`**:
    *   A concrete implementation (e.g., using basic primitives or simple sprites) for prototyping.

### 4. Management Layer
*   **`UnitManager`**:
    *   **Factory**: Responsible for instantiating Unit GameObjects. It combines a `UnitVisualization` prefab with a `Unit` logic component and initializes them with data from a `UnitSet`.
    *   **Lifecycle**: Handles global operations like `EraseAllUnits`.
    *   **Persistence**: Manages Save/Load operations.
        *   **Saving**: Serializes `UnitSaveData` (Grid Coordinates, UnitIndex, TeamID) to JSON.
        *   **Loading**: Recreates units from JSON and `RelinkUnitsToGrid` ensures they snap back to their correct `HexData`.

## Key Interactions

### Spawning a Unit
1.  **Request**: `UnitManager.SpawnUnit(index, team, hex)` is called.
2.  **Lookup**: Manager fetches `UnitType` from the active `UnitSet`.
3.  **Instantiation**: Visual Prefab is instantiated.
4.  **Assembly**: `Unit` component is added/fetched and `Initialize()` is called to copy stats.
5.  **Placement**: `Unit.SetHex(hex)` is called to link it to the grid and set its position.

### Movement
*   **Logic**: `Unit.SetHex(newHex)` updates the `CurrentHex` reference and clears the old one.
*   **Visuals**: The `Unit` component updates the `transform.position` (plus `yOffset` from the visualization). Future implementations will delegate smooth movement to `UnitVisualization.OnStartMoving`.

## Persistence Strategy
*   Units are **not** children of the Hexes in the hierarchy; they live under `UnitManager` to keep the scene clean.
*   **Saving**: We save the Unit's *Identity* (Index in Set) and *Location* (Q, R coords).
*   **Loading**: We clear the board, re-instantiate units based on Identity, and then ask the Grid to find the `HexData` at (Q, R) to re-establish the link.
