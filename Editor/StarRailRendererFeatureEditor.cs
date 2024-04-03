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
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

namespace HSR.NPRShader.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(StarRailRendererFeature))]
    public class StarRailRendererFeatureEditor : UnityEditor.Editor
    {
        public const string GitHubLink = "https://github.com/stalomeow/StarRailNPRShader";

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

            foreach (UniversalRendererData rendererData in GetRendererDataList())
            {
                if (rendererData.renderingMode == RenderingMode.Deferred)
                {
                    EditorGUILayout.HelpBox("Deferred Rendering Path is not supported.", MessageType.Error);
                    EditorGUILayout.Space();
                }
                else if (rendererData.depthPrimingMode != DepthPrimingMode.Disabled)
                {
                    EditorGUILayout.HelpBox("Depth Priming is not supported.", MessageType.Error);
                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.LabelField("GitHub Repository", EditorStyles.boldLabel);

            if (EditorGUILayout.LinkButton(GitHubLink))
            {
                Application.OpenURL(GitHubLink);
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
