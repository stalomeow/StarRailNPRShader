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
using UnityEngine;
using UnityEngine.Rendering;

namespace HSR.NPRShader.Shadow
{
    public class ShadowRendererList
    {
        private readonly struct RendererEntry
        {
            public readonly Renderer Renderer;
            public readonly int DrawCallIndexStartInclusive;
            public readonly int DrawCallIndexEndExclusive;

            public RendererEntry(Renderer renderer, int drawCallIndexStartInclusive, int drawCallIndexEndExclusive)
            {
                Renderer = renderer;
                DrawCallIndexStartInclusive = drawCallIndexStartInclusive;
                DrawCallIndexEndExclusive = drawCallIndexEndExclusive;
            }
        }

        private readonly struct DrawCallData
        {
            public readonly Material Material;
            public readonly int SubmeshIndex;
            public readonly int ShaderPass;

            public DrawCallData(Material material, int submeshIndex, int shaderPass)
            {
                Material = material;
                SubmeshIndex = submeshIndex;
                ShaderPass = shaderPass;
            }
        }

        private readonly List<RendererEntry> m_Renderers = new();
        private readonly List<DrawCallData> m_DrawCalls = new();

        public bool TryGetWorldBounds(out Bounds worldBounds, List<int> outAppendRendererIndices = null)
        {
            worldBounds = default;
            bool firstBounds = true;

            for (int i = 0; i < m_Renderers.Count; i++)
            {
                RendererEntry entry = m_Renderers[i];

                if (!IsEntryEnabled(in entry))
                {
                    continue;
                }

                outAppendRendererIndices?.Add(i);

                if (firstBounds)
                {
                    worldBounds = entry.Renderer.bounds;
                    firstBounds = false;
                }
                else
                {
                    worldBounds.Encapsulate(entry.Renderer.bounds);
                }
            }

            return !firstBounds;
        }

        private static bool IsEntryEnabled(in RendererEntry entry)
        {
            if (entry.DrawCallIndexEndExclusive - entry.DrawCallIndexStartInclusive <= 0)
            {
                // 没有 draw call
                return false;
            }

            Renderer renderer = entry.Renderer;

#if UNITY_EDITOR
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(renderer.gameObject))
            {
                return false;
            }
#endif

            return renderer.enabled && renderer.gameObject.activeInHierarchy &&
                   renderer.shadowCastingMode != ShadowCastingMode.Off;
        }

        public void Draw(CommandBuffer cmd, List<int> rendererIndices, int startIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                RendererEntry entry = m_Renderers[rendererIndices[startIndex + i]];
                for (int j = entry.DrawCallIndexStartInclusive; j < entry.DrawCallIndexEndExclusive; j++)
                {
                    DrawCallData dc = m_DrawCalls[j];
                    cmd.DrawRenderer(entry.Renderer, dc.Material, dc.SubmeshIndex, dc.ShaderPass);
                }
            }
        }

        public void Clear()
        {
            m_Renderers.Clear();
            m_DrawCalls.Clear();
        }

        public void Add(Renderer renderer)
        {
            int drawCallInitialCount = m_DrawCalls.Count;
            List<Material> materialList = ListPool<Material>.Get();

            try
            {
                renderer.GetSharedMaterials(materialList);
                for (int i = 0; i < materialList.Count; i++)
                {
                    Material material = materialList[i];
                    if (TryGetShadowCasterPass(material, out int passIndex))
                    {
                        m_DrawCalls.Add(new DrawCallData(material, i, passIndex));
                    }
                }

                if (m_DrawCalls.Count > drawCallInitialCount)
                {
                    m_Renderers.Add(new RendererEntry(renderer, drawCallInitialCount, m_DrawCalls.Count));
                }
            }
            catch
            {
                m_DrawCalls.RemoveRange(drawCallInitialCount, m_DrawCalls.Count - drawCallInitialCount);
                throw;
            }
            finally
            {
                ListPool<Material>.Release(materialList);
            }
        }

        private static readonly Lazy<ShaderTagId> s_LightModeTagName =
            new(() => new ShaderTagId("LightMode"));

        private static readonly Lazy<ShaderTagId> s_ShadowCasterTagId =
            new(() => new ShaderTagId("HSRPerObjectShadowCaster"));

        private static bool TryGetShadowCasterPass(Material material, out int passIndex)
        {
            Shader shader = material.shader;

            for (int i = 0; i < shader.passCount; i++)
            {
                if (shader.FindPassTagValue(i, s_LightModeTagName.Value) == s_ShadowCasterTagId.Value)
                {
                    passIndex = i;
                    return true;
                }
            }

            passIndex = -1;
            return false;
        }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this);
        }

        public readonly struct ReadOnly
        {
            private readonly ShadowRendererList m_List;

            internal ReadOnly(ShadowRendererList list)
            {
                m_List = list;
            }

            public bool TryGetWorldBounds(out Bounds worldBounds, List<int> outAppendRendererIndices = null)
            {
                return m_List.TryGetWorldBounds(out worldBounds, outAppendRendererIndices);
            }

            public void Draw(CommandBuffer cmd, List<int> rendererIndices, int startIndex, int count)
            {
                m_List.Draw(cmd, rendererIndices, startIndex, count);
            }
        }
    }
}
