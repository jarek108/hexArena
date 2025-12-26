# Tools System Architecture

## Overview
The Tools System follows a decoupled, modular, and polymorphic design. It distinguishes between **Active Tools** (ongoing state, e.g., painting terrain) and **Toggle Tools** (one-shot actions, e.g., toggling grid visibility). A central manager handles tool lifecycle, input delegation, and switching logic.

## Core Principles
1.  **Polymorphic Interfaces:** Tools implement specific interfaces (`IActiveTool` vs `IToggleTool`) to define their behavior and lifecycle requirements.
2.  **Centralized Management:** `ToolManager` is the single source of truth for the current interaction mode.
3.  **State-Driven Interaction:** Tools modify the logical state of `HexData` (or other systems), relying on the View layer to react to these changes.

## Architecture Structure

### 1. Interfaces

*   **`ITool` (Base Interface)**
    *   `CheckRequirements(out string reason)`: specific conditions for activation.
    *   `OnActivate()`: Setup logic.
    *   `OnDeactivate()`: Cleanup logic.
    *   `HandleInput(Hex hoveredHex)`: Receives input frames from the manager.

*   **`IActiveTool : ITool` (Ongoing Interaction)**
    *   Represents a tool that stays active until manually switched (e.g., Pathfinding, Terrain Painter).
    *   `IsEnabled { get; set; }`: State flag.
    *   `HandleHighlighting(Hex oldHex, Hex newHex)`: Manages preview visuals (e.g., brush outlines) as the mouse moves.

*   **`IToggleTool : ITool` (One-Shot Action)**
    *   Marker interface for tools that execute immediate logic in `OnActivate` and do not retain control.
    *   **Behavior:** Activation triggers `OnActivate()` and immediately returns control to the *previous* Active Tool.

### 2. ToolManager (MonoBehaviour)
The central hub for interaction.
*   **Responsibilities:**
    *   Maintains a registry of available tools (via `GetComponents<ITool>`).
    *   Manages the `ActiveTool` (currently selected `IActiveTool`).
    *   **Input Loop:** Runs a `ManualUpdate(Hex hoveredHex)` loop (driven by `HexRaycaster` or Unity Input System) to feed data to the active tool.
    *   **Switching Logic:** Handles the distinction between swapping Active Tools (old deactivates -> new activates) and firing Toggle Tools (interrupts -> fires -> resumes old).

### 3. Concrete Tools

#### Interaction & Gameplay
*   **`PathfindingTool` (IActiveTool)**: The default "cursor" tool. Handles selecting Source/Target hexes and visualizing paths using the A* algorithm.

#### Grid Editing (IActiveTool)
*   **`TerrainTool`**: Paints terrain types (Dirt, Grass, Water, etc.) onto hexes.
*   **`ElevationTool`**: Raises or lowers hex elevation.
*   **`UnitPlacementTool`**: Places or removes units on the grid.
*   **`BrushTool`**: Base class or utility for tools requiring variable brush sizes.

#### Utilities (IToggleTool)
*   **`HexGridTool`**: Toggles the visibility of the grid overlay.

## UI Integration
*   **`IconManager`:**
    *   Visual representation of the toolbar.
    *   Maps UI buttons to `ToolManager.SelectTool("ToolName")`.
    *   Listens for hotkeys to trigger tool switching.
