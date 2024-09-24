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

namespace HSR.NPRShader.PerObjectShadow
{
    internal unsafe struct ShadowCasterCullingArgs
    {
        /// <summary>
        /// 非零表示启用调试模式。
        /// </summary>
        public int DebugMode; // bool 不是 blittable 类型，所以不能直接用 bool
        public ShadowUsage Usage;
        [NoAlias] public float4* FrustumEightCorners;
        public float4x4 CameraLocalToWorldMatrix;
        public float4x4 MainLightLocalToWorldMatrix;

        public float3 AABBMin;
        public float3 AABBMax;
        public float3 CasterUpVector;

        public const int FrustumCornerCount = 8;
        private static readonly Vector3[] s_FrustumCornerBuffer = new Vector3[4];

        public const int FrustumTriangleCount = 12;

        /// <summary>
        /// 将视锥体的每个面都拆成两个三角形，得到 12 个三角形。
        /// 按照该数组的顺序遍历 <see cref="FrustumEightCorners"/> 可得到这些三角形。
        /// </summary>
        /// <remarks>该数组的正确性依赖于 <see cref="SetFrustumEightCorners"/></remarks>
        public static readonly int[] FrustumTriangleIndices = new int[FrustumTriangleCount * 3]
        {
            0, 3, 1,
            1, 3, 2,
            2, 3, 7,
            2, 7, 6,
            0, 5, 4,
            0, 1, 5,
            1, 2, 5,
            2, 6, 5,
            0, 7, 3,
            0, 4, 7,
            4, 7, 5,
            5, 7, 6,
        };

        public static void SetFrustumEightCorners(float4* frustumEightCorners, Camera camera)
        {
            Transform transform = camera.transform;
            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;

            if (camera.orthographic)
            {
                // Camera.CalculateFrustumCorners 不支持正交投影

                // The orthographicSize is half the size of the vertical viewing volume.
                // The horizontal size of the viewing volume depends on the aspect ratio.
                float top = camera.orthographicSize;
                float right = top * camera.aspect;

                // 顺序要和下一个分支里的一致
                frustumEightCorners[0] = TransformPoint(transform, -right, -top, near);
                frustumEightCorners[1] = TransformPoint(transform, -right, +top, near);
                frustumEightCorners[2] = TransformPoint(transform, +right, +top, near);
                frustumEightCorners[3] = TransformPoint(transform, +right, -top, near);
                frustumEightCorners[4] = TransformPoint(transform, -right, -top, far);
                frustumEightCorners[5] = TransformPoint(transform, -right, +top, far);
                frustumEightCorners[6] = TransformPoint(transform, +right, +top, far);
                frustumEightCorners[7] = TransformPoint(transform, +right, -top, far);
            }
            else
            {
                // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.CalculateFrustumCorners.html
                // The order of the corners is lower left, upper left, upper right, lower right.

                Rect viewport = new Rect(0, 0, 1, 1);
                const Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono;

                camera.CalculateFrustumCorners(viewport, near, eye, s_FrustumCornerBuffer);
                for (int i = 0; i < 4; i++)
                {
                    frustumEightCorners[i] = TransformPoint(transform, s_FrustumCornerBuffer[i]);
                }

                camera.CalculateFrustumCorners(viewport, far, eye, s_FrustumCornerBuffer);
                for (int i = 0; i < 4; i++)
                {
                    frustumEightCorners[i + 4] = TransformPoint(transform, s_FrustumCornerBuffer[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 TransformPoint(Transform transform, float x, float y, float z)
        {
            return TransformPoint(transform, new Vector3(x, y, z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 TransformPoint(Transform transform, Vector3 point)
        {
            return new float4(transform.TransformPoint(point), 1);
        }
    }
}
