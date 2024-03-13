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

using HSR.NPRShader.Utils;
using UnityEditor;
using UnityEngine;

namespace HSR.NPRShader.Editor.AssetProcessors
{
    internal class AvatarModelPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            AssetProcessorConfig config = AssetProcessorGlobalSettings.instance.AvatarModelProcessConfig;
            context.DependsOnCustomDependency(AssetProcessorGlobalSettings.AvatarModelDependencyName);

            if (config.IsEnableAndAssetPathMatch(assetPath))
            {
                config.ApplyPreset(context, assetImporter);
            }
        }

        private void OnPostprocessModel(GameObject go)
        {
            AssetProcessorConfig config = AssetProcessorGlobalSettings.instance.AvatarModelProcessConfig;

            if (config.IsEnableAndAssetPathMatch(assetPath))
            {
                NormalUtility.SmoothAndStore(go, config.SmoothNormalStoreMode, false);
                go.AddComponent<StarRailCharacterRenderingController>();
            }
        }

        public override uint GetVersion() => 31u;
    }
}
