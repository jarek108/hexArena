using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Units
{
    [System.Serializable]
    public class UnitStatDefinition
    {
        public string id = "ID";
        public string name = "Description";
    }

    [CreateAssetMenu(fileName = "NewUnitSchema", menuName = "HexGame/Unit Schema")]
    public class UnitSchema : ScriptableObject
    {
        public string id = "NewSchema";
        public List<UnitStatDefinition> definitions = new List<UnitStatDefinition> 
        { 
            new UnitStatDefinition { id = "HP", name = "Hit Points" },
            new UnitStatDefinition { id = "Move", name = "Movement Range" },
            new UnitStatDefinition { id = "Dmg", name = "Damage" },
            new UnitStatDefinition { id = "Acc", name = "Accuracy" },
            new UnitStatDefinition { id = "Arm", name = "Armour" }
        };

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public void FromJson(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
    }
}