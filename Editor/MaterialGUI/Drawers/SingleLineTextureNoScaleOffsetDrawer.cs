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

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HSR.NPRShader.Editor.MaterialGUI.Drawers
{
    internal class SingleLineTextureNoScaleOffsetDrawer : MaterialPropertyDrawer
    {
        private static readonly MethodInfo s_ExtraPropertyAfterTextureMethod = typeof(MaterialEditor)
            .GetMethod("ExtraPropertyAfterTexture", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly string m_ColorPropName;

        public SingleLineTextureNoScaleOffsetDrawer() : this(null) { }

        public SingleLineTextureNoScaleOffsetDrawer(string colorPropName)
        {
            m_ColorPropName = colorPropName;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            // using (EditorGUIScopes.LabelWidth())
            {
                editor.TexturePropertyMiniThumbnail(position, prop, label.text, label.tooltip);

                if (string.IsNullOrEmpty(m_ColorPropName))
                {
                    return;
                }

                MaterialProperty colorProp = FindColorProperty(editor);
                Rect colorRect = MaterialEditor.GetRectAfterLabelWidth(position);
                s_ExtraPropertyAfterTextureMethod.Invoke(editor, new object[] { colorRect, colorProp, false });
            }
        }

        private MaterialProperty FindColorProperty(MaterialEditor editor)
        {
            return MaterialEditor.GetMaterialProperty(editor.targets, m_ColorPropName);
        }
    }
}
