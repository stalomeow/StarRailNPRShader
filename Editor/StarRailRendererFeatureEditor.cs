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
using System.Linq;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEngine;

namespace HSR.NPRShader.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(StarRailRendererFeature))]
    internal class StarRailRendererFeatureEditor : UnityEditor.Editor
    {
        public const string ScreenSpaceShadowsTypeName = "UnityEngine.Rendering.Universal.ScreenSpaceShadows";

        private SerializedProperty m_EnableTransparentFrontHair;
        private SerializedProperty m_SceneShadowDepthBits;
        private SerializedProperty m_SceneShadowTileResolution;
        private SerializedProperty m_EnableSelfShadow;
        private SerializedProperty m_SelfShadowDepthBits;
        private SerializedProperty m_SelfShadowTileResolution;

        private void OnEnable()
        {
            m_EnableTransparentFrontHair = serializedObject.FindProperty(nameof(m_EnableTransparentFrontHair));
            m_SceneShadowDepthBits = serializedObject.FindProperty(nameof(m_SceneShadowDepthBits));
            m_SceneShadowTileResolution = serializedObject.FindProperty(nameof(m_SceneShadowTileResolution));
            m_EnableSelfShadow = serializedObject.FindProperty(nameof(m_EnableSelfShadow));
            m_SelfShadowDepthBits = serializedObject.FindProperty(nameof(m_SelfShadowDepthBits));
            m_SelfShadowTileResolution = serializedObject.FindProperty(nameof(m_SelfShadowTileResolution));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Transparent Front Hair", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_EnableTransparentFrontHair, EditorGUIUtility.TrTextContent("Enable"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Shadow", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_SceneShadowTileResolution, EditorGUIUtility.TrTextContent("Tile Resolution"));
                EditorGUILayout.PropertyField(m_SceneShadowDepthBits, EditorGUIUtility.TrTextContent("Depth Bits"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Self Shadow", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_EnableSelfShadow, EditorGUIUtility.TrTextContent("Enable"));
                EditorGUILayout.PropertyField(m_SelfShadowTileResolution, EditorGUIUtility.TrTextContent("Tile Resolution"));
                EditorGUILayout.PropertyField(m_SelfShadowDepthBits, EditorGUIUtility.TrTextContent("Depth Bits"));
            }

            ShowErrors();
        }

        private void ShowErrors()
        {
            foreach (UniversalRendererData rendererData in GetRendererDataList())
            {
                if (rendererData.renderingMode == RenderingMode.Deferred)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Deferred Rendering Path is not supported.", MessageType.Error);
                    Debug.LogError("Deferred Rendering Path is not supported.");
                }
                else if (rendererData.depthPrimingMode != DepthPrimingMode.Disabled)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Depth Priming is not supported.", MessageType.Error);
                    Debug.LogError("Depth Priming is not supported.");
                }

                if (rendererData.rendererFeatures.Exists(f => f.GetType().FullName == ScreenSpaceShadowsTypeName))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Screen Space Shadows must be removed.", MessageType.Error);
                    Debug.LogError("Screen Space Shadows must be removed.");
                }
            }
        }

        private List<UniversalRendererData> GetRendererDataList()
        {
            List<UniversalRendererData> rendererDataList = new();

            foreach (string path in targets.Select(AssetDatabase.GetAssetPath))
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) is UniversalRendererData data)
                {
                    rendererDataList.Add(data);
                }
            }

            return rendererDataList;
        }
    }
}
