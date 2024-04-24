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

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.AnimatedValues;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;

namespace HSR.NPRShader.Editor.AssetProcessors
{
    internal class AssetProcessorGlobalSettingsProvider : SettingsProvider
    {
        private SerializedObject m_SerializedObject;

        private SerializedProperty AvatarModelProcessConfig;
        private AnimBool m_AvatarModelProcessConfigFoldout;
        private SerializedProperty RampTextureProcessConfig;
        private AnimBool m_RampTextureProcessConfigFoldout;
        private SerializedProperty LightMapProcessConfig;
        private AnimBool m_LightMapProcessConfigFoldout;
        private SerializedProperty ColorTextureProcessConfig;
        private AnimBool m_ColorTextureProcessConfigFoldout;
        private SerializedProperty StockingsRangeMapProcessConfig;
        private AnimBool m_StockingsRangeMapProcessConfigFoldout;
        private SerializedProperty FaceMapProcessConfig;
        private AnimBool m_FaceMapProcessConfigFoldout;
        private SerializedProperty FaceExpressionMapProcessConfig;
        private AnimBool m_FaceExpressionMapProcessConfigFoldout;

        public AssetProcessorGlobalSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            AssetProcessorGlobalSettings.instance.Save();
            m_SerializedObject = AssetProcessorGlobalSettings.instance.AsSerializedObject();

            // initialize properties
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

             using (new MemberValueScope<float>(() => EditorGUIUtility.labelWidth, 190.0f))
             {
                 GUILayout.BeginHorizontal();
                 GUILayout.Space(10);
                 GUILayout.BeginVertical();
                 GUILayout.Space(15);

                 DrawAssetProcessorConfig("Avatar Model", ref m_AvatarModelProcessConfigFoldout, AvatarModelProcessConfig, showSmoothNormalStoreMode: true);
                 DrawAssetProcessorConfig("Color Texture", ref m_ColorTextureProcessConfigFoldout, ColorTextureProcessConfig);
                 DrawAssetProcessorConfig("Face Expression Map", ref m_FaceExpressionMapProcessConfigFoldout, FaceExpressionMapProcessConfig);
                 DrawAssetProcessorConfig("Face Map", ref m_FaceMapProcessConfigFoldout, FaceMapProcessConfig);
                 DrawAssetProcessorConfig("Light Map", ref m_LightMapProcessConfigFoldout, LightMapProcessConfig);
                 DrawAssetProcessorConfig("Ramp Texture", ref m_RampTextureProcessConfigFoldout, RampTextureProcessConfig);
                 DrawAssetProcessorConfig("Stockings Range Map", ref m_StockingsRangeMapProcessConfigFoldout, StockingsRangeMapProcessConfig);

                 GUILayout.Space(10);

                 GUILayout.EndVertical();
                 GUILayout.EndHorizontal();
             }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            AssetDatabase.Refresh(); // Force Reimport
        }

        private void DrawAssetProcessorConfig(string label, ref AnimBool foldoutAnim, SerializedProperty property, bool showSmoothNormalStoreMode = false)
        {
            SerializedProperty enable = property.FindPropertyRelative(nameof(AssetProcessorConfig.Enable));
            SerializedProperty matchMode = property.FindPropertyRelative(nameof(AssetProcessorConfig.MatchMode));
            SerializedProperty pathPattern = property.FindPropertyRelative(nameof(AssetProcessorConfig.PathPattern));
            SerializedProperty ignoreCase = property.FindPropertyRelative(nameof(AssetProcessorConfig.IgnoreCase));
            SerializedProperty overridePresetAssetPath = property.FindPropertyRelative(nameof(AssetProcessorConfig.OverridePresetAssetPath));
            SerializedProperty smoothNormalStoreMode = property.FindPropertyRelative(nameof(AssetProcessorConfig.SmoothNormalStoreMode));
            SerializedProperty foldout = property.FindPropertyRelative(nameof(AssetProcessorConfig.Foldout));

            EditorGUI.BeginChangeCheck();

            GUIContent title = EditorGUIUtility.TrTextContent(label);
            foldoutAnim ??= new AnimBool(foldout.boolValue, Repaint); // 变化后需要重新绘制，不然动画很卡
            foldoutAnim.target = CoreEditorUtils.DrawHeaderToggleFoldout(title, foldoutAnim.target, enable, null, null, null, string.Empty);
            if (foldout.boolValue != foldoutAnim.target)
            {
                foldout.boolValue = foldoutAnim.target;
                GUI.changed = true;
            }

            using (new EditorGUI.DisabledScope(!enable.boolValue))
            {
                if (EditorGUILayout.BeginFadeGroup(foldoutAnim.faded))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(matchMode);
                        EditorGUILayout.PropertyField(pathPattern);
                        if (matchMode.intValue != (int)AssetPathMatchMode.NameGlob)
                        {
                            EditorGUILayout.PropertyField(ignoreCase);
                        }

                        OverridePresetField(overridePresetAssetPath);

                        if (showSmoothNormalStoreMode)
                        {
                            EditorGUILayout.PropertyField(smoothNormalStoreMode);
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndFadeGroup();
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                AssetProcessorGlobalSettings.instance.Save();
            }
        }

        private static void OverridePresetField(SerializedProperty overridePresetAssetPath)
        {
            string path = overridePresetAssetPath.stringValue;
            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(path);
            Preset newPreset = EditorGUILayout.ObjectField("Custom Preset", preset,
                typeof(Preset), false) as Preset;

            if (newPreset != preset)
            {
                overridePresetAssetPath.stringValue = AssetDatabase.GetAssetPath(newPreset);
                GUI.changed = true;
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectSettingsProvider()
        {
            return new AssetProcessorGlobalSettingsProvider(AssetProcessorGlobalSettings.PathInProjectSettings, SettingsScope.Project);
        }
    }
}
