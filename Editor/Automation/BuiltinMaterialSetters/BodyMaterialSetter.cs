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
using UnityEngine;

namespace HSR.NPRShader.Editor.Automation.BuiltinMaterialSetters
{
    public class BodyMaterialSetter : BaseMaterialSetter
    {
        protected override IReadOnlyDictionary<string, string> SupportedShaderMap => new Dictionary<string, string>()
        {
            ["miHoYo/CRP_Character/CharacterBase"] = "Honkai Star Rail/Character/Body",
            ["miHoYo/CRP_Character/CharacterTransparent"] = "Honkai Star Rail/Character/Body (Transparent)",
        };

        protected override IEnumerable<(string, TextureJsonData)> ApplyTextures(IReadOnlyDictionary<string, TextureJsonData> textures)
        {
            yield return MakeProperty("_MainTex", textures);
            yield return MakeProperty("_LightMap", textures);
            yield return MakeProperty("_RampMapWarm", textures, "_DiffuseRampMultiTex");
            yield return MakeProperty("_RampMapCool", textures, "_DiffuseCoolRampMultiTex");

            yield return MakeProperty("_StockingsMap", textures, "_StockRangeTex");
        }

        protected override IEnumerable<(string, float)> ApplyFloats(IReadOnlyDictionary<string, float> floats)
        {
            // yield return MakeProperty("_Cull", floats, "_CullMode");

            // TODO Float 某些值不准，比如 _SrcBlend 和 _DstBlend
            // yield return MakeProperty("_SrcBlendColor", floats, "_SrcBlend");
            // yield return MakeProperty("_DstBlendColor", floats, "_DstBlend");

            yield return MakeProperty("_AlphaTest", floats, "_EnableAlphaCutoff");
            yield return MakeProperty("_AlphaTestThreshold", floats, "_AlphaCutoff");

            yield return MakeProperty("_EmissionThreshold", floats);
            yield return MakeProperty("_EmissionIntensity", floats);

            yield return MakeProperty("_RimShadowCt", floats);
            yield return MakeProperty("_RimShadowIntensity", floats);

            for (int i = 0; i <= 7; i++)
            {
                yield return MakeProperty($"_SpecularIntensity{i}", floats);
                yield return MakeProperty($"_SpecularShininess{i}", floats);
                yield return MakeProperty($"_SpecularRoughness{i}", floats);

                yield return MakeProperty($"_RimShadowWidth{i}", floats);
                yield return MakeProperty($"_RimShadowFeather{i}", floats);

                yield return MakeProperty($"_mmBloomIntensity{i}", floats, $"_mBloomIntensity{i}");
            }

            // Stockings
            yield return MakeProperty("_StockingsDarkWidth", floats, "_StockDarkWidth");
            yield return MakeProperty("_StockingsPower", floats, "_Stockpower");
            yield return MakeProperty("_StockingsLightedWidth", floats, "_Stockpower1");
            yield return MakeProperty("_StockingsLightedIntensity", floats, "_StockSP");
            yield return MakeProperty("_StockingsRoughness", floats, "_StockRoughness");
        }

        protected override IEnumerable<(string, Color)> ApplyColors(IReadOnlyDictionary<string, Color> colors)
        {
            yield return MakeProperty("_Color", colors);
            yield return MakeProperty("_BackColor", colors);
            yield return MakeProperty("_EmissionColor", colors, "_EmissionTintColor");
            yield return MakeProperty("_RimShadowOffset", colors);

            for (int i = 0; i <= 7; i++)
            {
                yield return MakeProperty($"_SpecularColor{i}", colors);
                yield return MakeProperty($"_RimColor{i}", colors);
                yield return MakeProperty($"_RimShadowColor{i}", colors);
                yield return MakeProperty($"_OutlineColor{i}", colors);
                yield return MakeProperty($"_BloomColor{i}", colors, $"_mBloomColor{i}");
            }

            // Texture Scale Offset
            yield return MakeProperty("_Maps_ST", colors, "_MainMaps_ST");

            // Stockings
            yield return MakeProperty("_StockingsColor", colors, "_Stockcolor");
            yield return MakeProperty("_StockingsColorDark", colors, "_StockDarkcolor");
        }
    }
}
