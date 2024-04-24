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
using UnityEngine.Scripting.APIUpdating;

namespace HSR.NPRShader.Editor.Automation
{
    [MovedFrom("HSR.NPRShader.Editor.Tools")]
    [ScriptedImporter(10, exts: new[] { "hsrmat" }, overrideExts: new[] { "json" })]
    public class MaterialJsonImporter : ScriptedImporter
    {
        [SerializeField] private string m_OverrideShaderName;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string text = File.ReadAllText(ctx.assetPath);
            JObject json = JObject.Parse(text);

            var matInfo = ScriptableObject.CreateInstance<MaterialJsonData>();
            matInfo.Name = GetMaterialName(json);
            matInfo.Shader = GetShaderName(json);
            matInfo.Textures = DictToEntries(ReadTextures(json, out matInfo.TexturesSkipCount));
            matInfo.Ints = DictToEntries(ReadValues<int>(json, "m_Ints", out matInfo.IntsSkipCount));
            matInfo.Floats = DictToEntries(ReadValues<float>(json, "m_Floats", out matInfo.FloatsSkipCount));
            matInfo.Colors = DictToEntries(ReadValues<Color>(json, "m_Colors", out matInfo.ColorsSkipCount));

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

            if (TryExecuteReaders(json["m_Shader"], out string shaderName, ReadShaderNameV1, ReadShaderNameV2))
            {
                return shaderName;
            }

            return string.Empty;
        }

        private static List<MaterialJsonData.Entry<T>> DictToEntries<T>(Dictionary<string, T> dict)
        {
            return dict.Select(kvp => new MaterialJsonData.Entry<T>
            {
                Key = kvp.Key,
                Value = kvp.Value
            }).OrderBy(entry => entry.Key).ToList();
        }

        private static Dictionary<string, TextureJsonData> ReadTextures(JObject json, out int skipCount)
        {
            return ReadValues(json, "m_TexEnvs", out skipCount,
                new Func<JToken, KeyValuePair<string, TextureJsonData>>[] { ReadTextureV1, ReadTextureV2 });
        }

        private static Dictionary<string, T> ReadValues<T>(JObject json, string propsName, out int skipCount)
        {
            return ReadValues(json, propsName, out skipCount,
                new Func<JToken, KeyValuePair<string, T>>[] { ReadValueV1<T>, ReadValueV2<T> });
        }

        private static Dictionary<string, T> ReadValues<T>(JObject json, string propsName, out int skipCount, Func<JToken, KeyValuePair<string, T>>[] readers)
        {
            Dictionary<string, T> results = new();
            skipCount = 0;

            foreach (var prop in json["m_SavedProperties"][propsName].Children())
            {
                if (TryExecuteReaders(prop, out KeyValuePair<string, T> entry, readers))
                {
                    results.Add(entry.Key, entry.Value);
                }
                else
                {
                    skipCount++;
                }
            }

            return results;
        }

        private static string ReadShaderNameV1(JToken prop)
        {
            string name = prop["Name"].ToObject<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException();
            }

