using UnityEngine;

namespace HexGame
{
    public abstract class Ruleset : ScriptableObject
    {
        public abstract float GetMoveCost(Unit unit, HexData fromHex, HexData toHex);
        public abstract void OnEntry(Unit unit, HexData hex);
        public abstract void OnDeparture(Unit unit, HexData hex);
    }
}
