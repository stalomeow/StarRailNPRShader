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
using UnityEngine;
using Stalo.ShaderUtils.Editor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace HSR.NPRShader.Editor.Tools
{
    public class HSRMaterialViewer : EditorWindow
    {
        [OnOpenAsset(callbackOrder: -42)]
        public static bool Open(int instanceID, int line, int column)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is not MaterialInfo matInfo)
            {
                return false;
            }

            var window = GetWindow<HSRMaterialViewer>("HSR Material Viewer");
            window.m_GameMatInfo = matInfo;
            return true;
        }

        private readonly string[] m_ToolbarLabels = new[]
        {
            "Textures", "Colors", "Floats", "Ints"
        };

        [SerializeField] private MaterialInfo m_GameMatInfo;
        [SerializeField] private string m_SearchText;
        [SerializeField] private int m_ToolbarIndex;
        [SerializeField] private Vector2[] m_ScrollPos = new Vector2[4];

        private SearchField m_Search;

        private void OnGUI()
        {
            m_Search ??= new SearchField();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                Rect searchRect = EditorGUILayout.GetControlRect();
                m_SearchText = m_Search.OnToolbarGUI(searchRect, m_SearchText);
            }

            EditorGUILayout.TextField("Material Name", m_GameMatInfo.Name);
            EditorGUILayout.TextField("Shader Name", m_GameMatInfo.Shader);
            EditorGUILayout.HelpBox("It's better to reset the material after changing its shader. Applying Floats and Ints are not well supported by this tool.", MessageType.Info);
            DoApplyToMaterialButton();
            EditorGUILayout.Space();

            m_ToolbarIndex = GUILayout.Toolbar(m_ToolbarIndex, m_ToolbarLabels);
            EditorGUILayout.Space();

            using (new MemberValueScope<float>(() => EditorGUIUtility.labelWidth, 240))
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPos[m_ToolbarIndex]))
                {
                    m_ScrollPos[m_ToolbarIndex] = scrollView.scrollPosition;

                    switch (m_ToolbarIndex)
                    {
                        case 0:
                            DrawEntries(m_GameMatInfo.Textures, TextureGUIAction);
                            break;
                        case 1:
                            DrawEntries(m_GameMatInfo.Colors, EditorGUILayout.ColorField);
                            break;
                        case 2:
                            DrawEntries(m_GameMatInfo.Floats, EditorGUILayout.FloatField);
                            break;
                        case 3:
                            DrawEntries(m_GameMatInfo.Ints, EditorGUILayout.IntField);
                            break;
                    }

                    EditorGUILayout.Space();
                }
            }
        }

        private void DoApplyToMaterialButton()
        {
            List<Object> materials = Selection.objects.Where(o => o is Material).ToList();

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(m_GameMatInfo.Shader) || materials.Count <= 0))
            {
                if (!GUILayout.Button("Apply to selected Material(s)"))
                {
                    return;
                }
            }

            List<BaseMaterialSetter> setters = new();
            foreach (var setterType in TypeCache.GetTypesDerivedFrom<BaseMaterialSetter>())
            {
                setters.Add((BaseMaterialSetter)Activator.CreateInstance(setterType));
            }
            setters.Sort((x, y) => x.Order - y.Order);

            foreach (var material in materials)
            {
                bool ok = false;

                foreach (var setter in setters)
                {
                    if (setter.TrySet(m_GameMatInfo, (Material)material))
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                {
                    Debug.LogError($"The shader of Material '{material.name}' is not compatible with '{m_GameMatInfo.Shader}'!", material);
                }
            }
        }

        private delegate T EntryGUIAction<T>(string label, T value, params GUILayoutOption[] options);

        private void DrawEntries<T>(List<MaterialInfo.Entry<T>> entries, EntryGUIAction<T> guiAction)
        {
            foreach (var entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(m_SearchText))
                {
                    if (!entry.Key.Contains(m_SearchText, StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }
                }

                guiAction(entry.Key, entry.Value);
            }
        }

        private MaterialInfo.TextureInfo TextureGUIAction(string key, MaterialInfo.TextureInfo value, params GUILayoutOption[] options)
        {
            using (new EditorGUI.DisabledScope(value.IsNull))
            {
                EditorGUILayout.LabelField(key, EditorStyles.boldLabel);
            }

            if (!value.IsNull)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    if (!string.IsNullOrWhiteSpace(value.Name))
                    {
                        EditorGUILayout.LabelField(value.Name);
                    }

                    EditorGUILayout.LabelField("Scale", value.Scale.ToString());
                    EditorGUILayout.LabelField("Offset", value.Offset.ToString());
                }
            }

            return value;
        }
    }
}
