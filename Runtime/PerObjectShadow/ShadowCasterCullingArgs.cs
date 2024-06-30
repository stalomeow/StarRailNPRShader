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

using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace HSR.NPRShader.PerObjectShadow
{
    internal unsafe struct ShadowCasterCullingArgs
    {
        public ShadowUsage Usage;
        [NoAlias] public float4* FrustumEightCorners;
        public float4x4 CameraLocalToWorldMatrix;
        public float4x4 MainLightLocalToWorldMatrix;

        public float3 AABBMin;
        public float3 AABBMax;
        public float3 CasterUpVector;

        public const int FrustumCornerCount = 8;
        private static readonly Vector3[] s_FrustumCornerBuffer = new Vector3[4];

        public static void SetFrustumEightCorners(float4* frustumEightCorners, Camera camera)
        {
            const Camera.MonoOrStereoscopicEye Eye = Camera.MonoOrStereoscopicEye.Mono;

            var viewport = new Rect(0, 0, 1, 1);
            Transform cameraTransform = camera.transform;

            camera.CalculateFrustumCorners(viewport, camera.nearClipPlane, Eye, s_FrustumCornerBuffer);

            for (int i = 0; i < 4; i++)
            {
                Vector3 xyz = cameraTransform.TransformPoint(s_FrustumCornerBuffer[i]);
                frustumEightCorners[i] = new float4(xyz, 1);
            }

            camera.CalculateFrustumCorners(viewport, camera.farClipPlane, Eye, s_FrustumCornerBuffer);

            for (int i = 0; i < 4; i++)
            {
                Vector3 xyz = cameraTransform.TransformPoint(s_FrustumCornerBuffer[i]);
                frustumEightCorners[i + 4] = new float4(xyz, 1);
            }
        }
    }
}
