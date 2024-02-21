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
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HSR.NPRShader.Editor.Tools
{
    [ScriptedImporter(4, exts: new[] { "hsrmat" }, overrideExts: new[] { "json" })]
    public class MaterialJsonImporter : ScriptedImporter
    {
        [SerializeField] private string m_OverrideShaderName;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string text = File.ReadAllText(ctx.assetPath);
            JObject json = JObject.Parse(text);

            var matInfo = ScriptableObject.CreateInstance<MaterialInfo>();
            matInfo.Name = GetMaterialName(json);
            matInfo.Shader = GetShaderName(json);
            matInfo.Textures = DictToEntries(ReadTextures(json));
            matInfo.Ints = DictToEntries(ReadValues<int>(json, "m_Ints"));
            matInfo.Floats = DictToEntries(ReadValues<float>(json, "m_Floats"));
            matInfo.Colors = DictToEntries(ReadValues<Color>(json, "m_Colors"));

            ctx.AddObjectToAsset("MaterialInfo", matInfo);
            ctx.SetMainObject(matInfo);
        }

        private static string GetMaterialName(JObject json)
        {
            try
            {
                return json["m_Name"].ToObject<string>();
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetShaderName(JObject json)
        {
            if (!string.IsNullOrWhiteSpace(m_OverrideShaderName))
            {
                return m_OverrideShaderName;
            }

            try
            {
                return json["m_Shader"]["Name"].ToObject<string>();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<MaterialInfo.Entry<T>> DictToEntries<T>(Dictionary<string, T> dict)
        {
            return dict.Select(kvp => new MaterialInfo.Entry<T>
            {
                Key = kvp.Key,
                Value = kvp.Value
            }).OrderBy(entry => entry.Key).ToList();
        }

        private static Dictionary<string, MaterialInfo.TextureInfo> ReadTextures(JObject json)
        {
            Dictionary<string, MaterialInfo.TextureInfo> results = new();

            foreach (var prop in json["m_SavedProperties"]["m_TexEnvs"].Children())
            {
                (string key, MaterialInfo.TextureInfo tex) entry;
                if (!TryExecuteReaders(prop, out entry, ReadTextureV1, ReadTextureV2))
                {
                    continue;
                }
                results.Add(entry.key, entry.tex);
            }

            return results;
        }

        private static Dictionary<string, T> ReadValues<T>(JObject json, string propsName)
        {
            Dictionary<string, T> results = new();

            foreach (var prop in json["m_SavedProperties"][propsName].Children())
            {
                (string key, T value) entry;
                if (!TryExecuteReaders(prop, out entry, ReadValueV1<T>, ReadValueV2<T>))
                {
                    continue;
                }
                results.Add(entry.key, entry.value);
            }

            return results;
        }

        private static (string, MaterialInfo.TextureInfo) ReadTextureV1(JToken prop)
        {
            string label = prop.Path.Split('.')[^1];
            return (label, new MaterialInfo.TextureInfo
            {
                Name = prop.First["m_Texture"]["Name"]?.ToObject<string>() ?? string.Empty,
                IsNull = prop.First["m_Texture"]["IsNull"].ToObject<bool>(),
                Scale = prop.First["m_Scale"].ToObject<Vector2>(),
                Offset = prop.First["m_Offset"].ToObject<Vector2>(),
            });
        }

        private static (string, MaterialInfo.TextureInfo) ReadTextureV2(JToken prop)
        {
            string label = prop["Key"].ToObject<string>();
            return (label, new MaterialInfo.TextureInfo
            {
                Name = prop["Value"]["m_Texture"]["Name"]?.ToObject<string>() ?? string.Empty,
                IsNull = prop["Value"]["m_Texture"]["IsNull"].ToObject<bool>(),
                Scale = prop["Value"]["m_Scale"].ToObject<Vector2>(),
                Offset = prop["Value"]["m_Offset"].ToObject<Vector2>(),
            });
        }

        private static (string, T) ReadValueV1<T>(JToken prop)
        {
            string label = prop.Path.Split('.')[^1];
            T value = prop.First.ToObject<T>();
            return (label, value);
        }

        private static (string, T) ReadValueV2<T>(JToken prop)
        {
            string label = prop["Key"].ToObject<string>();
            T value = prop["Value"].ToObject<T>();
            return (label, value);
        }

        private static bool TryExecuteReaders<T>(JToken prop, out T result, params Func<JToken, T>[] readers)
        {
            foreach (Func<JToken, T> read in readers)
            {
                try
                {
                    result = read(prop);
                    return true;
                }
                catch
                {
                }
            }

            result = default;
            return false;
        }
    }
}
