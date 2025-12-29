# Battle Brothers Ruleset Design

## Combat Statistics

This ruleset uses a simplified version of the Battle Brothers combat system. Statistics are divided into base attributes and primary combat skills.

### The Exclusive Skill Rule
A unit can only be a specialist in one form of combat. 
*   **Melee Units**: Have a non-zero Melee Skill (`MAT`) and `RAT` set to 0.
*   **Ranged Units**: Have a non-zero Ranged Skill (`RAT`) and `MAT` set to 0.

All other combat attributes (`RNG`, `DMIN`, `DMAX`, `ABY`, `ADM`, `AFAT`) refer to the weapon associated with this primary skill.

### Stat Definitions
*   **HP**: Hitpoints. If this reaches 0, the unit dies.
*   **ARM**: Armour. Currently a placeholder for future damage reduction logic.
*   **FAT**: Max Fatigue.
*   **INI**: Initiative. Determines turn order (higher is faster).
*   **DMIN / DMAX**: Damage range per hit.
*   **RNG**: Maximum range of the attack. Melee is typically 1 or 2.
*   **ABY**: Armour Bypass (percentage of damage that ignores armour).
*   **ADM**: Armour Damage (multiplier for damage dealt to armour).
*   **AP**: Action Points per turn.
*   **RES**: Resolve. Bravery and mental fortitude.
*   **AFAT**: Attack Fatigue. The cost in stamina to perform an attack.

## Weapon Balancing (Baked Stats)
To keep the logic data-driven and simple, weapon bonuses are incorporated directly into the unit's skills:
*   **Swords**: +10% to `MAT`.
*   **Spears**: +20% to `MAT`.
*   **Axes/Maces/Hammers**: No skill bonus, but higher `ADM` or `DMAX`.
*   **Bows**: High `RNG`, lower `ABY`.
*   **Crossbows**: Lower `RNG`, higher `ABY`.

## Gameplay Mechanics

### Zone of Control (ZoC)
Units with Melee Range > 0 exert a Zone of Control on all adjacent hexes within `maxElevationDelta`. Entering an enemy's ZoC adds a movement penalty.

### Surround Bonus
Melee hit chance is increased by +5% for every ally (excluding the attacker) currently exerting Zone of Control on the target.

### Elevation
*   **Melee**: High ground provides a bonus to hit; low ground applies a penalty.
*   **Ranged**: High ground provides a bonus to hit. Ranged attacks cannot target units at significantly lower elevations if obstructed.
