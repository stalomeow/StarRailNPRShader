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
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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

        private readonly List<int> m_CasterRendererIndexList = new();
        private readonly PriorityBuffer<float, ShadowCasterCullingResult> m_SceneShadowCullResults = new();
        private readonly PriorityBuffer<float, ShadowCasterCullingResult> m_SelfShadowCullResults = new();

        public int GetCasterCount(ShadowUsage usage)
        {
            return GetCullResults(usage).Count;
        }

        public void GetMatrices(ShadowUsage usage, int index, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix)
        {
            ref ShadowCasterCullingResult result = ref GetCullResults(usage)[index];
            viewMatrix = result.ViewMatrix;
            projectionMatrix = result.ProjectionMatrix;
        }

        public int GetCasterId(ShadowUsage usage, int index)
        {
            ref ShadowCasterCullingResult result = ref GetCullResults(usage)[index];
            return result.Caster.Id;
        }

        public Vector4 GetLightDirection(ShadowUsage usage, int index)
        {
            return GetCullResults(usage)[index].LightDirection;
        }

        public void Draw(CommandBuffer cmd, ShadowUsage usage, int index)
        {
            ref ShadowCasterCullingResult result = ref GetCullResults(usage)[index];
            for (int i = result.RendererIndexStartInclusive; i < result.RendererIndexEndExclusive; i++)
            {
                result.Caster.RendererList.Draw(cmd, m_CasterRendererIndexList[i]);
            }
        }

        private PriorityBuffer<float, ShadowCasterCullingResult> GetCullResults(ShadowUsage usage) => usage switch
        {
            ShadowUsage.Scene => m_SceneShadowCullResults,
            ShadowUsage.Self => m_SelfShadowCullResults,
            _ => throw new NotImplementedException()
        };

        public void Clear()
        {
            m_CasterRendererIndexList.Clear();
            m_SceneShadowCullResults.Reset();
            m_SelfShadowCullResults.Reset();
        }

        public unsafe void Cull(Camera camera, Quaternion mainLightRotation, int maxCount)
        {
            m_CasterRendererIndexList.Clear();
            m_SceneShadowCullResults.Reset(maxCount);
            m_SelfShadowCullResults.Reset(maxCount);

            if (s_Casters.Count <= 0)
            {
                return;
            }

            float4* frustumCorners = stackalloc float4[ShadowCasterCullingArgs.FrustumCornerCount];
            ShadowCasterCullingArgs.SetFrustumEightCorners(frustumCorners, camera);

            var args = new ShadowCasterCullingArgs
            {
                CameraPosition = camera.transform.position,
                CameraNormalizedForward = camera.transform.forward,
                FrustumEightCorners = frustumCorners,
            };

            quaternion lightRotation = mainLightRotation;
            quaternion viewRotation = math.slerp(mainLightRotation, camera.transform.rotation, 0.9f);
            // Debug.DrawRay(Vector3.zero, -math.rotate(viewRotation, math.forward()) * 1000);
            // Debug.Log(math.normalize(-math.rotate(viewRotation, math.forward())));

            foreach (var caster in s_Casters)
            {
                if (!caster.IsEnabled)
                {
                    continue;
                }

                int rendererIndexInitialCount = m_CasterRendererIndexList.Count;
                if (!caster.RendererList.TryGetWorldBounds(out Bounds bounds, m_CasterRendererIndexList))
                {
                    continue;
                }

                var baseResult = new ShadowCasterCullingResult
                {
                    Caster = caster,
                    RendererIndexStartInclusive = rendererIndexInitialCount,
                    RendererIndexEndExclusive = m_CasterRendererIndexList.Count,
                };

                args.AABBMin = bounds.min;
                args.AABBMax = bounds.max;

                args.LightRotation = lightRotation;
                args.Usage = ShadowUsage.Scene;
                CullAndAppend(m_SceneShadowCullResults, baseResult, in args);

                args.LightRotation = viewRotation;
                args.Usage = ShadowUsage.Self;
                CullAndAppend(m_SelfShadowCullResults, baseResult, in args);
            }
        }

        private static void CullAndAppend(PriorityBuffer<float, ShadowCasterCullingResult> buffer,
            ShadowCasterCullingResult baseResult, in ShadowCasterCullingArgs args)
        {
            bool visible = ShadowCasterUtility.Cull(in args, out float4x4 viewMatrix, out float4x4 projectionMatrix,
                out float priority, out float4 lightDirection);

            if (!visible)
            {
                return;
            }

            baseResult.ViewMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref viewMatrix);
            baseResult.ProjectionMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref projectionMatrix);
            baseResult.LightDirection = UnsafeUtility.As<float4, Vector4>(ref lightDirection);
            buffer.TryAppend(priority, in baseResult);
        }
    }
}
