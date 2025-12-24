using UnityEditor;
using UnityEngine;

namespace HexGame.Units.Editor
{
    [CustomEditor(typeof(UnitSchema))]
            public class UnitSchemaEditor : UnityEditor.Editor
        {
            private Vector2 scrollPos;
    
            public override void OnInspectorGUI()        {
            UnitSchema schema = (UnitSchema)target;
            serializedObject.Update();

            UnitEditorUI.DrawSchemaEditor(schema, ref scrollPos);

            serializedObject.ApplyModifiedProperties();
        }
    }
}