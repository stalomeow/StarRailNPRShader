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
using UnityEngine.Rendering;

namespace HSR.NPRShader.Shadow
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PerObjectShadowCaster : MonoBehaviour
    {
        [NonSerialized] private readonly List<Renderer> m_Renderers = new();

        private void OnEnable()
        {
            UpdateRendererList();
            PerObjectShadowManager.Register(this);
        }

        private void OnDisable()
        {
            PerObjectShadowManager.Unregister(this);
            m_Renderers.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Color color = Gizmos.color;
            Gizmos.color = Color.green;

            Bounds bounds = GetActiveRenderersAndBounds();
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            Gizmos.color = color;
        }

        public void UpdateRendererList()
        {
            m_Renderers.Clear();
            GetComponentsInChildren(true, m_Renderers);
        }

        public Bounds GetActiveRenderersAndBounds(List<Renderer> outRendererList = null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UpdateRendererList();
            }
#endif

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

                    outRendererList?.Add(r);
                }
            }

            return bounds;
        }
    }
}
