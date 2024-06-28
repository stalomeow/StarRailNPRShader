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
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace HSR.NPRShader.Shadow
{
    public class ShadowCasterManager
    {
        private static readonly HashSet<IShadowCaster> s_Casters = new();

        public static bool Register(IShadowCaster caster) => s_Casters.Add(caster);

        public static bool Unregister(IShadowCaster caster) => s_Casters.Remove(caster);

        private readonly struct CasterCullCandidate
        {
            public readonly IShadowCaster Caster;
            public readonly int RendererIndexStart;
            public readonly int RendererIndexCount;

            public CasterCullCandidate(IShadowCaster caster, int rendererIndexStart, int rendererIndexCount)
            {
                Caster = caster;
                RendererIndexStart = rendererIndexStart;
                RendererIndexCount = rendererIndexCount;
            }
        }

        private readonly List<CasterCullCandidate> m_CullCandidateList = new();
        private readonly List<int> m_CullCandidateRendererIndexList = new();
        private CullShadowCasterResult[] m_MainLightCullResultBuffer = Array.Empty<CullShadowCasterResult>();
        private CullShadowCasterResult[] m_ViewCullResultBuffer = Array.Empty<CullShadowCasterResult>();
        private int m_MainLightCullResultCount;
        private int m_ViewCullResultCount;

        public int GetVisibleCasterCount(ShadowCasterCategory category) => category switch
        {
            ShadowCasterCategory.MainLight => m_MainLightCullResultCount,
            ShadowCasterCategory.View => m_ViewCullResultCount,
            _ => throw new NotImplementedException(),
        };

        public void GetVisibleCasterMatrices(ShadowCasterCategory category, int index, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix)
        {
            ref CullShadowCasterResult result = ref GetVisibleCaster(category, index);
            viewMatrix = result.ViewMatrix;
            projectionMatrix = result.ProjectionMatrix;
        }

        public Vector4 GetVisibleCasterLightDirection(ShadowCasterCategory category, int index)
        {
            return GetVisibleCaster(category, index).LightDirection;
        }

        public void DrawVisibleCaster(CommandBuffer cmd, ShadowCasterCategory category, int index)
        {
            ref CullShadowCasterResult result = ref GetVisibleCaster(category, index);
            CasterCullCandidate caster = m_CullCandidateList[result.CandidateIndex];
            caster.Caster.RendererList.Draw(cmd, m_CullCandidateRendererIndexList,
                caster.RendererIndexStart, caster.RendererIndexCount);
        }

        private ref CullShadowCasterResult GetVisibleCaster(ShadowCasterCategory category, int index)
        {
            switch (category)
            {
                case ShadowCasterCategory.MainLight: return ref m_MainLightCullResultBuffer[index];
                case ShadowCasterCategory.View: return ref m_ViewCullResultBuffer[index];
                default: throw new NotImplementedException();
            }
        }

        public void ClearVisibleCasters()
        {
            m_CullCandidateList.Clear();
            m_CullCandidateRendererIndexList.Clear();
            m_MainLightCullResultCount = 0;
            m_ViewCullResultCount = 0;
        }

        public unsafe void UpdateVisibleCasters(Camera camera, Quaternion mainLightRotation, int maxCount)
        {
            ClearVisibleCasters();

            using NativeArray<float3x2> worldBounds = FillCasterCullCandidateList();

            if (m_CullCandidateList.Count <= 0)
            {
                return;
            }

            const int FrustumCornerCount = 8;
            float4* frustumCorners = stackalloc float4[FrustumCornerCount];
            CalculateFrustumEightCorners(camera, frustumCorners);

            int mainLightCullResultCount = 0;
            int viewCullResultCount = 0;
            EnsureCasterCullResultBufferSize(ref m_MainLightCullResultBuffer);
            EnsureCasterCullResultBufferSize(ref m_ViewCullResultBuffer);

            fixed (CullShadowCasterResult* mainLightCullResults = m_MainLightCullResultBuffer)
            fixed (CullShadowCasterResult* viewCullResults = m_ViewCullResultBuffer)
            {
                CullShadowCasterJob.LightData* lights = stackalloc CullShadowCasterJob.LightData[2];
                lights[0].LightRotation = mainLightRotation;
                lights[0].ResultBuffer = mainLightCullResults;
                lights[0].ResultCount = &mainLightCullResultCount;
                lights[1].LightRotation = Quaternion.Slerp(mainLightRotation, camera.transform.rotation, 0.8f);
                lights[1].ResultBuffer = viewCullResults;
                lights[1].ResultCount = &viewCullResultCount;

                CullShadowCasterJob job = new()
                {
                    CameraPosition = camera.transform.position,
                    CameraNormalizedForward = camera.transform.forward,
                    FrustumCorners = frustumCorners,
                    FrustumCornerCount = FrustumCornerCount,
                    WorldBounds = worldBounds,
                    Lights = lights,
                    LightCount = 2,
                };
                job.ScheduleByRef(m_CullCandidateList.Count, 2).Complete();
            }

            Array.Sort(m_MainLightCullResultBuffer, 0, mainLightCullResultCount);
            mainLightCullResultCount = Mathf.Min(mainLightCullResultCount, maxCount);
            m_MainLightCullResultCount = mainLightCullResultCount;

            Array.Sort(m_ViewCullResultBuffer, 0, viewCullResultCount);
            viewCullResultCount = Mathf.Min(viewCullResultCount, maxCount);
            m_ViewCullResultCount = viewCullResultCount;

            for (int i = 0; i < viewCullResultCount; i++)
            {
                ref CullShadowCasterResult result = ref m_ViewCullResultBuffer[i];
                m_CullCandidateList[result.CandidateIndex].Caster.SetCasterIndex(i);
            }
        }

        private void EnsureCasterCullResultBufferSize(ref CullShadowCasterResult[] buffer)
        {
            if (buffer.Length >= m_CullCandidateList.Count)
            {
                return;
            }

            int newSize = Mathf.Max(buffer.Length * 2, m_CullCandidateList.Count);
            buffer = new CullShadowCasterResult[newSize];
        }

        private NativeArray<float3x2> FillCasterCullCandidateList()
        {
            if (s_Casters.Count <= 0)
            {
                return default;
            }

            m_CullCandidateList.Clear();
            m_CullCandidateRendererIndexList.Clear();
            NativeArray<float3x2> worldBounds = FastAllocateTempJob<float3x2>(s_Casters.Count);

            foreach (var caster in s_Casters)
            {
                caster.SetCasterIndex(-1); // 重置

                if (!caster.IsEnabled)
                {
                    continue;
                }

                int rendererIndexInitialCount = m_CullCandidateRendererIndexList.Count;
                if (!caster.RendererList.TryGetWorldBounds(out Bounds bounds, m_CullCandidateRendererIndexList))
                {
                    continue;
                }

                worldBounds[m_CullCandidateList.Count] = new float3x2(bounds.min, bounds.max);
                m_CullCandidateList.Add(new CasterCullCandidate(caster, rendererIndexInitialCount,
                    m_CullCandidateRendererIndexList.Count - rendererIndexInitialCount));
            }

            return worldBounds;
        }

        private static NativeArray<T> FastAllocateTempJob<T>(int length) where T : struct
        {
            return new NativeArray<T>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        private static readonly Vector3[] s_FrustumCornerBuffer = new Vector3[4];

        private static unsafe void CalculateFrustumEightCorners(Camera camera, float4* outCorners)
        {
            const Camera.MonoOrStereoscopicEye Eye = Camera.MonoOrStereoscopicEye.Mono;

            var viewport = new Rect(0, 0, 1, 1);
            Transform cameraTransform = camera.transform;

            camera.CalculateFrustumCorners(viewport, camera.nearClipPlane, Eye, s_FrustumCornerBuffer);

            for (int i = 0; i < 4; i++)
            {
                Vector3 xyz = cameraTransform.TransformPoint(s_FrustumCornerBuffer[i]);
                outCorners[i] = new float4(xyz, 1);
            }

            camera.CalculateFrustumCorners(viewport, camera.farClipPlane, Eye, s_FrustumCornerBuffer);

            for (int i = 0; i < 4; i++)
            {
                Vector3 xyz = cameraTransform.TransformPoint(s_FrustumCornerBuffer[i]);
                outCorners[i + 4] = new float4(xyz, 1);
            }
        }
    }
}
