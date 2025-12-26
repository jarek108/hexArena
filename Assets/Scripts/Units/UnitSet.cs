using UnityEngine;
using System.Collections.Generic;

namespace HexGame.Units
{
    [CreateAssetMenu(fileName = "NewUnitSet", menuName = "HexGame/Unit Set")]
    public class UnitSet : ScriptableObject
    {
        public string setName = "NewSet";
        public UnitSchema schema;

        public List<UnitType> units = new List<UnitType>();
    }
}
