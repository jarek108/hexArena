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
                ApplyVisualIdentity(unitLogic.UnitName);
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
        }

        public override void OnAttack(Unit target)
        {
            if (target == null) return;
            
            if (activeLunge != null) StopCoroutine(activeLunge);
            activeLunge = StartCoroutine(LungeCoroutine(target.transform.position));
        }

        private Coroutine activeLunge;
        private System.Collections.IEnumerator LungeCoroutine(Vector3 targetPos)
        {
            Vector3 startPos = transform.position;
            // Original logic position should be the center of the hex it's on.
            // Since transform might already be offset by yOffset or previous lunge, 
            // we should be careful. But unit logic usually snaps transform at end of move.
            
            float size = GridVisualizationManager.Instance != null ? GridVisualizationManager.Instance.HexSize : 1f;
            float lungeDist = size / 3f;
            
            Vector3 dir = (targetPos - startPos).normalized;
            dir.y = 0; // Keep horizontal
            
            Vector3 peakPos = startPos + dir * lungeDist;

            // 1. Lunge Out (Fast)
            float t = 0;
            float outDuration = 0.08f;
            while (t < 1f)
            {
                t += Time.deltaTime / outDuration;
                transform.position = Vector3.Lerp(startPos, peakPos, t);
                yield return null;
            }
            transform.position = peakPos;

            // 2. Return (Slightly slower)
            t = 0;
            float backDuration = 0.15f;
            while (t < 1f)
            {
                t += Time.deltaTime / backDuration;
                transform.position = Vector3.Lerp(peakPos, startPos, t);
                yield return null;
            }
            transform.position = startPos;
            
            activeLunge = null;
        }

        public override void OnTakeDamage(int amount)
        {
            // Flash red, etc.
        }

        public override void OnDie()
        {
            // Play death anim, then destroy object
        }
    }
}
