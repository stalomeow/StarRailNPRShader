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

using UnityEditor;

namespace HSR.NPRShader.Editor.AssetProcessors
{
    internal class PresetModificationProcessor : AssetModificationProcessor
    {
        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            UpdateAssetProcessorGlobalSettings(sourcePath, destinationPath);
            return AssetMoveResult.DidNotMove;
        }

        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            UpdateAssetProcessorGlobalSettings(assetPath, null);
            return AssetDeleteResult.DidNotDelete;
        }

        private static void UpdateAssetProcessorGlobalSettings(string assetSourcePath, string assetDestinationPath)
        {
            var settings = AssetProcessorGlobalSettings.instance;
            bool changed = false;

            foreach (AssetProcessorConfig config in settings.AllConfigs)
            {
                if (config.OverridePresetAssetPath == assetSourcePath)
                {
                    config.OverridePresetAssetPath = assetDestinationPath;
                    changed = true;
                }
            }

            if (changed)
            {
                // https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.OnWillMoveAsset.html
                // You should not call any Unity AssetDatabase API from within this callback,
                // preferably restrict yourself to the usage of file operations or VCS APIs.
                EditorApplication.delayCall += () => settings.Save();
            }
        }
    }
}
