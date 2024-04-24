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
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Scripting.APIUpdating;

namespace HSR.NPRShader.Editor.Automation
{
    [MovedFrom("HSR.NPRShader.Editor.Tools")]
    public class HSRMaterialViewer : EditorWindow
    {
        [OnOpenAsset(callbackOrder: -42)]
        public static bool Open(int instanceID, int line, int column)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is not MaterialJsonData matInfo)
            {
                return false;
            }

            var window = GetWindow<HSRMaterialViewer>("HSR Material Viewer");
            window.m_GameMatJsonData = matInfo;
            return true;
        }

        private readonly string[] m_ToolbarLabels = new[]
        {
            "Textures", "Colors", "Floats", "Ints"
        };

        [SerializeField] private MaterialJsonData m_GameMatJsonData;
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

            EditorGUILayout.TextField("Material Name", m_GameMatJsonData.Name);
            EditorGUILayout.TextField("Shader Name", m_GameMatJsonData.Shader);
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
                            DrawEntries(m_GameMatJsonData.Textures, TextureGUIAction);
                            break;
                        case 1:
                            DrawEntries(m_GameMatJsonData.Colors, EditorGUILayout.ColorField);
                            break;
                        case 2:
                            DrawEntries(m_GameMatJsonData.Floats, EditorGUILayout.FloatField);
                            break;
                        case 3:
                            DrawEntries(m_GameMatJsonData.Ints, EditorGUILayout.IntField);
                            break;
                    }

                    EditorGUILayout.Space();
                }
            }
        }

        private delegate T EntryGUIAction<T>(string label, T value, params GUILayoutOption[] options);

        private void DrawEntries<T>(List<MaterialJsonData.Entry<T>> entries, EntryGUIAction<T> guiAction)
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

        private TextureJsonData TextureGUIAction(string key, TextureJsonData value, params GUILayoutOption[] options)
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
