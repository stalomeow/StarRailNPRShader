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
using HSR.NPRShader.Shadow;
using HSR.NPRShader.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader.Passes
{
    public class MainLightPerObjectShadowCasterPass : ScriptableRenderPass, IDisposable
    {
        private const int MaxShadowCount = 16;
        private const int TileResolution = 512;
        private const int ShadowMapBufferBits = 16;

        private readonly List<PerObjectShadowCaster> m_ShadowCasterList;
        private readonly List<Renderer> m_CachedRendererList;
        private readonly Matrix4x4[] m_ShadowMatrixArray;
        private readonly Vector4[] m_ShadowMapRectArray;
        private int m_ShadowMapSizeInTile; // 一行/一列有多少个 tile
        private RTHandle m_ShadowMap;
        private float m_DepthBias;
        private float m_NormalBias;
        private float m_MaxShadowDistance;

        public MainLightPerObjectShadowCasterPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("MainLightPerObjectShadow");

            m_ShadowCasterList = new List<PerObjectShadowCaster>();
            m_CachedRendererList = new List<Renderer>();
            m_ShadowMatrixArray = new Matrix4x4[MaxShadowCount];
            m_ShadowMapRectArray = new Vector4[MaxShadowCount];
        }

        public void Dispose()
        {
            m_ShadowMap?.Release();
        }

        public void Setup(float depthBias, float normalBias, float maxShadowDistance)
        {
            m_DepthBias = depthBias;
            m_NormalBias = normalBias;
            m_MaxShadowDistance = maxShadowDistance;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            Camera camera = renderingData.cameraData.camera;
            PerObjectShadowManager.GetCasterList(camera, m_ShadowCasterList, m_MaxShadowDistance, MaxShadowCount);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            if (m_ShadowCasterList.Count <= 0)
            {
                return;
            }

            // 保证 shadow map 是正方形
            m_ShadowMapSizeInTile = Mathf.CeilToInt(Mathf.Sqrt(m_ShadowCasterList.Count));
            int shadowRTSize = m_ShadowMapSizeInTile * TileResolution;
            ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_ShadowMap, shadowRTSize, shadowRTSize, ShadowMapBufferBits);

            ConfigureTarget(m_ShadowMap);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            m_ShadowCasterList.Clear();

            base.OnCameraCleanup(cmd);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            int mainLightIndex = renderingData.lightData.mainLightIndex;

            using (new ProfilingScope(cmd, profilingSampler))
            {
                bool hasShadow = false;

                if (mainLightIndex >= 0 && m_ShadowCasterList.Count > 0)
                {
                    VisibleLight mainLight = renderingData.lightData.visibleLights[mainLightIndex];

                    if (mainLight.lightType == LightType.Directional)
                    {
                        ExecuteShadowCasters(cmd, ref mainLight);
                        SetShadowSamplingData(cmd);
                        hasShadow = true;
                    }
                }

                if (!hasShadow)
                {
                    cmd.SetGlobalInt(PropertyIds._PerObjShadowCount, 0);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ExecuteShadowCasters(CommandBuffer cmd, ref VisibleLight mainLight)
        {
            cmd.SetGlobalDepthBias(1.0f, 2.5f); // these values match HDRP defaults (see https://github.com/Unity-Technologies/Graphics/blob/9544b8ed2f98c62803d285096c91b44e9d8cbc47/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAtlas.cs#L197 )

            Quaternion mainLightRotation = mainLight.localToWorldMatrix.rotation;
            Vector2 tileSize = new Vector2(TileResolution, TileResolution);
            float scale = 1.0f / m_ShadowMapSizeInTile;

            for (int i = 0; i < m_ShadowCasterList.Count; i++)
            {
                Bounds boundsWS = m_ShadowCasterList[i].GetActiveRenderersAndBounds(m_CachedRendererList);
                var viewMatrix = Matrix4x4.TRS(boundsWS.center, mainLightRotation, Vector3.one).inverse;
                viewMatrix.SetRow(2, -viewMatrix.GetRow(2)); // flip z

                Bounds boundsVS = TransformBounds(boundsWS, viewMatrix);
                Matrix4x4 projectionMatrix = ToProjectionMatrix(boundsVS);

                Vector4 shadowBias = GetShadowBias(ref mainLight, projectionMatrix, m_ShadowMap.rt.width);
                ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref mainLight, shadowBias);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CastingPunctualLightShadow, false);

                Vector2Int offset = new(i % m_ShadowMapSizeInTile, i / m_ShadowMapSizeInTile);
                Rect viewport = new(offset * TileResolution, tileSize);
                DrawRenderers(cmd, viewport, in viewMatrix, in projectionMatrix, m_CachedRendererList);
                m_CachedRendererList.Clear();

                Matrix4x4 coordMatrix = new Matrix4x4();
                coordMatrix.SetRow(0, new Vector4(0.5f * scale, 0, 0, (0.5f + offset.x) * scale));
                coordMatrix.SetRow(1, new Vector4(0, 0.5f * scale, 0, (0.5f + offset.y) * scale));
                coordMatrix.SetRow(2, new Vector4(0, 0, 0.5f, 0.5f));
                coordMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

                if (SystemInfo.usesReversedZBuffer)
                {
                    var projMat = projectionMatrix;
                    projMat.SetRow(2, -projMat.GetRow(2));
                    m_ShadowMatrixArray[i] = coordMatrix * projMat * viewMatrix;
                }
                else
                {
                    m_ShadowMatrixArray[i] = coordMatrix * projectionMatrix * viewMatrix;
                }

                // x: xMin
                // y: xMax
                // z: yMin
                // w: yMax
                m_ShadowMapRectArray[i] = scale * new Vector4(offset.x, 1 + offset.x, offset.y, 1 + offset.y);
            }

            cmd.SetGlobalDepthBias(0.0f, 0.0f); // Restore previous depth bias values

            cmd.SetGlobalTexture(PropertyIds._PerObjShadowMap, m_ShadowMap);
            cmd.SetGlobalInt(PropertyIds._PerObjShadowCount, m_ShadowCasterList.Count);
            cmd.SetGlobalMatrixArray(PropertyIds._PerObjShadowMatrices, m_ShadowMatrixArray);
            cmd.SetGlobalVectorArray(PropertyIds._PerObjShadowMapRects, m_ShadowMapRectArray);
        }

        private Vector4 GetShadowBias(ref VisibleLight shadowLight, Matrix4x4 lightProjectionMatrix, float shadowResolution)
        {
            // Frustum size is guaranteed to be a cube as we wrap shadow frustum around a sphere
            float frustumSize = 2.0f / lightProjectionMatrix.m00;

            // depth and normal bias scale is in shadowmap texel size in world space
            float texelSize = frustumSize / shadowResolution;
            float depthBias = -m_DepthBias * texelSize;
            float normalBias = -m_NormalBias * texelSize;

            // The current implementation of NormalBias in Universal RP is the same as in Unity Built-In RP (i.e moving shadow caster vertices along normals when projecting them to the shadow map).
            // This does not work well with Point Lights, which is why NormalBias value is hard-coded to 0.0 in Built-In RP (see value of unity_LightShadowBias.z in FrameDebugger, and native code that sets it: https://github.cds.internal.unity3d.com/unity/unity/blob/a9c916ba27984da43724ba18e70f51469e0c34f5/Runtime/Camera/Shadows.cpp#L1686 )
            // We follow the same convention in Universal RP:
            if (shadowLight.lightType == LightType.Point)
                normalBias = 0.0f;

            if (shadowLight.light.shadows == LightShadows.Soft)
            {
                SoftShadowQuality softShadowQuality = SoftShadowQuality.Medium;
                if (shadowLight.light.TryGetComponent(out UniversalAdditionalLightData additionalLightData))
                    softShadowQuality = additionalLightData.softShadowQuality;

                // TODO: depth and normal bias assume sample is no more than 1 texel away from shadowmap
                // This is not true with PCF. Ideally we need to do either
                // cone base bias (based on distance to center sample)
                // or receiver place bias based on derivatives.
                // For now we scale it by the PCF kernel size of non-mobile platforms (5x5)
                float kernelRadius = 2.5f;

                switch (softShadowQuality)
                {
                    case SoftShadowQuality.High: kernelRadius = 3.5f; break; // 7x7
                    case SoftShadowQuality.Medium: kernelRadius = 2.5f; break; // 5x5
                    case SoftShadowQuality.Low: kernelRadius = 1.5f; break; // 3x3
                    default: break;
                }

                depthBias *= kernelRadius;
                normalBias *= kernelRadius;
            }

            return new Vector4(depthBias, normalBias, 0.0f, 0.0f);
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

        private static void DrawRenderers(CommandBuffer cmd, Rect viewport, in Matrix4x4 viewMatrix, in Matrix4x4 projectionMatrix, List<Renderer> renderers)
        {
            cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            cmd.SetViewport(viewport);
            cmd.EnableScissorRect(new Rect(viewport.x + 4, viewport.y + 4, viewport.width - 8, viewport.height - 8));

            foreach (var renderer in renderers)
            {
#if UNITY_EDITOR
                Material[] materials = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
#else
                Material[] materials = renderer.materials;
#endif

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    int passId = material.FindPass("PerObjectShadow");

                    if (passId == -1)
                    {
                        continue;
                    }

                    cmd.DrawRenderer(renderer, material, i, passId);
                }
            }

            cmd.DisableScissorRect();
        }

        private static readonly Vector3[] s_BoundsCornerBuffer = new Vector3[8];

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 transformMatrix)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            s_BoundsCornerBuffer[0] = min;
            s_BoundsCornerBuffer[1] = new Vector3(max.x, min.y, min.z);
            s_BoundsCornerBuffer[2] = new Vector3(min.x, max.y, min.z);
            s_BoundsCornerBuffer[3] = new Vector3(min.x, min.y, max.z);
            s_BoundsCornerBuffer[4] = new Vector3(max.x, max.y, min.z);
            s_BoundsCornerBuffer[5] = new Vector3(max.x, min.y, max.z);
            s_BoundsCornerBuffer[6] = new Vector3(min.x, max.y, max.z);
            s_BoundsCornerBuffer[7] = max;

            return GeometryUtility.CalculateBounds(s_BoundsCornerBuffer, transformMatrix);
        }

        private static Matrix4x4 ToProjectionMatrix(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            float left = min.x;
            float right = max.x;
            float bottom = min.y;
            float top = max.y;
            float zNear = min.z;
            float zFar = max.z + 1000;

            // https://docs.unity3d.com/ScriptReference/Matrix4x4.Ortho.html
            // The returned matrix embeds a z-flip operation
            // whose purpose is to cancel the z-flip performed by the camera view matrix.
            // If the view matrix is an identity or some custom matrix that doesn't perform a z-flip,
            // consider multiplying the third column of the projection matrix (i.e. m02, m12, m22 and m32) by -1.
            return Matrix4x4.Ortho(left, right, bottom, top, zNear, zFar);
        }

        private static class PropertyIds
        {
            public static readonly int _PerObjShadowMap = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _PerObjShadowCount = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _PerObjShadowMatrices = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _PerObjShadowMapRects = StringHelpers.ShaderPropertyIDFromMemberName();

            public static readonly int _PerObjShadowOffset0 = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _PerObjShadowOffset1 = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _PerObjShadowMapSize = StringHelpers.ShaderPropertyIDFromMemberName();
        }
    }
}
