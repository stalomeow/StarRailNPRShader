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
    public class HairMaterialSetter : BaseMaterialSetter
    {
        protected override IReadOnlyDictionary<string, string> SupportedShaderMap => new Dictionary<string, string>()
        {
            ["miHoYo/CRP_Character/CharacterHair"] = "Honkai Star Rail/Character/Hair"
        };

        protected override IEnumerable<(string, TextureJsonData)> ApplyTextures(IReadOnlyDictionary<string, TextureJsonData> textures)
        {
            yield return MakeProperty("_MainTex", textures);
            yield return MakeProperty("_LightMap", textures);
            yield return MakeProperty("_RampMapWarm", textures, "_DiffuseRampMultiTex");
            yield return MakeProperty("_RampMapCool", textures, "_DiffuseCoolRampMultiTex");
        }

        protected override IEnumerable<(string, float)> ApplyFloats(IReadOnlyDictionary<string, float> floats)
        {
            // yield return MakeProperty("_Cull", floats, "_CullMode");

            yield return MakeProperty("_AlphaTest", floats, "_EnableAlphaCutoff");
            yield return MakeProperty("_AlphaTestThreshold", floats, "_AlphaCutoff");

            yield return MakeProperty("_EmissionThreshold", floats);
            yield return MakeProperty("_EmissionIntensity", floats);

            yield return MakeProperty("_RimShadowCt", floats);
            yield return MakeProperty("_RimShadowIntensity", floats);
            yield return MakeProperty("_RimShadowWidth0", floats);
            yield return MakeProperty("_RimShadowFeather0", floats);

            yield return MakeProperty("_SpecularIntensity0", floats);
            yield return MakeProperty("_SpecularShininess0", floats);
            yield return MakeProperty("_SpecularRoughness0", floats);

            yield return MakeProperty("_mmBloomIntensity0", floats, "_mBloomIntensity0");
        }

        protected override IEnumerable<(string, Color)> ApplyColors(IReadOnlyDictionary<string, Color> colors)
        {
            yield return MakeProperty("_Color", colors);
            yield return MakeProperty("_BackColor", colors);
            yield return MakeProperty("_SpecularColor0", colors);
            yield return MakeProperty("_RimColor0", colors);
            yield return MakeProperty("_OutlineColor0", colors);

            yield return MakeProperty("_RimShadowOffset", colors);
            yield return MakeProperty("_RimShadowColor0", colors);

            // Texture Scale Offset
            yield return MakeProperty("_Maps_ST", colors, "_MainMaps_ST");
        }
    }
}
