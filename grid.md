# Features

*   **Coordinate System**: Axial coordinate system to identify and access each hex.
*   **Adjacency**: Ability to find all 6 neighbors of a given hex.
*   **Distance Calculation**: Methods to calculate the distance between two hexes.
*   **Pathfinding**: A* pathfinding to find the shortest path between two hexes, considering movement costs.
*   **Line of Sight**: Determine if there is a clear line of sight between two hexes, considering obstacles and elevation.
*   **Elevation**: Each hex has an elevation, affecting movement, range, and combat.
*   **Terrain**: Each hex has a terrain type (e.g., plains, forest, mountain) with associated movement costs.
*   **Data Storage**: Store and retrieve data for each hex (e.g., occupying unit, status effects).
*   **Map Generation**: Support for both procedurally generated and predefined map layouts.
*   **World Conversion**: Convert hex coordinates to and from Unity world space coordinates.
*   **Mouse Interaction**: Highlight hexes on mouse-over and select them on click.

## Plan

### Phase 1: Core Data Structures

1.  **`Hex.cs`**:
    *   A C# class to represent a single hexagonal tile.
    *   It will use axial coordinates (`q`, `r`) for its position in the grid. An `s` coordinate will be derived (`s = -q - r`).
    *   Properties for game-related data:
        *   `Elevation`: A float to represent the height of the hex.
        *   `TerrainType`: An enum (`Plains`, `Water`, `Mountains`, `Forest`, `Desert`, etc.) to define the type of terrain.
        *   `IsWalkable`: A boolean to quickly check if a unit can traverse this hex.
    *   Static methods for hex-to-hex calculations (distance, neighbors, etc.), using the axial coordinate system.

2.  **`HexGrid.cs`**:
    *   A C# class to manage the entire grid.
    *   It will store all `Hex` objects in a `Dictionary<Vector2Int, Hex>` where the key is the axial coordinate `(q, r)`.
    *   A constructor to generate a grid of a specific size (e.g., rectangular, hexagonal).
    *   Methods to:
        *   `GetHexAt(q, r)`: Retrieve a hex at a given coordinate.
        *   `GetNeighbors(Hex hex)`: Get all valid neighbors of a hex.
        *   Pathfinding: Implement A* algorithm to find paths between hexes, considering movement costs associated with terrain and elevation changes.

### Phase 2: Visual Representation

1.  **Hex Prefabs**:
    *   Create a simple 3D model for a single hex tile in a modeling tool or use Unity's built-in shapes (e.g., a cylinder with 6 sides).
    *   Create prefabs from this model. We'll need different materials for each terrain type (water, grass, rock, etc.) as seen in the image.
    *   For variations like the grassy patches on the plains, we could use decals or additional small objects on top of the hex.

2.  **`HexGridManager.cs`**:
    *   A `MonoBehaviour` script to be placed on a GameObject in the scene.
    *   It will hold the `HexGrid` data instance.
    *   It will be responsible for instantiating the hex prefabs and placing them in the correct world positions based on their hex coordinates.
    *   It will include a layout function to convert hex coordinates to world coordinates (`Hex to World`).
    *   It will handle updates to the visual representation when the grid data changes.

3.  **`HexGridEditor.cs`**:
    *   A custom Unity Editor script for the `HexGridManager`.
    *   It will provide a visual representation of the grid in the Scene view for easier level design and debugging.
    *   It could include features like:
        *   Displaying hex coordinates on each tile.
        *   Allowing "painting" of terrain types onto the grid directly in the editor.
        *   Adjusting elevation of hexes with the mouse.

### Phase 3: Map Generation

1.  **Procedural Generation**:
    *   Implement algorithms for procedural map generation. This could start with simple noise-based generation (e.g., using Perlin noise) to create varied terrain and elevation.
    *   Define rules for terrain placement, for example, mountains tend to appear in clusters, and forests next to plains.

2.  **Manual Map Creation**:
    *   Leverage the `HexGridEditor` to allow for manual creation and modification of maps.
    *   Implement serialization to save and load map data (terrain, elevation, etc.) to/from files. This would allow for pre-designed levels.

### Phase 4: Interaction

