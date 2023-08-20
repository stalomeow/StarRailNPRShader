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

#if PACKAGE_NEWTONSOFT_JSON
using System;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Stalo.ShaderUtils.Editor;

namespace HSR.Editor.Tools
{
    public class GameMaterialInspector : EditorWindow
    {
        [MenuItem("HSR Tools/Game Material Inspector")]
        public static void Open()
        {
            GetWindow<GameMaterialInspector>("Game Material Inspector");
        }

        [SerializeField] private TextAsset m_MatJsonAsset;
        [SerializeField] private Vector2 m_ScrollPos;
        [SerializeField] private bool m_ShowTextures;
        [SerializeField] private bool m_ShowFloats;
        [SerializeField] private bool m_ShowColors;

        [NonSerialized] private bool m_IsLegacyVersion;
        [NonSerialized] private JToken m_Props;

        private void OnGUI()
        {
            UpdateProps();

            EditorGUILayout.Space();

            using (new MemberValueScope<float>(() => EditorGUIUtility.labelWidth, 240))
            {
                try
                {
                    DrawProps();
                }
                catch (Exception e) when (e is not ExitGUIException) // ExitGUIException is used by unity
                {
                    m_IsLegacyVersion = !m_IsLegacyVersion;
                }
            }
        }

        private void UpdateProps()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();

                m_MatJsonAsset = (TextAsset)EditorGUILayout.ObjectField("Material Json", m_MatJsonAsset,
                    typeof(TextAsset), false, GUILayout.Height(16));

                if (EditorGUI.EndChangeCheck() || m_Props == null)
                {
                    try
                    {
                        JObject obj = JObject.Parse(m_MatJsonAsset.text);
                        m_Props = obj["m_SavedProperties"];
                    }
                    catch
                    {
                        m_Props = null;
                    }
                }
            }
        }

        private void DrawProps()
        {
            if (m_Props == null)
            {
                return;
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPos))
            {
                m_ScrollPos = scrollView.scrollPosition;

                DrawTextureProps(m_Props);
                EditorGUILayout.Space();

                DrawFloatProps(m_Props);
                EditorGUILayout.Space();

                DrawColorProps(m_Props);
                EditorGUILayout.Space();
            }
        }

        private void DrawTextureProps(JToken props)
        {
            m_ShowTextures = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowTextures, "Textures");

            try
            {
                if (!m_ShowTextures)
                {
                    return;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var textureProp in props["m_TexEnvs"].Children())
                    {
                        if (m_IsLegacyVersion)
                        {
                            string label = textureProp.Path.Split('.')[^1];
                            bool isNull = textureProp.First["m_Texture"]["IsNull"].ToObject<bool>();

                            using (new EditorGUI.DisabledScope(isNull))
                            {
                                EditorGUILayout.LabelField(label);
                            }

                            if (!isNull)
                            {
                                Vector2 scale = textureProp.First["m_Scale"].ToObject<Vector2>();
                                Vector2 offset = textureProp.First["m_Offset"].ToObject<Vector2>();

                                using (new EditorGUI.IndentLevelScope())
                                {
                                    EditorGUILayout.LabelField("Scale", $"{scale.x},  {scale.y}");
                                    EditorGUILayout.LabelField("Offset", $"{offset.x},  {offset.y}");
                                }
                            }
                        }
                        else
                        {
                            string label = textureProp["Key"].ToObject<string>();
                            bool isNull = textureProp["Value"]["m_Texture"]["IsNull"].ToObject<bool>();

                            using (new EditorGUI.DisabledScope(isNull))
                            {
                                EditorGUILayout.LabelField(label);
                            }

                            if (!isNull)
                            {
                                Vector2 scale = textureProp["Value"]["m_Scale"].ToObject<Vector2>();
                                Vector2 offset = textureProp["Value"]["m_Offset"].ToObject<Vector2>();

                                using (new EditorGUI.IndentLevelScope())
                                {
                                    EditorGUILayout.LabelField("Scale", $"{scale.x},  {scale.y}");
                                    EditorGUILayout.LabelField("Offset", $"{offset.x},  {offset.y}");
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void DrawFloatProps(JToken props)
        {
            m_ShowFloats = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowFloats, "Floats");

            try
            {
                if (!m_ShowFloats)
                {
                    return;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var floatProp in props["m_Floats"].Children())
                    {
                        if (m_IsLegacyVersion)
                        {
                            string label = floatProp.Path.Split('.')[^1];
                            float value = floatProp.First.ToObject<float>();
                            EditorGUILayout.FloatField(label, value);
                        }
                        else
                        {
                            string label = floatProp["Key"].ToObject<string>();
                            float value = floatProp["Value"].ToObject<float>();
                            EditorGUILayout.FloatField(label, value);
                        }
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void DrawColorProps(JToken props)
        {
            m_ShowColors = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowColors, "Colors");

            try
            {
                if (!m_ShowColors)
                {
                    return;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var colorProp in props["m_Colors"].Children())
                    {
                        if (m_IsLegacyVersion)
                        {
                            string label = colorProp.Path.Split('.')[^1];
                            Color value = colorProp.First.ToObject<Color>();
                            EditorGUILayout.ColorField(label, value);
                        }
                        else
                        {
                            string label = colorProp["Key"].ToObject<string>();
                            Color value = colorProp["Value"].ToObject<Color>();
                            EditorGUILayout.ColorField(label, value);
                        }
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }
    }
}
#endif
