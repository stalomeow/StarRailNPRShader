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

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

namespace HSR.NPRShader.PerObjectShadow
{
    [BurstCompile]
    internal static class ShadowCasterUtility
    {
        private static readonly float4x4 s_FlipZMatrix = new(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, -1, 0,
            0, 0, 0, 1
        );

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public static bool Cull(in float3 aabbMin, in float3 aabbMax, in ShadowCasterCullingArgs args,
            out float4x4 viewMatrix, out float4x4 projectionMatrix, out float priority, out float4 lightDirection)
        {
            float3 aabbCenter = (aabbMin + aabbMax) * 0.5f;
            GetLightRotationAndDirection(aabbCenter, in args, out quaternion lightRotation, out lightDirection);

            viewMatrix = float4x4.TRS(-aabbCenter, inverse(lightRotation), 1);
            viewMatrix = mul(s_FlipZMatrix, viewMatrix); // 翻转 z 轴

            if (GetProjectionMatrix(in aabbMin, in aabbMax, in args, in viewMatrix, out projectionMatrix))
            {
                float3 cameraForward = args.CameraLocalToWorldMatrix.c2.xyz;
                float3 cameraPosition = args.CameraLocalToWorldMatrix.c3.xyz;

                float distSq = distancesq(aabbCenter, cameraPosition);
                float cosAngle = dot(cameraForward, normalizesafe(aabbCenter - cameraPosition));
                priority = saturate(distSq / 1e4f) + mad(-cosAngle, 0.5f, 0.5f); // 越小越优先
                return true;
            }

            priority = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetLightRotationAndDirection(float3 aabbCenter, in ShadowCasterCullingArgs args,
            out quaternion lightRotation, out float4 lightDirection)
        {
            switch (args.Usage)
            {
                case ShadowUsage.Scene:
                {
                    lightRotation = quaternion(args.MainLightLocalToWorldMatrix);
                    lightDirection = float4(-args.MainLightLocalToWorldMatrix.c2.xyz, 0);
                    break;
                }
                case ShadowUsage.Self:
                {
                    float3 cameraPosition = args.CameraLocalToWorldMatrix.c3.xyz;
                    float3 cameraUp = args.CameraLocalToWorldMatrix.c1.xyz;

                    // 混合视角和主光源的方向，视角方向不用 camera forward，避免转动视角时阴影方向变化
                    // 直接用向量插值，四元数插值会导致部分情况跳变
                    // 以视角方向为主，减少背面 artifact
                    float3 viewForward = normalizesafe(aabbCenter - cameraPosition);
                    float3 lightForward = args.MainLightLocalToWorldMatrix.c2.xyz;
                    float3 forward = lerp(viewForward, lightForward, 0.2f);

                    lightRotation = quaternion.LookRotation(forward, cameraUp);
                    lightDirection = float4(-forward, 0);
                    break;
                }
                default:
                {
                    Debug.LogError("Unknown shadow usage.");
                    lightRotation = default;
                    lightDirection = default;
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool GetProjectionMatrix(in float3 aabbMin, in float3 aabbMax, in ShadowCasterCullingArgs args,
            in float4x4 viewMatrix, out float4x4 projectionMatrix)
        {
            float4* aabbPoints = stackalloc float4[8]
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
            EightPointsAABB(aabbPoints, in viewMatrix, out float3 shadowMin, out float3 shadowMax);
            EightPointsAABB(args.FrustumEightCorners, in viewMatrix, out float3 frustumMin, out float3 frustumMax);

            // 剔除一定不可见的阴影
            if (any(shadowMax < frustumMin) || any(shadowMin.xy > frustumMax.xy))
            {
                projectionMatrix = default;
                return false;
            }

            if (args.Usage == ShadowUsage.Self)
            {
                // 自阴影只在自己身上，不会打到无穷远处，可以检查 z 方向进一步剔除
                if (shadowMin.z > frustumMax.z)
                {
                    projectionMatrix = default;
                    return false;
                }
            }
            else
            {
                // 包住自己还有视锥体里所有物体
                // 但包围盒太长的话深度都集中在 0 或者 1 处，精度不够，目前限制最多向后扩展 100 个单位
                shadowMin.z = clamp(frustumMin.z, shadowMin.z - 100, shadowMin.z);
            }

            // 计算投影矩阵
            float left = shadowMin.x;
            float right = shadowMax.x;
            float bottom = shadowMin.y;
            float top = shadowMax.y;
            float zNear = -shadowMax.z;
            float zFar = -shadowMin.z;
            projectionMatrix = float4x4.OrthoOffCenter(left, right, bottom, top, zNear, zFar);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EightPointsAABB([NoAlias] float4* points, in float4x4 transform, out float3 aabbMin, out float3 aabbMax)
        {
            aabbMin = float3(float.PositiveInfinity);
            aabbMax = float3(float.NegativeInfinity);

            for (int i = 0; i < 8; i++)
            {
                float3 p = mul(transform, points[i]).xyz;
                aabbMin = min(aabbMin, p);
                aabbMax = max(aabbMax, p);
            }
        }
    }
}
