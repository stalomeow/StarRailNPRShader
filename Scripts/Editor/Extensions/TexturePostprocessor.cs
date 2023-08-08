using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HSR.Editor.Extensions
{
    public class TexturePostprocessor : AssetPostprocessor
    {
        public override uint GetVersion() => 4u;

        private void OnPreprocessTexture()
        {
            string textureName = Path.GetFileNameWithoutExtension(assetPath);

            if (textureName.StartsWith("Avatar_", StringComparison.Ordinal))
            {
                string[] entries = textureName.Split('_', StringSplitOptions.RemoveEmptyEntries);

                if (entries[^1].ToLowerInvariant() == "ramp")
                {
                    PreprocessRamp();
                }
                else switch (entries[4].ToLowerInvariant())
                {
                    case "lightmap":
                    {
                        PreprocessLightMap(GetTextureModifiers(entries));
                        break;
                    }
                    case "color":
                    {
                        PreprocessColor(GetTextureModifiers(entries));
                        break;
                    }
                    case "stockings":
                    {
                        PreprocessStockingsRangeMap();
                        break;
                    }
                }
            }
            else if (textureName.Contains("_FaceMap", StringComparison.Ordinal))
            {
                PreprocessFaceMap();
            }
            else if (textureName.Contains("_Face_ExpressionMap", StringComparison.Ordinal))
            {
                PreprocessFaceExpressionMap();
            }

            static HashSet<string> GetTextureModifiers(string[] entries)
            {
                HashSet<string> modifiers = new();

                for (int i = 5; i < entries.Length; i++)
                {
                    modifiers.Add(entries[i]);
                }

                return modifiers;
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

        private void PreprocessLightMap(HashSet<string> modifiers)
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

        private void PreprocessColor(HashSet<string> modifiers)
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
