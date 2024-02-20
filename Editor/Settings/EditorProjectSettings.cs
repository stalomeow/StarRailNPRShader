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
using UnityEngine;

namespace HSR.NPRShader.Editor.Settings
{
    [FilePath("ProjectSettings/StarRailNPRShader.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class EditorProjectSettings : ScriptableSingleton<EditorProjectSettings>
    {
        [Delayed] public string AvatarModelPathPattern = @"^Assets(/|\\)(.+(/|\\))?Avatar_[^/\\]+\.fbx$";
        [Delayed] public string RampTexturePathPattern = @"^Assets(/|\\)(.+(/|\\))?Avatar_[^/\\]+_Ramp[^/\\]+$";
        [Delayed] public string LightMapPathPattern = @"^Assets(/|\\)(.+(/|\\))?Avatar_[^/\\]+_LightMap[^/\\]+$";
        [Delayed] public string ColorTexturePathPattern = @"^Assets(/|\\)(.+(/|\\))?Avatar_[^/\\]+_Color[^/\\]+$";
        [Delayed] public string StockingsRangeMapPathPattern = @"^Assets(/|\\)(.+(/|\\))?Avatar_[^/\\]+_Stockings[^/\\]+$";
        [Delayed] public string FaceMapPathPattern = @"^Assets(/|\\)(.+(/|\\))?(M|W)_[0-9]+_[a-zA-Z]+_FaceMap[^/\\]+$";
        [Delayed] public string FaceExpressionMapPathPattern = @"^Assets(/|\\)(.+(/|\\))?(M|W)_[0-9]+_[a-zA-Z]+_Face_ExpressionMap[^/\\]+$";

        public uint AvatarModelPostprocessorVersion = 0u;
        public uint TexturePostprocessorVersion = 0u;

        private void EnsureEditable() => hideFlags &= ~HideFlags.NotEditable;

        private void OnEnable() => EnsureEditable();

        private void OnDisable() => Save();

        public void Save()
        {
            EnsureEditable();
            Save(true);
        }

        public SerializedObject AsSerializedObject()
        {
            EnsureEditable();
            return new SerializedObject(this);
        }

        public const string PathInProjectSettings = "Project/Honkai Star Rail/NPR Shader";

        public static void OpenInProjectSettings() => SettingsService.OpenProjectSettings(PathInProjectSettings);
    }
}
