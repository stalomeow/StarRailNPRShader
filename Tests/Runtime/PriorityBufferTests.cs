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
using System.Linq;
using HSR.NPRShader.PerObjectShadow;
using NUnit.Framework;

namespace HSR.NPRShader.Tests
{
    internal class PriorityBufferTests
    {
        [Test]
        public void PriorityBuffer_TryAppend()
        {
            (int priority, int value)[] nums =
            {
                (1, 1), (2, 2), (3, 3), (4, 4), (5, 5),
                (1, 5), (2, 4), (3, 3), (4, 2), (5, 1),
                (-1, 1), (-2, 2), (-3, 3), (-4, 4), (-5, 5),
                (-1, 5), (-2, 4), (-3, 3), (-4, 2), (-5, 1),
            };

            Test(40, nums);
            Test(20, nums);
            Test(10, nums);
            Test(5, nums);
            Test(0, nums);
        }

        private static void Test(int maxCount, params (int priority, int value)[] data)
        {
            var buffer = new PriorityBuffer<int, int>();
            buffer.Reset(maxCount);
            Assert.Zero(buffer.Count);
            Assert.AreEqual(maxCount, buffer.Capacity);

            foreach (var (priority, value) in data)
            {
                buffer.TryAppend(priority, value);
            }

            List<int> values = data.
                OrderBy(x => x.priority).
                Select(x => x.value).
                Take(maxCount).
                ToList();
            Assert.AreEqual(values.Count, buffer.Count);

            for (int i = 0; i < buffer.Count; i++)
            {
                Assert.IsTrue(values.Remove(buffer[i]));
            }
            Assert.Zero(values.Count);

            buffer.Reset(maxCount);
            Assert.Zero(buffer.Count);
        }
    }
}
