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
using System.Diagnostics;
using HSR.NPRShader.Shadow;
using HSR.NPRShader.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace HSR.NPRShader
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("StarRail NPR Shader/StarRail Character Rendering Controller")]
    public sealed class StarRailCharacterRenderingController : MonoBehaviour, IPerObjectShadowCaster
    {
        private enum TransformDirection
        {
            [InspectorName("Use Forward Vector")] Forward,
            [InspectorName("Use Back Vector")] Back,
            [InspectorName("Use Left Vector")] Left,
            [InspectorName("Use Right Vector")] Right,
            [InspectorName("Use Up Vector")] Up,
            [InspectorName("Use Down Vector")] Down
        }

        [SerializeField] [Range(0, 1)] private float m_RampCoolWarmMix = 1;
        [SerializeField] [Range(0, 1)] private float m_DitherAlpha = 1;
        [SerializeField] [Range(0, 1)] private float m_ExCheekIntensity = 0;
        [SerializeField] [Range(0, 1)] private float m_ExShyIntensity = 0;
        [SerializeField] [Range(0, 1)] private float m_ExShadowIntensity = 0;
        [SerializeField] private bool m_IsCastingShadow = true;

        [SerializeField] private Transform m_MMDHeadBone;
        [SerializeField] private TransformDirection m_MMDHeadBoneForward = TransformDirection.Forward;
        [SerializeField] private TransformDirection m_MMDHeadBoneUp = TransformDirection.Up;
        [SerializeField] private TransformDirection m_MMDHeadBoneRight = TransformDirection.Right;

        [NonSerialized] private readonly List<Renderer> m_Renderers = new();
        [NonSerialized] private readonly Lazy<MaterialPropertyBlock> m_PropertyBlock = new();

        public float RampCoolWarmMix
        {
            get => m_RampCoolWarmMix;
            set => m_RampCoolWarmMix = Mathf.Clamp01(value);
        }

        public float DitherAlpha
        {
            get => m_DitherAlpha;
            set => m_DitherAlpha = Mathf.Clamp01(value);
        }

        public float ExpressionCheekIntensity
        {
            get => m_ExCheekIntensity;
            set => m_ExCheekIntensity = Mathf.Clamp01(value);
        }

        public float ExpressionShyIntensity
        {
            get => m_ExShyIntensity;
            set => m_ExShyIntensity = Mathf.Clamp01(value);
        }

        public float ExpressionShadowIntensity
        {
            get => m_ExShadowIntensity;
            set => m_ExShadowIntensity = Mathf.Clamp01(value);
        }

        public bool IsCastingShadow
        {
            get => m_IsCastingShadow;
            set => m_IsCastingShadow = value;
        }

        public MaterialPropertyBlock PropertyBlock => m_PropertyBlock.Value;

        private void OnEnable()
        {
            UpdateRendererList();
            PerObjectShadowManager.Register(this);
        }

        private void OnDisable()
        {
            PerObjectShadowManager.Unregister(this);

            // 这里就不要更新 RendererList 了，只清除之前的
            foreach (Renderer renderer in m_Renderers)
            {
                renderer.SetPropertyBlock(null);
            }

            m_Renderers.Clear();
            m_PropertyBlock.Value.Clear();
        }

        private void Update()
        {
            MaterialPropertyBlock properties = m_PropertyBlock.Value;

            properties.SetFloat(PropertyIds._RampCoolWarmLerpFactor, m_RampCoolWarmMix);
            properties.SetFloat(PropertyIds._DitherAlpha, m_DitherAlpha);
            properties.SetFloat(PropertyIds._ExCheekIntensity, m_ExCheekIntensity);
            properties.SetFloat(PropertyIds._ExShyIntensity, m_ExShyIntensity);
            properties.SetFloat(PropertyIds._ExShadowIntensity, m_ExShadowIntensity);

            if (m_MMDHeadBone != null)
            {
                Vector4 forward = GetTransformDirection(m_MMDHeadBone, m_MMDHeadBoneForward);
                Vector4 up = GetTransformDirection(m_MMDHeadBone, m_MMDHeadBoneUp);
                Vector4 right = GetTransformDirection(m_MMDHeadBone, m_MMDHeadBoneRight);

                properties.SetVector(PropertyIds._MMDHeadBoneForward, forward);
                properties.SetVector(PropertyIds._MMDHeadBoneUp, up);
                properties.SetVector(PropertyIds._MMDHeadBoneRight, right);
            }

            ForceUpdateRendererListInEditor();

            foreach (Renderer renderer in m_Renderers)
            {
                renderer.SetPropertyBlock(properties);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color color = Gizmos.color;
            Gizmos.color = Color.green;

            IPerObjectShadowCaster caster = this;
            Bounds bounds = caster.GetActiveRenderersAndBounds(null);
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            Gizmos.color = color;
        }

        public void UpdateRendererList()
        {
            m_Renderers.Clear();
            GetComponentsInChildren(true, m_Renderers);
        }

        [Conditional("UNITY_EDITOR")]
        private void ForceUpdateRendererListInEditor()
        {
            if (!Application.isPlaying)
            {
                UpdateRendererList();
            }
        }

        bool IPerObjectShadowCaster.IsActiveAndCastingShadow => isActiveAndEnabled && m_IsCastingShadow;

        Bounds IPerObjectShadowCaster.GetActiveRenderersAndBounds(List<Renderer> outRendererListOrNull)
        {
            ForceUpdateRendererListInEditor();

            Bounds bounds = default;
            bool first = true;

            foreach (var r in m_Renderers)
            {
                if (r.gameObject.activeInHierarchy && r.enabled && r.shadowCastingMode != ShadowCastingMode.Off)
                {
                    if (first)
                    {
                        bounds = r.bounds;
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }

                    outRendererListOrNull?.Add(r);
                }
            }

            return bounds;
        }

        private static Vector3 GetTransformDirection(Transform transform, TransformDirection direction)
        {
            return direction switch
            {
                TransformDirection.Forward => transform.forward,
                TransformDirection.Back => -transform.forward,
                TransformDirection.Left => -transform.right,
                TransformDirection.Right => transform.right,
                TransformDirection.Up => transform.up,
                TransformDirection.Down => -transform.up,
                _ => throw new NotSupportedException()
            };
        }

        private static class PropertyIds
        {
            public static readonly int _RampCoolWarmLerpFactor = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _DitherAlpha = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _ExCheekIntensity = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _ExShyIntensity = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _ExShadowIntensity = StringHelpers.ShaderPropertyIDFromMemberName();

            public static readonly int _MMDHeadBoneForward = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _MMDHeadBoneUp = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _MMDHeadBoneRight = StringHelpers.ShaderPropertyIDFromMemberName();
        }
    }
}
