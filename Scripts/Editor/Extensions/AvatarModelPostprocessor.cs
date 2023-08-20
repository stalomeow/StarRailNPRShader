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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using HSR.Utils;

namespace HSR.Editor.Extensions
{
    public class AvatarModelPostprocessor : AssetPostprocessor
    {
        public static readonly ModelImporterTangents ImportTangents = ModelImporterTangents.None;
        public static readonly NormalUtility.StoreMode NormalStoreMode = NormalUtility.StoreMode.ObjectSpaceTangent;
        public static readonly uint Version = 10u;

        private bool IsAvatarModel
        {
            get
            {
                string modelName = Path.GetFileNameWithoutExtension(assetPath);
                return Regex.IsMatch(modelName, @"^Avatar_.+_00$");
            }
        }

        private void OnPreprocessModel()
        {
            if (!IsAvatarModel)
            {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.importTangents = ImportTangents;
        }

        private void OnPostprocessModel(GameObject go)
        {
            if (!IsAvatarModel)
            {
                return;
            }

            List<GameObject> modifiedObjs = new();
            NormalUtility.SmoothAndStore(go, NormalStoreMode, false, modifiedObjs);
            string subObjList = string.Join('\n', modifiedObjs.Select(o => o.name));
            Debug.Log($"<b>[Smooth Normal]</b> {assetPath}\n" + subObjList);
        }

        public override uint GetVersion()
        {
            return Version;
        }
    }
}
