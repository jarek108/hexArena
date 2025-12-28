# Tools System Architecture

## Overview
The Tools System follows a decoupled, modular design using **Active Tools** (ongoing state) and **Toggle Tools** (one-shot actions). A central manager handles tool lifecycle, input delegation, and switching logic.

## Core Principles
1.  **Polymorphic Interfaces:** Tools implement `IActiveTool` (cursor-based) or `IToggleTool` (trigger-based).
2.  **Toggle Base Class:** All toggles inherit from `ToggleTool`, providing a shared `isActive` flag for the Inspector and UI.
3.  **Real-Time Interaction**: Tools like Pathfinding support "Continuous" modes for immediate visual feedback on hover.

## Architecture Structure

### 1. Tool Class Hierarchy

*   **`ITool` (Base Interface)**: `CheckRequirements`, `OnActivate`, `OnDeactivate`, `HandleInput`.
*   **`IActiveTool` (Ongoing)**: Represents a persistent mode (e.g., painting). Includes `HandleHighlighting`.
*   **`ToggleTool` (Abstract Base)**: 
    *   Implements `IToggleTool`.
    *   Holds `public bool isActive`.
    *   Automates state flipping and provides the `OnToggle(bool newState)` hook.

### 2. ToolManager (MonoBehaviour)
*   **Input Loop:** Runs `ManualUpdate(Hex hoveredHex)` to feed data to the active tool.
*   **Highlighting Logic**: Detects hover changes and notifies the active tool to refresh its preview visuals.

### 3. Concrete Tools

#### Interaction
*   **`PathfindingTool` (IActiveTool)**: 
    *   **Selection Visuals**: Immediately triggers `OnUnitSelected` and calculates path visuals (Ghost + Area of Attack) when a unit is first clicked.
    *   **Continuous Mode**: Calculates paths in real-time on hover if a source is selected. Supports snapping visuals back to the unit's feet when hovering over the `SourceHex`.
    *   **Locking**: Right-Click locks a target, ignoring subsequent hovers until cleared.
    *   **Ruleset Integrated**: 
        *   Notifies the Ruleset of pathfinding lifecycle events (`OnStartPathfinding`, `OnFinishPathfinding`).
        *   Supports **Path Truncation**: Visualizes only the reachable portion of a path if targeting an enemy (handled by Ruleset).
        *   **Ghosting**: Triggers the Ruleset to show a movement ghost at the destination.
        *   **Delegated Execution**: Calls `ruleset.ExecutePath` to handle the final movement and any follow-up actions (like attacking).

#### Grid Editing
*   **`BrushTool` (Base)**: Supports scroll-wheel resizing and `maxBrushSize`.
*   **`TerrainTool`**: Paints terrain types.
*   **`ElevationTool`**: Modifies height.
*   **`UnitPlacementTool`**: Places/Removes units.

#### Utilities
*   **`GridTool` (ToggleTool)**: Toggles the hex overlay.
*   **`ZoCTool` (ToggleTool)**: Toggles the visibility of Zone of Control states.
*   **`AoATool` (ToggleTool)**: Toggles the visibility of Area of Attack states by flipping priority signs in the visualizer.

## UI Integration
*   **`IconManager`:**
    *   **Dynamic Population**: Generates toolbar icons from the specified `iconFolder`.
    *   **State Highlighting**: Visually highlights selected Active Tools and "ON" Toggle Tools.
    *   **Bold Hotkeys**: Displays shortcut keys in bold text for clarity.