1.  **`HexRaycaster.cs`**:
    *   A `MonoBehaviour` script that casts a ray from the camera to the mouse position.
    *   It will detect which hex is under the mouse pointer.
    *   This can be done by either:
        *   Attaching colliders to each hex prefab and using Unity's physics raycasting.
        *   Converting the mouse position to a hex coordinate and looking it up in the `HexGrid` (requires a `World to Hex` function).

2.  **Highlighting**:
    *   When the `HexRaycaster` detects a new hex under the mouse, it should trigger a highlighting effect.
    *   This can be implemented by:
        *   Changing the material of the hex prefab.
        *   Adding a highlight overlay on top of the hex.
        *   Using a shader that can show an outline or a color change.

3.  **Selection**:
    *   When the user clicks the mouse, the `HexRaycaster` will notify a selection manager.
    *   **`SelectionManager.cs`**:
        *   A `MonoBehaviour` that keeps track of the currently selected hex.
        *   It will receive the selected hex from the `HexRaycaster`.
        *   It will provide a public property to access the selected hex, so other parts of the game (like a UI panel or a unit movement system) can use it.
        *   It could also handle deselection logic (e.g., clicking on an empty area).

# Sessions

## Session 1

### Features Implemented

*   **Core Data Structures:** Implemented `Hex.cs` (basic hexagonal tile representation with axial coordinates, elevation, and terrain type), `HexGrid.cs` (manages the collection of hexes, provides lookup and neighbor finding), and `TerrainType.cs` (an enum for different terrain types).
*   **Visual Representation:**
    *   Implemented `HexGridManager.cs` as a `MonoBehaviour` to handle grid creation, world-to-hex coordinate conversion, and visual instantiation of hexes in the scene. This script now runs in `ExecuteAlways` mode, allowing editor visibility.
    *   Implemented `CreateHexPrefab.cs` (Editor Script) to generate a persistent `Hex.prefab` (a flat hexagonal mesh) and a `HexMaterial.mat` asset with the correct URP shader.
    *   Implemented `HexGridEditor.cs` (Custom Editor) to provide a visual representation of the grid in the scene view, including hex coordinates, and basic painting tools for elevation and terrain.
*   **Interaction:**
    *   Implemented `HexRaycaster.cs` to detect the hex under the mouse pointer using raycasting and world-to-hex conversion.
    *   Implemented `SelectionManager.cs` to handle mouse-over highlighting and click-based selection of hexes, visually changing their color.

### Bugs encountered and fixed

*   **Bug:** `IndexOutOfRangeException` in `CreateHexPrefab.cs` during mesh generation.
    *   **Human Effort:** (Expertise: Low, Time: 5-10 minutes) Identified that the `ti` index variable for `sideTriangles` was initialized incorrectly.
    *   **Solution:** Corrected the initialization of the `ti` variable from `24` to `0` in `CreateHexPrefab.cs`.
*   **Bug:** `InvalidOperationException` due to using old `UnityEngine.Input` system in `HexRaycaster.cs` and `SelectionManager.cs`.
    *   **Human Effort:** (Expertise: Medium, Time: 10-15 minutes) Identified that the project's Player Settings were configured for the new Unity Input System package.
    *   **Solution:** Updated `HexRaycaster.cs` and `SelectionManager.cs` to use the `UnityEngine.InputSystem` API (e.g., `Mouse.current.position.ReadValue()`, `Mouse.current.leftButton.wasPressedThisFrame`).
*   **Bug:** Hex prefab appeared invisible in Unity despite being instantiated, due to missing material and the mesh not being properly assigned.
    *   **Human Effort:** (Expertise: High, Time: 30-45 minutes) Analyzed the provided prefab inspector screenshot, confirming the absence of a material and an unassigned mesh. Realized `new Material(...)` in an editor script doesn't create a persistent asset.
    *   **Solution:** Modified `CreateHexPrefab.cs` to robustly create and save `HexMaterial.mat` (using `Universal Render Pipeline/Lit` shader) and `HexMesh.asset`. These persistent assets are now correctly assigned to the `MeshRenderer` and `MeshCollider` of the `Hex.prefab`.
