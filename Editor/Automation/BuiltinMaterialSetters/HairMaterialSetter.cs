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
            yield return ("_MainTex", textures["_MainTex"]);
            yield return ("_LightMap", textures["_LightMap"]);
            yield return ("_RampMapWarm", textures["_DiffuseRampMultiTex"]);
            yield return ("_RampMapCool", textures["_DiffuseCoolRampMultiTex"]);
        }

        protected override IEnumerable<(string, float)> ApplyFloats(IReadOnlyDictionary<string, float> floats)
        {
            // yield return ("_Cull", floats["_CullMode"]);

            yield return ("_AlphaTest", floats["_EnableAlphaCutoff"]);
            yield return ("_AlphaTestThreshold", floats["_AlphaCutoff"]);

            yield return ("_EmissionThreshold", floats["_EmissionThreshold"]);
            yield return ("_EmissionIntensity", floats["_EmissionIntensity"]);

            yield return ("_RimShadowCt", floats["_RimShadowCt"]);
            yield return ("_RimShadowIntensity", floats["_RimShadowIntensity"]);
            yield return ("_RimShadowWidth0", floats["_RimShadowWidth0"]);
            yield return ("_RimShadowFeather0", floats["_RimShadowFeather0"]);

            yield return ("_SpecularIntensity0", floats["_SpecularIntensity0"]);
            yield return ("_SpecularShininess0", floats["_SpecularShininess0"]);
            yield return ("_SpecularRoughness0", floats["_SpecularRoughness0"]);

            yield return ("_mmBloomIntensity0", floats["_mBloomIntensity0"]);
        }

        protected override IEnumerable<(string, Color)> ApplyColors(IReadOnlyDictionary<string, Color> colors)
        {
            yield return ("_Color", colors["_Color"]);
            yield return ("_BackColor", colors["_BackColor"]);
            yield return ("_SpecularColor0", colors["_SpecularColor0"]);
            yield return ("_RimColor0", colors["_RimColor0"]);
            yield return ("_OutlineColor0", colors["_OutlineColor0"]);

            yield return ("_RimShadowOffset", colors["_RimShadowOffset"]);
            yield return ("_RimShadowColor0", colors["_RimShadowColor0"]);

            // Texture Scale Offset
            yield return ("_Maps_ST", colors["_MainMaps_ST"]);
        }
    }
}
