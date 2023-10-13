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

using Stalo.ShaderUtils.Editor;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HSR.NPRShader.Editor.Settings
{
    internal class EditorProjectSettingsProvider : SettingsProvider
    {
        private SerializedObject m_SerializedObject;

        private SerializedProperty AvatarModelPathPattern;
        private SerializedProperty RampTexturePathPattern;
        private SerializedProperty LightMapPathPattern;
        private SerializedProperty ColorTexturePathPattern;
        private SerializedProperty StockingsRangeMapPathPattern;
        private SerializedProperty FaceMapPathPattern;
        private SerializedProperty FaceExpressionMapPathPattern;

        private SerializedProperty AvatarModelPostprocessorVersion;
        private SerializedProperty TexturePostprocessorVersion;

        public EditorProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            EditorProjectSettings.instance.Save();
            m_SerializedObject = EditorProjectSettings.instance.AsSerializedObject();

            // initialize properities
            FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(SerializedProperty))
                {
                    SerializedProperty property = m_SerializedObject.FindProperty(field.Name);
                    field.SetValue(this, property);
                }
            }
        }

        public override void OnGUI(string searchContext)
        {
            m_SerializedObject.Update();

            using (new MemberValueScope<float>(() => EditorGUIUtility.labelWidth, 250.0f))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();
                GUILayout.Space(15);

                EditorGUILayout.LabelField("Asset Path Patterns", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(AvatarModelPathPattern, EditorGUIUtility.TrTextContent("Avatar Model"));
                if (EditorGUI.EndChangeCheck())
                {
                    AvatarModelPostprocessorVersion.uintValue++;
                    m_SerializedObject.ApplyModifiedProperties();
                    EditorProjectSettings.instance.Save();
                    EditorUtility.RequestScriptReload(); // Reload AvatarModelPostprocessor
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(RampTexturePathPattern, EditorGUIUtility.TrTextContent("Ramp Texture"));
                EditorGUILayout.PropertyField(LightMapPathPattern, EditorGUIUtility.TrTextContent("Light Map"));
                EditorGUILayout.PropertyField(ColorTexturePathPattern, EditorGUIUtility.TrTextContent("Color Texture"));
                EditorGUILayout.PropertyField(StockingsRangeMapPathPattern, EditorGUIUtility.TrTextContent("Stockings Range Map"));
                EditorGUILayout.PropertyField(FaceMapPathPattern, EditorGUIUtility.TrTextContent("Face Map"));
                EditorGUILayout.PropertyField(FaceExpressionMapPathPattern, EditorGUIUtility.TrTextContent("Face Expression Map"));
                if (EditorGUI.EndChangeCheck())
                {
                    TexturePostprocessorVersion.uintValue++;
                    m_SerializedObject.ApplyModifiedProperties();
                    EditorProjectSettings.instance.Save();
                    EditorUtility.RequestScriptReload(); // Reload TexturePostprocessor
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectSettingsProvider()
        {
            return new EditorProjectSettingsProvider(EditorProjectSettings.PathInProjectSettings, SettingsScope.Project);
        }
    }
}
