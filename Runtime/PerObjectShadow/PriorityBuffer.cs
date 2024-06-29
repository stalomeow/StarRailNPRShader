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
using System.Runtime.CompilerServices;

namespace HSR.NPRShader.PerObjectShadow
{
    /// <summary>
    /// 固定大小的 buffer，只保留优先级最小的一组元素。遍历不保证顺序
    /// </summary>
    /// <typeparam name="TPriority"></typeparam>
    /// <typeparam name="TData"></typeparam>
    internal class PriorityBuffer<TPriority, TData> where TPriority : IComparable<TPriority>
    {
        private TPriority[] m_Priorities = Array.Empty<TPriority>();
        private TData[] m_Data = Array.Empty<TData>();

        public int Count { get; private set; }

        public int Capacity { get; private set; }

        public ref TData this[int index] => ref m_Data[index];

        public void Reset(int capacity)
        {
            EnsureArraySizeAndReset(ref m_Priorities, capacity);
            EnsureArraySizeAndReset(ref m_Data, capacity);
            Count = 0;
            Capacity = capacity;
        }

        public bool TryAppend(TPriority priority, in TData data)
        {
            if (Capacity <= 0)
            {
                return false;
            }

            // 维护大顶堆
            int i;

            if (Count < Capacity)
            {
                i = Count++;
                while (i > 0)
                {
                    int parent = GetParentIndex(i);
                    if (m_Priorities[parent].CompareTo(priority) >= 0)
                    {
                        break;
                    }

                    m_Priorities[i] = m_Priorities[parent];
                    m_Data[i] = m_Data[parent];
                    i = parent;
                }
            }
            else
            {
                if (m_Priorities[0].CompareTo(priority) <= 0)
                {
                    return false;
                }

                i = 0;
                while (GetChildIndex(i) < Count)
                {
                    int child = GetChildIndex(i);
                    if (child + 1 < Count && m_Priorities[child].CompareTo(m_Priorities[child + 1]) < 0)
                    {
                        child++;
                    }

                    if (m_Priorities[child].CompareTo(priority) <= 0)
                    {
                        break;
                    }

                    m_Priorities[i] = m_Priorities[child];
                    m_Data[i] = m_Data[child];
                    i = child;
                }
            }

            m_Priorities[i] = priority;
            m_Data[i] = data;
            return true;
        }

        private static int GetChildIndex(int i) => 2 * i + 1;

        private static int GetParentIndex(int i) => (i - 1) / 2;

        private static void EnsureArraySizeAndReset<T>(ref T[] array, int size)
        {
            if (array.Length < size)
            {
                array = new T[size];
            }
            else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(array, 0, array.Length);
            }
        }
    }
}
