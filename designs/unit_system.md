# Unit System Architecture

## Overview
The Unit System manages dynamic game entities ("Units") that occupy the grid. It bridges the gap between static data definitions (Prototypes) and active scene objects, employing a separation between gameplay state, visual representation, and game-specific logic (Rulesets).

## Core Principles
1.  **Data-Driven Prototypes:** Unit attributes (Type, Base Stats) are defined in ScriptableObjects (`UnitSet`, `UnitSchema`), allowing designers to balance the game without touching prefabs or code.
2.  **View Abstraction:** The `Unit` logic class handles gameplay state (Position, Stats), while `UnitVisualization` handles rendering (Animations, Models). This allows swapping 2D/3D representations without affecting logic.
3.  **Grid Integration:** Units are spatially aware. They link strictly to `HexData` and synchronize their `Transform` position to the grid's layout.
4.  **Ruleset Driven:** Units notify the active `Ruleset` of events (Entry/Departure), allowing the ruleset to manage side-effects like Zone of Control (ZoC).

## Architecture Structure

### 1. Data Definition Layer (ScriptableObjects)
This layer defines "what a unit is" before it enters the scene.

*   **`UnitSchema`**: Defines the *structure* of unit statistics.
*   **`UnitType`**: The "Prototype" or "Template" containing Name and base stats.
*   **`UnitSet`**: A collection (Library) of `UnitType`s.

### 2. Logic & State Layer (MonoBehaviour)
*   **`Unit`**:
    *   **Identity**: Unique `Id` (mapped to `GetInstanceID()`).
    *   **State**: Holds runtime data: `CurrentHex`, `Stats` (Dictionary), and `teamId`.
    *   **Lifecycle**: Re-initializes base stats and visualization identity on changes to "Type Index" or "Unit Set" in the Inspector.
    *   **Grid Link**: `SetHex(Hex)` updates the logical position, notifies the `Ruleset`, and snaps the transform to the Hex's world coordinates.

### 3. View Layer (MonoBehaviour)
*   **`UnitVisualization` (Abstract)**: Interface for the visual puppet with hooks for `OnStartMoving`, `OnAttack`, etc.
*   **`SimpleUnitVisualization`**: Concrete implementation providing deterministic colors and `yOffset` support.

### 4. Management & Rules Layer
*   **`UnitManager`**: Handles spawning, erasing, and persistence.
*   **`GameMaster`**: A singleton that holds the active `Ruleset` asset.
*   **`Ruleset` (Abstract SO)**: The "brain" of the game. Handles movement costs and grid-state side effects.
*   **`BattleBrothersRuleset`**: Concrete implementation managing terrain costs and per-unit Zone of Control strings.

## Key Interactions

### Spawning & Relinking
1.  **Placement**: `Unit.SetHex(hex)` is called.
2.  **Departure**: Previous hex is notified via `ruleset.OnDeparture(this, oldHex)`.
3.  **Entry**: New hex is notified via `ruleset.OnEntry(this, newHex)`. In `BattleBrothersRuleset`, this adds `"Occupied_T"`. If the unit has `MRNG > 0` (Melee Range), it also adds `"ZoC_T_U"` states to reachable neighbors (checking elevation).
4.  **Visualization**: Unit snaps to world position + `yOffset`.

### Real-Time Refresh
The `UnitEditor` ensures that any manual change to a Unit's properties in the Inspector triggers a re-initialization and a `SetHex` call, keeping the Ruleset and Grid states perfectly in sync during Edit Mode.

## Persistence Strategy
*   **Saving**: Serializes `UnitSaveData` (Coords, Type Index, TeamID).
*   **Loading**: Re-instantiates units. `RelinkUnitsToGrid` is called at `Start` or after loading to ensure `OnEntry` is fired for every unit, restoring transient grid states (like ZoC) that aren't saved to JSON.