*   **Bug:** Hexes were visible but rendered upside down (only visible from underneath, not from the top-down camera view).
    *   **Human Effort:** (Expertise: Medium, Time: 10-15 minutes) Identified incorrect winding order in the `CreateHexMesh` method's triangle definitions.
    *   **Solution:** Swapped the second and third vertices in the triangle definition within `CreateHexMesh()` to reverse the winding order, ensuring correct rendering from above.

## Session 2

### Features Implemented

*   **Testing Infrastructure:** Established a robust testing environment by creating the `Assets/Tests` directory structure with separate `EditMode` and `PlayMode` folders.
*   **Assembly Definitions:**
    *   Created `Assets/Scripts/Scripts.asmdef` (assembly name: `HexGame`) to wrap the game code into a defined assembly.
    *   Created `Assets/Tests/EditMode/Tests.EditMode.asmdef` and `Assets/Tests/PlayMode/Tests.PlayMode.asmdef` to reference `HexGame`, ensuring tests can access game scripts.
*   **Unit Tests (EditMode):**
    *   Implemented `HexMathTests.cs` to verify hex arithmetic (Add, Subtract, Scale), neighbor calculation, and distance logic.
    *   Implemented `HexGridTests.cs` to verify grid initialization dimensions and coordinate lookup (`GetHexAt`).
*   **Visual/Integration Tests (PlayMode):**
    *   Implemented `HexGridVisualTests.cs` to verify that `HexGridManager` correctly spawns hex GameObjects in the scene.

### Bugs encountered and fixed

*   **Bug:** Tests failed to compile with `CS0246: The type or namespace name 'HexGame' could not be found`.
    *   **Solution:** Created `Scripts.asmdef` (named `HexGame`) in `Assets/Scripts` to explicitly define the game assembly. Updated test asmdefs to reference `HexGame` instead of `Assembly-CSharp`.
*   **Bug:** `HexGridVisualTests` failed in EditMode due to `Awake` error and `Destroy` call.

## Session 3

### Features Implemented

*   **Hex as MonoBehaviour:** Converted `Hex.cs` from a pure C# class to a `MonoBehaviour`, allowing `Hex` data (coordinates, elevation, terrain type) to be directly visible and editable on individual Hex GameObjects in the Unity Inspector.
*   **Procedural Elevation with Perlin Noise:** Integrated Perlin noise generation into `HexGridManager.cs` to procedurally assign varied elevation values to hexes, creating dynamic terrain landscapes.
*   **Procedural Hex Mesh Generation:** Removed dependency on `Hex.prefab` and `HexMesh.asset`. The `HexGridManager` now dynamically generates the skirted hexagonal mesh entirely in code, including `MeshFilter`, `MeshRenderer`, and `MeshCollider` components.
*   **Hard Edge Shading (Submeshes):** Implemented submeshes for the hex geometry within `CreateHexMesh` in `HexGridManager.cs`. The top face and side faces now use separate sets of vertices, eliminating shared normals and creating sharp, distinct edges for improved visual clarity, especially between different elevation levels.
*   **Multi-Material Support:** Configured `HexGridManager.cs` to support two materials (`Hex Material Top` and `Hex Material Sides`) assigned to the respective submeshes, allowing distinct textures/colors for the top and sides of each hex. Default green for top and brown for sides were set in code for URP compatibility.
*   **Data-Driven Color Mapping:** Implemented a `TerrainColorMapping` system in `HexGridManager` to map `TerrainType` enums to specific `Color` values directly in the Inspector. Added `OnValidate` logic to automatically populate these mappings with defaults, as well as auto-loading interaction material assets.
*   **Centralized Interaction Colors:** Moved highlight and selection color configuration to public fields in `HexGridManager`, allowing for easy color tweaking in the Inspector without editing code or material assets. Updated `SelectionManager` to use these centralized values.

### Bugs encountered and fixed

*   **Bug:** `CS0618` warnings for `Object.FindObjectOfType` being obsolete.
    *   **Human Effort:** (Expertise: Low, Time: 5 minutes) Identified usage of deprecated API in `HexRaycaster.cs` and `SelectionManager.cs`.
    *   **Solution:** Replaced `FindObjectOfType<T>()` with `FindFirstObjectByType<T>()` to adhere to newer Unity API standards.
