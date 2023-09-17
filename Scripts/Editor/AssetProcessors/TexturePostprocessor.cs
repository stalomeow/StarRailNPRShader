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

using HSR.NPRShader.Editor.Settings;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HSR.NPRShader.Editor.AssetProcessors
{
    public class TexturePostprocessor : AssetPostprocessor
    {
        public static readonly uint LogicVersion = 6u;

        public override uint GetVersion() => EditorProjectSettings.instance.TexturePostprocessorVersion + LogicVersion;

        private void OnPreprocessTexture()
        {
            var settings = EditorProjectSettings.instance;

            if (Regex.IsMatch(assetPath, settings.RampTexturePathPattern, RegexOptions.IgnoreCase))
            {
                PreprocessRamp();
            }
            else if (Regex.IsMatch(assetPath, settings.LightMapPathPattern, RegexOptions.IgnoreCase))
            {
                PreprocessLightMap();
            }
            else if (Regex.IsMatch(assetPath, settings.ColorTexturePathPattern, RegexOptions.IgnoreCase))
            {
                PreprocessColor();
            }
            else if (Regex.IsMatch(assetPath, settings.StockingsRangeMapPathPattern, RegexOptions.IgnoreCase))
            {
                PreprocessStockingsRangeMap();
            }
            else if (Regex.IsMatch(assetPath, settings.FaceMapPathPattern, RegexOptions.IgnoreCase))
            {
                PreprocessFaceMap();
            }
            else if (Regex.IsMatch(assetPath, settings.FaceExpressionMapPathPattern, RegexOptions.IgnoreCase))
            {
                PreprocessFaceExpressionMap();
            }
        }

        private void PreprocessRamp()
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                maxTextureSize = 256, // 宽度 256，高度 2 或 16
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed, // 不能压缩，不然会有奇怪的颜色出现
            });

            Debug.Log("<b>[Preprocess Ramp]</b> " + assetPath);
        }

        private void PreprocessLightMap()
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                // maxTextureSize = modifiers.Contains("L") ? 2048 : 1024,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
            });

            Debug.Log("<b>[Preprocess LightMap]</b> " + assetPath);
        }

        private void PreprocessColor()
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                // maxTextureSize = modifiers.Contains("L") ? 2048 : 1024,
                format = /* modifiers.Contains("A") ?*/ TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
            });

            Debug.Log("<b>[Preprocess Color]</b> " + assetPath);
        }

        private void PreprocessFaceMap()
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                maxTextureSize = 1024,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
            });

            Debug.Log("<b>[Preprocess FaceMap]</b> " + assetPath);
        }

        private void PreprocessFaceExpressionMap()
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                maxTextureSize = 1024,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
            });

            Debug.Log("<b>[Preprocess FaceExpressionMap]</b> " + assetPath);
        }

        private void PreprocessStockingsRangeMap()
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                // maxTextureSize = modifiers.Contains("L") ? 2048 : 1024,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
            });

            Debug.Log("<b>[Preprocess StockingsRangeMap]</b> " + assetPath);
        }
    }
}
