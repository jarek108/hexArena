using UnityEngine;
using UnityEngine.UI;

namespace HexGame.UI
{
    [AddComponentMenu("UI/Effects/Gradient")]
    public class UIGradient : BaseMeshEffect
    {
        [SerializeField] private Color m_colorTop = Color.white;
        [SerializeField] private Color m_colorBottom = Color.black;

        public Color colorTop
        {
            get => m_colorTop;
            set { if (m_colorTop != value) { m_colorTop = value; if (graphic != null) graphic.SetVerticesDirty(); } }
        }

        public Color colorBottom
        {
            get => m_colorBottom;
            set { if (m_colorBottom != value) { m_colorBottom = value; if (graphic != null) graphic.SetVerticesDirty(); } }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            int count = vh.currentVertCount;
            if (count == 0) return;

            UIVertex vertex = new UIVertex();
            float top = float.MinValue;
            float bottom = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                top = Mathf.Max(top, vertex.position.y);
                bottom = Mathf.Min(bottom, vertex.position.y);
            }

            float height = top - bottom;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.color *= Color.Lerp(m_colorBottom, m_colorTop, (vertex.position.y - bottom) / height);
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
