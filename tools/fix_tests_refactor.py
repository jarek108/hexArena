import os
import re

test_dir = 'Assets/Tests/EditMode'
replacements = {
    r'ruleset\s*\.\s*plainsCost': 'ruleset.movement.plainsCost',
    r'ruleset\s*\.\s*zocPenalty': 'ruleset.movement.zocPenalty',
    r'ruleset\s*\.\s*uphillPenalty': 'ruleset.movement.uphillPenalty',
    r'ruleset\s*\.\s*maxElevationDelta': 'ruleset.movement.maxElevationDelta',
    r'ruleset\s*\.\s*meleeHighGroundBonus': 'ruleset.combat.meleeHighGroundBonus',
    r'ruleset\s*\.\s*meleeLowGroundPenalty': 'ruleset.combat.meleeLowGroundPenalty',
    r'ruleset\s*\.\s*surroundBonus': 'ruleset.combat.surroundBonus',
    r'ruleset\s*\.\s*longWeaponProximityPenalty': 'ruleset.combat.longWeaponProximityPenalty',
    r'ruleset\s*\.\s*rangedHighGroundBonus': 'ruleset.combat.rangedHighGroundBonus',
    r'ruleset\s*\.\s*rangedLowGroundPenalty': 'ruleset.combat.rangedLowGroundPenalty',
    r'ruleset\s*\.\s*rangedDistancePenalty': 'ruleset.combat.rangedDistancePenalty',
    r'ruleset\s*\.\s*coverMissChance': 'ruleset.combat.coverMissChance',
    r'ruleset\s*\.\s*scatterHitPenalty': 'ruleset.combat.scatterHitPenalty',
    r'ruleset\s*\.\s*scatterDamagePenalty': 'ruleset.combat.scatterDamagePenalty',
}

for filename in os.listdir(test_dir):
    if filename.endswith('.cs'):
        path = os.path.join(test_dir, filename)
        content = None
        for encoding in ['utf-8-sig', 'utf-8', 'latin-1']:
            try:
                with open(path, 'r', encoding=encoding) as f:
                    content = f.read()
                break
            except UnicodeDecodeError:
                continue
        
        if content is None:
            continue
            
        original = content
        for pattern, replacement in replacements.items():
            content = re.sub(pattern, replacement, content)
        
        if content != original:
            with open(path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Updated {filename}")
