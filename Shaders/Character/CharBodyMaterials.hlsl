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

#ifndef _CHAR_BODY_MATERIALS_INCLUDED
#define _CHAR_BODY_MATERIALS_INCLUDED

int GetCharMaterialId(float4 lightMap)
{
    return clamp(floor(8 * lightMap.a), 0, 7);
}

#define DEF_CHAR_MAT_PROP(type, name) \
    type name##0;                     \
    type name##1;                     \
    type name##2;                     \
    type name##3;                     \
    type name##4;                     \
    type name##5;                     \
    type name##6;                     \
    type name##7;

#define SETUP_CHAR_MAT_PROP(type, name, materialId) \
    type name; {                                    \
        type arr[] = {                              \
            name##0,                                \
            name##1,                                \
            name##2,                                \
            name##3,                                \
            name##4,                                \
            name##5,                                \
            name##6,                                \
            name##7                                 \
        };                                          \
        name = arr[materialId];                     \
    }

#endif
