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
    internal class RampTextureDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
#if UNITY_2022_1_OR_NEWER
            MaterialEditor.BeginProperty(position, prop);
#endif

            using (new MemberValueScope<bool>(() => EditorGUI.showMixedValue, prop.hasMixedValue))
            using (new MemberValueScope<float>(() => EditorGUIUtility.labelWidth, 0))
            {
                position.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.BeginChangeCheck();
                Rect rampRect = EditorGUI.PrefixLabel(position, label);
                Texture2D ramp = (Texture2D)EditorGUI.ObjectField(rampRect, prop.textureValue, typeof(Texture2D), false);

                if (EditorGUI.EndChangeCheck())
                {
                    prop.textureValue = ramp;
                }

                if (!prop.hasMixedValue && ramp)
                {
                    Rect previewRect = new(rampRect.x + 1, rampRect.y + 1, rampRect.width - 20, rampRect.height - 2);
                    EditorGUI.DrawPreviewTexture(previewRect, ramp);
                }

                // NoScaleOffset
            }

#if UNITY_2022_1_OR_NEWER
            MaterialEditor.EndProperty();
#endif
        }
    }
}
