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
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace HSR.NPRShader.Shadow
{
    public static class PerObjectShadowManager
    {
        private static readonly Vector3[] s_FrustumCornerBuffer = new Vector3[4];
        private static readonly ShaderTagId s_LightModeTagName = new("LightMode");
        private static readonly ShaderTagId s_ShadowCasterTagId = new("HSRPerObjectShadowCaster");
        private static readonly List<Material> s_CachedMaterialList = new();

        private struct ShadowCasterManagedData
        {
            public int Version;
            public List<ShadowRendererData> Renderers;
        }

        private static Vector3[] s_WorldSpaceBoundsMinMaxFlatArray = Array.Empty<Vector3>();
        private static ShadowCasterManagedData[] s_ManagedDataArray = Array.Empty<ShadowCasterManagedData>();
        private static int NextIndex = 0;
        private static readonly Queue<int> s_FreeIndices = new();

        public static bool IsValid(in this PerObjectShadowCasterHandle handle)
        {
            if (handle.Index >= s_ManagedDataArray.Length)
            {
                return false;
            }

            return handle.Version == s_ManagedDataArray[handle.Index].Version;
        }

        public static void AllocateIfNot(ref PerObjectShadowCasterHandle handle)
        {
            if (handle.IsValid())
            {
                return;
            }

            if (s_FreeIndices.TryDequeue(out int index))
            {
                int version = s_ManagedDataArray[index].Version;
                handle = new PerObjectShadowCasterHandle(index, version);
                return;
            }

            if (NextIndex == s_ManagedDataArray.Length)
            {
                int newSize = Mathf.Max(10, s_ManagedDataArray.Length * 2);
                Array.Resize(ref s_WorldSpaceBoundsMinMaxFlatArray, newSize * 2);
                Array.Resize(ref s_ManagedDataArray, newSize);
            }

            handle = new PerObjectShadowCasterHandle(NextIndex++, 1);
            s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index] = Vector3.zero;
            s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index + 1] = Vector3.zero;
            s_ManagedDataArray[handle.Index].Version = handle.Version;
            s_ManagedDataArray[handle.Index].Renderers = new List<ShadowRendererData>();
        }

        public static void FreeIfNot(in PerObjectShadowCasterHandle handle)
        {
            if (!handle.IsValid())
            {
                return;
            }

            s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index] = Vector3.zero;
            s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index + 1] = Vector3.zero;
            s_ManagedDataArray[handle.Index].Version++;
            s_ManagedDataArray[handle.Index].Renderers.Clear();
            s_FreeIndices.Enqueue(handle.Index);
        }

        public static bool TryUpdateRenderersAndBounds(in this PerObjectShadowCasterHandle handle, IReadOnlyList<Renderer> renderers)
        {
            if (!handle.IsValid())
            {
                return false;
            }

            List<ShadowRendererData> shadowRendererList = s_ManagedDataArray[handle.Index].Renderers;
            shadowRendererList.Clear();

            Bounds bounds = default;
            bool firstBounds = true;

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];

                if (TryAppendShadowRenderers(renderer, shadowRendererList))
                {
                    if (firstBounds)
                    {
                        bounds = renderer.bounds;
                        firstBounds = false;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index] = bounds.min;
            s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index + 1] = bounds.max;
            return true;
        }

        public static bool TryUpdateBounds(in this PerObjectShadowCasterHandle handle)
        {
            if (!handle.IsValid())
            {
                return false;
            }

            Bounds bounds = default;
            bool firstBounds = true;

            foreach (ShadowRendererData renderer in s_ManagedDataArray[handle.Index].Renderers)
            {
                if (firstBounds)
                {
                    bounds = renderer.Renderer.bounds;
                    firstBounds = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.Renderer.bounds);
                }
            }

            s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index] = bounds.min;
            s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index + 1] = bounds.max;
            return true;
        }

        public static bool TryGetBounds(in this PerObjectShadowCasterHandle handle, out Bounds bounds)
        {
            bounds = default;

            if (!handle.IsValid())
            {
                return false;
            }

            Vector3 min = s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index];
            Vector3 max = s_WorldSpaceBoundsMinMaxFlatArray[2 * handle.Index + 1];
            bounds.SetMinMax(min, max);
            return true;
        }

        private static bool TryAppendShadowRenderers(Renderer renderer, List<ShadowRendererData> outShadowRenderers)
        {
#if UNITY_EDITOR
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(renderer.gameObject))
            {
                return false;
            }
