using UnityEngine;

namespace HexGame
{
    [ExecuteAlways]
    public class GameMaster : MonoBehaviour
    {
        public static GameMaster Instance { get; private set; }

        public Ruleset ruleset;

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }
    }
}
