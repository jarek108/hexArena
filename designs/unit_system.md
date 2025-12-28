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
    *   **Hooks**: `OnEntry`, `OnDeparture` (return booleans to allow movement interruption), `OnUnitSelected`, `OnUnitDeselected`.
    *   **Combat Probability (Probability Map)**: `GetPotentialHits(attacker, target)` returns a `List<PotentialHit>`.
        *   **`PotentialHit`**: Defines a target, probability range (`min` to `max`), `drawIndex` (correlating or isolating rolls), and `damageMultiplier`.
*   **`BattleBrothersRuleset`**: Manages terrain costs, Zone of Control, and attack-range aware pathfinding.
    *   **Probability Engine**: Unifies all combat logic into a shared data structure:
        *   **Exclusive Outcomes**: Shared `drawIndex` with non-overlapping ranges (e.g., Target vs Cover Interception).
        *   **Conditional Outcomes**: Conditioned on previous draws (e.g., Stray shots triggered only if the trajectory draw misses).
        *   **Independent Outcomes**: Unique `drawIndex` values (e.g., multi-target cleaves).
    *   **Combat Modifiers**: Uses base stats (`MSKL` vs `MDEF`, `RSKL` vs `RDEF`) and applies configurable modifiers:
        *   **Elevation**: `elevationBonus`, `elevationPenalty`, `rangedHighGroundBonus`.
        *   **Surround (Backstabber)**: Cumulative `surroundBonus` based on ally Zone of Control states on the target hex.
        *   **Proximity Penalty**: `longWeaponProximityPenalty` for Range-2 melee weapons at Distance 1.
        *   **Cover & Scattering**: Uses the probability map to model the 75% cover interception and miss scattering (behind/adjacent tiles).
        *   **Ranged Units**: Only melee units (`MRNG > 0`) produce ZoC, meaning ranged-only units do not contribute to surround bonuses.

## Key Interactions

### Pathfinding & Combat Flow
1.  **Selection**: `PathfindingTool` identifies a `SourceHex`.
    *   Triggers `ruleset.OnUnitSelected`.
    *   Immediately initiates a zero-length pathfinding call to show initial visuals.
2.  **Hover**: Ruleset receives `OnStartPathfinding`. It determines if the target is an enemy and calculates the appropriate `AttackType` (Melee/Ranged).
3.  **Pathing**: `Pathfinder` calculates the raw path. The Ruleset evaluates costs, allowing pass-through for friendly units but forbidding ending moves on occupied hexes.
4.  **Preview**: Ruleset receives `OnFinishPathfinding`.
    *   Uses `GetMoveStopIndex` to truncate the path (stopping at attack range).
    *   Spawns a **Ghost** at the stop position.
    *   Calculates **Area of Attack (AoA)**: Highlights all hexes within the unit's max range from the stopping hex. Melee ranges respect `maxElevationDelta`.
5.  **Execution**: On click, the tool calls `ruleset.ExecutePath`. 
    *   The unit "unoccupies" its start hex.
    *   At each step, `OnDeparture` and `OnEntry` are called. If either returns `false`, movement stops immediately.
    *   Upon completion or interruption, the unit "occupies" its current hex.
    *   If the target was an enemy, performs an `OnAttack` sequence (facing the target, triggering animations).

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