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
                
            if (unitLogic != null)
            {
                ApplyVisualIdentity(unitLogic.unitName);
            }
        }

        public override void SetPreviewIdentity(string unitName)
        {
            if (meshRenderer == null) 
                meshRenderer = GetComponentInChildren<MeshRenderer>();
            
            ApplyVisualIdentity(unitName);
        }

        private void ApplyVisualIdentity(string name)
        {
            if (meshRenderer == null) return;

            // Simple deterministic hash-to-hue
            int hash = name.GetHashCode();
            float hue = Mathf.Abs(hash % 1000) / 1000f;
            Color color = Color.HSVToRGB(hue, 0.7f, 0.8f);

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            meshRenderer.SetPropertyBlock(mpb);
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
