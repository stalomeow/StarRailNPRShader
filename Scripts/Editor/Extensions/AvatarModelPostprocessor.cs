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
