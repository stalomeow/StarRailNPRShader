using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace HSR.Editor.MaterialDrawers
{
    [PublicAPI]
    public class HSRMaterialIDPropertyDrawer : MaterialPropertyDrawer
    {
        private readonly string m_FoldoutName;
        private readonly int m_MaterialID;

        public HSRMaterialIDPropertyDrawer(string foldoutName, float materialID)
        {
            m_FoldoutName = foldoutName;
            m_MaterialID = (int)materialID;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (GetFoldoutState(editor))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            return 0;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (!GetFoldoutState(editor))
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                editor.DefaultShaderProperty(position, prop, $"Material ID = {m_MaterialID}");
            }
        }

        public bool GetFoldoutState(MaterialEditor editor)
        {
            MaterialProperty property = MaterialEditor.GetMaterialProperty(editor.targets, m_FoldoutName);
            return property.type switch
            {
                MaterialProperty.PropType.Float => (int)property.floatValue == 1,
                MaterialProperty.PropType.Range => (int)property.floatValue == 1,
                MaterialProperty.PropType.Int => property.intValue == 1,
                _ => false
            };
        }
    }
}
