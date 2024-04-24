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
using UnityEditor;

namespace HSR.NPRShader.Editor.MaterialGUI.Wrappers
{
    internal class HelpBoxWrapper : MaterialPropertyWrapper
    {
        private readonly MessageType m_MsgType;
        private readonly string m_Message;

        public HelpBoxWrapper(string rawArgs) : base(rawArgs)
        {
            string[] args = rawArgs.Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Trim();
            }

            m_MsgType = Enum.Parse<MessageType>(args[0]);
            m_Message = string.Join(", ", args[1..]);
        }

        public override void OnWillDrawProperty(MaterialProperty prop, string label, MaterialEditor editor)
        {
            EditorGUILayout.HelpBox(m_Message, m_MsgType);
        }
    }
}
