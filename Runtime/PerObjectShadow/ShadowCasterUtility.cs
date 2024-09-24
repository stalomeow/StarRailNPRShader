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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using Debug = UnityEngine.Debug;
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
        public static bool Cull(in ShadowCasterCullingArgs args,
            out float4x4 viewMatrix, out float4x4 projectionMatrix, out float priority, out float4 lightDirection)
        {
            float3 aabbCenter = (args.AABBMin + args.AABBMax) * 0.5f;
            GetLightRotationAndDirection(in aabbCenter, in args, out quaternion lightRotation, out lightDirection);

            viewMatrix = inverse(float4x4.TRS(aabbCenter, lightRotation, 1));
            viewMatrix = mul(s_FlipZMatrix, viewMatrix); // 翻转 z 轴

            if (GetProjectionMatrix(in args, in viewMatrix, out projectionMatrix))
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
        private static void GetLightRotationAndDirection(in float3 aabbCenter, in ShadowCasterCullingArgs args,
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
                    float3 forward = normalize(lerp(viewForward, lightForward, 0.2f));

                    // 超低角度观察会出现不该有的阴影
                    float cosAngle = dot(forward, args.CasterUpVector);
                    float cosAngleClamped = clamp(cosAngle, -0.866f, 0); // 限制在 90° ~ 150° 之间
                    forward = normalize(forward + (cosAngleClamped - cosAngle) * args.CasterUpVector);

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
        private static bool GetProjectionMatrix(in ShadowCasterCullingArgs args,
            in float4x4 viewMatrix, out float4x4 projectionMatrix)
        {
            GetViewSpaceShadowAABB(in args, in viewMatrix, out float3 shadowMin, out float3 shadowMax);

            if (AdjustViewSpaceShadowAABB(in args, in viewMatrix, ref shadowMin, ref shadowMax))
            {
                DebugDrawViewSpaceAABB(in args, in shadowMin, in shadowMax, in viewMatrix, Color.blue);

                float width = shadowMax.x * 2;
                float height = shadowMax.y * 2;
                float zNear = -shadowMax.z;
                float zFar = -shadowMin.z;
                projectionMatrix = float4x4.Ortho(width, height, zNear, zFar);
                return true;
            }

            projectionMatrix = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void GetViewSpaceShadowAABB(in ShadowCasterCullingArgs args,
            in float4x4 viewMatrix, out float3 shadowMin, out float3 shadowMax)
        {
            // 8 个顶点
            float4* points = stackalloc float4[8]
            {
                float4(args.AABBMin, 1),
                float4(args.AABBMax.x, args.AABBMin.y, args.AABBMin.z, 1),
                float4(args.AABBMin.x, args.AABBMax.y, args.AABBMin.z, 1),
                float4(args.AABBMin.x, args.AABBMin.y, args.AABBMax.z, 1),
                float4(args.AABBMax.x, args.AABBMax.y, args.AABBMin.z, 1),
                float4(args.AABBMax.x, args.AABBMin.y, args.AABBMax.z, 1),
                float4(args.AABBMin.x, args.AABBMax.y, args.AABBMax.z, 1),
                float4(args.AABBMax, 1),
            };

            shadowMin = float3(float.PositiveInfinity);
            shadowMax = float3(float.NegativeInfinity);

            for (int i = 0; i < 8; i++)
            {
                float3 p = mul(viewMatrix, points[i]).xyz;
                shadowMin = min(shadowMin, p);
                shadowMax = max(shadowMax, p);
            }

            if (args.Usage == ShadowUsage.Scene)
            {
                // 理论上场景阴影可以打到无穷远处，但包围盒太长的话深度都集中在 0 或者 1 处，精度不够
                // 目前限制最多向后扩展 100 个单位
                shadowMin.z = min(shadowMin.z, shadowMax.z - 100);
            }
        }

        private ref struct TriangleData
        {
            public float3 P0;
            public float3 P1;
            public float3 P2;
            public bool IsCulled;
        }

        private enum EdgeType
        {
            Min,
            Max,
        }

        private ref struct EdgeData
        {
            public int ComponentIndex;
            public float Value;
            public EdgeType Type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool AdjustViewSpaceShadowAABB(in ShadowCasterCullingArgs args,
            in float4x4 viewMatrix, ref float3 shadowMin, ref float3 shadowMax)
        {
            float3* frustumCorners = stackalloc float3[ShadowCasterCullingArgs.FrustumCornerCount];

            for (int i = 0; i < ShadowCasterCullingArgs.FrustumCornerCount; i++)
            {
                frustumCorners[i] = mul(viewMatrix, args.FrustumEightCorners[i]).xyz;
            }

            EdgeData* edges = stackalloc EdgeData[4]
            {
                new() { ComponentIndex = 0, Value = shadowMin.x, Type = EdgeType.Min },
                new() { ComponentIndex = 0, Value = shadowMax.x, Type = EdgeType.Max },
                new() { ComponentIndex = 1, Value = shadowMin.y, Type = EdgeType.Min },
                new() { ComponentIndex = 1, Value = shadowMax.y, Type = EdgeType.Max },
            };

            // 最坏情况：1 个三角形被拆成 2**4 = 16 个三角形
            TriangleData* triangles = stackalloc TriangleData[16];

            bool isVisibleXY = false;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            for (int i = 0; i < ShadowCasterCullingArgs.FrustumTriangleCount; i++)
            {
                int triangleCount = 1;
                triangles[0].P0 = frustumCorners[ShadowCasterCullingArgs.FrustumTriangleIndices[i * 3 + 0]];
                triangles[0].P1 = frustumCorners[ShadowCasterCullingArgs.FrustumTriangleIndices[i * 3 + 1]];
                triangles[0].P2 = frustumCorners[ShadowCasterCullingArgs.FrustumTriangleIndices[i * 3 + 2]];
                triangles[0].IsCulled = false;

                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < triangleCount; k++)
                    {
                        CullTriangle(triangles, ref k, ref triangleCount, in edges[j]);
                    }
                }

                for (int j = 0; j < triangleCount; j++)
                {
                    ref TriangleData tri = ref triangles[j];

                    if (tri.IsCulled)
                    {
                        continue;
                    }

                    DebugDrawViewSpaceTriangle(in args, in tri, in viewMatrix, Color.red);

                    isVisibleXY = true;
                    minZ = min(minZ, min(tri.P0.z, min(tri.P1.z, tri.P2.z)));
                    maxZ = max(maxZ, max(tri.P0.z, max(tri.P1.z, tri.P2.z)));
                }
            }

            if (isVisibleXY && minZ < shadowMax.z && maxZ > shadowMin.z)
            {
                // 为了阴影的完整性，不应该修改 shadowMax.z
                shadowMin.z = max(shadowMin.z, minZ);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CullTriangle([NoAlias] TriangleData* triangles,
            ref int triangleIndex, ref int triangleCount, in EdgeData edge)
        {
            ref TriangleData tri = ref triangles[triangleIndex];

            if (tri.IsCulled)
            {
                return;
            }

            int insideInfo = 0b000;
            if (IsPointInsideEdge(in edge, in tri.P0)) insideInfo |= 0b001;
            if (IsPointInsideEdge(in edge, in tri.P1)) insideInfo |= 0b010;
            if (IsPointInsideEdge(in edge, in tri.P2)) insideInfo |= 0b100;

            bool isOnePointInside;

            // 将在边界里的点移动到 [P0, P1, P2] 列表的前面
            switch (insideInfo)
            {
                // 没有点在里面
                case 0b000: tri.IsCulled = true; return;

                // 有一个点在里面
                case 0b001: isOnePointInside = true; break;
                case 0b010: isOnePointInside = true; Swap(ref tri.P0, ref tri.P1); break;
                case 0b100: isOnePointInside = true; Swap(ref tri.P0, ref tri.P2); break;

                // 有两个点在里面
                case 0b011: isOnePointInside = false; break;
                case 0b101: isOnePointInside = false; Swap(ref tri.P1, ref tri.P2); break;
                case 0b110: isOnePointInside = false; Swap(ref tri.P0, ref tri.P2); break;

                // 所有点在里面
                case 0b111: return;

                // Unreachable
                default: Debug.LogError("Unknown triangleInsideInfo"); return;
            }

            if (isOnePointInside)
            {
                // 只有 P0 在里面
                float3 v01 = tri.P1 - tri.P0;
                float3 v02 = tri.P2 - tri.P0;

                float dist = edge.Value - tri.P0[edge.ComponentIndex];
                tri.P1 = v01 * rcp(v01[edge.ComponentIndex]) * dist + tri.P0;
                tri.P2 = v02 * rcp(v02[edge.ComponentIndex]) * dist + tri.P0;
            }
            else
            {
                // 只有 P2 在外面
                float3 v20 = tri.P0 - tri.P2;
                float3 v21 = tri.P1 - tri.P2;

                float dist = edge.Value - tri.P2[edge.ComponentIndex];
                float3 p0 = v20 * rcp(v20[edge.ComponentIndex]) * dist + tri.P2;
                float3 p1 = v21 * rcp(v21[edge.ComponentIndex]) * dist + tri.P2;

                // 第一个三角形
                tri.P2 = p0;

                // 把下一个三角形拷贝到列表最后新的位置上，然后把新三角形数据写入到下个位置
                // 新的三角形必定三个点都在边界内，所以 ++triangleIndex 跳过检查
                ref TriangleData newTri = ref triangles[++triangleIndex];
                triangles[triangleCount++] = newTri;

                // 第二个三角形
                newTri.P0 = p0;
                newTri.P1 = tri.P1;
                newTri.P2 = p1;
                newTri.IsCulled = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPointInsideEdge(in EdgeData edge, in float3 p)
        {
            // EdgeType.Min => p[edge.ComponentIndex] > edge.Value
            // EdgeType.Max => p[edge.ComponentIndex] < edge.Value

            float delta = p[edge.ComponentIndex] - edge.Value;
            return select(-delta, delta, edge.Type == EdgeType.Min) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref float3 a, ref float3 b) => (a, b) = (b, a);

        [Conditional("UNITY_EDITOR")]
        private static unsafe void DebugDrawViewSpaceAABB(in ShadowCasterCullingArgs args, in float3 aabbMin, in float3 aabbMax,
            in float4x4 viewMatrix, Color color)
        {
            if (args.DebugMode == 0)
            {
                return;
            }

            float3* points = stackalloc float3[8]
            {
                float3(aabbMin),
                float3(aabbMax.x, aabbMin.y, aabbMin.z),
                float3(aabbMin.x, aabbMax.y, aabbMin.z),
                float3(aabbMin.x, aabbMin.y, aabbMax.z),
                float3(aabbMax.x, aabbMax.y, aabbMin.z),
                float3(aabbMax.x, aabbMin.y, aabbMax.z),
                float3(aabbMin.x, aabbMax.y, aabbMax.z),
                float3(aabbMax),
            };

            float4x4 invViewMatrix = inverse(viewMatrix);

            // View Space 的 AABB 在 World Space 可能是斜的，所以 8 个点都要转换到 World Space
            for (int i = 0; i < 8; i++)
            {
                points[i] = mul(invViewMatrix, float4(points[i], 1)).xyz;
            }

            Debug.DrawLine(points[0], points[1], color);
            Debug.DrawLine(points[0], points[2], color);
            Debug.DrawLine(points[0], points[3], color);
            Debug.DrawLine(points[1], points[4], color);
            Debug.DrawLine(points[1], points[5], color);
            Debug.DrawLine(points[2], points[4], color);
            Debug.DrawLine(points[2], points[6], color);
            Debug.DrawLine(points[3], points[5], color);
            Debug.DrawLine(points[3], points[6], color);
            Debug.DrawLine(points[4], points[7], color);
            Debug.DrawLine(points[5], points[7], color);
            Debug.DrawLine(points[6], points[7], color);
        }

        [Conditional("UNITY_EDITOR")]
        private static void DebugDrawViewSpaceTriangle(in ShadowCasterCullingArgs args, in TriangleData triangle,
            in float4x4 viewMatrix, Color color)
        {
            if (args.DebugMode == 0)
            {
                return;
            }

            float4x4 invViewMatrix = inverse(viewMatrix);
            float3 w0 = mul(invViewMatrix, float4(triangle.P0, 1)).xyz;
            float3 w1 = mul(invViewMatrix, float4(triangle.P1, 1)).xyz;
            float3 w2 = mul(invViewMatrix, float4(triangle.P2, 1)).xyz;

            Debug.DrawLine(w0, w1, color);
            Debug.DrawLine(w0, w2, color);
            Debug.DrawLine(w1, w2, color);
        }
    }
}