*   **Bug:** Hex objects not showing elevation data in Inspector despite having `Elevation` property.
    *   **Human Effort:** (Expertise: Medium, Time: 10 minutes) Realized `Hex.cs` was a plain C# class, not a `MonoBehaviour`, preventing it from appearing in the Inspector.
    *   **Solution:** Refactored `Hex.cs` to inherit from `MonoBehaviour`, making its properties visible and editable in the Inspector. Updated `HexGridManager.cs`, `HexGrid.cs`, `HexMathTests.cs`, `HexGridTests.cs`, `HexRaycaster.cs`, `SelectionManager.cs`, and `HexGridEditor.cs` to adapt to this architectural change.
*   **Bug:** Hexes of the same altitude had "puffy" shading boundaries, and materials were not appearing correctly.
    *   **Human Effort:** (Expertise: Medium, Time: 15 minutes) Identified that shared vertices between the top and sides of the hex mesh caused smoothed normals, and material application for URP needed to use `_BaseColor`.
    *   **Solution:** Implemented submeshes and separated vertices for the top and side faces in `CreateHexMesh` to create hard edges and distinct normals. Updated `HexGridManager` to assign two materials (one for the top, one for the sides) and correctly set `_BaseColor` for URP materials.
*   **Bug:** CLI tool failed to assign materials to `HexGridManager` component.
    *   **Human Effort:** (Expertise: Low, Time: 2 minutes) Diagnosed a tool limitation or incorrect parameter format for `set_component_property`.
    *   **Solution:** Changed the strategy to ensure default materials were correctly set in code using `_BaseColor` property for URP compatibility, so manual assignment via tool or inspector is not strictly required for initial visual feedback.
*   **Bug:** Highlighting and Deselection Logic Error: Hexes turned white instead of restoring their original color after being un-hovered or deselected.
    *   **Human Effort:** (Expertise: Medium, Time: 20 minutes) Created comprehensive unit tests (`HighlightingTests.cs`) to reproduce the issue. Identified that `SelectionManager` was explicitly setting the color to `Color.white` instead of querying the hex's default.
    *   **Solution:** Implemented `GetDefaultHexColor` in `HexGridManager` and refactored `SelectionManager` to use this method to restore the correct base color. Updated `SetHexColor` to use `MaterialPropertyBlock` to prevent material leaks in the editor. Updated tests to correctly check runtime colors using property blocks.
*   **Bug:** `CS1061` errors in `HighlightingTests.cs` due to direct access to private `hexMaterialSides` field, and side faces of hexes visually changing color during highlighting/selection in Play Mode.
    *   **Human Effort:** (Expertise: Medium, Time: 25 minutes) Identified that `hexMaterialSides` was private, causing compilation errors in tests. Diagnosed that `MaterialPropertyBlock` was being applied globally instead of targeting only the top submesh, leading to the visual bug.
    *   **Solution:** Modified `HexGridManager.SetHexColor` to use `renderer.SetPropertyBlock(propertyBlock, 0);` to explicitly apply the color override only to the first material (top face). This resolved the visual bug. **Verification:** Updated `HighlightingTests.cs` to correctly check specific material indices and account for empty property blocks on side faces. All 16 EditMode tests now pass, confirming the fix programmatically.
*   **Bug:** Selected hexes turned Green instead of Red.
    *   **Human Effort:** (Expertise: Low, Time: 5 minutes) Identified that `SelectionManager.cs` had `selectionColor` explicitly initialized to `Color.green`.
    *   **Solution:** Changed the default `selectionColor` in `SelectionManager.cs` to `Color.red`. Updated `HighlightingTests.cs` to verify that selection results in a Red color override.
*   **Bug:** Hex objects created without MeshFilter/Renderer components (invisible).
    *   **Human Effort:** (Expertise: High, Time: 20 minutes) Diagnosed that `Hex.OnValidate` was throwing a `MissingComponentException` because it attempted to access `GetComponent<Renderer>()` immediately upon creation (during `GenerateGrid`), before the `MeshRenderer` was added. This exception aborted the generation loop for each hex.
    *   **Solution:** Added a null check for `GetComponent<Renderer>()` in `Hex.OnValidate`. Also ensured `HexGridManager.GenerateGrid` forces mesh recreation to prevent stale references.

