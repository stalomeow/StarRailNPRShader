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
    public class FaceMaterialSetter : BaseMaterialSetter
    {
        protected override IReadOnlyDictionary<string, string> SupportedShaderMap => new Dictionary<string, string>()
        {
            ["miHoYo/CRP_Character/CharacterFace"] = "Honkai Star Rail/Character/Face"
        };

        protected override IEnumerable<(string, TextureJsonData)> ApplyTextures(IReadOnlyDictionary<string, TextureJsonData> textures)
        {
            yield return MakeProperty("_MainTex", textures);
            yield return MakeProperty("_FaceMap", textures);
            yield return MakeProperty("_ExpressionMap", textures, "_FaceExpression");
        }

        protected override IEnumerable<(string, float)> ApplyFloats(IReadOnlyDictionary<string, float> floats)
        {
            yield return MakeProperty("_FaceMapUV2", floats, "_UseUVChannel2");

            yield return MakeProperty("_EmissionThreshold", floats);
            yield return MakeProperty("_EmissionIntensity", floats);

            yield return MakeProperty("_NoseLinePower", floats);

            yield return MakeProperty("_mmBloomIntensity0", floats, "_mBloomIntensity0");
        }

        protected override IEnumerable<(string, Color)> ApplyColors(IReadOnlyDictionary<string, Color> colors)
        {
            yield return MakeProperty("_Color", colors);
            yield return MakeProperty("_ShadowColor", colors);
            yield return MakeProperty("_EyeShadowColor", colors);
            yield return MakeProperty("_EmissionColor", Color.white); // 眼睛高亮
            yield return MakeProperty("_OutlineColor0", colors, "_OutlineColor");
            yield return MakeProperty("_NoseLineColor", colors);

            // Texture Scale Offset
            yield return MakeProperty("_Maps_ST", colors, "_MainMaps_ST");

            // Expression
            yield return MakeProperty("_ExCheekColor", colors);
            yield return MakeProperty("_ExShyColor", colors);
            yield return MakeProperty("_ExShadowColor", colors);
            yield return MakeProperty("_ExEyeColor", colors);
        }
    }
}
