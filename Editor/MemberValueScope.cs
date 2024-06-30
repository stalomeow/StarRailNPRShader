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
using System.Linq.Expressions;
using System.Reflection;

namespace HSR.NPRShader.Editor
{
    internal readonly ref struct MemberValueScope<T>
    {
        private readonly MemberInfo m_Member;
        private readonly object m_TargetObj;
        private readonly object m_PrevValue;

        public MemberValueScope(Expression<Func<T>> fieldOrProp, T tempValue)
            : this(fieldOrProp, tempValue, false) { }

        public MemberValueScope(Expression<Func<T>> fieldOrProp, Func<T, T> tempValueFactory)
            : this(fieldOrProp, tempValueFactory, true) { }

        private MemberValueScope(LambdaExpression fieldOrProp, object tempValue, bool tempValueIsFactory)
        {
            if (fieldOrProp.Body is not MemberExpression memberExpr)
            {
                throw new ArgumentException();
            }

            m_Member = memberExpr.Member;

            if (memberExpr.Expression is { } expr)
            {
                Delegate objGetter = Expression.Lambda(expr).Compile();
                m_TargetObj = objGetter.DynamicInvoke();
            }
            else
            {
                m_TargetObj = null;
            }

            UpdateMemberValue(m_Member, m_TargetObj, tempValue, out m_PrevValue, tempValueIsFactory);
        }

        public void Dispose()
        {
            UpdateMemberValue(m_Member, m_TargetObj, m_PrevValue, out _, false);
        }

        private static void UpdateMemberValue(
            MemberInfo member,
            object targetObj,
            object newValue,
            out object prevValue,
            bool newValueFromFactory)
        {
            switch (member)
            {
                case FieldInfo fieldInfo:
                    prevValue = fieldInfo.GetValue(targetObj);

                    if (newValueFromFactory)
                    {
                        Delegate factory = (Delegate)newValue;
                        newValue = factory.DynamicInvoke(prevValue);
                    }

                    fieldInfo.SetValue(targetObj, newValue);
                    break;

                case PropertyInfo propInfo:
                    prevValue = propInfo.GetValue(targetObj);

                    if (newValueFromFactory)
                    {
                        Delegate factory = (Delegate)newValue;
                        newValue = factory.DynamicInvoke(prevValue);
                    }

                    propInfo.SetValue(targetObj, newValue);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
