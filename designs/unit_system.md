# Unit System Architecture

## Overview
The Unit System manages dynamic game entities ("Units") that occupy the grid. It employs a **JSON-only data architecture**, separating gameplay state, visual representation, and game-specific logic (Rulesets). All unit definitions and stat schemas are stored in JSON files, allowing for easy modification and version control outside of Unity's binary serialization.

## Core Principles
1.  **JSON-Driven Prototypes:** Unit attributes (Type, Base Stats) are defined in JSON files (`Assets/Data/Sets/*.json`), linked to Schemas (`Assets/Data/Schemas/*.json`).
2.  **Stable Identity:** `UnitType` prototypes use a persistent 8-character random string `id`. This ensures that unit references in save files remain valid even if lists are reordered or units are deleted.
3.  **Centralized Management:** `UnitManager` acts as the single source of truth for the active `UnitSet`. Units resolve their stats and definitions logically from the manager's active set.
4.  **View Abstraction:** The `Unit` logic class handles gameplay state, while `UnitVisualization` handles rendering.
5.  **Grid Integration:** Units are spatially aware agents linked strictly to `HexData`.
6.  **Ruleset Driven:** Rulesets manage movement costs and combat side-effects (e.g., ZoC).

## Architecture Structure

### 1. Data Definition Layer (JSON)
*   **`UnitSchema`**: Defines the stat structure (ID and Name). Loaded from JSON into POCO objects.
*   **`UnitSet`**: A collection of `UnitType` prototypes. Resolves its schema by ID from the Schemas folder.
*   **`UnitType`**: Individual unit prototype with specific stat values and a unique stable `id`.

### 2. Logic & State Layer (MonoBehaviour)
*   **`Unit`**:
    *   **Identity**: Unique `Id` (GetInstanceID). GameObject named `UnitName_ID`.
    *   **State**: `unitTypeId` (string), `teamId` (int). Resolves `unitSet` and `Stats` from `UnitManager.Instance.ActiveUnitSet` using the ID.
    *   **Lifecycle**: Re-initializes stats and visualization on ID or set changes.
    *   **Movement**: `MoveAlongPath` interpolation.

### 3. View Layer (MonoBehaviour)
*   **`UnitVisualization`**: Visual representation interface.
*   **`SimpleUnitVisualization`**: Concrete mesh-based implementation with automated lunge animation handling.

### 4. Management & Rules Layer
*   **`UnitManager`**: 
    *   Holds `activeUnitSetPath` (JSON path).
    *   Provides `ActiveUnitSet` (transient POCO).
    *   Handles layout saving/loading, ensuring the correct UnitSet is restored with the unit positions.
    *   Tracks `lastLayoutPath` for quick saving (overwriting the active file).
*   **`UnitDataEditorWindow`**: Centralized UI (HexArena menu) for creating and editing Schemas/Sets with auto-save and automatic ID generation.
*   **`Ruleset` (Abstract SO)**: Game brain handling costs and combat flow.
*   **`BattleBrothersRuleset`**: BB-specific implementation (ZoC, AoA, bucket-based probability).

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
    *   At each step, `ruleset.TryMoveStep` and `ruleset.PerformMove` manage resource deduction, `Occupied` states, and `ZoC` triggers.
    *   If the target was an enemy, the unit performs an `OnAttack` sequence upon reaching the stop position.
6.  **Attack Resolution**:
    *   `PerformAttack` generates the probability buckets via `GetPotentialHits`.
    *   A single `Random.value` roll is matched against the buckets to determine if the target, a cover unit, or a stray unit was hit.
    *   Animates the attack and applies damage/visuals.

### Spawning & Relinking
1.  **Placement**: `Unit.SetHex(hex)` is called.
2.  **Ruleset Integration**: The Ruleset handles occupancy and influence projection during `PerformMove`. In `BattleBrothersRuleset`, this adds unit-unique states (e.g., `Occupied0_123`). If the unit has `MAT > 0`, it also adds `ZoCT_U` states to reachable neighbors.
3.  **Visualization**: Unit snaps to world position + `yOffset`.

### Real-Time Refresh
The `UnitEditor` ensures that any manual change to a Unit's properties in the Inspector triggers a re-initialization and a `SetHex` call, keeping the Ruleset and Grid states perfectly in sync during Edit Mode.

## Persistence Strategy
*   **Saving**: Serializes `UnitSaveData` (Coords, `unitTypeId` string, TeamID).
*   **Loading**: Re-instantiates units. `RelinkUnitsToGrid` is called at `Start` or after loading to ensure `OnEntry` is fired for every unit, restoring transient grid states (like ZoC) that aren't saved to JSON.
*   **Auto-Save**: The Unit Data Editor automatically writes changes to JSON files on every field modification.
    *   **Layout Persistence UI**:
    *   **Row 1**: [Layout Dropdown] [Refresh] (Quick-access to Data/UnitLayouts).
    *   **Row 2**: 
        *   **Save**: Overwrites the `lastLayoutPath`.
        *   **Save As**: Prompt for a new filename.
        *   **Reload**: Loads the file selected in the Row 1 dropdown.
        *   **Load...**: Opens a file browser to load any JSON.
    *   **Operations**: [Relink Units] [Erase All] consolidated into a single compact row.