## Session 5

**Start Time:** 2025-12-16 HH:MM
**End Time:** 2025-12-16 HH:MM
**Token Usage:** [TBD]

### Features Implemented

*   **Procedural Generation Refinement:**
    *   Enhanced `HexGridManager.cs` to support sophisticated terrain generation based on elevation and noise thresholds.
    *   Implemented `TerrainType` assignment logic (Water < 0.4, Plains, Mountains > 0.8) and secondary noise for Vegetation (Forests).
    *   Exposed generation parameters (`waterLevel`, `mountainLevel`, `forestLevel`, `forestScale`) to the Inspector for easy tuning.
*   **Unit Placement Logic:**
    *   Created `Unit.cs` component to represent game units.
    *   Implemented `Unit.SetHex(Hex)` logic to handle unit occupancy, ensuring a unit snaps to the hex's position and updates the `Hex.Unit` reference.
    *   Updated `Hex.cs` to include a `Unit` property for bidirectional reference.
*   **Custom Editor:**
    *   Created `HexEditor.cs` (Custom Inspector) to display Hex coordinates (`Q`, `R`, `S`) as Read-Only fields, preventing accidental modification while allowing editing of `Elevation` and `TerrainType`.
*   **Test Improvements:**
    *   Resolved PlayMode test discovery issues by identifying that `[UnityTest]` in Editor assemblies runs as EditMode tests.
    *   Added `UnitPlacementTests.cs` to verify unit occupancy and positioning logic.
    *   Achieved 100% pass rate (21/21 tests) across all modules.

### Bugs encountered and fixed

*   **Bug:** PlayMode tests failing to execute (0 results).
    *   **Human Effort:** (Expertise: Medium, Time: 15 minutes) Investigated Assembly Definitions and Test Runner behavior. Identified that tests in `Assets/Tests/PlayMode` were configured with `includePlatforms: ["Editor"]` and `[UnityTest]`, effectively making them EditMode tests running in the Editor loop.
    *   **Solution:** Confirmed tests were valid and passing as EditMode tests. Proceeded with this understanding as they correctly validated the logic without requiring a full Player build.
*   **Bug:** `UnitTests` not being discovered by the test runner.
    *   **Human Effort:** (Expertise: Low, Time: 5 minutes) Suspected caching or class attribute issues.
    *   **Solution:** Renamed class to `UnitPlacementTests` and added `[TestFixture]` attribute to ensure proper discovery by the Unity Test Framework.
*   **Bug:** Test output was excessively verbose, consuming context.
    *   **Human Effort:** (Expertise: Low, Time: 5 minutes) Identified numerous `Debug.Log` calls within `HexGridManager.GenerateGrid`.
    *   **Solution:** Removed unnecessary `Debug.Log` statements from `HexGridManager.cs` to streamline test output.
*   **Bug:** PlayMode tests (e.g., `HexGridVisualTests.cs`, `HighlightingTests.cs`) were running as EditMode tests and were located in the `PlayMode` directory.
    *   **Human Effort:** (Expertise: Low, Time: 10 minutes) Identified a mismatch between test location/configuration and intended execution mode.
    *   **Solution:** Consolidated all existing tests into the `Assets/Tests/EditMode` directory and removed the `Assets/Tests/PlayMode` directory and its associated assembly definition. All tests are now treated as EditMode tests.

## Session 6

**Start Time:** 2025-12-17 12:00
**End Time:** 2025-12-17 13:45
**Token Usage:** [TBD]

### Features Implemented

*   **Hexagonal Rim Effect (Shader Graph):**
    *   Implemented a custom URP Shader (`HexSurfaceShader`) to render a dynamic rim around the top face of each hex.
    *   **Technique:** Shifted from Fresnel (unsuitable for flat surfaces) to a **UV-Based Distance** mask. The shader calculates the distance from the center UV `(1,0)` to the edge `(0,0)` and uses a `Step` function to create a hard-edged mask.
    *   **Visuals:** The rim blends between a `BaseColor` and a `RimColor` (using `Lerp`) instead of additive Emission, ensuring distinct visibility.
    *   **Pulsation:** Added a time-based pulsation effect (`Sine` wave driven by `_RimPulsation`) to the rim color for dynamic feedback.
