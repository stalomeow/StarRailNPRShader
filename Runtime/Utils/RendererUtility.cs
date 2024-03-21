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

#if !UNITY_EDITOR
#define NOT_UNITY_EDITOR
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace HSR.NPRShader.Utils
{
    public static class RendererUtility
    {
        public static void SetMaterialPropertiesPerRenderer(List<Renderer> renderers, Lazy<MaterialPropertyBlock> propertyBlock, List<(int, float)> floats, List<(int, Vector4)> vectors)
        {
            // SRPBatcher 不支持 MaterialPropertyBlock
            // 但是在 Editor 里不用 MaterialPropertyBlock 的话不好搞
            // 所以 Editor 里用 MaterialPropertyBlock，Build 之后用 Material

            SetPropertiesViaPropertyBlock(renderers, propertyBlock, floats, vectors);
            SetPropertiesViaMaterial(renderers, floats, vectors);
        }

        [Conditional("UNITY_EDITOR")]
        private static void SetPropertiesViaPropertyBlock(List<Renderer> renderers, Lazy<MaterialPropertyBlock> propertyBlock, List<(int, float)> floats, List<(int, Vector4)> vectors)
        {
            MaterialPropertyBlock properties = propertyBlock.Value;

            foreach (Renderer renderer in renderers)
            {
                renderer.GetPropertyBlock(properties);

                for (int i = 0; i < floats.Count; i++)
                {
                    properties.SetFloat(floats[i].Item1, floats[i].Item2);
                }

                for (int i = 0; i < vectors.Count; i++)
                {
                    properties.SetVector(vectors[i].Item1, vectors[i].Item2);
                }

                renderer.SetPropertyBlock(properties);
            }
        }

        [Conditional("NOT_UNITY_EDITOR")]
        private static void SetPropertiesViaMaterial(List<Renderer> renderers, List<(int, float)> floats, List<(int, Vector4)> vectors)
        {
            List<Material> materials = ListPool<Material>.Get();

            try
            {
                foreach (Renderer renderer in renderers)
                {
                    materials.Clear();
                    renderer.GetMaterials(materials);

                    foreach (var material in materials)
                    {
                        for (int i = 0; i < floats.Count; i++)
                        {
                            material.SetFloat(floats[i].Item1, floats[i].Item2);
                        }

                        for (int i = 0; i < vectors.Count; i++)
                        {
                            material.SetVector(vectors[i].Item1, vectors[i].Item2);
                        }
                    }
                }
            }
            finally
            {
                ListPool<Material>.Release(materials);
            }
        }
    }
}
