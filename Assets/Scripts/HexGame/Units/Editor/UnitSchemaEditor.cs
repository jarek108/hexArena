using UnityEditor;
using UnityEngine;

namespace HexGame.Units.Editor
{
    [CustomEditor(typeof(UnitSchema))]
    public class UnitSchemaEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UnitSchema schema = (UnitSchema)target;
            serializedObject.Update();

            UnitEditorUI.DrawSchemaEditor(schema);

            serializedObject.ApplyModifiedProperties();
        }
    }
}