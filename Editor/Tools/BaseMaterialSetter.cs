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
using UnityEditor;
using UnityEngine;

namespace HSR.NPRShader.Editor.Tools
{
    public abstract class BaseMaterialSetter
    {
        public bool TrySet(MaterialInfo srcMatInfo, Material dstMaterial)
        {
            if (!CanSet(srcMatInfo, dstMaterial))
            {
                return false;
            }

            foreach (var (key, value) in ApplyTextures(EntriesToDict(srcMatInfo.Textures)))
            {
                if (value.IsNull)
                {
                    dstMaterial.SetTexture(key, null);
                }
                else if (!string.IsNullOrWhiteSpace(value.Name))
                {
                    string[] textures = AssetDatabase.FindAssets($"t:{nameof(Texture)} {value.Name}");

                    if (textures.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(textures[0]);
                        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                        dstMaterial.SetTexture(key, texture);
                    }
                    else
                    {
                        Debug.LogWarning($"Texture {value.Name} was not found.");
                    }
                }

                dstMaterial.SetTextureScale(key, value.Scale);
                dstMaterial.SetTextureOffset(key, value.Offset);
            }

            foreach (var (key, value) in ApplyInts(EntriesToDict(srcMatInfo.Ints)))
            {
                dstMaterial.SetInt(key, value);
            }

            foreach (var (key, value) in ApplyFloats(EntriesToDict(srcMatInfo.Floats)))
            {
                dstMaterial.SetFloat(key, value);
            }

            foreach (var (key, value) in ApplyColors(EntriesToDict(srcMatInfo.Colors)))
            {
                dstMaterial.SetColor(key, value);
            }

            return true;
        }

        private bool CanSet(MaterialInfo srcMatInfo, Material dstMaterial)
        {
            if (!SupportedShaderMap.TryGetValue(srcMatInfo.Shader, out string shader))
            {
                return false;
            }

            return dstMaterial.shader.name == shader;
        }

        private static Dictionary<string, T> EntriesToDict<T>(List<MaterialInfo.Entry<T>> entries)
        {
            return entries.ToDictionary(e => e.Key, e => e.Value);
        }

        public virtual int Order => 9999;

        public abstract Dictionary<string, string> SupportedShaderMap { get; }

        protected virtual IEnumerable<(string, MaterialInfo.TextureInfo)> ApplyTextures(Dictionary<string, MaterialInfo.TextureInfo> textures)
        {
            yield break;
        }

        protected virtual IEnumerable<(string, int)> ApplyInts(Dictionary<string, int> ints)
        {
            yield break;
        }

        protected virtual IEnumerable<(string, float)> ApplyFloats(Dictionary<string, float> floats)
        {
            yield break;
        }

        protected virtual IEnumerable<(string, Color)> ApplyColors(Dictionary<string, Color> colors)
        {
            yield break;
        }
    }
}
