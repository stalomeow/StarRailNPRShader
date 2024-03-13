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

using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

namespace HSR.NPRShader.Shadow
{
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct CullShadowCasterJob : IJobParallelFor
    {
        private static readonly float4x4 s_FlipZMatrix = new(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, -1, 0,
            0, 0, 0, 1
        );

        public quaternion MainLightRotationInv;
        public float3 CameraPosition;
        public float3 CameraNormalizedForward;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public float4* FrustumCorners;
        public int FrustumCornerCount;

        [ReadOnly, NoAlias]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> WorldBounds;

        [WriteOnly, NoAlias]
        [NativeDisableParallelForRestriction]
        public NativeArray<float4x4> ViewProjectionMatrices;

        [WriteOnly, NoAlias]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> Priorities;

        [WriteOnly, NoAlias]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> VisibleIndices;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public int* VisibleCount;

        public void Execute(int index)
        {
            float3 aabbMin = WorldBounds[2 * index];
            float3 aabbMax = WorldBounds[2 * index + 1];

            if (distancesq(aabbMin, aabbMax) <= 0.00001f)
            {
                return;
            }

            float3 aabbCenter = (aabbMin + aabbMax) * 0.5f;
            float4x4 viewMatrix = mul(s_FlipZMatrix, float4x4.TRS(-aabbCenter, MainLightRotationInv, 1));

            if (GetProjectionMatrix(in aabbMin, in aabbMax, in viewMatrix, out float4x4 projectionMatrix))
            {
                float distSq = distancesq(aabbCenter, CameraPosition);
                float cosAngle = dot(CameraNormalizedForward, normalizesafe(aabbCenter - CameraPosition));
                float priority = saturate(distSq / 1e4f) + mad(-cosAngle, 0.5f, 0.5f);

                int slot = Interlocked.Increment(ref *VisibleCount) - 1;
                ViewProjectionMatrices[2 * slot] = viewMatrix;
                ViewProjectionMatrices[2 * slot + 1] = projectionMatrix;
                Priorities[slot] = priority; // 越小越优先
                VisibleIndices[slot] = index;
            }
        }

        private bool GetProjectionMatrix(in float3 aabbMin, in float3 aabbMax, in float4x4 viewMatrix, out float4x4 projectionMatrix)
        {
            const int AABBPointCount = 8;
            float4* aabbPoints = stackalloc float4[AABBPointCount]
            {
                float4(aabbMin, 1),
                float4(aabbMax.x, aabbMin.y, aabbMin.z, 1),
                float4(aabbMin.x, aabbMax.y, aabbMin.z, 1),
                float4(aabbMin.x, aabbMin.y, aabbMax.z, 1),
                float4(aabbMax.x, aabbMax.y, aabbMin.z, 1),
                float4(aabbMax.x, aabbMin.y, aabbMax.z, 1),
                float4(aabbMin.x, aabbMax.y, aabbMax.z, 1),
                float4(aabbMax, 1),
            };
            CalculateAABB(aabbPoints, AABBPointCount, in viewMatrix, out float3 shadowMin, out float3 shadowMax);

            CalculateAABB(FrustumCorners, FrustumCornerCount, in viewMatrix, out float3 frustumMin, out float3 frustumMax);

            // 剔除一定不可见的阴影
            if (any(shadowMax < frustumMin) || any(shadowMin.xy > frustumMax.xy))
            {
                projectionMatrix = default;
                return false;
            }

            // 计算投影矩阵
            float left = shadowMin.x;
            float right = shadowMax.x;
            float bottom = shadowMin.y;
            float top = shadowMax.y;
            float zNear = -shadowMax.z;
            float zFar = -min(shadowMin.z, frustumMin.z);
            zFar = zNear + min(zFar - zNear, 5000); // 避免视锥体太长，深度的精度可能不够

            projectionMatrix = float4x4.OrthoOffCenter(left, right, bottom, top, zNear, zFar);
            return true;
        }

        private static void CalculateAABB(float4* points, int count, in float4x4 transform, out float3 aabbMin, out float3 aabbMax)
        {
            aabbMin = float3(float.PositiveInfinity);
            aabbMax = float3(float.NegativeInfinity);

            for (int i = 0; i < count; i++)
            {
                float3 p = mul(transform, points[i]).xyz;
                aabbMin = min(aabbMin, p);
                aabbMax = max(aabbMax, p);
            }
        }
    }
}
