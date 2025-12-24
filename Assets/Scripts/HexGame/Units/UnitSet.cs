using UnityEngine;
using System.Collections.Generic;

namespace HexGame.Units
{
    [CreateAssetMenu(fileName = "NewUnitSet", menuName = "HexGame/Unit Set")]
    public class UnitSet : ScriptableObject
    {
        public string setName = "NewSet";
        public UnitSchema schema;
        
        [HideInInspector]
        public string cachedSchemaHash;

        public List<UnitType> units = new List<UnitType>();

        /// <summary>
        /// Validates if the current schema matches the cached hash.
        /// </summary>
        public bool IsHashValid()
        {
            if (schema == null) return false;
            return schema.GetHash() == cachedSchemaHash;
        }

        /// <summary>
        /// Updates the hash to match the current schema.
        /// Should be called after successful migration or save.
        /// </summary>
        public void UpdateHash()
        {
            if (schema != null)
            {
                cachedSchemaHash = schema.GetHash();
            }
        }
    }
}
