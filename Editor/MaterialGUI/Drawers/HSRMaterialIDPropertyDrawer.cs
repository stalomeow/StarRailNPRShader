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
    internal class HSRMaterialIDPropertyDrawer : MaterialPropertyDrawer
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
