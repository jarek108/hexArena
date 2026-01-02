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

## Resource Management

This ruleset implements dynamic resource tracking using Action Points (AP) and Fatigue (FAT).

### Action Points (AP)
*   **Action Costs**: Every action (moving, attacking) costs AP. 
*   **Insufficient AP**: If a unit's `CAP` (Current Action Points) is lower than the action cost, the action is blocked. 
*   **Restoration**: Currently, units are manually initialized with their max AP (`AP` stat) when selected.

### Fatigue (FAT)
*   **Stamina System**: Actions increase a unit's `CFAT` (Current Fatigue).
*   **Exhaustion**: If `CFAT` reaches or exceeds the `FAT` (Max Fatigue) stat, the unit cannot perform further actions.
*   **Initiative Impact**: A unit's effective initiative is reduced by its current fatigue (`EffectiveINI = INI - CFAT`).

## Advanced Combat Model

### Attack Type Resolution
Attack types are determined by the **Exclusive Skill Rule**:
*   **Melee**: If `MAT > 0`. Uses `MAT` vs `MDF`.
*   **Ranged**: If `RAT > 0`. Uses `RAT` vs `RDF`.

### Advanced Damage Calculation
Combat uses a multi-layered damage resolution process involving Armour and Health:
1.  **Damage Roll**: A base value is rolled between `DMIN` and `DMAX`.
2.  **Armour Damage**: Total damage dealt to armour is `base * (ADM / 100)`.
3.  **Direct HP Damage**: A portion of damage always bypasses armour: `base * (ABY / 100)`.
4.  **Armour Depletion**: If target `ARM` reaches 0, all remaining armour damage is applied directly to `HP`.

### Terrain & Movement
*   **AP Costs**: Terrain types dictate the AP cost to enter a hex (e.g., Plains = 2 AP).
*   **Fatigue Costs**: Moving increases fatigue by an amount equal to the AP cost.
*   **Elevation**: Moving uphill adds an `uphillPenalty` to both AP and Fatigue costs.
*   **Zone of Control**: Entering an enemy's ZoC adds a significant `zocPenalty` to the cost.

## Gameplay Mechanics

### Zone of Control (ZoC)
Units with `MAT > 0` (Melee specialists) exert a Zone of Control on adjacent hexes. Ranged-only units (`RAT > 0, MAT = 0`) do not exert ZoC.

### Surround Bonus
Melee hit chance is increased by +5% for every ally currently exerting Zone of Control on the target hex.

### Elevation Modifiers
*   **Melee High Ground**: +10 hit chance (`meleeHighGroundBonus`).
*   **Melee Low Ground**: -10 hit chance (`meleeLowGroundPenalty`).
*   **Ranged High Ground**: +10 hit chance (`rangedHighGroundBonus`).
*   **Ranged Low Ground**: -10 hit chance (`rangedLowGroundPenalty`).
