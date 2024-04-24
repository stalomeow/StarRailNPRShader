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
    internal class HSRMaterialIDSelectorDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
#if UNITY_2022_1_OR_NEWER
            MaterialEditor.BeginProperty(position, prop);
#endif

            using (new MemberValueScope<bool>(() => EditorGUI.showMixedValue, prop.hasMixedValue))
            {
                var materialId = (HSRMaterialIDEnum)prop.floatValue;

                EditorGUI.BeginChangeCheck();
                materialId = (HSRMaterialIDEnum)EditorGUI.EnumPopup(position, label, materialId);

                if (EditorGUI.EndChangeCheck())
                {
                    prop.floatValue = (float)materialId;
                    SetKeyword(prop, materialId);
                }
            }

#if UNITY_2022_1_OR_NEWER
            MaterialEditor.EndProperty();
#endif
        }

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);
            SetKeyword(prop, (HSRMaterialIDEnum)prop.floatValue);
        }

        private static void SetKeyword(MaterialProperty prop, HSRMaterialIDEnum materialId)
        {
            const string KeywordName = "_SINGLEMATERIAL_ON";

            foreach (var obj in prop.targets)
            {
                Material material = (Material)obj;

                if (materialId != HSRMaterialIDEnum.All)
                {
                    material.EnableKeyword(KeywordName);
                }
                else
                {
                    material.DisableKeyword(KeywordName);
                }
            }
        }
    }
}
