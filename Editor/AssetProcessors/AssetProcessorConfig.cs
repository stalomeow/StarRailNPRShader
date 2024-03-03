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
using System.Text.RegularExpressions;
using HSR.NPRShader.Utils;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Presets;
using UnityEngine;

namespace HSR.NPRShader.Editor.AssetProcessors
{
    [Serializable]
    public class AssetProcessorConfig
    {
        public bool Enable = true;
        public AssetPathMatchMode MatchMode;
        [Delayed] public string PathPattern;
        public bool IgnoreCase = true;
        public string OverridePresetGUIDHex;
        public NormalUtility.StoreMode SmoothNormalStoreMode = NormalUtility.StoreMode.ObjectSpaceTangent;

        // Default Setting
        [NonSerialized] public string DefaultPresetPath;

        // Editor State
        [HideInInspector] public bool Foldout;

        public bool IsEnableAndAssetPathMatch(string assetPath)
        {
            if (!Enable)
            {
                return false;
            }

            assetPath = assetPath.Replace('\\', '/');

            if (!assetPath.StartsWith("Assets/"))
            {
                return false;
            }

            switch (MatchMode)
            {
                case AssetPathMatchMode.NameGlob:
                {
                    string assetName = Path.GetFileName(assetPath);
                    RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Singleline;
                    IEnumerable<string> patterns = PathPattern.Split('|').Select(p =>
                    {
                        p = Regex.Escape(p.Trim());
                        p = p.Replace(@"\*", @".*");
                        p = p.Replace(@"\?", @".");
                        return $"^{p}$";
                    });
                    return patterns.Any(p => Regex.IsMatch(assetName, p, options));
                }

                case AssetPathMatchMode.Regex:
                {
                    var options = IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                    return Regex.IsMatch(assetPath, PathPattern, options);
                }

                case AssetPathMatchMode.Equals:
                {
                    return assetPath.Equals(PathPattern, GetStringComparison());
                }

                case AssetPathMatchMode.Contains:
                {
                    return assetPath.Contains(PathPattern, GetStringComparison());
                }

                case AssetPathMatchMode.StartsWith:
                {
                    return assetPath.StartsWith(PathPattern, GetStringComparison());
                }

                case AssetPathMatchMode.EndsWith:
                {
                    return assetPath.EndsWith(PathPattern, GetStringComparison());
                }

                default:
                {
                    throw new NotImplementedException();
                }
            }
        }

        public void ApplyPreset(AssetImportContext context, AssetImporter importer)
        {
            // https://docs.unity3d.com/Manual/DefaultPresetsByFolder.html

            GUID overridePresetGUID = new(OverridePresetGUIDHex);

            if (overridePresetGUID.Empty())
            {
                Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(DefaultPresetPath);

                // The script adds a Presets dependency to an Asset in two cases:
                // 1. If the Asset is imported before the Preset, the Preset will not load because it is not yet imported.
                // Adding a dependency between the Asset and the Preset allows the Asset to be re-imported so that Unity loads
                // the assigned Preset and can try to apply its values.
                // 2. If the Preset loads successfully, the ApplyTo method returns true if the Preset applies to this Asset's import settings.
                // Adding the Preset as a dependency to the Asset ensures that any change in the Preset values will re-import the Asset using the new values.
                if (preset == null || preset.ApplyTo(importer))
                {
                    // Using DependsOnArtifact here because Presets are native assets and using DependsOnSourceAsset would not work.
                    context.DependsOnArtifact(DefaultPresetPath);
                }
            }
            else
            {
                // overridePreset 必须自己手动从 AssetDatabase 里加载，避免出现下面的警告
                // dependency isn't used and therefore not registered in the asset database.
                string path = AssetDatabase.GUIDToAssetPath(overridePresetGUID);
                Preset preset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Preset>(path);

                // The script adds a Presets dependency to an Asset in two cases:
                // 1. If the Asset is imported before the Preset, the Preset will not load because it is not yet imported.
                // Adding a dependency between the Asset and the Preset allows the Asset to be re-imported so that Unity loads
                // the assigned Preset and can try to apply its values.
                // 2. If the Preset loads successfully, the ApplyTo method returns true if the Preset applies to this Asset's import settings.
                // Adding the Preset as a dependency to the Asset ensures that any change in the Preset values will re-import the Asset using the new values.
                if (preset == null || preset.ApplyTo(importer))
                {
                    // Using DependsOnArtifact here because Presets are native assets and using DependsOnSourceAsset would not work.
                    context.DependsOnArtifact(overridePresetGUID);
                }
            }
        }

        public void AppendToHash(ref Hash128 hash, bool includeSmoothNormalStoreMode = false)
        {
            hash.Append(ref Enable);
            hash.Append(ref MatchMode);
            hash.Append(PathPattern);
            hash.Append(ref IgnoreCase);
            hash.Append(OverridePresetGUIDHex);

            if (includeSmoothNormalStoreMode)
            {
                hash.Append(ref SmoothNormalStoreMode);
            }
        }

        private StringComparison GetStringComparison()
        {
            return IgnoreCase
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture;
        }
    }
}
