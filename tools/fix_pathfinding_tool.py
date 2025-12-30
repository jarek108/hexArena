import sys
import os

file_path = "Assets/Scripts/Tools/PathfindingTool.cs"
with open(file_path, "r") as f:
    content = f.read()

pats = ["SourceHex.Unit", "TargetHex.Unit", "hoveredHex.Unit", "hex.Unit", "oldHex.Unit", "newHex.Unit", "target.Unit"]
for p in pats:
    content = content.replace(p, p.replace(".Unit", ".Data.Unit"))

with open(file_path, "w") as f:
    f.write(content)
