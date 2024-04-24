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
using UnityEngine;

namespace HSR.NPRShader.Editor.Automation
{
    [Serializable]
    public class TextureJsonData
    {
        [SerializeField] private string m_Name;
        [SerializeField] private long m_PathId;
        [SerializeField] private bool m_IsNull;
        [SerializeField] private Vector2 m_Scale;
        [SerializeField] private Vector2 m_Offset;

        public TextureJsonData() : this(string.Empty, 0, true, Vector2.one, Vector2.zero) { }

        public TextureJsonData(string name, long pathId, bool isNull, Vector2 scale, Vector2 offset)
        {
            m_Name = name;
            m_PathId = pathId;
            m_IsNull = isNull;
            m_Scale = scale;
            m_Offset = offset;
        }

        public string Name => m_Name;

        public long PathId => m_PathId;

        public bool IsNull => m_IsNull;

        public Vector2 Scale => m_Scale;

        public Vector2 Offset => m_Offset;
    }
}
