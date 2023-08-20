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

using System.IO;
using UnityEditor;
using UnityEngine;

namespace HSR.Editor.Tools
{
    public class SplitTextures : EditorWindow
    {
        [MenuItem("HSR Tools/Split Textures")]
        public static void Open()
        {
            GetWindow<SplitTextures>("Split Textures");
        }

        [SerializeField] private Texture2D m_Texture;
        [SerializeField] private int m_ColumnCount = 1;
        [SerializeField] private int m_RowCount = 1;

        private void OnGUI()
        {
            m_Texture = (Texture2D)EditorGUILayout.ObjectField("Texture", m_Texture, typeof(Texture2D), false);
            m_ColumnCount = EditorGUILayout.IntField("Columns", m_ColumnCount);
            m_RowCount = EditorGUILayout.IntField("Rows", m_RowCount);

            if (GUILayout.Button("Split"))
            {
                string textureAssetPath = AssetDatabase.GetAssetPath(m_Texture);
                string outputFileFormat = Path.Combine(
                    Path.GetDirectoryName(textureAssetPath),
                    Path.GetFileNameWithoutExtension(textureAssetPath) + "_Pos{0}-{1}.png");

                int width = m_Texture.width / m_ColumnCount;
                int height = m_Texture.height / m_RowCount;
                Texture2D tex = new(width, height, TextureFormat.RGBA32, -1, false);

                for (int i = 0; i < m_RowCount; i++)
                {
                    for (int j = 0; j < m_ColumnCount; j++)
                    {
                        int x = j * width;
                        int y = i * height;
                        Graphics.CopyTexture(m_Texture, 0, 0, x, y, width, height, tex, 0, 0, 0, 0);
                        File.WriteAllBytes(string.Format(outputFileFormat, j, i), tex.EncodeToPNG());
                    }
                }

                AssetDatabase.Refresh();
            }
        }
    }
}
