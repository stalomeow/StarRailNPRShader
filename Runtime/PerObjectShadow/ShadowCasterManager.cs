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
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader.PerObjectShadow
{
    public class ShadowCasterManager
    {
        private static readonly HashSet<IShadowCaster> s_Casters = new();
        private static int s_NextCasterId = 1;

        public static void Register(IShadowCaster caster)
        {
            if (s_Casters.Add(caster))
            {
                caster.Id = s_NextCasterId;
                s_NextCasterId++;
            }
        }

        public static void Unregister(IShadowCaster caster) => s_Casters.Remove(caster);

        private readonly List<int> m_RendererIndexList = new();
        private readonly PriorityBuffer<float, ShadowCasterCullingResult> m_CullResults = new();

        public ShadowUsage Usage { get; }

        public ShadowCasterManager(ShadowUsage usage) => Usage = usage;

        public int VisibleCount => m_CullResults.Count;

        public int GetId(int index) => m_CullResults[index].Caster.Id;

        public Vector4 GetLightDirection(int index) => m_CullResults[index].LightDirection;

        public void GetMatrices(int index, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix)
        {
            ref ShadowCasterCullingResult result = ref m_CullResults[index];
            viewMatrix = result.ViewMatrix;
            projectionMatrix = result.ProjectionMatrix;
        }

        public void Draw(CommandBuffer cmd, int index)
        {
            ref ShadowCasterCullingResult result = ref m_CullResults[index];
            for (int i = result.RendererIndexStartInclusive; i < result.RendererIndexEndExclusive; i++)
            {
                result.Caster.RendererList.Draw(cmd, m_RendererIndexList[i]);
            }
        }

        public unsafe void Cull(in RenderingData renderingData, int maxCount)
        {
            m_RendererIndexList.Clear();
            m_CullResults.Reset(maxCount);

            if (s_Casters.Count <= 0 || !TryGetMainLight(in renderingData, out VisibleLight mainLight))
            {
                return;
            }

            Camera camera = renderingData.cameraData.camera;
            Transform cameraTransform = camera.transform;

            float4* frustumCorners = stackalloc float4[ShadowCasterCullingArgs.FrustumCornerCount];
            ShadowCasterCullingArgs.SetFrustumEightCorners(frustumCorners, camera);

            var baseArgs = new ShadowCasterCullingArgs
            {
                Usage = Usage,
                FrustumEightCorners = frustumCorners,
                CameraLocalToWorldMatrix = cameraTransform.localToWorldMatrix,
                MainLightLocalToWorldMatrix = mainLight.localToWorldMatrix,
            };

            foreach (var caster in s_Casters)
            {
                CullAndAppend(caster, ref baseArgs);
            }
        }

        private void CullAndAppend(IShadowCaster caster, ref ShadowCasterCullingArgs baseArgs)
        {
            if (!caster.CanCastShadow(baseArgs.Usage))
            {
                return;
            }

            int rendererIndexInitialCount = m_RendererIndexList.Count;
            if (!caster.RendererList.TryGetWorldBounds(baseArgs.Usage, out Bounds bounds, m_RendererIndexList))
            {
                return;
            }

            baseArgs.AABBMin = bounds.min;
            baseArgs.AABBMax = bounds.max;
            baseArgs.CasterUpVector = caster.Transform.up;
            bool visible = ShadowCasterUtility.Cull(in baseArgs, out float4x4 viewMatrix,
                out float4x4 projectionMatrix, out float priority, out float4 lightDirection);

            if (!visible)
            {
                return;
            }

            m_CullResults.TryAppend(priority, new ShadowCasterCullingResult
            {
                Caster = caster,
                RendererIndexStartInclusive = rendererIndexInitialCount,
                RendererIndexEndExclusive = m_RendererIndexList.Count,
                LightDirection = UnsafeUtility.As<float4, Vector4>(ref lightDirection),
                ViewMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref viewMatrix),
                ProjectionMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref projectionMatrix),
            });
        }

        private static bool TryGetMainLight(in RenderingData renderingData, out VisibleLight mainLight)
        {
            int mainLightIndex = renderingData.lightData.mainLightIndex;

            if (mainLightIndex < 0)
            {
                mainLight = default;
                return false;
            }

            mainLight = renderingData.lightData.visibleLights[mainLightIndex];
            return mainLight.lightType == LightType.Directional;
        }
    }
}
