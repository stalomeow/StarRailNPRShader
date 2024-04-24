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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HSR.NPRShader.Editor.Automation
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MaterialJsonImporter))]
    internal class MaterialJsonImporterEditor : ScriptedImporterEditor
    {
        private static readonly Lazy<GUIStyle> s_WrapMiniLabelStyle = new(() =>
        {
            return new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        });

        private SerializedProperty m_OverrideShaderName;

        public override void OnEnable()
        {
            base.OnEnable();

            m_OverrideShaderName = serializedObject.FindProperty("m_OverrideShaderName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            ShaderOverrideDropdown();

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("The results are for reference only. It is recommended that you further adjust the detailed properties yourself.", s_WrapMiniLabelStyle.Value);

                using (new EditorGUI.DisabledScope(hasUnsavedChanges))
                {
                    GenerateMaterialButton();
                    OverwriteMaterialButton();
                }
            }

            ApplyRevertGUI();
        }

        private void ShaderOverrideDropdown()
        {
            Rect rect = EditorGUILayout.GetControlRect(true);
            rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.TrTextContent("Shader Override"));

            if (EditorGUI.DropdownButton(rect, EditorGUIUtility.TrTextContent(m_OverrideShaderName.stringValue), FocusType.Passive))
            {
                GenericMenu menu = new();

                foreach (var shaderName in BaseMaterialSetter.AllShaderMap.Keys.OrderBy(x => x))
                {
                    bool isOn = shaderName == m_OverrideShaderName.stringValue;
                    menu.AddItem(new GUIContent(shaderName), isOn, n =>
                    {
                        m_OverrideShaderName.stringValue = (string)n;
                        serializedObject.ApplyModifiedProperties();
                    }, shaderName);
                }

                menu.DropDown(rect);
            }

            EditorGUILayout.HelpBox("If the Shader field below is missing, you can manually specify it here.", MessageType.Info);
        }

        private void GenerateMaterialButton()
        {
            if (!GUILayout.Button("Generate Material"))
            {
                return;
            }

            Dictionary<string, BaseMaterialSetter> shaderMap = BaseMaterialSetter.AllShaderMap;

            foreach (MaterialJsonData matInfo in assetTargets.Select(x => x as MaterialJsonData))
            {
                bool ok = false;

                if (shaderMap.TryGetValue(matInfo.Shader, out BaseMaterialSetter setter))
                {
                    if (setter.TryCreate(matInfo, out Material material))
                    {
                        string path = AssetDatabase.GetAssetPath(matInfo);
                        path = Path.ChangeExtension(path, ".mat");
                        path = AssetDatabase.GenerateUniqueAssetPath(path);

                        AssetDatabase.CreateAsset(material, path);
                        ok = true;
                    }
                }

                if (!ok)
                {
                    Debug.LogError($"Failed to generate material for {matInfo.name}.", matInfo);
                }
            }
        }

        private void OverwriteMaterialButton()
        {
            if (!GUILayout.Button("Overwrite Material"))
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(assetTargets[0]);
            string matFilePath = EditorUtility.OpenFilePanelWithFilters("Select Material",
                Path.GetDirectoryName(assetPath), new[] { "Material files", "mat" });

            if (string.IsNullOrEmpty(matFilePath) || !matFilePath.StartsWith(Application.dataPath + "/"))
            {
                return;
            }

            matFilePath = "Assets" + matFilePath.Substring(Application.dataPath.Length);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(matFilePath);

            if (material == null)
            {
                Debug.LogError($"Invalid material file: {matFilePath}");
                return;
            }

            Dictionary<string, BaseMaterialSetter> shaderMap = BaseMaterialSetter.AllShaderMap;
            bool ok = false;

            foreach (MaterialJsonData matInfo in assetTargets.Select(x => x as MaterialJsonData))
            {
                if (shaderMap.TryGetValue(matInfo.Shader, out BaseMaterialSetter setter))
                {
                    if (setter.TrySet(matInfo, material))
                    {
                        ok = true;
                        break;
                    }
                }
            }

            if (!ok)
            {
                Debug.LogError($"Failed to overwrite material {material.name}.", material);
            }
        }
    }
}
