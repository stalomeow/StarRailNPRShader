using System.Collections.Generic;
using UnityEngine;

namespace HSR.Utils
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class SyncMMDHeadBone : MonoBehaviour
    {
        [SerializeField, Delayed]
        private string m_HeadBonePath = "センター/グルーブ/腰/上半身/上半身2/首/頭";

        [SerializeField, Delayed]
        private List<string> m_ShaderWhitelist = new()
        {
            "Honkai Star Rail/Character/Face",
            "Honkai Star Rail/Character/Hair"
        };

        private SkinnedMeshRenderer m_Renderer;
        private Transform m_HeadBone;

        private void Start() => Init();

        private void OnValidate() => Init();

        private void Init()
        {
            m_Renderer = GetComponent<SkinnedMeshRenderer>();
            m_HeadBone = m_Renderer.rootBone.Find(m_HeadBonePath);
        }

        private void Update()
        {
            if (m_HeadBone == null)
            {
                return;
            }

            Vector4 forward = m_HeadBone.forward;
            Vector4 up = m_HeadBone.up;
            Vector4 right = m_HeadBone.right;

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

        private static class ShaderConstants
        {
            public static readonly int _MMDHeadBoneForward = Shader.PropertyToID("_MMDHeadBoneForward");
            public static readonly int _MMDHeadBoneUp = Shader.PropertyToID("_MMDHeadBoneUp");
            public static readonly int _MMDHeadBoneRight = Shader.PropertyToID("_MMDHeadBoneRight");
        }
    }
}
