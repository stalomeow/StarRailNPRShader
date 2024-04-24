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

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HSR.NPRShader.Editor.Automation
{
    public static class MaterialUtility
    {
        public static void SetShaderAndResetProperties(Material material, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            material.shader = shader;

            SerializedObject matObj = new(material);

            try
            {
                CleanProperties(shader, matObj.FindProperty("m_SavedProperties.m_TexEnvs"));
                CleanProperties(shader, matObj.FindProperty("m_SavedProperties.m_Floats"));
                CleanProperties(shader, matObj.FindProperty("m_SavedProperties.m_Ints"));
                CleanProperties(shader, matObj.FindProperty("m_SavedProperties.m_Colors"));
            }
            finally
            {
                matObj.ApplyModifiedProperties();
            }
        }

        private static void CleanProperties(Shader shader, SerializedProperty propArray)
        {
            if (!propArray.isArray)
            {
                return;
            }

            for (int i = propArray.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty prop = propArray.GetArrayElementAtIndex(i);
                string propName = prop.FindPropertyRelative("first").stringValue;
                int propIndex = shader.FindPropertyIndex(propName);

                if (propIndex >= 0)
                {
                    SerializedProperty propValue = prop.FindPropertyRelative("second");

                    // 重置为默认值
                    switch (propValue.propertyType)
                    {
                        case SerializedPropertyType.Float:
                            propValue.floatValue = shader.GetPropertyDefaultFloatValue(propIndex);
                            break;

                        case SerializedPropertyType.Integer:
                            propValue.intValue = shader.GetPropertyDefaultIntValue(propIndex);
                            break;

                        case SerializedPropertyType.Color:
                            propValue.colorValue = shader.GetPropertyDefaultVectorValue(propIndex);
                            break;

                        // Texture
                        case SerializedPropertyType.Generic:
                        {
                            SerializedProperty texture = propValue.FindPropertyRelative("m_Texture");
                            SerializedProperty scale = propValue.FindPropertyRelative("m_Scale");
                            SerializedProperty offset = propValue.FindPropertyRelative("m_Offset");

                            texture.objectReferenceValue = null;
                            scale.vector2Value = Vector2.one;
                            offset.vector2Value = Vector2.zero;
                            break;
                        }

                        default:
                            throw new NotSupportedException($"Material property type {propValue.propertyType} is not supported.");
                    }
                }
                else
                {
                    // 删除多余的 property
                    propArray.DeleteArrayElementAtIndex(i);
                }
            }
        }

        [MenuItem("Assets/Import as StarRail Material Json")]
        private static void ImportAsHSRMaterialJson()
        {
            foreach (Object obj in Selection.objects)
            {
                if (!EditorUtility.IsPersistent(obj) || obj is not TextAsset)
                {
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(obj);
                AssetDatabase.SetImporterOverride<MaterialJsonImporter>(path);
            }
        }

        [MenuItem("Assets/Import as StarRail Material Json", isValidateFunction: true)]
        private static bool ImportAsHSRMaterialJsonValidate()
        {
            return Selection.objects.Any(obj => EditorUtility.IsPersistent(obj) && obj is TextAsset);
        }
    }
}
