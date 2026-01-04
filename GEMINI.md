# STRICT MANDATE: NO UNCONSULTED COMMITS
**NEVER commit or push changes to Git without explicit, turn-by-frame consultation and confirmation from the user.** Even if a task feels "complete" or the user says "finish the task", you MUST propose the commit message and wait for an explicit "Yes, commit this" for that specific set of changes. This is a zero-tolerance policy.

# Development environment
- Machine: Windows 10 pro
- Unity 6000.3.2f1; 3D project using URP.
- Target platform: PC
- C# gameplay logic separated from UI/presentation.

## Architecture

We follow a **Data-Driven Architecture** prioritizing strict Logic/View separation. High-level summaries are below; for detailed architectural breakdowns, refer to the specific system notes in the `designs/` folder.

### Systems
*   **Grid System** (Detailed in `designs/grid_system.md`):
    *   **Logic**: `HexData` & `Grid` (Pure C#).
    *   **View**: `Hex`, `GridVisualizationManager`.
    *   **Principle**: Logic dictates state; View reacts. `GridVisualizationManager` resolves styling (rims/colors) based on string-based state priorities. Visibility is gated by a `priority > 0` check (negative priorities hide states while preserving data).
*   **Unit System** (Detailed in `designs/unit_system.md`):
    *   **Data**: JSON-based Schemas and Sets (`Assets/Data/`).
    *   **Logic**: `Unit` (Unique `Id`, stats, team affiliation). GameObject naming convention: `UnitName_ID`.
    *   **View**: `UnitVisualization` (Models/Anims). Automated lunge animations.
    *   **Principle**: Units are grid-aware agents. All configuration data is stored in JSON files, managed via the `UnitDataEditorWindow`. `UnitManager` centralizes the active set and persists it within layout files. Quick-save support via `lastLayoutPath`.
*   **Ruleset & Game Logic** (Detailed in `designs/grid_system.md` & `unit_system.md`):
    *   **Logic**: `Ruleset` (Abstract SO), `BattleBrothersRuleset`.
    *   **Management**: `GameMaster` (Persistent Singleton).
    *   **Principle**: The Ruleset is the game's "brain," dictating movement costs and grid-state side effects (e.g., Zone of Control). The `Pathfinder` and `Unit` systems query the `GameMaster` to execute specific gameplay rules.
    *   **Combat**: `BattleBrothersRuleset` uses a bucket-based probability engine for unified resolution of hits, cover interception, and scatter (stray shots). Pathfinding is attack-aware, stopping units at optimal range and visualizing the Area of Attack (AoA).
*   **Interaction**: `ToolManager` (Central Hub), `PathfindingTool` (Real-time pathing), `ToggleTool` (Base for utility triggers like `GridTool` and `ZoCTool`), and various Editor Tools (HexArena menu).

# Workflow and coding standards

## Documentation workflow
*   **Central Hub**: `GEMINI.md` contains high-level context, environment details
*   **System Documentation**: Detailed architecture, decision logs, and structures for specific systems live in `designs/<system_name>.md`.
*   **Policy**:
    *   Always keep documentation high-level in the root file and detailed in the designs.
    *   **User Confirmation**: Never delete or significantly restructure documentation without explicit user confirmation.
    *   Update `designs/` files as features are completed and verified.
*   **Unit Data Management**: Use the **`Unit Data Editor`** (`HexArena` menu) for all Schema and Unit Set modifications. These changes are auto-saved directly to JSON files. Avoid manual JSON edits unless necessary.

## TDD Feature introduction workflow
1.  **Understand & Strategize:** Analyze the required feature, propose options, select approach with the user.
2.  **Plan:** Develop a concise plan and proposed tests for the selected approach.
3.  **Write tests:** Write a complete list of tests covering the new feature
4.  **Implement:** Write/correct game code
5.  **Testing**: Run `python tools/diagnose_unity.py`. This is the ONLY acceptable way to verify your changes. Full diagnostics (including tests) must always be run.
6.  **Correction loop**: Repeat steps 4 and 5 until `diagnose_unity.py` reports "Verification Successful!" with zero console errors and all tests passing. Confirm with the user once this state is reached.
7.  **Verification & finalization:** Once user confirms feature is done, update documentation, PROPOSE committing.
8.  **Test Persistence:** Always store created tests as permanent artifacts in the codebase as long as they remain relevant and the feature they test exists. Never delete tests after verification unless explicitly requested.

## Python coding
1. When writing Python, be very concise and prefer brevity over handling all errors etc.

## Unity Editor Coding
*   **ScrollViews**: In custom editors, never use `GUILayout.Height` inside `EditorGUILayout.BeginScrollView`. It restricts the layout unnecessarily and often looks poor.
*   **Headers**: Never use `[Header("...")]` attributes in MonoBehaviours. All section labeling and organization must be handled within the custom editor.
*   **JSON-Only Data**: For gameplay configuration (like unit stats), always prefer JSON serialization over Unity Assets. Use the pattern of transient ScriptableObjects loaded from JSON at runtime/editor-time.
*   **Auto-Save**: Custom data editor windows must implement auto-save functionality to prevent data loss. Use `EditorGUI.BeginChangeCheck` to detect modifications.
*   **Menu**: Centralize specialized project tools under the **`HexArena`** top-level menu. Avoid creating multiple proprietary menu categories.

# Tool Usage
0. **Shell Commands**: NEVER use `&&` to chain multiple shell commands. Execute each independently.
1. **Screenshots & Visual Inspection** - NEVER use MCP for screenshots. ALWAYS use `python tools/capture_unity.py "<purpose>"` to capture the Unity window.
2. **Testing & Diagnostics** - NEVER use MCP's `read_console`, `run_tests`, or `manage_scene (save)` directly. ALWAYS use `python tools/diagnose_unity.py`. This is the mandatory "Quality Gate" for the project. Full diagnostics (including tests) are mandatory and MUST always be run. It ensures stability by:
    1. **Monitoring Compilation**: Intelligently waits for background compilation to stabilize (polling DLL vs CS timestamps) so tests never run on stale code.
    2. **Auditing Console**: Performs a deep scan for errors/exceptions while filtering out infrastructure noise.
    3. **Unified Verification**: Handles scene saving and test execution in a single atomic pass.
    If the script fails (exit code 1), you MUST fix the reported errors before proceeding.
3. **Git Usage**: 
    * **CRITICAL**: NEVER commit or push without explicit confirmation.
    * Propose commit messages first, listing all modified files.
    * Run each git command independently.
4. **Scene inspection/Management**: Use `manage_gameobject` or `manage_scene`.
5. **Editing**: Break long edits into smaller ones to avoid context matching issues.