#endif

            if (!renderer.enabled || renderer.shadowCastingMode == ShadowCastingMode.Off)
            {
                return false;
            }

            if (!renderer.gameObject.activeInHierarchy)
            {
                return false;
            }

            try
            {
                bool hasShadowCaster = false;
                renderer.GetSharedMaterials(s_CachedMaterialList);

                for (int i = 0; i < s_CachedMaterialList.Count; i++)
                {
                    Material material = s_CachedMaterialList[i];

                    if (TryGetShadowCasterPass(material, out int passIndex))
                    {
                        hasShadowCaster = true;
                        outShadowRenderers.Add(new ShadowRendererData(renderer, material, i, passIndex));
                    }
                }

                return hasShadowCaster;
            }
            finally
            {
                s_CachedMaterialList.Clear();
            }
        }

        private static bool TryGetShadowCasterPass(Material material, out int passIndex)
        {
            Shader shader = material.shader;

            for (int i = 0; i < shader.passCount; i++)
            {
                if (shader.FindPassTagValue(i, s_LightModeTagName) == s_ShadowCasterTagId)
                {
                    passIndex = i;
                    return true;
                }
            }

            passIndex = -1;
            return false;
        }

        public static unsafe void GetCasterList(Camera camera, Quaternion mainLightRotation, List<ShadowCasterData> outCasterList, int maxCount)
        {
            int casterCount = NextIndex;
            int casterCountTwice = casterCount * 2;

            if (casterCount <= 0)
            {
                return;
            }

            const int FrustumCornerCount = 8;
            float4* frustumCorners = stackalloc float4[FrustumCornerCount];
            CalculateFrustumEightCorners(camera, frustumCorners);

            using NativeArray<float4x4> viewProjectionMatrices = new(casterCountTwice, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            using NativeArray<float> priorities = new(casterCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            using NativeArray<int> visibleIndices = new(casterCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            int visibleCount = 0;

            fixed (void* boundsPtr = s_WorldSpaceBoundsMinMaxFlatArray)
            {
                NativeArray<float3> worldBounds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float3>(boundsPtr, casterCountTwice, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref worldBounds, AtomicSafetyHandle.Create());
#endif

                CullShadowCasterJob job = new()
                {
                    MainLightRotationInv = Quaternion.Inverse(mainLightRotation),
                    CameraPosition = camera.transform.position,
                    CameraNormalizedForward = camera.transform.forward,
                    FrustumCorners = frustumCorners,
                    FrustumCornerCount = FrustumCornerCount,
                    WorldBounds = worldBounds,
                    ViewProjectionMatrices = viewProjectionMatrices,
                    Priorities = priorities,
                    VisibleIndices = visibleIndices,
                    VisibleCount = &visibleCount,
                };
                job.ScheduleByRef(casterCount, 4).Complete();
            }

            // Debug.Log(visibleCount);

            for (int i = 0; i < visibleCount; i++)
            {
                outCasterList.Add(new ShadowCasterData
                {
                    Priority = priorities[i],
                    ViewMatrix = viewProjectionMatrices.ReinterpretLoad<Matrix4x4>(2 * i),
                    ProjectionMatrix = viewProjectionMatrices.ReinterpretLoad<Matrix4x4>(2 * i + 1),
                    ShadowRenderers = s_ManagedDataArray[visibleIndices[i]].Renderers,
                });
            }

            if (outCasterList.Count > maxCount)
            {
                outCasterList.Sort();
                outCasterList.RemoveRange(maxCount, outCasterList.Count - maxCount);
            }
        }

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