            return name;
        }

        private static string ReadShaderNameV2(JToken prop)
        {
            return prop["m_PathID"].ToObject<long>() switch
            {
                -6550255339530601893 => "miHoYo/CRP_Character/Character Stencil Clear",
                -7016092255023927970 => "miHoYo/CRP_Character/CharacterBase",
                -5659630117084023487 => "miHoYo/CRP_Character/CharacterEyeShadow",
                -7682587881522096242 => "miHoYo/CRP_Character/CharacterFace",
                -8335713502764873453 => "miHoYo/CRP_Character/CharacterHair",
                -569915600545274916 => "miHoYo/CRP_Character/CharacterTransparent",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static KeyValuePair<string, TextureJsonData> ReadTextureV1(JToken prop)
        {
            string name = prop.First["m_Texture"]["Name"]?.ToObject<string>() ?? string.Empty;
            long pathId = prop.First["m_Texture"]["m_PathID"].ToObject<long>();
            bool isNull = prop.First["m_Texture"]["IsNull"].ToObject<bool>();
            Vector2 scale = prop.First["m_Scale"].ToObject<Vector2>();
            Vector2 offset = prop.First["m_Offset"].ToObject<Vector2>();

            ValidateTextureName(ref name, pathId, isNull);

            string label = prop.Path.Split('.')[^1];
            return new KeyValuePair<string, TextureJsonData>(label, new TextureJsonData(name, pathId, isNull, scale, offset));
        }

        private static KeyValuePair<string, TextureJsonData> ReadTextureV2(JToken prop)
        {
            string name = prop["Value"]["m_Texture"]["Name"]?.ToObject<string>() ?? string.Empty;
            long pathId = prop["Value"]["m_Texture"]["m_PathID"].ToObject<long>();
            bool isNull = prop["Value"]["m_Texture"]["IsNull"].ToObject<bool>();
            Vector2 scale = prop["Value"]["m_Scale"].ToObject<Vector2>();
            Vector2 offset = prop["Value"]["m_Offset"].ToObject<Vector2>();

            ValidateTextureName(ref name, pathId, isNull);

            string label = prop["Key"].ToObject<string>();
            return new KeyValuePair<string, TextureJsonData>(label, new TextureJsonData(name, pathId, isNull, scale, offset));
        }

        private static void ValidateTextureName(ref string name, long pathId, bool IsNullTexture)
        {
            if (!IsNullTexture && string.IsNullOrWhiteSpace(name))
            {
                name = s_TextureMap.GetValueOrDefault(pathId, name);
            }
        }

        private static KeyValuePair<string, T> ReadValueV1<T>(JToken prop)
        {
            string label = prop.Path.Split('.')[^1];
            T value = prop.First.ToObject<T>();
            return new KeyValuePair<string, T>(label, value);
        }

        private static KeyValuePair<string, T> ReadValueV2<T>(JToken prop)
        {
            string label = prop["Key"].ToObject<string>();
            T value = prop["Value"].ToObject<T>();
            return new KeyValuePair<string, T>(label, value);
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

        private static readonly Dictionary<long, string> s_TextureMap = new()
        {
            [752139084919133423] = "Avatar_Acheron_00_Body_Color_A_L",
            [-4313374206812397860] = "Avatar_Acheron_00_Body_Cool_Ramp",
            [2010736737923672304] = "Avatar_Acheron_00_Body_LightMap_L",
            [-8513798832380216520] = "Avatar_Acheron_00_Body_Warm_Ramp",
            [4039476340761124022] = "Avatar_Acheron_00_Face_Color",
            [-8522062242829977213] = "Avatar_Acheron_00_Hair_Color",
            [3061932055843903474] = "Avatar_Acheron_00_Hair_Cool_Ramp",
            [3969948169981267558] = "Avatar_Acheron_00_Hair_LightMap",
            [5311528504471976475] = "Avatar_Acheron_00_Hair_Warm_Ramp",
            [658105689580313835] = "Avatar_Acheron_00_Weapon_Color_A",
            [-5883828631030793743] = "Avatar_Acheron_00_Weapon_LightMap",
            [-3491631232450863659] = "Avatar_Acheron_00_Weapon_Warm_Ramp",
            [7488602963906160611] = "Avatar_Argenti_00_Body_Color_L",
            [611757061799462206] = "Avatar_Argenti_00_Body_Cool_Ramp",
            [3492759889527002899] = "Avatar_Argenti_00_Body_LightMap_L",
            [-1290575586761365056] = "Avatar_Argenti_00_Body_Warm_Ramp",
            [7843565357078513949] = "Avatar_Argenti_00_Effect_Color",
            [-6457320118828778844] = "Avatar_Argenti_00_Face_Color",
            [961098711806128838] = "Avatar_Argenti_00_Hair_Color",
            [-296295533577610819] = "Avatar_Argenti_00_Hair_Cool_Ramp",
            [-1097145778184561811] = "Avatar_Argenti_00_Hair_LightMap",
            [-1129641603689595278] = "Avatar_Argenti_00_Hair_Warm_Ramp",
            [7494814650949804789] = "Avatar_Argenti_00_Weapon_Color_A",
            [-7208280718334135732] = "Avatar_Argenti_00_Weapon_LightMap",
            [-3763939451674256378] = "Avatar_Arlan_00_Body1_Color_A",
            [4446860592853069554] = "Avatar_Arlan_00_Body1_LightMap",
            [-6761684249515194050] = "Avatar_Arlan_00_Body2_Color",
            [-3864857103167768887] = "Avatar_Arlan_00_Body2_LightMap",
            [-1959055843606036638] = "Avatar_Arlan_00_Cool_Ramp",
            [3871724782080146257] = "Avatar_Arlan_00_Face_Color",
            [-9062766659720324321] = "Avatar_Arlan_00_Hair_Color",
            [3405035535374398130] = "Avatar_Arlan_00_Hair_Cool_Ramp",
            [-166442640737510678] = "Avatar_Arlan_00_Hair_LightMap",
            [3675118986469821042] = "Avatar_Arlan_00_Hair_Warm_Ramp",
            [-3292307027845882641] = "Avatar_Arlan_00_Warm_Ramp",
            [6065087566918726468] = "Avatar_Arlan_00_Weapon_Color",
            [-4876140833928440751] = "Avatar_Arlan_00_Weapon_LightMap",
            [-1643318770467668006] = "Avatar_Asta_00_Body1_Color",
            [-8120317390147890696] = "Avatar_Asta_00_Body1_LightMap",
            [736906978680356368] = "Avatar_Asta_00_Body2_Color_A",
            [5527049121772680213] = "Avatar_Asta_00_Body2_LightMap",
            [-4184754957752818901] = "Avatar_Asta_00_Cool_Ramp",
            [-5457919005122227869] = "Avatar_Asta_00_Effect_Color",
            [-8022909458548160306] = "Avatar_Asta_00_Face_Color",
            [-7051025998882928111] = "Avatar_Asta_00_Hair_Color",
            [5855393562969211801] = "Avatar_Asta_00_Hair_Cool_Ramp",
            [9025336285171755141] = "Avatar_Asta_00_Hair_LightMap",
            [-4537991494913724170] = "Avatar_Asta_00_Hair_Warm_Ramp",
            [-7925723972496367427] = "Avatar_Asta_00_Warm_Ramp",
            [4606527878215071724] = "Avatar_Asta_00_Weapon_Color",
            [-5394264138204095] = "Avatar_Asta_00_Weapon_LigthMap",
            [-5544038412738525844] = "Avatar_Aventurine_00_Body_Color_A_L",
            [1319246591275098332] = "Avatar_Aventurine_00_Body_Cool_Ramp",
            [8739662921468591394] = "Avatar_Aventurine_00_Body_LightMap_L",
            [-7987746711609986026] = "Avatar_Aventurine_00_Body_Warm_Ramp",
            [1073152017336471089] = "Avatar_Aventurine_00_Face_Color",
            [1847546643573949230] = "Avatar_Aventurine_00_Hair_Color",
            [-1667136176460891916] = "Avatar_Aventurine_00_Hair_Cool_Ramp",
            [-3181187929054307594] = "Avatar_Aventurine_00_Hair_LightMap",
            [-3975536267350712021] = "Avatar_Aventurine_00_Hair_Warm_Ramp",
            [7493574259059352986] = "Avatar_Aventurine_00_Weapon_Color",
            [-7960631417293661872] = "Avatar_Aventurine_00_Weapon_LightMap",
            [-1365665263598427124] = "Avatar_Aventurine_00_Weapon_Ramp",
            [2876768762235482377] = "Avatar_Bailu_00_Body1_Color",
            [551872960303545324] = "Avatar_Bailu_00_Body1_LightMap",
            [-1584194112628746798] = "Avatar_Bailu_00_Body2_Color",
            [-2129287734525861759] = "Avatar_Bailu_00_Body2_LightMap",
            [6492376933833913343] = "Avatar_Bailu_00_Body_Cool_Ramp",
            [4945827544498674046] = "Avatar_Bailu_00_Body_Warm_Ramp",
            [-1800468038000408461] = "Avatar_Bailu_00_Face_Color",
            [-5041039905289830313] = "Avatar_Bailu_00_Hair_Color",
            [761971759105351453] = "Avatar_Bailu_00_Hair_Cool_Ramp",
            [5773675435656211594] = "Avatar_Bailu_00_Hair_LightMap",
            [-5166700933939244221] = "Avatar_Bailu_00_Hair_Warm_Ramp",
            [2137890914313992781] = "Avatar_BlackSwan_00_Body_Color_L",
            [-5072107447199421088] = "Avatar_BlackSwan_00_Body_Cool_Ramp",
            [-5503892064790080015] = "Avatar_BlackSwan_00_Body_LightMap_L",
            [-1440701182196327045] = "Avatar_BlackSwan_00_Body_Stockings",
            [-7535544034105244137] = "Avatar_BlackSwan_00_Body_Warm_Ramp",
            [-3323414587620992203] = "Avatar_BlackSwan_00_Face_Color",
            [-993457006248438628] = "Avatar_BlackSwan_00_Hair_Color",
            [4646819823869362028] = "Avatar_BlackSwan_00_Hair_Cool_Ramp",
            [-2720848693849216685] = "Avatar_BlackSwan_00_Hair_LightMap",
            [834307085305834258] = "Avatar_BlackSwan_00_Hair_Warm_Ramp",
            [1380826874726895505] = "Avatar_BlackSwan_00_Hand_Color",
            [-527795332882361710] = "Avatar_BlackSwan_00_Hand_Effect_Mask",
            [3735760485779421546] = "Avatar_BlackSwan_00_Hand_LightMap",
            [1160930605142432092] = "Avatar_BlackSwan_00_Weapon_Color",
            [-6133855926391475826] = "Avatar_BlackSwan_00_Weapon_LightMap",
            [-6875563159197145702] = "Avatar_BlackSwan_00_Window_Color",
            [6018832322658576524] = "Avatar_BlackSwan_00_Window_LightMap",
            [5370278451869559689] = "Avatar_Bronya_00_Body1_Color",
            [-8081830546898782175] = "Avatar_Bronya_00_Body1_LightMap",
            [-7062301532559311598] = "Avatar_Bronya_00_Body1_Stockings",
            [1167717075941442460] = "Avatar_Bronya_00_Body2_Color",
            [-2580725813317941429] = "Avatar_Bronya_00_Body2_LightMap",
            [4763692274692550601] = "Avatar_Bronya_00_Body2_Stockings",
            [-2887060029699278074] = "Avatar_Bronya_00_Body_Cool_Ramp",
            [5062587298771747566] = "Avatar_Bronya_00_Body_Warm_Ramp",
            [-3582459545573805732] = "Avatar_Bronya_00_Face_Color",
            [-6620896971116698570] = "Avatar_Bronya_00_Hair_Color",
            [7253430124776449665] = "Avatar_Bronya_00_Hair_Cool_Ramp",
            [-3829766250831243008] = "Avatar_Bronya_00_Hair_LightMap",
            [-1455993691711857725] = "Avatar_Bronya_00_Hair_Warm_Ramp",
            [4498716783225854944] = "Avatar_Bronya_00_Weapon_Color_A",
            [1470775604502036832] = "Avatar_Bronya_00_Weapon_LightMap",
            [3892974075019430566] = "Avatar_Cocolia_00_Body2_Stockings",
            [8553376127294344289] = "Avatar_Cocolia_00_Face_Color",
            [8398965005286363660] = "Avatar_DanHengIL_00_Body_Color_A_L",
            [-5697757468457240714] = "Avatar_DanHengIL_00_Body_Cool_Ramp",
            [2619988560051557321] = "Avatar_DanHengIL_00_Body_Lightmap_L",
            [184474832033623985] = "Avatar_DanHengIL_00_Body_Warm_Ramp",
            [-632366641231032874] = "Avatar_DanHengIL_00_Dragon_Color",
            [-1805280458551245206] = "Avatar_DanHengIL_00_Dragon_ColorPalette",
            [7249475716881389548] = "Avatar_DanHengIL_00_Dragon_Effect",
            [3619185015080228639] = "Avatar_DanHengIL_00_Dragon_Ramp",
            [5175008792922541101] = "Avatar_DanHengIL_00_Dragon_Stars",
            [8147194505924799659] = "Avatar_DanHengIL_00_Face_Color",
            [7236148699622500936] = "Avatar_DanHengIL_00_Hair_Color_A",
            [-5931583755410112990] = "Avatar_DanHengIL_00_Hair_Cool_Ramp",
            [4561110398705921651] = "Avatar_DanHengIL_00_Hair_Warm_Ramp",
            [-7056121446911870143] = "Avatar_DanHengIL_00_Hair_lightmap",
            [-2228667770255919308] = "Avatar_DanHengIL_00_Weapon_Color_A",
            [8126689304594013438] = "Avatar_DanHengIL_00_Weapon_LightMap",
            [2914736551629825697] = "Avatar_DanHengIL_00_Weapon_Ramp",
            [3499084691643782313] = "Avatar_DanHeng_00_Body_Color",
            [-6223091992781427379] = "Avatar_DanHeng_00_Body_Cool_Ramp",
            [-2005317220911649083] = "Avatar_DanHeng_00_Body_Lightmap",
            [8165212426668067423] = "Avatar_DanHeng_00_Body_Warm_Ramp",
            [522352929035400596] = "Avatar_DanHeng_00_Effect_Color",
            [7836442526467636791] = "Avatar_DanHeng_00_Face_Color",
            [-8454221100210709393] = "Avatar_DanHeng_00_Hair_Color",
            [7648954705722063081] = "Avatar_DanHeng_00_Hair_Cool_Ramp",
            [-8521367394918834228] = "Avatar_DanHeng_00_Hair_Warm_Ramp",
            [8250559274822704062] = "Avatar_DanHeng_00_Hair_lightmap",
            [-8827462607040358117] = "Avatar_DanHeng_00_Weapon_Color_A",
            [2266208227486436006] = "Avatar_DanHeng_00_Weapon_LightMap",
            [8880592106059966252] = "Avatar_DanHeng_00_Weapon_Ramp",
            [-3192889077811327630] = "Avatar_Dr_Ratio_00_Body_Color_L",
            [8893669577385705595] = "Avatar_Dr_Ratio_00_Body_Cool_Ramp",
            [4286440132614311448] = "Avatar_Dr_Ratio_00_Body_LightMap_L",
            [6440757246084435987] = "Avatar_Dr_Ratio_00_Body_Warm_Ramp",
            [185314499163456618] = "Avatar_Dr_Ratio_00_Effect_Color",
            [5219068109801966455] = "Avatar_Dr_Ratio_00_Effect_LightMap",
            [-4259574426702723974] = "Avatar_Dr_Ratio_00_Face_Color",
            [8350012704047127900] = "Avatar_Dr_Ratio_00_Hair_Color",
            [-4075368039419829732] = "Avatar_Dr_Ratio_00_Hair_Cool_Ramp",
            [-1697096187501256813] = "Avatar_Dr_Ratio_00_Hair_LightMap",
            [-4556177940762400839] = "Avatar_Dr_Ratio_00_Hair_Warm_Ramp",
            [8579823166119325511] = "Avatar_Dr_Ratio_00_Weapon_Color",
            [-5220741573027062158] = "Avatar_Dr_Ratio_00_Weapon_LightMap",
            [7803326561367671373] = "Avatar_Dr_Ratio_01_Body_Color",
            [6597923813425848499] = "Avatar_Dr_Ratio_01_Body_LightMap",
            [2787751111692848840] = "Avatar_Firefly_00_Body_Color_A_L",
            [7668746644183393589] = "Avatar_Firefly_00_Body_Cool_Ramp",
            [1918834053328011198] = "Avatar_Firefly_00_Body_LightMap_L",
            [-6357700780602754279] = "Avatar_Firefly_00_Body_Warm_Ramp",
            [-2959941426704617589] = "Avatar_Firefly_00_Face_Color",
            [-3297296406107807005] = "Avatar_Firefly_00_Hair_Color",
            [-704901334552675246] = "Avatar_Firefly_00_Hair_Cool_Ramp",
            [3074177499865404465] = "Avatar_Firefly_00_Hair_LightMap",
            [-7384422247375425488] = "Avatar_Firefly_00_Hair_Warm_Ramp",
            [-6383967813543309099] = "Avatar_FuXuan_00_Body1_Color",
            [-447221637994043682] = "Avatar_FuXuan_00_Body1_LightMap",
            [3127860506341031630] = "Avatar_FuXuan_00_Body2_Color",
            [-7362780123219834577] = "Avatar_FuXuan_00_Body2_LightMap",
            [-2636705699903521869] = "Avatar_FuXuan_00_Body_Stockings",
            [-8597779923115231629] = "Avatar_FuXuan_00_Cool_Ramp",
            [700827436584168858] = "Avatar_FuXuan_00_Face_Color",
            [3675319825208023342] = "Avatar_FuXuan_00_Hair_Color",
            [2255483551384403156] = "Avatar_FuXuan_00_Hair_Cool_Ramp",
            [-394202119940328701] = "Avatar_FuXuan_00_Hair_LightMap",
            [6794838997406254804] = "Avatar_FuXuan_00_Hair_Warm_Ramp",
            [320265798764662592] = "Avatar_FuXuan_00_Warm_Ramp",
            [-3255935199880095072] = "Avatar_FuXuan_00_Weapon_Color",
            [2166230152215315468] = "Avatar_FuXuan_00_Weapon_Lightmap",
            [-537619016593596537] = "Avatar_Gallagher_00_Body_Color_L",
            [1502368844612007745] = "Avatar_Gallagher_00_Body_Cool_Ramp",
            [3838071222048646900] = "Avatar_Gallagher_00_Body_LightMap_L",
            [1361619145148489510] = "Avatar_Gallagher_00_Body_Warm_Ramp",
            [-589307456780002648] = "Avatar_Gallagher_00_Face_Color",
            [-4201890693790182452] = "Avatar_Gallagher_00_Hair_Color",
            [7101516115703719898] = "Avatar_Gallagher_00_Hair_Cool_Ramp",
            [-5297743632519688236] = "Avatar_Gallagher_00_Hair_LightMap",
            [709303383575031394] = "Avatar_Gallagher_00_Hair_Warm_Ramp",
            [-3192857815014655435] = "Avatar_Gallagher_00_WeaponGT_Color",
            [6570574473213016953] = "Avatar_Gallagher_00_WeaponGT_LigthMap",
            [-6779047795124297547] = "Avatar_Gallagher_00_Weapon_Color",
            [5413238298639760874] = "Avatar_Gallagher_00_Weapon_LightMap",
            [-1426044696201472792] = "Avatar_Gepard_00_Body1_Color",
            [5195391704765482858] = "Avatar_Gepard_00_Body1_LightMap",
            [-106966508648745643] = "Avatar_Gepard_00_Body2_Color",
            [-7798091774237825967] = "Avatar_Gepard_00_Body2_LightMap",
            [3157969146340490859] = "Avatar_Gepard_00_Body_Cool_Ramp",
            [1667486243541414629] = "Avatar_Gepard_00_Body_Warm_Ramp",
            [5329875027371323493] = "Avatar_Gepard_00_Face_Color",
            [-3172638180833424548] = "Avatar_Gepard_00_Hair_Color",
            [7789733191541313573] = "Avatar_Gepard_00_Hair_Cool_Ramp",
            [-6279169235714348720] = "Avatar_Gepard_00_Hair_LightMap",
            [5721455698892273227] = "Avatar_Gepard_00_Hair_Warm_Ramp",
            [-1253057295744349041] = "Avatar_Gepard_00_Weapon_Color_A",
            [-3132383179511114950] = "Avatar_Gepard_00_Weapon_LightMap",
            [-5758049644295535623] = "Avatar_Gepard_00_Weapon_Ramp",
            [-6730245200701386539] = "Avatar_Guinaifen_00_Body1_Color",
            [-2783783009439614023] = "Avatar_Guinaifen_00_Body1_LightMap",
            [1386352900445789127] = "Avatar_Guinaifen_00_Body2_Color",
            [4318487761937553731] = "Avatar_Guinaifen_00_Body2_LightMap",
            [-3399890716220037922] = "Avatar_Guinaifen_00_Body_Cool_Ramp",
            [-4118207527673218469] = "Avatar_Guinaifen_00_Body_Stockings",
            [3611848557257830193] = "Avatar_Guinaifen_00_Body_Warm_Ramp",
            [8764128837865262842] = "Avatar_Guinaifen_00_Face_Color",
            [-5799201127583337374] = "Avatar_Guinaifen_00_Hair_Color",
            [3041194150421315560] = "Avatar_Guinaifen_00_Hair_Cool_Ramp",
            [6409516182966212805] = "Avatar_Guinaifen_00_Hair_LightMap",
            [-310003786807407404] = "Avatar_Guinaifen_00_Hair_Warm_Ramp",
            [-1906500482394690428] = "Avatar_Guinaifen_00_Weapon_Color_A",
            [578645049322750671] = "Avatar_Guinaifen_00_Weapon_LightMap",
            [-2005750120827496363] = "Avatar_Guinaifen_00_Weapon_Ramp",
            [1752177422310782515] = "Avatar_Hanya_00_Body1_Color",
            [3082972133835032828] = "Avatar_Hanya_00_Body1_LightMap",
            [428242624198414624] = "Avatar_Hanya_00_Body1_Stockings",
            [9098504940577671638] = "Avatar_Hanya_00_Body2_Color",
            [7649448538942871032] = "Avatar_Hanya_00_Body2_LightMap",
            [-4626922469381072250] = "Avatar_Hanya_00_Body2_Stockings",
            [-8044057635744349765] = "Avatar_Hanya_00_Cool_Ramp",
            [7534881953854322246] = "Avatar_Hanya_00_Face_Color",
            [-7779632983285828526] = "Avatar_Hanya_00_Hair_Color",
            [-2357562552661614936] = "Avatar_Hanya_00_Hair_Cool_Ramp",
            [3769708874578668273] = "Avatar_Hanya_00_Hair_LightMap",
            [6815832501427211699] = "Avatar_Hanya_00_Hair_Warm_Ramp",
            [4072333168358818856] = "Avatar_Hanya_00_Warm_Ramp",
            [-358237632663260651] = "Avatar_Hanya_00_Weapon_Color_A",
            [589323615638873430] = "Avatar_Hanya_00_Weapon_LightMap",
            [-1422311714874032766] = "Avatar_Herta_00_Body_Color_A",
            [3778404680101236109] = "Avatar_Herta_00_Body_Cool_Ramp",
            [-4381913982273783806] = "Avatar_Herta_00_Body_LightMap",
            [-5544155463081094551] = "Avatar_Herta_00_Body_Warm_Ramp",
            [250132987575774932] = "Avatar_Herta_00_Effect_Color",
            [8380468935325961990] = "Avatar_Herta_00_Effect_Color_02",
            [2624977135279047588] = "Avatar_Herta_00_Effect_Color_05",
            [-2415054801972395653] = "Avatar_Herta_00_Face_Color",
            [8175790722119741597] = "Avatar_Herta_00_Hair_Color",
            [8789443084651973993] = "Avatar_Herta_00_Hair_Cool_Ramp",
            [-4231171025600662865] = "Avatar_Herta_00_Hair_LightMap",
            [-4973038011536773211] = "Avatar_Herta_00_Hair_Warm_Ramp",
            [6732607762465134877] = "Avatar_Herta_00_Weapon_Color",
            [-6964163605976000543] = "Avatar_Herta_00_Weapon_LightMap",
            [891135346967264543] = "Avatar_Herta_00_Weapon_ParallaxMap",
            [-9001145787757413827] = "Avatar_Himeko_00_Body1_Color",
            [-4262753468713807999] = "Avatar_Himeko_00_Body1_Lightmap",
            [-2529664788602772411] = "Avatar_Himeko_00_Body2_Color",
            [-8277314614553107246] = "Avatar_Himeko_00_Body2_Lightmap",
            [2212075837306860720] = "Avatar_Himeko_00_Body2_Mask",
            [3083462932577500956] = "Avatar_Himeko_00_Body_Cool_Ramp",
            [-4849435412096482967] = "Avatar_Himeko_00_Body_Warm_Ramp",
            [-1029458824110116349] = "Avatar_Himeko_00_Book_Color",
            [5139436219553056101] = "Avatar_Himeko_00_Cup_Color",
            [4237279936394588904] = "Avatar_Himeko_00_Face_Color",
            [-3993805266674300479] = "Avatar_Himeko_00_Hair_Color",
            [6796691295031579587] = "Avatar_Himeko_00_Hair_Cool_Ramp",
            [-4334584427942647951] = "Avatar_Himeko_00_Hair_LightMap",
            [2685286548606056465] = "Avatar_Himeko_00_Hair_Warm_Ramp",
            [-5778298410304019012] = "Avatar_Himeko_00_Mirror_Color",
            [-2176253253816878507] = "Avatar_Himeko_00_RailgunRamp",
            [-7002112842148849897] = "Avatar_Himeko_00_Railgun_Color_A",
            [-4794529234385757967] = "Avatar_Himeko_00_Railgun_Color_A2",
            [-6333768192572026252] = "Avatar_Himeko_00_Railgun_Lightmap",
            [7820288281911880524] = "Avatar_Himeko_00_Spoon_Color",
            [-2020383129401741452] = "Avatar_Himeko_00_Weapon_Color",
            [-8532875547241175690] = "Avatar_Himeko_00_Weapon_Lightmap",
            [862092503939628653] = "Avatar_Himeko_Weapon_Ramp",
            [-3739897124168315253] = "Avatar_Hook_00_Body_Color",
            [3606786211636489557] = "Avatar_Hook_00_Body_Cool_Ramp",
            [-2724698222303909564] = "Avatar_Hook_00_Body_LightMap",
            [5938617257612685707] = "Avatar_Hook_00_Body_Warm_Ramp",
            [1567830588888885299] = "Avatar_Hook_00_Face_Color",
            [-4975997729076593883] = "Avatar_Hook_00_Hair_Color",
            [2765418058872843047] = "Avatar_Hook_00_Hair_Cool_Ramp",
            [892461570355145811] = "Avatar_Hook_00_Hair_LightMap",
            [-7751709459370249547] = "Avatar_Hook_00_Hair_Warm_Ramp",
            [3045229258946511444] = "Avatar_Hook_00_Weapon_Color",
            [-8319806022034657581] = "Avatar_Hook_00_Weapon_LightMap",
            [2634834998996228605] = "Avatar_Huohuo_00_Body_Color_A_L",
            [5084870827160494665] = "Avatar_Huohuo_00_Body_Cool_Ramp",
            [6414699519919059938] = "Avatar_Huohuo_00_Body_LightMap_L",
            [2070900998961403053] = "Avatar_Huohuo_00_Body_Warm_Ramp",
            [8524280642897111725] = "Avatar_Huohuo_00_Effect",
            [6733708476268713628] = "Avatar_Huohuo_00_Face_Color",
            [-8266555882604575697] = "Avatar_Huohuo_00_Hair_Color",
            [-1795826999158101208] = "Avatar_Huohuo_00_Hair_Cool_Ramp",
            [241720144341966554] = "Avatar_Huohuo_00_Hair_LightMap",
            [4251400501002675766] = "Avatar_Huohuo_00_Hair_Warm_Ramp",
            [733941240779302462] = "Avatar_Huohuo_00_Weapon_Color",
            [8920866698775457405] = "Avatar_Huohuo_00_Weapon_LightMap",
            [-5312744429843992328] = "Avatar_Huohuo_00_Weapon_Ramp",
            [-2964833458194334377] = "Avatar_JingYuan_00_Body1_Color",
            [5578414831070572787] = "Avatar_JingYuan_00_Body1_LightMap",
            [3276423381090282055] = "Avatar_JingYuan_00_Body2_Color",
            [856424652098158773] = "Avatar_JingYuan_00_Body2_LightMap",
            [1844363120324597154] = "Avatar_JingYuan_00_Body_Cool_Ramp",
            [1088503319549794898] = "Avatar_JingYuan_00_Body_Warm_Ramp",
            [5248841654806636372] = "Avatar_JingYuan_00_Effect_Color",
            [-4688075751835819294] = "Avatar_JingYuan_00_Effect_LightMap",
            [5695397920653982447] = "Avatar_JingYuan_00_Effect_Ramp",
            [-713582720746835357] = "Avatar_JingYuan_00_Face_Color",
            [-2948082658169263856] = "Avatar_JingYuan_00_Hair_Color",
            [5216268035327002055] = "Avatar_JingYuan_00_Hair_Cool_Ramp",
            [-4192081548772540829] = "Avatar_JingYuan_00_Hair_LightMap",
            [-3859571449723824554] = "Avatar_JingYuan_00_Hair_Warm_Ramp",
            [-6406431064606270196] = "Avatar_JingYuan_00_Spirit_Color",
            [4680589536005261894] = "Avatar_JingYuan_00_Spirit_Ramp",
            [3132430551193619483] = "Avatar_JingYuan_00_Weapon_Color",
            [-5828220837259476233] = "Avatar_JingYuan_00_Weapon_LightMap",
            [3062249815683546958] = "Avatar_JingYuan_00_Weapon_Ramp",
            [-4382536120313606423] = "Avatar_Jingliu_00_Body_Color_L",
            [6319739122788697506] = "Avatar_Jingliu_00_Body_Cool_Ramp",
            [2913414123612114584] = "Avatar_Jingliu_00_Body_LightMap_L",
            [3782369575341041408] = "Avatar_Jingliu_00_Body_Warm_Ramp",
            [2422381559191983086] = "Avatar_Jingliu_00_Face_Color",
            [2276720683103118602] = "Avatar_Jingliu_00_Hair_Color",
            [-2638098074941075471] = "Avatar_Jingliu_00_Hair_Cool_Ramp",
            [7189297899375470983] = "Avatar_Jingliu_00_Hair_LightMap",
            [-2861358354594732566] = "Avatar_Jingliu_00_Hair_Warm_Ramp",
            [3538793619505598655] = "Avatar_Jingliu_00_Weapon_Color",
            [227290234160639042] = "Avatar_Jingliu_00_Weapon_LightMap",
            [8117829902802560794] = "Avatar_Jingliu_00_Weapon_Weapon_Ramp",
            [2689829367772038873] = "Avatar_Kafka_00_Body1_Color_A",
            [8488891475362533859] = "Avatar_Kafka_00_Body1_LightMap",
            [1854839510634475002] = "Avatar_Kafka_00_Body2_Color",
            [-8436955906228540796] = "Avatar_Kafka_00_Body2_LightMap",
            [-5422809015907608353] = "Avatar_Kafka_00_Body_Cool_Ramp",
            [7467321469221535162] = "Avatar_Kafka_00_Body_Stockings",
            [-2582163183270284505] = "Avatar_Kafka_00_Body_Warm_Ramp",
            [641898764047444970] = "Avatar_Kafka_00_Face_Color",
            [-775911140979561048] = "Avatar_Kafka_00_Grenade_Color_A",
            [2473870386076547417] = "Avatar_Kafka_00_Grenade_LightMap",
            [8942247433373147509] = "Avatar_Kafka_00_Grenade_Ramp",
            [7559548700598780297] = "Avatar_Kafka_00_Hair_Color",
            [-7188023120016181427] = "Avatar_Kafka_00_Hair_Cool_Ramp",
            [-844758840389274481] = "Avatar_Kafka_00_Hair_LightMap",
            [701593079424401538] = "Avatar_Kafka_00_Hair_Warm_Ramp",
            [7611025910446504050] = "Avatar_Kafka_00_Weapon_Color",
            [2161190057001407667] = "Avatar_Kafka_00_Weapon_LightMap",
            [5589626011158889929] = "Avatar_Kafka_00_Weapon_Ramp",
            [-1466345631573908214] = "Avatar_Kafka_01_Body1_Color",
            [8433055695962589269] = "Avatar_Kafka_01_Body1_LightMap",
            [-4144953308000701271] = "Avatar_Kafka_01_Body2_Color",
            [1439511702951434850] = "Avatar_Kafka_01_Body2_LightMap",
            [-8745111683508017130] = "Avatar_Kafka_01_Body_Cool_Ramp",
            [-6394749320829085060] = "Avatar_Kafka_01_Body_Stockings",
            [3590630938632864970] = "Avatar_Kafka_01_Body_Warm_Ramp",
            [8441784744652539447] = "Avatar_Kafka_01_Face_Color",
            [-6627300232250490782] = "Avatar_Kafka_01_Weapon_Color",
            [-3872024472253668969] = "Avatar_Kafka_01_Weapon_LightMap",
            [6765500545743880510] = "Avatar_Kafka_01_Weapon_Ramp",
            [2624706223798020831] = "Avatar_Klara_00_Body1_Color",
            [-8915965961269477108] = "Avatar_Klara_00_Body1_LightMap",
            [-8239859743375306710] = "Avatar_Klara_00_Body2_Color",
            [7612298201090471770] = "Avatar_Klara_00_Body2_LightMap",
            [5289999619474933165] = "Avatar_Klara_00_Body_Cool_Ramp",
            [-7675303775540127982] = "Avatar_Klara_00_Body_Warm_Ramp",
            [-5248967136250248893] = "Avatar_Klara_00_Face_Color",
            [-5127397107998492355] = "Avatar_Klara_00_Hair_Color",
            [4991246946971487470] = "Avatar_Klara_00_Hair_Cool_Ramp",
            [-3458690462673081821] = "Avatar_Klara_00_Hair_Lightmap",
            [-5618853132446738523] = "Avatar_Klara_00_Hair_Warm_Ramp",
            [1606144572326603640] = "Avatar_Luka_00_Body1_Color",
            [-1697335904666270315] = "Avatar_Luka_00_Body1_LightMap",
            [-3995622898265534614] = "Avatar_Luka_00_Body2_Color_A",
            [-3438836020591790985] = "Avatar_Luka_00_Body2_LightMap",
            [5076104324746406341] = "Avatar_Luka_00_Body_Cool_Ramp",
            [5650897162161605008] = "Avatar_Luka_00_Body_Warm_Ramp",
            [8176172753501236929] = "Avatar_Luka_00_Face_Color",
            [-8257802390271270927] = "Avatar_Luka_00_Hair_Color",
            [5267884725466097412] = "Avatar_Luka_00_Hair_Cool_Ramp",
            [-770924816898996616] = "Avatar_Luka_00_Hair_LightMap",
            [-5659838776892430653] = "Avatar_Luka_00_Hair_Warm_Ramp",
            [1224373964361302373] = "Avatar_Luka_00_Weapon_Color_A",
            [1513333844335750259] = "Avatar_Luka_00_Weapon_LightMap",
            [9149529148014942102] = "Avatar_Luocha_00_Body1_Color",
            [-7499575811148877626] = "Avatar_Luocha_00_Body1_LightMap",
            [-523636421359447569] = "Avatar_Luocha_00_Body2_Color",
            [5492568242440348465] = "Avatar_Luocha_00_Body2_LightMap",
            [8460665662272796032] = "Avatar_Luocha_00_Body_Cool_Ramp",
            [-997493982227272803] = "Avatar_Luocha_00_Body_Warm_Ramp",
            [-134696874221935557] = "Avatar_Luocha_00_Effect_Color",
            [2605659946875895557] = "Avatar_Luocha_00_Effect_HLightMap",
            [-1141764357764689034] = "Avatar_Luocha_00_Face_Color",
            [-6246533564865299535] = "Avatar_Luocha_00_Hair_Color",
            [-4777880211087170322] = "Avatar_Luocha_00_Hair_Cool_Ramp",
            [3353458607455464439] = "Avatar_Luocha_00_Hair_LightMap",
            [2984665615337082020] = "Avatar_Luocha_00_Hair_Warm_Ramp",
            [8314861987810852097] = "Avatar_Luocha_00_Weapon_Color",
            [-8262715519127025718] = "Avatar_Luocha_00_Weapon_LightMap",
            [6032853408224340781] = "Avatar_Luocha_00_Weapon_Ramp",
            [5883590120865391244] = "Avatar_Lynx_00_Body_Color",
            [3410094812733676314] = "Avatar_Lynx_00_Body_Cool_Ramp",
            [-7249157388929194871] = "Avatar_Lynx_00_Body_LightMap",
            [-3315833467921493845] = "Avatar_Lynx_00_Body_Warm_Ramp",
            [-645894414655172159] = "Avatar_Lynx_00_Effect_Color",
            [-5678745415648715122] = "Avatar_Lynx_00_Effect_LightMap",
            [-8068132975673669101] = "Avatar_Lynx_00_Face_Color",
            [-6894235086920118508] = "Avatar_Lynx_00_Hair_Color",
            [-1131183676081890454] = "Avatar_Lynx_00_Hair_Cool_Ramp",
            [-3360723657485478016] = "Avatar_Lynx_00_Hair_LightMap",
            [919612138880941734] = "Avatar_Lynx_00_Hair_Warm_Ramp",
            [-8303424575363135499] = "Avatar_Lynx_00_Weapon_Color_A",
            [-8752930664311857434] = "Avatar_Lynx_00_Weapon_Cool_Ramp",
            [3130042033182123411] = "Avatar_Lynx_00_Weapon_LightMap",
            [-3456071953915489557] = "Avatar_Lynx_00_Weapon_Warm_Ramp",
            [2927380234812174464] = "Avatar_Mar_7th_00_Body1_Color",
            [1890025118598073819] = "Avatar_Mar_7th_00_Body1_LightMap",
            [-4972643705052286944] = "Avatar_Mar_7th_00_Body2_Color",
            [-4013433636145120766] = "Avatar_Mar_7th_00_Body2_LightMap",
            [5480850476681388659] = "Avatar_Mar_7th_00_Body_Cool_Ramp",
            [-7156132043134184668] = "Avatar_Mar_7th_00_Body_Warm_Ramp",
            [7845424999579585118] = "Avatar_Mar_7th_00_Effect",
            [7467446543743866804] = "Avatar_Mar_7th_00_Effect1",
            [-7707402005304901277] = "Avatar_Mar_7th_00_Face_Color",
            [5325893543790594498] = "Avatar_Mar_7th_00_Hair_Color",
            [-396024932371916406] = "Avatar_Mar_7th_00_Hair_Cool_Ramp",
            [-3866950522219543851] = "Avatar_Mar_7th_00_Hair_LightMap",
            [6208750749359915030] = "Avatar_Mar_7th_00_Hair_Warm_Ramp",
            [-3744134305287805464] = "Avatar_Mar_7th_00_Weapon_Color",
            [-3093211538173187322] = "Avatar_Mar_7th_00_Weapon_LightMap",
            [7218042602679668685] = "Avatar_Mar_7th_01_Body_Color_L",
            [8152104537995991686] = "Avatar_Mar_7th_01_Body_Cool_Ramp",
            [-6866214736077251999] = "Avatar_Mar_7th_01_Body_LightMap_L",
            [-1139984921236366945] = "Avatar_Mar_7th_01_Body_Stockings",
            [-5643920183605853415] = "Avatar_Mar_7th_01_Body_Warm_Ramp",
            [-1231203928530640154] = "Avatar_Mar_7th_01_Hair_Color",
            [-1227100727360656707] = "Avatar_Mar_7th_01_Hair_Cool_Ramp",
            [5153693746022184541] = "Avatar_Mar_7th_01_Hair_LightMap",
            [1204730655076662271] = "Avatar_Mar_7th_01_Hair_Warm_Ramp",
            [-3320563409211616901] = "Avatar_Mar_7th_01_Weapon_Color",
            [8531388475514613052] = "Avatar_Misha_00_Body_Color_L",
            [2115108241594554101] = "Avatar_Misha_00_Body_Cool_Ramp",
            [-7457617016445059528] = "Avatar_Misha_00_Body_LightMap_L",
            [7701228215038396853] = "Avatar_Misha_00_Body_Warm_Ramp",
            [3407820091817138765] = "Avatar_Misha_00_Effect1_Color",
            [-8181784478647885631] = "Avatar_Misha_00_Effect2_Color",
            [-1701550063302824730] = "Avatar_Misha_00_Face_Color",
            [702942578728093547] = "Avatar_Misha_00_Hair_Color",
            [-424978743340348613] = "Avatar_Misha_00_Hair_Cool_Ramp",
            [-8787121746111987413] = "Avatar_Misha_00_Hair_LightMap",
            [4414231007793238145] = "Avatar_Misha_00_Hair_Warm_Ramp",
            [-8125543746607903390] = "Avatar_Misha_00_Weapon_Color",
            [-7345227786575091431] = "Avatar_Misha_00_Weapon_Cool_Ramp",
            [4191916481368047549] = "Avatar_Misha_00_Weapon_LightMap",
            [3216346032829290808] = "Avatar_Misha_00_Weapon_Warm_Ramp",
            [-7306262695622409722] = "Avatar_Natasha_00_Body1_Color",
            [-4903351067357311876] = "Avatar_Natasha_00_Body1_LightMap",
            [4913769176050199976] = "Avatar_Natasha_00_Body1_Stockings",
            [1660740536399272484] = "Avatar_Natasha_00_Body2_Color",
            [329319281222267431] = "Avatar_Natasha_00_Body2_LightMap",
            [4375855396300477926] = "Avatar_Natasha_00_Body2_Stockings",
            [-17988100392130736] = "Avatar_Natasha_00_Body_Cool_Ramp",
            [3557148112500024731] = "Avatar_Natasha_00_Body_Warm_Ramp",
            [-1332718805095498973] = "Avatar_Natasha_00_Face_Color",
            [1214047113350402668] = "Avatar_Natasha_00_Glass",
            [-1214494621110287196] = "Avatar_Natasha_00_Hair_Color",
            [-1200817423498851985] = "Avatar_Natasha_00_Hair_Cool_Ramp",
            [5252673702971727809] = "Avatar_Natasha_00_Hair_LightMap",
            [-1852255231419492398] = "Avatar_Natasha_00_Hair_Warm_Ramp",
            [-1222295254260545158] = "Avatar_Natasha_00_Weapon_Color",
            [1631015563282022801] = "Avatar_Natasha_00_Weapon_LightMap",
            [7945635116641729080] = "Avatar_Natasha_00_Weapon_Ramp",
            [1255345781108052619] = "Avatar_Pela_00_Body_Color",
            [8672982392198957639] = "Avatar_Pela_00_Body_Cool_Ramp",
            [-8428179258279151089] = "Avatar_Pela_00_Body_Lightmap",
            [8345593300131696863] = "Avatar_Pela_00_Body_Stockings",
            [-6359225426381684775] = "Avatar_Pela_00_Body_Warm_Ramp",
            [-1794498306558243078] = "Avatar_Pela_00_Face_Color",
            [-567506107546436378] = "Avatar_Pela_00_Hair_Color",
            [7838874178832922052] = "Avatar_Pela_00_Hair_Cool_Ramp",
            [3822291023978041754] = "Avatar_Pela_00_Hair_LightMap",
            [-7667061972620050288] = "Avatar_Pela_00_Hair_Warm_Ramp",
            [-5504564827047304525] = "Avatar_Pela_00_Weapon_Color",
            [411601619807693657] = "Avatar_Pela_00_Weapon_Lightmap",
            [8716514932342035109] = "Avatar_Pela_00_Weapon_Ramp",
            [-1953260577935033837] = "Avatar_Pela_00_Weapon_Screen_Color",
            [2191414845459912757] = "Avatar_PlayerBoy_00_Body_Color_A",
            [3969980787741218061] = "Avatar_PlayerBoy_00_Body_Cool_Ramp",
            [-4065649347861076961] = "Avatar_PlayerBoy_00_Body_LightMap",
            [-8526739221565031559] = "Avatar_PlayerBoy_00_Body_Warm_Ramp",
            [3473660383103673499] = "Avatar_PlayerBoy_00_Face_Color",
            [5203066521267957073] = "Avatar_PlayerBoy_00_Hair_Color",
            [-1869338519819931266] = "Avatar_PlayerBoy_00_Hair_Cool_Ramp",
            [-3169293532010947361] = "Avatar_PlayerBoy_00_Hair_LightMap",
            [5881779557384358832] = "Avatar_PlayerBoy_00_Hair_Warm_Ramp",
            [7806323049603344532] = "Avatar_PlayerBoy_00_Weapon_Color_A",
            [6216001429863971100] = "Avatar_PlayerBoy_00_Weapon_LightMap",
            [-6738353761222724227] = "Avatar_PlayerBoy_10_Weapon_Color",
            [-5778502633364493818] = "Avatar_PlayerBoy_10_Weapon_LightMap",
            [-7288913841004835051] = "Avatar_PlayerBoy_10_Weapon_ParallaxMap",
            [-868084227570951558] = "Avatar_PlayerBoy_10_Weapon_Ramp",
            [6732696616754327536] = "Avatar_PlayerGirl_00_Body_Color_A",
            [989454209843647545] = "Avatar_PlayerGirl_00_Body_Cool_Ramp",
            [1182070584156356408] = "Avatar_PlayerGirl_00_Body_LightMap",
            [8045976113467216798] = "Avatar_PlayerGirl_00_Body_Warm_Ramp",
            [-5723194571169457874] = "Avatar_PlayerGirl_00_Face_Color",
            [4161572309650763345] = "Avatar_PlayerGirl_00_Hair_Color",
            [1295232513999645183] = "Avatar_PlayerGirl_00_Hair_Cool_Ramp",
            [-3480522312857938916] = "Avatar_PlayerGirl_00_Hair_LightMap",
            [8654570870203554885] = "Avatar_PlayerGirl_00_Hair_Warm_Ramp",
            [-3061116263622507083] = "Avatar_Player_M_00_Hair_LightMap",
            [767792958791241797] = "Avatar_Qingque_00_Body1_Color",
            [4670604546162500991] = "Avatar_Qingque_00_Body1_LightMap",
            [6384893692946939846] = "Avatar_Qingque_00_Body2_Color",
            [-462434486730068066] = "Avatar_Qingque_00_Body2_LightMap",
            [2759597768590242969] = "Avatar_Qingque_00_Body_Cool_Ramp",
            [-4918345360637524965] = "Avatar_Qingque_00_Body_Warm_Ramp",
            [7071956691412653638] = "Avatar_Qingque_00_Face_Color",
            [4035488302831569894] = "Avatar_Qingque_00_Hair_Color",
            [4240841552054745289] = "Avatar_Qingque_00_Hair_Cool_Ramp",
            [8835691240992101795] = "Avatar_Qingque_00_Hair_LightMap",
            [-509286202827528820] = "Avatar_Qingque_00_Hair_Warm_Ramp",
            [-365844057130638247] = "Avatar_Qingque_00_Weapon_Color",
            [41880736888086760] = "Avatar_Qingque_00_Weapon_LightMap ",
            [-7956617481315507039] = "Avatar_Qingque_00_Weapon_Ramp",
            [-7910594898138596254] = "Avatar_Ren_00_Body1_Color",
            [1634060994498685217] = "Avatar_Ren_00_Body1_LightMap",
            [-7935891602556425320] = "Avatar_Ren_00_Body2_Color",
            [2117937104722561374] = "Avatar_Ren_00_Body2_LightMap",
            [-2778945696501294180] = "Avatar_Ren_00_Body_Cool_Ramp",
            [-3337489456496892741] = "Avatar_Ren_00_Body_Warm_Ramp",
            [-7517515924503056526] = "Avatar_Ren_00_Effect_Color",
            [6010470694635004132] = "Avatar_Ren_00_Effect_LightMap",
            [-4898700700934960820] = "Avatar_Ren_00_Face_Color",
            [3985477323860137459] = "Avatar_Ren_00_Hair_Color",
            [-6196448582671190611] = "Avatar_Ren_00_Hair_Cool_Ramp",
            [-1967538258179371265] = "Avatar_Ren_00_Hair_LightMap",
            [1998919274625907326] = "Avatar_Ren_00_Hair_Warm_Ramp",
            [-7496483080282315121] = "Avatar_Ren_00_Weapon_Color",
            [-1788766588268655790] = "Avatar_Ren_00_Weapon_LightMap",
            [-7865138408139347054] = "Avatar_Ren_00_Weapon_Ramp",
            [-2957857997731719567] = "Avatar_Robin_00_Body_Color_A_L",
            [657864676713100252] = "Avatar_Robin_00_Body_Cool_Ramp",
            [370836042432503556] = "Avatar_Robin_00_Body_LightMap_L",
            [6584742638131692441] = "Avatar_Robin_00_Body_Mask_L",
            [-8103377509333089589] = "Avatar_Robin_00_Body_Warm_Ramp",
            [5748738597639381261] = "Avatar_Robin_00_Face_Color",
            [1546770049481632087] = "Avatar_Robin_00_Hair_Color",
            [7005828961235391128] = "Avatar_Robin_00_Hair_Cool_Ramp",
            [1587404197941870871] = "Avatar_Robin_00_Hair_LightMap",
            [3432974089799486068] = "Avatar_Robin_00_Hair_Warm_Ramp",
            [-1843061347332859558] = "Avatar_Robin_00_Weapon_Color",
            [-377489318230309261] = "Avatar_Robin_00_Weapon_LightMap",
            [7412694358078220110] = "Avatar_RuanMei_00_Body_Color_L",
            [-6612554049605651082] = "Avatar_RuanMei_00_Body_Cool_Ramp",
            [-423797490594687719] = "Avatar_RuanMei_00_Body_LightMap_L",
            [-3014822050888821487] = "Avatar_RuanMei_00_Body_Warm_Ramp",
            [-468841830726738586] = "Avatar_RuanMei_00_Face_Color",
            [2666438775159079040] = "Avatar_RuanMei_00_Hair_Color",
            [-8294309347765895744] = "Avatar_RuanMei_00_Hair_Cool_Ramp",
            [5136435716192139452] = "Avatar_RuanMei_00_Hair_LightMap",
            [309943007573153879] = "Avatar_RuanMei_00_Hair_Warm_Ramp",
            [-2578345140013189509] = "Avatar_RuanMei_00_Weapon_Color",
            [-5108106732401297284] = "Avatar_RuanMei_00_Weapon_Lightmap",
            [1161312427176027195] = "Avatar_Sam_00_Body_Color_A_L",
            [2187340397453039893] = "Avatar_Sam_00_Body_LightMap_L",
            [368967202797640350] = "Avatar_Sam_00_Cool_Ramp",
            [1044174951185018140] = "Avatar_Sam_00_Warm_Ramp",
            [7229773681521510971] = "Avatar_Sam_00_Weapon_Color_A",
            [-8762952490799504826] = "Avatar_Sam_00_Weapon_LightMap",
            [1747853249844576068] = "Avatar_Sampo_00_Body1_Color",
            [-9020204991333433887] = "Avatar_Sampo_00_Body1_LightMap",
            [-1292595707699649941] = "Avatar_Sampo_00_Body2_Color",
            [-3740637714147382606] = "Avatar_Sampo_00_Body2_LightMap",
            [6693283988780727762] = "Avatar_Sampo_00_Body_Cool_Ramp",
            [-4743494213135815244] = "Avatar_Sampo_00_Body_Warm_Ramp",
            [4716050426519692020] = "Avatar_Sampo_00_Face_Color",
            [-1821590053369766604] = "Avatar_Sampo_00_Hair_Color",
            [-7121369269963040855] = "Avatar_Sampo_00_Hair_LightMap",
            [-6410227440156446754] = "Avatar_Sampo_00_Handbag_Color",
            [8189434295312968676] = "Avatar_Sampo_00_Handbag_LightMap",
            [7031686397512387122] = "Avatar_Sampo_00_Hangbag_Ramp",
            [6198570150528619629] = "Avatar_Sampo_00_Weapon_Color",
            [5094886977699629355] = "Avatar_Sampo_00_Weapon_LightMap",
            [4947359806909840015] = "Avatar_Sampo_Hair_Cool_Ramp",
            [5470140835150272034] = "Avatar_Sampo_Hair_Warm_Ramp",
            [-5308669208149136783] = "Avatar_Screwllum_00_Body1_Color_A",
            [2941736403912147879] = "Avatar_Screwllum_00_Body1_LightMap",
            [4146034183989232785] = "Avatar_Screwllum_00_Body2_Color",
            [-443390263827397026] = "Avatar_Screwllum_00_Body2_LightMap",
            [5891004496203485484] = "Avatar_Screwllum_00_Body3_Color_A",
            [-8956276238746474614] = "Avatar_Screwllum_00_Body3_LightMap",
            [-3504946815022321237] = "Avatar_Screwllum_00_Body_Cool_Ramp",
            [-7641792589258485704] = "Avatar_Screwllum_00_Body_Warm_Ramp",
            [-3566374767527815609] = "Avatar_Screwllum_00_Spirit1_Color_A",
            [-5114294281991220070] = "Avatar_Screwllum_00_Spirit1_LightMap",
            [-1825761611504564950] = "Avatar_Screwllum_00_Spirit2_Color_A",
            [-1400369164856017131] = "Avatar_Screwllum_00_Spirit2_LightMap",
            [-4955171757113923752] = "Avatar_Screwllum_00_Spirit_Cool_Ramp",
            [-5615317319751492065] = "Avatar_Screwllum_00_Spirit_Warm_Ramp",
            [-1312150174316549667] = "Avatar_Screwllum_00_WeaponA_Color_A",
            [742284521987260700] = "Avatar_Screwllum_00_WeaponA_LigthMap",
            [-5205287232212253155] = "Avatar_Screwllum_00_WeaponA_Wings_Color_A",
            [-5773829805384540387] = "Avatar_Screwllum_00_WeaponA_Wings_LigthMap",
            [319366328984047833] = "Avatar_Seele_00_Body1_Color",
            [-3729713976234528809] = "Avatar_Seele_00_Body1_LightMap",
            [-3265304938569453921] = "Avatar_Seele_00_Body2_Color",
            [5200304582070208384] = "Avatar_Seele_00_Body2_LightMap",
            [6828022564347665373] = "Avatar_Seele_00_Body_Cool_Ramp",
            [-6529751733359567867] = "Avatar_Seele_00_Body_Warm_Ramp",
            [-6691424408086103264] = "Avatar_Seele_00_Face_Color",
            [-2320180385814660061] = "Avatar_Seele_00_Hair_Color",
            [8571174540655769604] = "Avatar_Seele_00_Hair_Cool_Ramp",
            [-8329852351025450553] = "Avatar_Seele_00_Hair_LightMap",
            [-8243921453019153612] = "Avatar_Seele_00_Hair_Warm_Ramp",
            [7041544404207684995] = "Avatar_Seele_00_Weapon_Color",
            [2462700623905524492] = "Avatar_Seele_00_Weapon_LightMap",
            [-6253594322361747662] = "Avatar_Seele_00_Weapon_Ramp",
            [3381238124171283001] = "Avatar_Serval_00_Body1_Color",
            [-6785824745364531707] = "Avatar_Serval_00_Body1_LightMap",
            [7906526135025211104] = "Avatar_Serval_00_Body2_Color",
            [7523472292256739842] = "Avatar_Serval_00_Body2_LightMap",
            [837900126238060180] = "Avatar_Serval_00_Body_Cool_Ramp",
            [-3977884435790292282] = "Avatar_Serval_00_Body_Stockings",
            [-7613075802976867957] = "Avatar_Serval_00_Body_Warm_Ramp",
            [-6176771831659437325] = "Avatar_Serval_00_Face_Color",
            [-8858754717432902110] = "Avatar_Serval_00_Hair_Color",
            [-402663817270627456] = "Avatar_Serval_00_Hair_Cool_Ramp",
            [8601774385907573415] = "Avatar_Serval_00_Hair_LightMap",
            [-7809448631276394569] = "Avatar_Serval_00_Hair_Warm_Ramp",
            [6383467504322376627] = "Avatar_Serval_00_Weapon_Color_A",
            [1445840693442768306] = "Avatar_Serval_00_Weapon_LightMap",
            [-3141943743101262982] = "Avatar_Serval_00_Weapon_Ramp",
            [178394968228004308] = "Avatar_Silwolf_00_Body_Color_A_L",
            [8625780019258475576] = "Avatar_Silwolf_00_Body_LightMap_L",
            [-8362054578842429532] = "Avatar_Silwolf_00_Cool_Ramp",
            [-552689082647765408] = "Avatar_Silwolf_00_Face_Color",
            [150490852975515143] = "Avatar_Silwolf_00_Hair_Color",
            [7209030860284238536] = "Avatar_Silwolf_00_Hair_Cool_Ramp",
            [6334288693390212602] = "Avatar_Silwolf_00_Hair_LightMap",
            [-165805517285836558] = "Avatar_Silwolf_00_Hair_Warm_Ramp",
            [7608427605774140286] = "Avatar_Silwolf_00_Warm_Ramp",
            [-8022921956441544467] = "Avatar_Silwolf_00_Weapon_Color",
            [1186644439773948156] = "Avatar_Silwolf_00_Weapon_LightMap",
            [8312624015610212176] = "Avatar_Silwolf_00_Weapon_Screen_color_A",
            [-5025449193646793123] = "Avatar_Silwolf_00_Weapon_Sword_color",
            [-4211577521017442000] = "Avatar_Sparkle_00_Body_Color_L",
            [8600635666345990859] = "Avatar_Sparkle_00_Body_Cool_Ramp",
            [-7602828026982034423] = "Avatar_Sparkle_00_Body_LightMap_L",
            [4333723729344673299] = "Avatar_Sparkle_00_Body_Warm_Ramp",
            [1358955121607352747] = "Avatar_Sparkle_00_Effect_Color",
            [954611614722801614] = "Avatar_Sparkle_00_Effect_LightMap",
            [-8258779258179040341] = "Avatar_Sparkle_00_Face_Color",
            [-8000698323545729372] = "Avatar_Sparkle_00_Glass_RangeTex",
            [5334731767742894224] = "Avatar_Sparkle_00_Hair_Color",
            [-5397138913529333694] = "Avatar_Sparkle_00_Hair_Cool_Ramp",
            [2946941655526448985] = "Avatar_Sparkle_00_Hair_LightMap",
            [-5580501344416326219] = "Avatar_Sparkle_00_Hair_Warm_Ramp",
            [2829943181116629798] = "Avatar_Sparkle_00_Kendama_Color",
            [7713403315227642773] = "Avatar_Sparkle_00_Kendama_LightMap",
            [-7307381767317619021] = "Avatar_Sunday_00_Body_Color_L",
            [-2921812312623505102] = "Avatar_Sunday_00_Body_Cool_Ramp",
            [-555829641247798723] = "Avatar_Sunday_00_Body_LightMap_L",
            [-8698842353801808686] = "Avatar_Sunday_00_Body_Warm_Ramp",
            [-6222651219170094502] = "Avatar_Sunday_00_Face_Color",
            [-6458697972040963599] = "Avatar_Sunday_00_Hair_Color",
            [217722477932318912] = "Avatar_Sunday_00_Hair_Cool_Ramp",
            [5886191532302600214] = "Avatar_Sunday_00_Hair_LightMap",
            [-3715035626165399724] = "Avatar_Sunday_00_Hair_Warm_Ramp",
            [-958249853227026840] = "Avatar_Sushang_00_Body1_Color",
            [4457400812067610654] = "Avatar_Sushang_00_Body1_LightMap",
            [-836516393638825364] = "Avatar_Sushang_00_Body2_Color",
            [-5576937373523369868] = "Avatar_Sushang_00_Body2_LightMap",
            [-5334407923996329811] = "Avatar_Sushang_00_Body_Cool_Ramp",
            [-7286156464376871592] = "Avatar_Sushang_00_Body_Warm_Ramp",
            [-6205428552392701701] = "Avatar_Sushang_00_Book_Color",
            [-5558634064302562937] = "Avatar_Sushang_00_Face_Color",
            [8736154177957316682] = "Avatar_Sushang_00_Hair_Color",
            [1099288385136631490] = "Avatar_Sushang_00_Hair_Cool_Ramp",
            [4821134726215940030] = "Avatar_Sushang_00_Hair_LightMap",
            [-2652521426714153698] = "Avatar_Sushang_00_Hair_Warm_Ramp",
            [-4807385714355212399] = "Avatar_Sushang_00_Spirit_Color_A",
            [3495134957463158592] = "Avatar_Sushang_00_Spirit_LightMap",
            [-1894728212651833074] = "Avatar_Sushang_00_Spirit_Ramp",
            [-4012274337688263443] = "Avatar_Sushang_00_Weapon_Color",
            [2781961884960013872] = "Avatar_Sushang_00_Weapon_LightMap",
            [1694860800215366159] = "Avatar_Sushang_00_Weapon_Ramp",
            [-2785923748046850030] = "Avatar_Svarog_00_Body1_Color",
            [-2546548514632954718] = "Avatar_Svarog_00_Body1_LightMap",
            [-542955821191957747] = "Avatar_Svarog_00_Body2_Color",
            [3439670869744226723] = "Avatar_Svarog_00_Body2_LightMap",
            [-724176835697385259] = "Avatar_Svarog_00_Body_Ramp",
            [-7067856736214376359] = "Avatar_Tingyun_00_Body1_Color",
            [3203835039731234175] = "Avatar_Tingyun_00_Body1_LightMap",
            [-8831021305736816546] = "Avatar_Tingyun_00_Body2_Color_A",
            [-9207376626769978775] = "Avatar_Tingyun_00_Body2_LightMap",
            [-2197996308815170429] = "Avatar_Tingyun_00_Cool_Ramp",
            [-4926307564472014012] = "Avatar_Tingyun_00_Face_Color",
            [-5392740496867572422] = "Avatar_Tingyun_00_Hair_Color",
            [-3836011553380555329] = "Avatar_Tingyun_00_Hair_Cool_Ramp",
            [-1403750619738301805] = "Avatar_Tingyun_00_Hair_LightMap",
            [-3093676768448304885] = "Avatar_Tingyun_00_Hair_Warm_Ramp",
            [7345548855900348012] = "Avatar_Tingyun_00_Warm_Ramp",
            [2857695667068725652] = "Avatar_Tingyun_00_Weapon_Color",
            [-1159232955286852965] = "Avatar_Tingyun_00_Weapon_LightMap",
            [4316717937205549668] = "Avatar_Topaz_00_Body1_Color_A",
            [3262548862003395102] = "Avatar_Topaz_00_Body1_LightMap",
            [-9164390295917599859] = "Avatar_Topaz_00_Body2_Color_A",
            [6266846793859753691] = "Avatar_Topaz_00_Body2_LightMap",
            [-6042763898178176711] = "Avatar_Topaz_00_Body_Cool_Ramp",
            [-5038904413404235706] = "Avatar_Topaz_00_Body_Warm_Ramp",
            [-4709106455926283376] = "Avatar_Topaz_00_Face_Color",
            [8064601998650894853] = "Avatar_Topaz_00_Hair_Color",
            [-1729388970024466166] = "Avatar_Topaz_00_Hair_Cool_Ramp",
            [-4136756468196621870] = "Avatar_Topaz_00_Hair_LightMap",
            [-2200331775633411553] = "Avatar_Topaz_00_Hair_Warm_Ramp",
            [3089897556325198021] = "Avatar_Topaz_00_Pig_Color",
            [-2415124714827664864] = "Avatar_Topaz_00_Pig_ColorPalette",
            [-3308864702944866108] = "Avatar_Topaz_00_Pig_Cool_Ramp",
            [8197902790740867764] = "Avatar_Topaz_00_Pig_Effect",
            [5145572987801109073] = "Avatar_Topaz_00_Pig_LightMap",
            [5109554898197032944] = "Avatar_Topaz_00_Pig_Mask",
            [7266438822333964900] = "Avatar_Topaz_00_Pig_Warm_Ramp",
            [3812913818547229237] = "Avatar_Topaz_00_Weapon_Color_A",
            [5188898135929805055] = "Avatar_Topaz_00_Weapon_LigthMap",
            [4034328876080192107] = "Avatar_Topaz_Idleshow_Tel_Color",
            [-3387901488645402078] = "Avatar_Welt_00_Body1_Color",
            [3941947509732144627] = "Avatar_Welt_00_Body1_LightMap",
            [9179994088680588753] = "Avatar_Welt_00_Body2_Color",
            [6989767607463722770] = "Avatar_Welt_00_Body2_LightMap",
            [-8555607834091983958] = "Avatar_Welt_00_Body_Cool_Ramp",
            [1645748239564507627] = "Avatar_Welt_00_Body_Warm_Ramp",
            [-288985660817892492] = "Avatar_Welt_00_Face_Color",
            [-5428648364442157950] = "Avatar_Welt_00_Hair_Color",
            [-3931839377596027294] = "Avatar_Welt_00_Hair_Cool_Ramp",
            [-7733194853835936684] = "Avatar_Welt_00_Hair_LightMap",
            [-8331550149242442208] = "Avatar_Welt_00_Hair_Warm_Ramp",
            [2017116667075775928] = "Avatar_Welt_00_Weapon_Color",
            [5619216313375195269] = "Avatar_Welt_00_Weapon_LightMap",
            [-8845757495141548712] = "Avatar_Welt_00_Weapon_Ramp",
            [7709396641771773890] = "Avatar_Xueyi_00_Body1_Color",
            [-102890911852355682] = "Avatar_Xueyi_00_Body1_LightMap",
            [-7276353580042948196] = "Avatar_Xueyi_00_Body2_Color",
            [-6531577360081871647] = "Avatar_Xueyi_00_Body2_LightMap",
            [2380199583554197597] = "Avatar_Xueyi_00_Body_Cool_Ramp",
            [-4947193282090325589] = "Avatar_Xueyi_00_Body_Warm_Ramp",
            [-4756734563444485486] = "Avatar_Xueyi_00_Face_Color",
            [2603095415881529845] = "Avatar_Xueyi_00_Hair_Color",
            [-4623740048643703604] = "Avatar_Xueyi_00_Hair_Cool_Ramp",
            [-2740880220565445905] = "Avatar_Xueyi_00_Hair_LightMap",
            [6580666138920383160] = "Avatar_Xueyi_00_Hair_Warm_Ramp",
            [-9222240360530537721] = "Avatar_Xueyi_00_Weapon_Color",
            [-200508845224868941] = "Avatar_Xueyi_00_Weapon_LightMap",
            [7059690437879790550] = "Avatar_Yanqing_00_Body_Color_L",
            [-2482782025723680546] = "Avatar_Yanqing_00_Body_Cool_Ramp",
            [-9088136073746272017] = "Avatar_Yanqing_00_Body_LightMap_L",
            [7919668230398076297] = "Avatar_Yanqing_00_Body_Warm_Ramp",
            [7683775542402411959] = "Avatar_Yanqing_00_Face_Color",
            [132008991024212235] = "Avatar_Yanqing_00_Hair_Color",
            [1774452639767914270] = "Avatar_Yanqing_00_Hair_Cool_Ramp",
            [-6862852481721035746] = "Avatar_Yanqing_00_Hair_LightMap",
            [7735591621499280157] = "Avatar_Yanqing_00_Hair_Warm_Ramp",
            [3441036270752106987] = "Avatar_Yanqing_00_Weapon_Color",
            [2895380683039841041] = "Avatar_Yanqing_00_Weapon_LightMap",
            [-2142435062819214059] = "Avatar_Yanqing_00_Weapon_Ramp",
            [-7421633214750477385] = "Avatar_Yanqing_00_Weapon_Warm_Ramp",
            [407487719360063435] = "Avatar_Yukong_00_Body_Color_A_L",
            [4350518135591702214] = "Avatar_Yukong_00_Body_Cool_Ramp",
            [8728961650477485357] = "Avatar_Yukong_00_Body_LightMap_L",
            [1573192828687161638] = "Avatar_Yukong_00_Body_Warm_Ramp",
            [8875749091929081902] = "Avatar_Yukong_00_Face_Color",
            [-7694876797354072368] = "Avatar_Yukong_00_Hair_Color",
            [-8697826385654726061] = "Avatar_Yukong_00_Hair_Cool_Ramp",
            [-1075604174878870902] = "Avatar_Yukong_00_Hair_LightMap",
            [1832483394170066903] = "Avatar_Yukong_00_Hair_Warm_Ramp",
            [1669959087607399444] = "Avatar_Yukong_00_Weapon_Color",
            [2445706761285279236] = "Avatar_Yukong_00_Weapon_LightMap",
            [-734716690419969237] = "M_150_Boy_FaceMap_00",
            [-6304992710105542959] = "M_150_Boy_Face_ExpressionMap_00",
            [4621519982094589888] = "M_170_Lad_FaceMap_00",
            [-878639406805420856] = "M_170_Lad_Face_ExpressionMap",
            [-5227184930583754250] = "M_180_Male_FaceMap_00",
            [-3322946495965774944] = "M_180_Male_Face_ExpressionMap_00",
            [-6019816189048073809] = "W_120_Kid_FaceMap_00",
            [-1804355598555400137] = "W_120_Kid_Face_ExpressionMap_00",
            [-9027926430380391551] = "W_140_Girl_FaceMap_00",
            [8606849378485051751] = "W_140_Girl_Face_ExpressionMap_00",
            [-8847959300088780513] = "W_160_Maid_FaceMap_00",
            [-7086236821322118962] = "W_160_Maid_FaceMap_01",
            [2790083752718748404] = "W_160_Maid_Face_ExpressionMap_00",
            [4136592782978141262] = "W_168_Miss_FaceMap_00",
            [-586935756902647176] = "W_168_Miss_Face_ExpressionMap_00",
            [-770129216927981794] = "W_170_Lady_FaceMap_00",
            [-8819751207019834062] = "W_170_Lady_Face_ExpressionMap_00",
            [-7810635243566772290] = "W_170_Lady_Face_ExpressionMap_01",
        };
    }
}
