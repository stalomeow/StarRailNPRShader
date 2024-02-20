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
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace HSR.NPRShader.Editor.Build
{
    internal class BuiltinShaderProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            SerializedProperty shaderArray = GetAlwaysIncludedShaderArray();
            foreach (Shader shader in StarRailBuiltinShaders.Walk())
            {
                AddShaderIfNeeded(shaderArray, shader);
            }
            shaderArray.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            SerializedProperty shaderArray = GetAlwaysIncludedShaderArray();
            foreach (Shader shader in StarRailBuiltinShaders.Walk())
            {
                RemoveShaderIfNeeded(shaderArray, shader);
            }
            shaderArray.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static SerializedProperty GetAlwaysIncludedShaderArray()
        {
            SerializedObject settings = new(GraphicsSettings.GetGraphicsSettings());
            return settings.FindProperty("m_AlwaysIncludedShaders");
        }

        private static void AddShaderIfNeeded(SerializedProperty shaderArray, Shader shader)
        {
            for (int i = 0; i < shaderArray.arraySize; i++)
            {
                if (shaderArray.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    return;
                }
            }

            int newIndex = shaderArray.arraySize;
            shaderArray.InsertArrayElementAtIndex(newIndex);
            shaderArray.GetArrayElementAtIndex(newIndex).objectReferenceValue = shader;
        }

        private static void RemoveShaderIfNeeded(SerializedProperty shaderArray, Shader shader)
        {
            for (int i = shaderArray.arraySize - 1; i >= 0; i--)
            {
                if (shaderArray.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    shaderArray.DeleteArrayElementAtIndex(i);
                }
            }
        }
    }
}
