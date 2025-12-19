# Development environment
- Machine: Windows 10 pro
- Unity 6000.214f1; 3D project using URP.
- Target platform: PC
- C# gameplay logic separated from UI/presentation.

## Game Goal
- Turn‑based, squad‑scale tactics on hex maps focused on positioning, flanking, elevation

## Current Architecture
- **Data-Driven Map:** The map logic (`HexGrid`, `HexData`) is purely C# and independent of Unity `GameObjects`.
- **View Layer:** `Hex` MonoBehaviours act as visual puppets, synchronizing with `HexData` properties (`Elevation`, `TerrainType`) and handling serialized state for Editor persistence.
- **Manager:** `HexGridManager` bridges the two layers, handling initialization, serialization restoration, and editor updates via `OnValidate`.

# Workflow and coding standards

## Documentation 
- **Session Metadata:** At the start of each session, record the **Date**, **Start Time**, and at the end of the session, record the **End Time** and **Token Usage** in the corresponding session entry (e.g., `grid.md`).
- Append work to the most recent session block unless explicitly instructed to start a new one.
- While working on a specific system document the work in an appropriate md file (example grid.md)
- For each session section maintain subsections: ### Features Implemented, ### Bugs encountered and fixed
- In the ### Bugs encountered and fixed section list all the mistakes/bug fixing iterations with 'Bug', 'Human effort to resolve'(add in a bracket add estimation of how much expertise and time was needed to help you) and 'Solution' points
- Only update the bugs after their resolution is confirmed by the user

## Software Engineering Tasks
1.  **Understand & Strategize:** Analyze the problem/context.
2.  **Plan:** Develop a concise plan and proposed tests.
3.  **Implement:** Write tests first (failing), then game code (passing).
4.  **Verify:** Run tests (EditMode/PlayMode) and linting/standards.
5.  **Finalize:** Update documentation.

## Testing Protocol
*Follow this sequence for reliable verification:*

1.  **Pre-Check:**
    *   **Save Scene:** Use `manage_scene` (action: `save`) to prevent data loss.
    *   **Check Console:** Use `read_console` to ensure a clean state before starting.
2.  **Execution & Verification:**
    *   **Visuals:** Run `python unityGameScreenshot.py` to capture screenshots. *Note: This script automatically moves the screenshot outside the Assets folder to `hexagon/Screenshots`. Captures Game View only. Play Mode screenshots are unreliable; ask user for manual verification if needed.*
    *   **Composition:** Use `manage_gameobject` (action: `get_components`) or `manage_scene` (action: `get_hierarchy`) to verify object structure.
    *   **Post-Check Console:** Use `read_console` (types: `['error', 'warning']`) immediately after actions to catch new issues.
3.  **Runtime Check:**
    *   Start game (`manage_editor` action: `play`), confirm with user, then re-run Visual/Console checks.
4.  **Automated Tests:**
    *   Create `EditMode` tests for logic/math and `PlayMode` tests for interaction.
    *   Run with `run_tests`. *Troubleshooting: If 'No Unity plugins connected' error occurs, wait for recompilation.*