import os
import re

hex_vars = ["hex", "hex1", "hex2", "hex3", "startHex", "targetHex", "enemyHex", "SourceHex", "TargetHex", "hoveredHex", "oldHex", "newHex", "target", "hexEnd", "h", "n"]

def fix_file(file_path):
    try:
        with open(file_path, "r") as f:
            content = f.read()
        
        orig = content
        for v in hex_vars:
            # Replace v.Unit with v.Data.Unit
            content = re.sub(rf"\b{v}\.Unit\b", f"{v}.Data.Unit", content)
        
        if content != orig:
            with open(file_path, "w") as f:
                f.write(content)
            print(f"Fixed {file_path}")
    except Exception as e:
        print(f"Error fixing {file_path}: {e}")

for root, dirs, files in os.walk("Assets"):
    for file in files:
        if file.endswith(".cs"):
            fix_file(os.path.join(root, file))