*   **Custom UV Generation:**
    *   Modified `HexGridManager.CreateHexMesh` to generate specific UV coordinates for the top face: Center vertex at `(1, 0)`, outer ring vertices at `(0, 0)`. This "bakes" the distance-from-center data directly into the mesh, ensuring the rim is always perfectly hexagonal and rotation-independent.
*   **Architectural Refactoring:**
    *   **`HexGridManager` (Visual Authority):** Centralized all visual configuration for rims. Introduced `RimSettings` struct (Color, Width, Pulsation) and exposed `Default`, `Highlight`, and `Selection` settings in the Inspector. Added public API: `HighlightHex()`, `SelectHex()`, `ResetHex()`.
    *   **`SelectionManager` (Input Controller):** Stripped visual logic. Now acts as a pure controller, detecting input and calling the appropriate methods on `HexGridManager`.
*   **Testing:**
    *   Refactored `HighlightingTests.cs` to test the new `HexGridManager` API and verify `MaterialPropertyBlock` settings (`_RimColor`, `_RimWidth`, `_RimPulsation`) instead of the deprecated `_BaseColor`.

### Bugs encountered and fixed

*   **Bug:** "Inverted Hull" and Fresnel approaches failed to produce a clean hexagonal outline on flat geometry.
    *   **Human Effort:** (Expertise: High, Time: 30 minutes) Diagnosed that Fresnel is view-dependent and uniform on flat faces.
    *   **Solution:** Switched to a UV-based approach. Implemented custom UV generation in C# to map "center-to-edge" gradients, allowing the shader to render a mathematically perfect hexagon rim.
*   **Bug:** Tooling connectivity issues prevented final automated test run.
    *   **Solution:** Verified code logic via file inspection and "mental compilation." Manual verification in Unity Editor is recommended.
*   **Bug:** `Highlight_OnSelectedHex_Ignored` test failed: `HighlightHex` was overriding selection visuals.
    *   **Human Effort:** (Expertise: Medium, Time: 20 minutes) Diagnosed that `HexGridManager` needed to track `currentSelectedHex` to enforce visual priority.
    *   **Solution:** Modified `HexGridManager.cs`:
        *   Added `private Hex currentSelectedHex;` field.
        *   `GenerateGrid()` now resets `currentSelectedHex` to `null`.
        *   `HighlightHex()` includes a guard clause: `if (hex == currentSelectedHex) return;`.
        *   `SelectHex()` now properly resets any `currentSelectedHex` before setting a new one.
        *   `ResetHex()` now correctly handles clearing `currentSelectedHex` if the hex being reset was the selected one.
*   **Bug:** `Hex_Visuals_Update_On_Terrain_Change` test failed, showing `RGBA(0.000, 0.000, 0.000, 0.000)` instead of the expected terrain color.
    *   **Human Effort:** (Expertise: Low, Time: 10 minutes) Diagnosed a timing issue where `MaterialPropertyBlock` changes weren't fully applied before the test read them.
    *   **Solution:** Added `yield return null;` after `testHex.TerrainType = TerrainType.Water;` in `HexComponentTests.cs` to allow Unity a frame to process visual updates. Also streamlined `HexGridManager.SetHexColor` to directly set `_BaseColor`.
*   **Added Tests:**
    *   `HighlightingTests.SwitchHighlight_ResetsOldHex`
    *   `HighlightingTests.SwitchSelection_ResetsOldHex`
    *   `HighlightingTests.Highlight_OnSelectedHex_Ignored`
    *   `HexGridManager_InitializesHexBaseColorCorrectly` (HexComponentTests)

*   **Fixed:** `GenerateGrid` logic updated to explicitly set `_BaseColor` for initial terrain, ensuring correct visuals on start.
*   **Fixed:** `SelectionManager` explicitly initializes `selectedHex` to `null` in `Start()` to prevent accidental selection visuals when entering Play Mode.
*   **Bug:** All hex tiles turned red when entering Play Mode, regardless of terrain type or selection state.
    *   **Human Effort:** (Expertise: Low, Time: 5 minutes) Identified that setting the `_RimWidth` property directly on the `HexSurfaceMaterial` asset to 0 resolved the issue.
    *   **Solution:** Set the default `_RimWidth` property in the `HexSurfaceMaterial.mat` asset to `0`. This ensures that hexes display correctly (no red rim) even if `MaterialPropertyBlock` updates are delayed or overridden during Play Mode initialization.

