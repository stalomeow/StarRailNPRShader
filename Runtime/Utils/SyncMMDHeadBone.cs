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
using UnityEngine;

namespace HSR.NPRShader.Utils
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    // [AddComponentMenu("StarRail NPR Shader/Sync MMD Head Bone")]
    [Obsolete("Use " + nameof(StarRailCharacterRenderingController) + " instead.", true)]
    public class SyncMMDHeadBone : MonoBehaviour
    {
        private enum TransformDirection
        {
            Forward,
            Back,
            Left,
            Right,
            Up,
            Down
        }

        [SerializeField, Delayed]
        private string m_HeadBonePath = "センター/グルーブ/腰/上半身/上半身2/首/頭";
        [SerializeField]
        private TransformDirection m_HeadBoneForward = TransformDirection.Forward;
        [SerializeField]
        private TransformDirection m_HeadBoneUp = TransformDirection.Up;
        [SerializeField]
        private TransformDirection m_HeadBoneRight = TransformDirection.Right;

        [Space]

        [SerializeField, Delayed]
        private List<string> m_ShaderWhitelist = new()
        {
            "Honkai Star Rail/Character/Face",
            "Honkai Star Rail/Character/Hair"
        };

        private SkinnedMeshRenderer m_Renderer;
        private Transform m_HeadBone;

        [ContextMenu("Presets/MMD Model")]
        private void Presets_MMDModel()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, $"Assign MMD Model Preset to {name}");

            m_HeadBonePath = "センター/グルーブ/腰/上半身/上半身2/首/頭";
            m_HeadBoneForward = TransformDirection.Forward;
            m_HeadBoneUp = TransformDirection.Up;
            m_HeadBoneRight = TransformDirection.Right;

            Reinitialize();
#endif
        }

        [ContextMenu("Presets/Game Model")]
        private void Presets_GameModel()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, $"Assign Game Model Preset to {name}");

            m_HeadBonePath = "Root_M/Spine1_M/Spine2_M/Chest_M/Neck_M/Head_M";
            m_HeadBoneForward = TransformDirection.Up;
            m_HeadBoneUp = TransformDirection.Left;
            m_HeadBoneRight = TransformDirection.Back;

            Reinitialize();
#endif
        }

        private void Start() => Reinitialize();

        private void OnValidate() => Reinitialize();

        private void Reinitialize()
        {
            m_Renderer = GetComponent<SkinnedMeshRenderer>();
            m_HeadBone = m_Renderer.rootBone.Find(m_HeadBonePath);

            if (m_HeadBone == null)
            {
                Debug.LogWarning("Can not find head bone! ", this);
            }
        }

        private void Update()
        {
            if (m_HeadBone == null)
            {
                return;
            }

            Vector4 forward = GetTransformDirection(m_HeadBone, m_HeadBoneForward);
            Vector4 up = GetTransformDirection(m_HeadBone, m_HeadBoneUp);
            Vector4 right = GetTransformDirection(m_HeadBone, m_HeadBoneRight);

            foreach (Material material in m_Renderer.sharedMaterials)
            {
                if (!m_ShaderWhitelist.Contains(material.shader.name))
                {
                    continue;
                }

                material.SetVector(ShaderConstants._MMDHeadBoneForward, forward);
                material.SetVector(ShaderConstants._MMDHeadBoneUp, up);
                material.SetVector(ShaderConstants._MMDHeadBoneRight, right);
            }
        }

        private static Vector3 GetTransformDirection(Transform transform, TransformDirection direction) =>
            direction switch
            {
                TransformDirection.Forward => transform.forward,
                TransformDirection.Back => -transform.forward,
                TransformDirection.Left => -transform.right,
                TransformDirection.Right => transform.right,
                TransformDirection.Up => transform.up,
                TransformDirection.Down => -transform.up,
                _ => throw new NotSupportedException()
            };

        private static class ShaderConstants
        {
            public static readonly int _MMDHeadBoneForward = Shader.PropertyToID("_MMDHeadBoneForward");
            public static readonly int _MMDHeadBoneUp = Shader.PropertyToID("_MMDHeadBoneUp");
            public static readonly int _MMDHeadBoneRight = Shader.PropertyToID("_MMDHeadBoneRight");
        }
    }
}
