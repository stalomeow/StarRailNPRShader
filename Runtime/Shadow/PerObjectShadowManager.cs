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

using System.Collections.Generic;
using UnityEngine;

namespace HSR.NPRShader.Shadow
{
    public static class PerObjectShadowManager
    {
        private readonly struct CasterInfo
        {
            public readonly IPerObjectShadowCaster Caster;
            public readonly float Depth;

            public CasterInfo(IPerObjectShadowCaster caster, Camera camera)
            {
                Vector3 posVS = camera.worldToCameraMatrix.MultiplyPoint(caster.transform.position);
                Caster = caster;
                Depth = -posVS.z;
            }
        }

        private static readonly HashSet<IPerObjectShadowCaster> s_Casters = new();
        private static readonly List<CasterInfo> s_CachedCasterList = new();

        public static void Register(IPerObjectShadowCaster caster)
        {
            s_Casters.Add(caster);
        }

        public static void Unregister(IPerObjectShadowCaster caster)
        {
            s_Casters.Remove(caster);
        }

        public static void GetCasterList(Camera camera, List<IPerObjectShadowCaster> outCasterList, float maxShadowDistance, int maxCount)
        {
            foreach (IPerObjectShadowCaster caster in s_Casters)
            {
                if (!caster.IsActiveAndCastingShadow)
                {
                    continue;
                }

                s_CachedCasterList.Add(new CasterInfo(caster, camera));
            }

            s_CachedCasterList.Sort(CompareCaster);

            for (int i = 0; i < maxCount && i < s_CachedCasterList.Count; i++)
            {
                if (Mathf.Abs(s_CachedCasterList[i].Depth) > maxShadowDistance)
                {
                    continue;
                }

                outCasterList.Add(s_CachedCasterList[i].Caster);
            }

            s_CachedCasterList.Clear();
        }

        private static int CompareCaster(CasterInfo x, CasterInfo y)
        {
            // 尽可能选择在视野内的，离相机近的

            if (x.Depth >= 0)
            {
                return y.Depth >= 0 ? x.Depth.CompareTo(y.Depth) : -1;
            }

            return y.Depth >= 0 ? 1 : y.Depth.CompareTo(x.Depth);
        }
    }
}
