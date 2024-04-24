/*
 * StarRailNPRShader - Fan-made shaders for Unity URP attempting to replicate
 * the shading of Honkai: Star Rail.
 * https://github.com/stalomeow/StarRailNPRShader
 *
 * Copyright (C) 2023 Stalo <stalowork@163.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using UnityEditor;
using UnityEngine;

namespace HSR.NPRShader.Editor.MaterialGUI.Drawers
{
    internal class HSRMaterialIDFoldoutDrawer : MaterialPropertyDrawer
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
