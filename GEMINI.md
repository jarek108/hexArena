# Development environment
- Machine: Windows 10 pro
- Unity 6000.214f1; 3D project using URP.
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
    *   **Data**: `UnitSet`, `UnitSchema` (ScriptableObjects).
    *   **Logic**: `Unit` (Unique `Id`, stats, team affiliation).
    *   **View**: `UnitVisualization` (Models/Anims).
    *   **Principle**: Units are grid-aware agents that link to `HexData`. Inspector changes trigger real-time re-initialization and grid synchronization.
*   **Ruleset & Game Logic** (Detailed in `designs/grid_system.md` & `unit_system.md`):
    *   **Logic**: `Ruleset` (Abstract SO), `BattleBrothersRuleset`.
    *   **Management**: `GameMaster` (Persistent Singleton).
    *   **Principle**: The Ruleset is the game's "brain," dictating movement costs and grid-state side effects (e.g., Zone of Control). The `Pathfinder` and `Unit` systems query the `GameMaster` to execute specific gameplay rules.
*   **Interaction**: `ToolManager` (Central Hub), `PathfindingTool` (Real-time pathing), `ToggleTool` (Base for utility triggers like `GridTool` and `ZoCTool`), and various Editor Tools.

# Workflow and coding standards

## Documentation workflow
*   **Central Hub**: `GEMINI.md` contains high-level context, environment details
*   **System Documentation**: Detailed architecture, decision logs, and structures for specific systems live in `designs/<system_name>.md`.
*   **Policy**:
    *   Always keep documentation high-level in the root file and detailed in the designs.
    *   **User Confirmation**: Never delete or significantly restructure documentation without explicit user confirmation.
    *   Update `designs/` files as features are completed and verified.

## TDD Feature introduction workflow
1.  **Understand & Strategize:** Analyze the required feature, propose options, select approach with the user.
2.  **Plan:** Develop a concise plan and proposed tests for the selected approach.
3.  **Write tests:** Write a complete list of tests covering the new feature
4.  **Implement:** Write/correct game code
5.  **Testing** Run the tests. MAKE SURE to inspect console errors
6.  **Correction loop** Go to 4 till tests pass, and you MADE SURE there is no console errors. Confirm with user you are done
7.  **Verification & finalization:** Once user confirms feature is done, update documentation, PROPOSE committing.

## Python coding
1. When writing Python, be very concise and prefer brevity over handling all errors etc.

## Unity Editor Coding
*   **ScrollViews**: In custom editors, never use `GUILayout.Height` inside `EditorGUILayout.BeginScrollView`. It restricts the layout unnecessarily and often looks poor.
*   **Headers**: Never use `[Header("...")]` attributes in MonoBehaviours. All section labeling and organization must be handled within the custom editor.

# Tool Usage
0. **Shell Commands**: NEVER use `&&` to chain multiple shell commands (e.g., `git status && git diff`). This syntax is often rejected by the shell parser. Always execute each command independently in its own tool call.
1. **Game Screenshots** - run `python tools/unityGameScreenshot.py` without any arguments to capture screenshots (and not unity-mcp). NEVER provide a custom filename. You MUST use the filename provided in the tool's stdout output when referring to the screenshot. *Note: This script automatically moves the screenshot outside the Assets folder to `Screenshots`. Captures Game View only. Edit Mode screenshots are ok, but Play Mode may be unreliable; ask user for manual verification if needed.*
2. **Waiting** - in interactions where you suspect some ongoing process needs to finish use `python tools/waitFewSeconds.py`. It accepts a single integer - the number of seconds to wait before returning. Useful in interactions where a delay is needed, for example, to allow Unity to recompile scripts or process asset changes.
3. **Window Activation** - run `python tools/windowActivator.py "<title_substring>"` to bring windows containing the substring in their title to the foreground. Useful for switching back to Unity or the Game view.
4. **Testing** Prefer `EditMode` over `PlayMode` testing. Always follow this testing sequence:
    *   **Activate Unity**: ALWAYS run `python tools/windowActivator.py "Unity 6.2"` before running any tests or commands that require Unity's attention.
    *   **Initial Console Check** BEFORE ruining the tests always use unity-mcp's `read_console` (types: `['error', 'warning']`). In case of any console errors abandon testing till they are resolved.
    *   **Test run** always use unity-mcp's `run_tests`. *Troubleshooting: If 'No Unity plugins connected' error occurs, try waiting 20s or more for recompilation.*
    *   **Post-test Console Check** BEFORE reporting test results further, check the console again. In Unity errors may indicate unreliable tests results
4.  **Git Usage:** 
    *   You may propose commit msgs after a major feature/bug fix is verified and CONFIRMED BY THE USER. Never commit changes without user confirmation.
    *   Prepare commit messages based on conversation context and a list of modified files. Do not do long git diff HEAD analysis etc.
    *   Use simple language and list all areas of change
    *   **Independent Commands**: Run each git command independently. NEVER chain them with `&&`.
    *   Make sure you push after each commit
5.  **Scene inspection/Management:** - Use use unity-mcp's `manage_gameobject` (action: `get_components`) or `manage_scene` (action: `get_hierarchy`) to inspect scene
6. **Editing** - while using edit replacing 'old_string' with new always break long edits into smaller to avoid tool issues due to the errors in the replaced strings ('The exact text in old_string was not found')