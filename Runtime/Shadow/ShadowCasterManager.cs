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
    public static class ShadowCasterManager
    {
        private readonly struct CasterCullCandidate
        {
            public readonly IShadowCaster Caster;
            public readonly int RendererIndicesStart;
            public readonly int RendererIndicesCount;

            public CasterCullCandidate(IShadowCaster caster, int rendererIndicesStart, int rendererIndicesCount)
            {
                Caster = caster;
                RendererIndicesStart = rendererIndicesStart;
                RendererIndicesCount = rendererIndicesCount;
            }
        }

        private static readonly HashSet<IShadowCaster> s_Casters = new();
        private static readonly List<CasterCullCandidate> s_CasterCullCandidateList = new();
        private static readonly List<int> s_CasterCullCandidateRendererIndexList = new();
        private static CullShadowCasterResult[] s_CasterCullResultBuffer = Array.Empty<CullShadowCasterResult>();
        private static int s_CasterCullResultCount;

        public static bool Register(IShadowCaster caster)
        {
            return s_Casters.Add(caster);
        }

        public static bool Unregister(IShadowCaster caster)
        {
            return s_Casters.Remove(caster);
        }

        public static int VisibleCasterCount => s_CasterCullResultCount;

        public static void GetVisibleCasterMatrices(int index, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix)
        {
            ref CullShadowCasterResult result = ref s_CasterCullResultBuffer[index];
            viewMatrix = result.ViewMatrix;
            projectionMatrix = result.ProjectionMatrix;
        }

        public static void DrawVisibleCaster(CommandBuffer cmd, int index)
        {
            ref CullShadowCasterResult result = ref s_CasterCullResultBuffer[index];
            CasterCullCandidate caster = s_CasterCullCandidateList[result.CandidateIndex];
            caster.Caster.RendererList.Draw(cmd, s_CasterCullCandidateRendererIndexList,
                caster.RendererIndicesStart, caster.RendererIndicesCount);
        }

        public static void ClearVisibleCasters()
        {
            s_CasterCullCandidateList.Clear();
            s_CasterCullCandidateRendererIndexList.Clear();
            s_CasterCullResultCount = 0;
        }

        public static unsafe void UpdateVisibleCasters(Camera camera, Quaternion mainLightRotation, int maxCount)
        {
            ClearVisibleCasters();

            using NativeArray<float3x2> worldBounds = FillCasterCullCandidateList();

            if (s_CasterCullCandidateList.Count <= 0)
            {
                return;
            }

            const int FrustumCornerCount = 8;
            float4* frustumCorners = stackalloc float4[FrustumCornerCount];
            CalculateFrustumEightCorners(camera, frustumCorners);

            int resultCount = 0;
            EnsureCasterCullResultBufferSize();
            fixed (CullShadowCasterResult* results = s_CasterCullResultBuffer)
            {
                CullShadowCasterJob job = new()
                {
                    MainLightRotationInv = Quaternion.Inverse(mainLightRotation),
                    CameraPosition = camera.transform.position,
                    CameraNormalizedForward = camera.transform.forward,
                    FrustumCorners = frustumCorners,
                    FrustumCornerCount = FrustumCornerCount,
                    WorldBounds = worldBounds,
                    Results = results,
                    ResultCount = &resultCount,
                };
                job.ScheduleByRef(s_CasterCullCandidateList.Count, 2).Complete();
            }

            Array.Sort(s_CasterCullResultBuffer, 0, resultCount);
            resultCount = Mathf.Min(resultCount, maxCount);
            s_CasterCullResultCount = resultCount;

            for (int i = 0; i < resultCount; i++)
            {
                ref CullShadowCasterResult result = ref s_CasterCullResultBuffer[i];
                s_CasterCullCandidateList[result.CandidateIndex].Caster.SetCasterIndex(i);
            }
        }

        private static void EnsureCasterCullResultBufferSize()
        {
            if (s_CasterCullResultBuffer.Length >= s_CasterCullCandidateList.Count)
            {
                return;
            }

            int newSize = Mathf.Max(s_CasterCullResultBuffer.Length * 2, s_CasterCullCandidateList.Count);
            s_CasterCullResultBuffer = new CullShadowCasterResult[newSize];
        }

        private static NativeArray<float3x2> FillCasterCullCandidateList()
        {
            if (s_Casters.Count <= 0)
            {
                return default;
            }

            s_CasterCullCandidateList.Clear();
            s_CasterCullCandidateRendererIndexList.Clear();
            NativeArray<float3x2> worldBounds = FastAllocateTempJob<float3x2>(s_Casters.Count);

            foreach (var caster in s_Casters)
            {
                caster.SetCasterIndex(-1); // 重置

                if (!caster.IsEnabled)
                {
                    continue;
                }

                int rendererIndexInitialCount = s_CasterCullCandidateRendererIndexList.Count;
                if (!caster.RendererList.TryGetWorldBounds(out Bounds bounds, s_CasterCullCandidateRendererIndexList))
                {
                    continue;
                }

                worldBounds[s_CasterCullCandidateList.Count] = new float3x2(bounds.min, bounds.max);
                s_CasterCullCandidateList.Add(new CasterCullCandidate(caster, rendererIndexInitialCount,
                    s_CasterCullCandidateRendererIndexList.Count - rendererIndexInitialCount));
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
