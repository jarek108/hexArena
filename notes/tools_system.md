# Tools System Architecture

## Overview
The Tools System follows a decoupled, modular design, where each "tool" is a self-contained unit of logic that can be activated and used by a central manager. This allows for easy addition of new tools and clear separation of concerns.

## Core Principles
1.  **Unified Interface:** All tools will implement a common `ITool` interface, ensuring they can be managed polymorphically.
2.  **Tool Manager:** A central `ToolManager` will handle tool activation, deactivation, and input delegation.
3.  **State-Driven Interaction:** Tools interact with the game world by modifying the logical state of objects (e.g., `HexData`), not by directly manipulating visuals.

## Proposed Structure

*   **`ITool` (Interface):**
    *   `OnActivate()`
    *   `OnDeactivate()`
    *   `HandleInput(Hex hoveredHex)`: Handles input logic, receiving the currently hovered hex from the `SelectionManager`.
    *   `ToolName`: Name of the tool.

*   **`ToolManager` (MonoBehaviour):**
    *   Holds a list of available tools.
    *   Manages the currently active tool.
    *   Routes player input from `SelectionManager` to the active tool.

*   **`SelectionManager` (MonoBehaviour):**
    *   Input controller.
    *   Uses `HexRaycaster` to find the hovered hex.
    *   Delegates input to `ActiveTool.HandleInput(hoveredHex)`.

*   **Concrete Tools (Implementations of `ITool`):**
    *   **`SelectionTool`**: Manages highlighting and selecting hexes.
    *   **`TerrainTool`**: Modifies `TerrainType` on `HexData`.
    *   **`ElevationTool`**: Modifies `Elevation` on `HexData`.

## UI & Input Integration
*   **`IconManager` (View):**
    *   Manages the toolbar visuals and hotkey detection.
    *   **Architecture:** Uses a View-Logic separation where the `IconManager` exposes `UnityEvent` hooks via `IconData`.
    *   **Interaction:** 
        *   **Click:** Triggers the assigned `UnityEvent`.
        *   **Hotkey:** Checks for key press in `Update` and triggers the same `UnityEvent`.
    *   **Wiring:** Developers assign `ToolManager.SelectTool("ToolID")` (or similar methods) to these events in the Unity Inspector.