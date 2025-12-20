# Development environment
- Machine: Windows 10 pro
- Unity 6000.214f1; 3D project using URP.
- Target platform: PC
- C# gameplay logic separated from UI/presentation.

## Current Architecture

The project follows a **data-driven architecture** that separates the grid's logical state from its visual representation in Unity. This ensures a clean, testable, and flexible design.

-   **Logic Layer (`HexData.cs`, `HexGrid.cs`):**
    -   At its core, the map is a pure C# `HexGrid` class that manages a collection of `HexData` objects.
    -   `HexData` is a lightweight class holding the essential state for each hex: axial coordinates (`Q`, `R`), `Elevation`, `TerrainType`, and a reference to any occupying `Unit`.
    -   This layer is entirely independent of Unity's `GameObject` system, allowing for fast, headless operations like pathfinding and logical calculations.

-   **View Layer (`Hex.cs`):**
    -   `Hex` is a `MonoBehaviour` attached to each hexagonal `GameObject` in the scene.
    -   It acts as a visual "puppet," synchronizing its appearance with the state of its assigned `HexData` object.
    -   It holds serialized backing fields for its coordinates and properties (`viewQ`, `viewR`, etc.). This provides resilience against Unity's domain reloads, ensuring the grid state can be reconstructed even after scripts recompile.

-   **Bridge & Manager (`HexGridManager.cs`):**
    -   This central `MonoBehaviour` orchestrates the grid's runtime state.
    -   It holds the primary instance of the logical `HexGrid`.
    -   It handles procedural generation and the creation of the visual grid.
    -   It provides helper methods like `WorldToHex` and `GetHexView` to bridge the gap between Unity's world space and the grid's data layer.

-   **Persistence (`GridPersistence.cs`):**
    -   A separate `MonoBehaviour` that resides on the same `GameObject` as `HexGridManager`.
    -   It is solely responsible for saving and loading the grid state.
    -   It uses `JsonUtility` to serialize a list of simple `HexSaveData` objects to a file, ensuring the save format is stable and decoupled from runtime logic.

-   **Interaction (`SelectionManager.cs`, `HexGridEditor.cs`):**
    -   `SelectionManager` is a focused controller that handles player input and calls public methods on `HexGridManager` to manage visual states like highlighting and selection.
    -   `HexGridEditor` provides a powerful custom inspector with tools for "painting" terrain, adjusting elevation, and buttons that call the appropriate methods on `HexGridManager` or `GridPersistence` to generate, save, load, or clear the grid.

# Workflow and coding standards

## Documentation 
- **Session Metadata:** At the start of each session, record the **Date**, **Start Time**, and at the end of the session, record the **End Time**  in the corresponding session entry in an appropriate md file (e.g., `grid.md`)
- Append work to the most recent session block 
- Start a new session only when explicitly instructed
- For each session section maintain subsections: ### Features Implemented, ### Bugs encountered and fixed
- In the ### Bugs encountered and fixed section list all the mistakes/bug fixing iterations with 'Bug', 'Human effort to resolve'(add in a bracket add estimation of how much expertise and time was needed to help you) and 'Solution' points
- Only update the bugs after their resolution is confirmed by the user

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

# Tool Usage
1. **Game Screenshots** - run `python tools/unityGameScreenshot.py` to capture screenshots (and not unity-mcp). *Note: This script automatically moves the screenshot outside the Assets folder to `Screenshots`. Captures Game View only. Edit Mode screenshots are ok, but Play Mode may be unreliable; ask user for manual verification if needed.*
2. **Waiting** - in interactions where you suspect some ongoing process needs to finish use `python tools/waitFewSeconds.py`. It accepts a single integer - the number of seconds to wait before returning. Useful in interactions where a delay is needed, for example, to allow Unity to recompile scripts or process asset changes.
3. **Testing** Prefer `EditMode` over `PlayMode` testing. Always follow this testing sequence:
    *   **Initial Console Check** BEFORE ruining the tests always use unity-mcp's `read_console` (types: `['error', 'warning']`). In case of any console errors abandon testing till they are resolved.
    *   **Test run** always use unity-mcp's `run_tests`. *Troubleshooting: If 'No Unity plugins connected' error occurs, try waiting for recompilation.*
    *   **Post-test Console Check** BEFORE reporting test results further, check the console again. In Unity errors may indicate unreliable tests results
4.  **Git Usage:** 
    *   You may propose commit msgs after a major feature/bug fix is verified and CONFIRMED BY THE USER. 
    *   Even then only suggest the commit message and ask for confirmation to make such commit. Never commit changes without user confirmation.
    *   Prepare commit messages based on the session notes and your context. Avoid long git diff HEAD analysis etc.
5.  **Scene inspection/Management:** - Use use unity-mcp's `manage_gameobject` (action: `get_components`) or `manage_scene` (action: `get_hierarchy`) to inspect scene
6. **Editing** - while using edit replacing 'old_string' with new always break long edits into smaller to avoid tool issues due to the errors in the replaced strings ('The exact text in old_string was not found') 