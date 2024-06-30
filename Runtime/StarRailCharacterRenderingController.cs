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
using HSR.NPRShader.PerObjectShadow;
using HSR.NPRShader.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace HSR.NPRShader
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("StarRail NPR Shader/StarRail Character Rendering Controller")]
    public sealed class StarRailCharacterRenderingController : MonoBehaviour, IShadowCaster
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

        [NonSerialized] private int m_ShadowCasterId = -1;
        [NonSerialized] private readonly List<Renderer> m_Renderers = new();
        [NonSerialized] private readonly ShadowRendererList m_ShadowRendererList = new();
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

        int IShadowCaster.Id
        {
            get => m_ShadowCasterId;
            set => m_ShadowCasterId = value;
        }

        ShadowRendererList.ReadOnly IShadowCaster.RendererList => m_ShadowRendererList.AsReadOnly();

        Transform IShadowCaster.Transform => transform;

        bool IShadowCaster.CanCastShadow(ShadowUsage usage)
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            return usage != ShadowUsage.Scene || IsCastingShadow;
        }

        private void OnEnable()
        {
            UpdateRendererList(fullUpdate: true);
            ShadowCasterManager.Register(this);
        }

        private void OnDisable()
        {
            ShadowCasterManager.Unregister(this);

            m_Renderers.Clear();
            m_ShadowRendererList.Clear();

            if (m_PropertyBlock.IsValueCreated)
            {
                m_PropertyBlock.Value.Clear();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            // Editor 中 Shader 可以任意修改，所以每次都要更新
            UpdateRendererList(fullUpdate: !Application.isPlaying);
#else
            UpdateMaterialProperties();
#endif
        }

        private void OnDrawGizmosSelected()
        {
            if (!m_ShadowRendererList.TryGetWorldBounds(ShadowUsage.Scene, out Bounds bounds))
            {
                return;
            }

            Color color = Gizmos.color;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.color = color;
        }

        public void UpdateRendererList()
        {
            UpdateRendererList(true);
        }

        private void UpdateRendererList(bool fullUpdate)
        {
            if (fullUpdate)
            {
                m_Renderers.Clear();
                GetComponentsInChildren(true, m_Renderers);
            }

            // 因为 Build 之后需要实例化 Material，所以必须要先设置 MaterialProperties
            // 这样后面访问 SharedMaterial 时才能拿到正确的材质
            UpdateMaterialProperties();
            UpdateShadowRendererList();
        }

        private void UpdateMaterialProperties()
        {
            List<(int, float)> floats = ListPool<(int, float)>.Get();
            List<(int, Vector4)> vectors = ListPool<(int, Vector4)>.Get();

            try
            {
                floats.Add((PropertyIds._RampCoolWarmLerpFactor, m_RampCoolWarmMix));
                floats.Add((PropertyIds._DitherAlpha, m_DitherAlpha));
                floats.Add((PropertyIds._ExCheekIntensity, m_ExCheekIntensity));
                floats.Add((PropertyIds._ExShyIntensity, m_ExShyIntensity));
                floats.Add((PropertyIds._ExShadowIntensity, m_ExShadowIntensity));
                floats.Add((PropertyIds._PerObjShadowCasterId, m_ShadowCasterId));

                if (m_MMDHeadBone != null)
                {
                    Vector4 forward = GetTransformDirection(m_MMDHeadBone, m_MMDHeadBoneForward);
                    Vector4 up = GetTransformDirection(m_MMDHeadBone, m_MMDHeadBoneUp);
                    Vector4 right = GetTransformDirection(m_MMDHeadBone, m_MMDHeadBoneRight);

                    vectors.Add((PropertyIds._MMDHeadBoneForward, forward));
                    vectors.Add((PropertyIds._MMDHeadBoneUp, up));
                    vectors.Add((PropertyIds._MMDHeadBoneRight, right));
                }

                RendererUtility.SetMaterialPropertiesPerRenderer(m_Renderers, m_PropertyBlock, floats, vectors);
            }
            finally
            {
                ListPool<(int, float)>.Release(floats);
                ListPool<(int, Vector4)>.Release(vectors);
            }
        }

        private void UpdateShadowRendererList()
        {
            m_ShadowRendererList.Clear();
            foreach (Renderer r in m_Renderers)
            {
                m_ShadowRendererList.Add(r);
            }
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
            public static readonly int _RampCoolWarmLerpFactor = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _DitherAlpha = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _ExCheekIntensity = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _ExShyIntensity = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _ExShadowIntensity = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _PerObjShadowCasterId = MemberNameHelpers.ShaderPropertyID();

            public static readonly int _MMDHeadBoneForward = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _MMDHeadBoneUp = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _MMDHeadBoneRight = MemberNameHelpers.ShaderPropertyID();
        }
    }
}
