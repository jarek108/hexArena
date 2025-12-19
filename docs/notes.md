

## Documentation workflow 
- While working on a specific system document the work in an appropriate md file (example grid.md)
- Operate in sessions and document them in the # Sessions section in the the current md file (e.g. ## Session 3 etc.)
- Whenever you are in doubt about the current .md document or session number, ask for confirmation 
- For each session section maintain subsections: ### Features Implemented, ### Bugs encountered and fixed
- In the in ### Bugs encountered and fixed section list all the mistakes/bug fixing iterations with 'Bug', 'Human effort to resolve'(add in a bracket add estimation of how much expertise and time was needed) and 'Solution' points
- Only update the bugs after their resolution is confirmed by the user
- Use grid.md as a reference for the above

## Development workflow (including TDD!)
- Build in vertical slices: grid → selection → movement → attack → win/lose.
- Each slice adds unit tests for rules (movement, range, hit/damage, victory).
- Separate scenes for Main Menu, Campaign, Tactical Battle.
- Apply SOLID; prefer composition over inheritance; feature‑based namespaces.

## Tool usage (including MCPs)
- Use git for version control; enforce folder and naming conventions.
- Use assistants for documentation lookup, boilerplate, refactors, and safe scripted operations (tests, format, scaffolding).
- Keep auto‑apply limited to non‑destructive actions; review gameplay changes manually.
- Prefer built‑in Unity systems and minimal external packages; isolate any added packages.

# Project: Hex Tactics Strategy Game (to be developed)
Hex grid based on axial coordinates and Unity’s hex tile support.


## Main game systems
- Hex grid: coordinates, neighbors, range, paths, LOS, elevation/cost.
- Units and actions: turn order, AP/move, attacks, statuses, ScriptableObject‑driven data.
- AI: simple goal/utility‑based positioning and target choice.
- Procedural battle maps: terrain rules, obstacles, deployment zones.
- UI: selection, overlays, combat log; decoupled from rules.
- Save/state: campaign, roster, inventory, settings.
