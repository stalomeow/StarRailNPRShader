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
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HSR.NPRShader.Editor.Tools
{
    [CustomEditor(typeof(MaterialJsonImporter))]
    public class MaterialJsonImporterEditor : ScriptedImporterEditor
    {
        private SerializedProperty m_OverrideShaderName;

        public override void OnEnable()
        {
            base.OnEnable();

            m_OverrideShaderName = serializedObject.FindProperty("m_OverrideShaderName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            Rect rect = EditorGUILayout.GetControlRect(true);
            rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.TrTextContent("Shader Override"));

            if (EditorGUI.DropdownButton(rect, EditorGUIUtility.TrTextContent(m_OverrideShaderName.stringValue), FocusType.Passive))
            {
                HashSet<string> hashSet = new();
                foreach (var setterType in TypeCache.GetTypesDerivedFrom<BaseMaterialSetter>())
                {
                    var setter = (BaseMaterialSetter)Activator.CreateInstance(setterType);
                    hashSet.UnionWith(setter.SupportedShaderMap.Keys);
                }

                List<string> supportedShaderNames = hashSet.ToList();
                supportedShaderNames.Sort();

                GenericMenu menu = new();
                foreach (var shaderName in supportedShaderNames)
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

            ApplyRevertGUI();
        }
    }
}
