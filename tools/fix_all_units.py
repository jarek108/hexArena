import os
import re

files_to_check = [
    "Assets/Scripts/Core/BattleBrothersRuleset.cs",
    "Assets/Scripts/Tools/PathfindingTool.cs",
    "Assets/Scripts/Units/UnitManager.cs",
    "Assets/Scripts/Grid/Hex.cs"
]

hex_vars = ["targetHex", "SourceHex", "TargetHex", "hoveredHex", "hex", "oldHex", "newHex", "target"]

for file_path in files_to_check:
    if not os.path.exists(file_path): continue
    with open(file_path, "r") as f:
        lines = f.readlines()
    
    new_lines = []
    for line in lines:
        for v in hex_vars:
            # Match v.Unit but not v.Units or v.UnitType
            line = re.sub(rf"\b{v}\.Unit\b", f"{v}.Data.Unit", line)
        new_lines.append(line)
    
    with open(file_path, "w") as f:
        f.writelines(new_lines)
