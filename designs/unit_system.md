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
    *   **Lifecycle**: Re-initializes base stats and visualization identity on changes to `typeIndex` or `unitSet` in the Inspector (`OnValidate`).
    *   **Grid Link**: `SetHex(Hex)` updates the logical position, notifies the `Ruleset`, and snaps the transform to the Hex's world coordinates.
    *   **Movement**: `MoveAlongPath` handles interpolation between hexes with configurable speed and pauses.

### 3. View Layer (MonoBehaviour)
*   **`UnitVisualization` (Abstract)**: Interface for the visual puppet with hooks for `OnStartMoving`, `OnAttack`, etc.
*   **`SimpleUnitVisualization`**: Concrete implementation providing deterministic colors and `yOffset` support.
*   **Ghosting**: The Ruleset can spawn a "Ghost" visualization during pathfinding to preview the unit's final destination before confirming movement.

### 4. Management & Rules Layer
*   **`UnitManager`**: Handles spawning, erasing, and persistence.
*   **`GameMaster`**: A singleton that holds the active `Ruleset` asset.
*   **`Ruleset` (Abstract SO)**: The "brain" of the game. Handles movement costs, combat execution, and pathfinding lifecycle events.
    *   **Hooks**: `OnEntry`, `OnDeparture`, `OnUnitSelected`, `OnUnitDeselected`, `OnStartPathfinding`, `OnFinishPathfinding`, `OnAttacked`, `OnHit`, `OnDie`.
    *   **Combat Probability (Probability Map)**: `GetPotentialHits(attacker, target, fromHex)` returns a `List<PotentialHit>` representing the outcome distribution for a single roll.
        *   **`PotentialHit`**: Defines a target, probability range (`min` to `max`), `damageMultiplier`, and `logInfo`.
*   **`BattleBrothersRuleset`**: Manages terrain costs, Zone of Control, and attack-range aware pathfinding.
    *   **Bucket-Based Probability Engine**: Unifies all combat logic into a shared [0, 1] range:
        *   **Bucket A (Target)**: Primary hit chance, reduced by `coverMissChance` (75%) if obstructed.
        *   **Bucket B (Cover Interception)**: If the primary shot is intercepted by cover, it rolls against the intercepting unit's `RDEF`.
        *   **Bucket C (Miss Scatter/Stray)**: Missed primary shots (that weren't intercepted) can scatter to neighbors or hexes behind the target (at Distance 3+), rolling against their `RDEF`.
    *   **Combat Modifiers**: Uses base stats (`MSKL` vs `MDEF`, `RSKL` vs `RDEF`) and applies configurable modifiers:
        *   **Elevation**: `elevationBonus`, `elevationPenalty`, `rangedHighGroundBonus`.
        *   **Surround**: Cumulative `surroundBonus` based on the number of unique ally **Zone of Control** states currently active on the target hex.
        *   **Proximity Penalty**: `longWeaponProximityPenalty` (15%) for Range-2 melee weapons used at Distance 1.
    *   **Zone of Control (ZoC)**:
        *   Units with `MRNG > 0` (Melee) exert ZoC on neighbors within `maxElevationDelta`.
        *   Entering an enemy ZoC adds a significant `zocPenalty` (default 50) to the movement cost, but does not hard-stop movement (unless AP/Fatigue logic, currently implemented as high cost, prevents it).

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