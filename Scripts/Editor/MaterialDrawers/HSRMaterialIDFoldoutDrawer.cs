using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Stalo.ShaderUtils.Editor;

namespace HSR.Editor.MaterialDrawers
{
    [PublicAPI]
    public class HSRMaterialIDFoldoutDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            using (new MemberValueScope<bool>(() => EditorGUI.showMixedValue, prop.hasMixedValue))
            {
                if (prop.type is MaterialProperty.PropType.Float or MaterialProperty.PropType.Range)
                {
                    EditorGUI.BeginChangeCheck();
                    bool foldout = EditorGUI.Foldout(position, (int)prop.floatValue == 1, label, true);

                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.floatValue = foldout ? 1 : 0;
                    }
                }
                else if (prop.type is MaterialProperty.PropType.Int)
                {
                    EditorGUI.BeginChangeCheck();
                    bool foldout = EditorGUI.Foldout(position, prop.intValue == 1, label, true);

                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.intValue = foldout ? 1 : 0;
                    }
                }
                else
                {
                    Debug.LogErrorFormat("The type of {0} should be Float/Range/Int.", prop.name);
                }

                // Rect fieldRect = MaterialEditor.GetRectAfterLabelWidth(position);
                // EditorGUI.LabelField(fieldRect, " [ LUT ]", EditorStyles.boldLabel);
            }
        }
    }
}
