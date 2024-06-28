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
using UnityEngine;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

namespace HSR.NPRShader.Shadow
{
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct CullShadowCasterJob : IJobParallelFor
    {
        public struct LightData
        {
            public quaternion LightRotation;

            [NoAlias, NativeDisableUnsafePtrRestriction]
            public CullShadowCasterResult* ResultBuffer;

            [NoAlias, NativeDisableUnsafePtrRestriction]
            public int* ResultCount;
        }

        private static readonly float4x4 s_FlipZMatrix = new(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, -1, 0,
            0, 0, 0, 1
        );

        public float3 CameraPosition;
        public float3 CameraNormalizedForward;

        [NoAlias, NativeDisableUnsafePtrRestriction]
        public float4* FrustumCorners;
        public int FrustumCornerCount;

        [NoAlias, ReadOnly]
        public NativeArray<float3x2> WorldBounds;

        [NoAlias, NativeDisableUnsafePtrRestriction]
        public LightData* Lights;
        public int LightCount;

        public void Execute(int index)
        {
            float3 aabbMin = WorldBounds[index].c0;
            float3 aabbMax = WorldBounds[index].c1;

            if (distancesq(aabbMin, aabbMax) <= 0.00001f)
            {
                return;
            }

            float3 aabbCenter = (aabbMin + aabbMax) * 0.5f;

            for (int i = 0; i < LightCount; i++)
            {
                quaternion lightRotationInv = inverse(Lights[i].LightRotation);
                float4x4 viewMatrix = mul(s_FlipZMatrix, float4x4.TRS(-aabbCenter, lightRotationInv, 1));

                if (GetProjectionMatrix(in aabbMin, in aabbMax, in viewMatrix, out float4x4 projectionMatrix))
                {
                    float distSq = distancesq(aabbCenter, CameraPosition);
                    float cosAngle = dot(CameraNormalizedForward, normalizesafe(aabbCenter - CameraPosition));
                    float priority = saturate(distSq / 1e4f) + mad(-cosAngle, 0.5f, 0.5f);

                    float4 lightDirection = float4(-rotate(Lights[i].LightRotation, forward()), 0);

                    int slot = Interlocked.Increment(ref *Lights[i].ResultCount) - 1;
                    CullShadowCasterResult* result = Lights[i].ResultBuffer + slot;

                    result->Priority = priority; // 越小越优先
                    result->CandidateIndex = index;
                    result->LightDirection = UnsafeUtility.As<float4, Vector4>(ref lightDirection);;
                    result->ViewMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref viewMatrix);
                    result->ProjectionMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref projectionMatrix);
                }
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
            // TODO
            float zFar = max(-shadowMin.z, min(-frustumMin.z, zNear + 50)); // 视锥体太长的话深度都集中在 0 或者 1 处，精度不够

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
