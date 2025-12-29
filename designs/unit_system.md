# Unit System Architecture

## Overview
The Unit System manages dynamic game entities ("Units") that occupy the grid. It employs a **JSON-only data architecture**, separating gameplay state, visual representation, and game-specific logic (Rulesets). All unit definitions and stat schemas are stored in JSON files, allowing for easy modification and version control outside of Unity's binary serialization.

## Core Principles
1.  **JSON-Driven Prototypes:** Unit attributes (Type, Base Stats) are defined in JSON files (`Assets/Data/Sets/*.json`), linked to Schemas (`Assets/Data/Schemas/*.json`).
2.  **Centralized Management:** `UnitManager` acts as the single source of truth for the active `UnitSet`. Units resolve their stats and definitions logically from the manager's active set.
3.  **View Abstraction:** The `Unit` logic class handles gameplay state, while `UnitVisualization` handles rendering.
4.  **Grid Integration:** Units are spatially aware agents linked strictly to `HexData`.
5.  **Ruleset Driven:** Rulesets manage movement costs and combat side-effects (e.g., ZoC).

## Architecture Structure

### 1. Data Definition Layer (JSON)
*   **`UnitSchema`**: Defines the stat structure (ID and Name). Loaded from JSON into a transient SO.
*   **`UnitSet`**: A collection of `UnitType` prototypes. Resolves its schema by ID from the Schemas folder.
*   **`UnitType`**: Individual unit prototype with specific stat values.

### 2. Logic & State Layer (MonoBehaviour)
*   **`Unit`**:
    *   **Identity**: Unique `Id` (GetInstanceID). GameObject named `UnitName_ID`.
    *   **State**: `typeIndex`, `teamId`. Resolves `unitSet` and `Stats` from `UnitManager.Instance.ActiveUnitSet`.
    *   **Lifecycle**: Re-initializes stats and visualization on index or set changes.
    *   **Movement**: `MoveAlongPath` interpolation.

### 3. View Layer (MonoBehaviour)
*   **`UnitVisualization`**: Visual representation interface.
*   **`SimpleUnitVisualization`**: Concrete mesh-based implementation with automated lunge animation handling.

### 4. Management & Rules Layer
*   **`UnitManager`**: 
    *   Holds `activeUnitSetPath` (JSON path).
    *   Provides `ActiveUnitSet` (transient transient SO).
    *   Handles layout saving/loading, ensuring the correct UnitSet is restored with the unit positions.
*   **`UnitDataEditorWindow`**: Centralized UI (HexArena menu) for creating and editing Schemas/Sets with auto-save and draggable stat reordering.
*   **`Ruleset` (Abstract SO)**: Game brain handling costs and combat flow.
*   **`BattleBrothersRuleset`**: BB-specific implementation (ZoC, AoA, bucket-based probability).

## Key Interactions

### Pathfinding & Combat Flow
1.  **Selection**: `PathfindingTool` identifies a unit.
2.  **Hover**: Calculates path and AoA. Ruleset hides movement ghost if stationary.
3.  **Execution**: `ruleset.ExecutePath` with completion callback to maintain selection.
4.  **Attack**: `PerformAttack` uses bucket-based resolution. `OnHit` applies HP damage and triggers `OnDie`.

### Persistence Strategy
*   **Unit Layouts**: Serialized to `UnitSaveBatch` JSON, including the `unitSetPath` to ensure logical consistency on reload.
*   **Auto-Save**: The Unit Data Editor automatically writes changes to JSON files on every field modification.

## Key Interactions

### Pathfinding & Combat Flow
1.  **Selection**: `PathfindingTool` identifies a `SourceHex`.
    *   Triggers `ruleset.OnUnitSelected`.
    *   Immediately initiates a zero-length pathfinding call to show initial visuals (AoA).
2.  **Hover**: Tool calls `CalculateAndShowPath`. Ruleset's `OnStartPathfinding` determines if the target is an enemy and detects the `AttackType` (Melee/Ranged).
3.  **Pathing**: `Pathfinder` calculates the raw path. The Ruleset's `GetMoveCost` evaluates costs, applying `zocPenalty` and forbidding pass-through of enemies.
4.  **Preview**: Ruleset receives `OnFinishPathfinding`.
    *   Uses `GetMoveStopIndex` to truncate the path (stopping at the first hex within attack range).
    *   Spawns a **Ghost** at the stop position with 50% transparency.
    *   Calculates **Area of Attack (AoA)**: Highlights all hexes within the unit's max range from the stopping hex.
5.  **Execution**: On click, the tool calls `ruleset.ExecutePath`. 
    *   The unit calls `MoveAlongPath`.
    *   At each step, `OnDeparture` and `OnEntry` manage `Occupied` and `ZoC` states.
    *   If the target was an enemy, the unit performs an `OnAttack` sequence upon reaching the stop position.
6.  **Attack Resolution**:
    *   `PerformAttack` generates the probability buckets via `GetPotentialHits`.
    *   A single `Random.value` roll is matched against the buckets to determine if the target, a cover unit, or a stray unit was hit.
    *   Animates the attack and applies damage/visuals.

### Spawning & Relinking
1.  **Placement**: `Unit.SetHex(hex)` is called.
2.  **Departure**: Previous hex is notified via `ruleset.OnDeparture(this, oldHex)`.
3.  **Entry**: New hex is notified via `ruleset.OnEntry(this, newHex)`. In `BattleBrothersRuleset`, this adds a unit-unique state (e.g., `Occupied0_123`). If the unit has `MRNG > 0` (Melee Range), it also adds `ZoCT_U` states to reachable neighbors.
4.  **Visualization**: Unit snaps to world position + `yOffset`.

### Real-Time Refresh
The `UnitEditor` ensures that any manual change to a Unit's properties in the Inspector triggers a re-initialization and a `SetHex` call, keeping the Ruleset and Grid states perfectly in sync during Edit Mode.

## Persistence Strategy
*   **Saving**: Serializes `UnitSaveData` (Coords, Type Index, TeamID).
*   **Loading**: Re-instantiates units. `RelinkUnitsToGrid` is called at `Start` or after loading to ensure `OnEntry` is fired for every unit, restoring transient grid states (like ZoC) that aren't saved to JSON.