using UnityEngine;
using System.Collections.Generic;

namespace HexGame.Units
{
    public abstract class UnitVisualization : MonoBehaviour
    {
        [SerializeField] public float yOffset = 0f;

        /// <summary>
        /// Called when the visualization is linked to a unit.
        /// </summary>
        /// <param name="unitLogic">The logical unit driving this visualization.</param>
        public virtual void Initialize(Unit unitLogic) { }

        // Core visual events - abstract to force implementation
        public abstract void OnStartMoving(List<Vector3> path, float speed);
        public abstract void OnAttack(Unit target);
        public abstract void OnTakeDamage(int amount);
        public abstract void OnDie();
    }
}
