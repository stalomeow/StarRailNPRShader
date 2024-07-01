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
using HSR.NPRShader.PerObjectShadow;
using HSR.NPRShader.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader.Passes
{
    public class PerObjectShadowCasterPass : ScriptableRenderPass, IDisposable
    {
        public const int MaxShadowCount = 16;

        private readonly Matrix4x4[] m_ShadowMatrixArray;
        private readonly Vector4[] m_ShadowMapRectArray;
        private readonly float[] m_ShadowCasterIdArray;
        private ShadowCasterManager m_CasterManager;
        private int m_TileResolution;
        private int m_ShadowMapSizeInTile; // 一行/一列有多少个 tile
        private RTHandle m_ShadowMap;

        public PerObjectShadowCasterPass(string profilerTag, RenderPassEvent evt)
        {
            renderPassEvent = evt;
            profilingSampler = new ProfilingSampler(profilerTag);

            m_ShadowMatrixArray = new Matrix4x4[MaxShadowCount];
            m_ShadowMapRectArray = new Vector4[MaxShadowCount];
            m_ShadowCasterIdArray = new float[MaxShadowCount];
        }

        public void Dispose()
        {
            m_ShadowMap?.Release();
        }

        public void Setup(ShadowCasterManager casterManager, ShadowTileResolution tileResolution, DepthBits depthBits)
        {
            m_CasterManager = casterManager;
            m_TileResolution = (int)tileResolution;

            if (casterManager.VisibleCount <= 0)
            {
                return;
            }

            // 保证 shadow map 是正方形
            m_ShadowMapSizeInTile = Mathf.CeilToInt(Mathf.Sqrt(casterManager.VisibleCount));
            int shadowRTSize = m_ShadowMapSizeInTile * m_TileResolution;
            int shadowRTDepthBits = Mathf.Max((int)depthBits, (int)DepthBits.Depth8);
            ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_ShadowMap, shadowRTSize, shadowRTSize, shadowRTDepthBits);

            ConfigureTarget(m_ShadowMap);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler))
            {
                if (m_CasterManager.VisibleCount > 0)
                {
                    RenderShadowMap(cmd, ref renderingData);
                    SetShadowSamplingData(cmd);

                    if (renderPassEvent > RenderPassEvent.AfterRenderingShadows)
                    {
                        ScriptableRenderer.SetCameraMatrices(cmd, ref renderingData.cameraData, true);
                    }
                }
                else
                {
                    cmd.SetGlobalInt(PropertyIds._PerObjShadowCount, 0);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderShadowMap(CommandBuffer cmd, ref RenderingData renderingData)
        {
            cmd.SetGlobalDepthBias(1.0f, 2.5f); // these values match HDRP defaults (see https://github.com/Unity-Technologies/Graphics/blob/9544b8ed2f98c62803d285096c91b44e9d8cbc47/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAtlas.cs#L197 )
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CastingPunctualLightShadow, false);

            for (int i = 0; i < m_CasterManager.VisibleCount; i++)
            {
                m_CasterManager.GetMatrices(i, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix);

                if (m_CasterManager.Usage == ShadowUsage.Scene)
                {
                    int mainLightIndex = renderingData.lightData.mainLightIndex;
                    VisibleLight mainLight = renderingData.lightData.visibleLights[mainLightIndex];
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref mainLight, mainLightIndex,
                        ref renderingData.shadowData, projectionMatrix, m_ShadowMap.rt.width);
                    ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref mainLight, shadowBias);
                }
                else if (m_CasterManager.Usage == ShadowUsage.Self)
                {
                    Vector4 lightDirection = m_CasterManager.GetLightDirection(i);
                    cmd.SetGlobalVector("_LightDirection", lightDirection);
                    CoreUtils.SetKeyword(cmd, KeywordNames._CASTING_SELF_SHADOW, true);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported shadow usage: {m_CasterManager.Usage}.");
                }

                Vector2Int tilePos = new(i % m_ShadowMapSizeInTile, i / m_ShadowMapSizeInTile);
                DrawShadow(cmd, i, tilePos, in viewMatrix, in projectionMatrix);
                m_ShadowMatrixArray[i] = GetShadowMatrix(tilePos, in viewMatrix, projectionMatrix);
                m_ShadowMapRectArray[i] = GetShadowMapRect(tilePos);
                m_ShadowCasterIdArray[i] = m_CasterManager.GetId(i);
            }

            cmd.SetGlobalDepthBias(0.0f, 0.0f); // Restore previous depth bias values
            CoreUtils.SetKeyword(cmd, KeywordNames._CASTING_SELF_SHADOW, false);

            cmd.SetGlobalTexture(PropertyIds._PerObjShadowMap, m_ShadowMap);
            cmd.SetGlobalInt(PropertyIds._PerObjShadowCount, m_CasterManager.VisibleCount);
            cmd.SetGlobalMatrixArray(PropertyIds._PerObjShadowMatrices, m_ShadowMatrixArray);
            cmd.SetGlobalVectorArray(PropertyIds._PerObjShadowMapRects, m_ShadowMapRectArray);
            cmd.SetGlobalFloatArray(PropertyIds._PerObjShadowCasterIds, m_ShadowCasterIdArray);
        }

        private void SetShadowSamplingData(CommandBuffer cmd)
        {
            int renderTargetWidth = m_ShadowMap.rt.width;
            int renderTargetHeight = m_ShadowMap.rt.height;
            float invShadowAtlasWidth = 1.0f / renderTargetWidth;
            float invShadowAtlasHeight = 1.0f / renderTargetHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetGlobalVector(PropertyIds._PerObjShadowOffset0,
                new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight));
            cmd.SetGlobalVector(PropertyIds._PerObjShadowOffset1,
                new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, invHalfShadowAtlasWidth, invHalfShadowAtlasHeight));
            cmd.SetGlobalVector(PropertyIds._PerObjShadowMapSize,
                new Vector4(invShadowAtlasWidth, invShadowAtlasHeight, renderTargetWidth, renderTargetHeight));
        }

        private void DrawShadow(CommandBuffer cmd, int casterIndex, Vector2Int tilePos, in Matrix4x4 view, in Matrix4x4 proj)
        {
            cmd.SetViewProjectionMatrices(view, proj);

            Rect viewport = new(tilePos * m_TileResolution, new Vector2(m_TileResolution, m_TileResolution));
            cmd.SetViewport(viewport);

            cmd.EnableScissorRect(new Rect(viewport.x + 4, viewport.y + 4, viewport.width - 8, viewport.height - 8));
            m_CasterManager.Draw(cmd, casterIndex);
            cmd.DisableScissorRect();
        }

        private Matrix4x4 GetShadowMatrix(Vector2Int tilePos, in Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                projectionMatrix.m20 = -projectionMatrix.m20;
                projectionMatrix.m21 = -projectionMatrix.m21;
                projectionMatrix.m22 = -projectionMatrix.m22;
                projectionMatrix.m23 = -projectionMatrix.m23;
            }

            float oneOverTileCount = 1.0f / m_ShadowMapSizeInTile;

            Matrix4x4 textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f * oneOverTileCount;
            textureScaleAndBias.m11 = 0.5f * oneOverTileCount;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = (0.5f + tilePos.x) * oneOverTileCount;
            textureScaleAndBias.m13 = (0.5f + tilePos.y) * oneOverTileCount;
            textureScaleAndBias.m23 = 0.5f;

            // Apply texture scale and offset to save a MAD in shader.
            return textureScaleAndBias * projectionMatrix * viewMatrix;
        }

        private Vector4 GetShadowMapRect(Vector2Int tilePos)
        {
            // x: xMin
            // y: xMax
            // z: yMin
            // w: yMax
            return new Vector4(tilePos.x, 1 + tilePos.x, tilePos.y, 1 + tilePos.y) / m_ShadowMapSizeInTile;
        }

        private static class KeywordNames
        {
            public static readonly string _CASTING_SELF_SHADOW = MemberNameHelpers.String();
        }

        internal static class PropertyIds
        {
            public static readonly int _PerObjShadowMap = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _PerObjShadowCount = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _PerObjShadowMatrices = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _PerObjShadowMapRects = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _PerObjShadowCasterIds = MemberNameHelpers.ShaderPropertyID();

            public static readonly int _PerObjShadowOffset0 = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _PerObjShadowOffset1 = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _PerObjShadowMapSize = MemberNameHelpers.ShaderPropertyID();
        }
    }
}