**End Time:** 2025-12-18 23:45
**Token Usage:** [TBD]

### Features Implemented

*   **Hex Selection with Mouse Cursor:** Implemented raycasting with layer masks to correctly detect and select hexes in the grid.
*   **Data-Driven Map Architecture (Refactor):**
    *   **Architecture Shift:** Migrated from a GameObject-centric model to a data-driven architecture. `HexGrid` now manages pure C# `HexData` objects (Logic Layer), while `Hex` MonoBehaviours (View Layer) act as visual puppets.
    *   **`HexData.cs`:** Created to hold logic state (Coordinates, Elevation, TerrainType, Unit reference).
    *   **`Hex.cs` Refactor:** Transformed into a View component. Properties now proxy to `HexData`. Added serialized backing fields (`viewQ`, `viewR`, etc.) to provide "memory" for domain reloads.
    *   **`HexGridManager.cs` Update:** `GenerateGrid` now creates Data first, then View. Implemented `RebuildGridFromChildren` to reconstruct logic from serialized view state. Added `GetHexView(HexData)` to bridge logic back to visuals for Editor tools.
    *   **Decoupling:** Logic, pathfinding, and unit occupancy can now theoretically run without Unity GameObjects.
*   **Live Visual Updates & Selection Persistence:**
    *   Implemented `OnValidate` hook and a global `RefreshVisuals()` method in `HexGridManager` to ensure Inspector changes to `RimSettings` are reflected across the entire grid instantly.
    *   Refactored `ResetHex`, `SelectHex`, and introduced `DeselectHex` to create a clear and robust state machine for selection, deselection, and highlighting, preventing visual state corruption.
*   **Editor Tools Update:** Refactored `HexGridEditor.cs` to perform logic calculations on `HexData` while delegating visual updates to the `Hex` view.
*   **Unit Logic Restoration:** Restored bidirectional `Unit <-> Hex` reference, ensuring Units interact correctly with the new `HexData` layer.

### Bugs encountered and fixed

*   **Bug:** Mouse cursor not triggering selection.
    *   **Solution:** Added a Layer Mask to the raycast and assigned a specific layer to Hex prefabs.
*   **Bug:** Compilation errors during refactor (`CS1061`, `CS0029`).
    *   **Solution:** Updated `Hex.cs` to expose necessary properties via `HexData`. Added `GetHexView` helper to resolve type mismatches in Editor scripts.
*   **Bug:** "Material Instantiation" errors in tests.
    *   **Solution:** Reverted to `MaterialPropertyBlock` for all runtime/editor visual updates.
*   **Bug:** Grid resetting to flat `Hex(0,0)` instances after Play/Stop/Reload.
    *   **Cause:** `HexData` was lost on reload; `Hex` view had no serialized state to rebuild it from.
    *   **Solution:** Restored serialized backing fields to `Hex.cs` and updated `AssignData` to sync them.
*   **Bug:** A selected hex would lose its "selected" visual when the mouse cursor moved off it.
    *   **Cause:** `ResetHex` was incorrectly clearing the logical selection state.
    *   **Solution:** Refactored `ResetHex` to only manage visual states. A dedicated `DeselectHex` method was introduced for deselection logic, which resolved the issue and a subsequent test regression.
*   **Bug:** Default rim settings update was not propagating to neutral hexes.
    *   **Solution:** Modified `RefreshVisuals` to loop through all `Hex` components, ensuring global updates.
*   **Bug:** `UnitTests` and `HighlightingTests` failures due to outdated logic.
    *   **Solution:** Updated all relevant test fixtures to use the new `HexData` architecture and `DeselectHex` method correctly.

**Outcome:**
All 32 tests (EditMode) passed. The project's core grid, interaction, and visual feedback systems are now robust, data-driven, and fully unit-tested. The architecture is resilient to domain reloads and supports live editing of visual styles.
**Important:** You must click "Generate Grid" one last time in the Editor to populate the new serialized fields on existing GameObjects.

