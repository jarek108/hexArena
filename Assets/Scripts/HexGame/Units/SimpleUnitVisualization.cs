using UnityEngine;
using System.Collections.Generic;

namespace HexGame.Units
{
    public class SimpleUnitVisualization : UnitVisualization
    {
        [SerializeField] public MeshRenderer meshRenderer;
        
        public override void Initialize(Unit unitLogic)
        {
            if (meshRenderer == null) 
                meshRenderer = GetComponentInChildren<MeshRenderer>();
                
            // Example: Set color based on some logic if needed
            // if (meshRenderer) meshRenderer.material.color = Color.blue;
        }

        public override void OnStartMoving(List<Vector3> path, float speed)
        {
            // For now, just snap or log
            Debug.Log($"{name} started moving along path of {path.Count} steps.");
        }

        public override void OnAttack(Unit target)
        {
            Debug.Log($"{name} attacks {target.name}!");
            // Trigger animation or particle here
        }

        public override void OnTakeDamage(int amount)
        {
            Debug.Log($"{name} took {amount} damage!");
            // Flash red, etc.
        }

        public override void OnDie()
        {
            Debug.Log($"{name} died.");
            // Play death anim, then destroy object
        }
    }
}
