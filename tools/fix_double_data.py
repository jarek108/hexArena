import os

files_to_fix = [
    "Assets/Scripts/Core/BattleBrothersRuleset.cs",
    "Assets/Scripts/Units/UnitManager.cs",
    "Assets/Scripts/Tools/PathfindingTool.cs"
]

for file_path in files_to_fix:
    if not os.path.exists(file_path): continue
    with open(file_path, "r") as f:
        content = f.read()
    
    # Fix the double-Data bug for HexData variables
    content = content.replace("n.Data.Unit", "n.Unit")
    content = content.replace("h.Data.Unit", "h.Unit")
    content = content.replace("hexData.Data.Unit", "hexData.Unit")
    content = content.replace("target.Data.Data.Unit", "target.Data.Unit") # target.Data is already HexData

    with open(file_path, "w") as f:
        f.write(content)
