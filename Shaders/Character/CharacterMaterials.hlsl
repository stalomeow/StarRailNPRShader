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

#ifndef _CHARACTER_MATERIALS_INCLUDED
#define _CHARACTER_MATERIALS_INCLUDED

#define CHARACTER_MATERIAL_PROPERTY(type, name) \
    type name##0;                               \
    type name##1;                               \
    type name##2;                               \
    type name##3;                               \
    type name##4;                               \
    type name##5;                               \
    type name##6;                               \
    type name##7

// 根据角色的 LightMap.a 选择相应的属性

#define INIT_CHARACTER_MATERIAL_PROPERTIES_1(lightMap, type1, expr1) \
    type1 expr1##7;                                                  \
    if      (lightMap.a < 0.12) { expr1##0; }                        \
    else if (lightMap.a < 0.25) { expr1##1; }                        \
    else if (lightMap.a < 0.37) { expr1##2; }                        \
    else if (lightMap.a < 0.50) { expr1##3; }                        \
    else if (lightMap.a < 0.62) { expr1##4; }                        \
    else if (lightMap.a < 0.75) { expr1##5; }                        \
    else if (lightMap.a < 0.87) { expr1##6; }

#define INIT_CHARACTER_MATERIAL_PROPERTIES_2(lightMap, type1, expr1, type2, expr2) \
    type1 expr1##7; type2 expr2##7;                                                \
    if      (lightMap.a < 0.12) { expr1##0; expr2##0; }                            \
    else if (lightMap.a < 0.25) { expr1##1; expr2##1; }                            \
    else if (lightMap.a < 0.37) { expr1##2; expr2##2; }                            \
    else if (lightMap.a < 0.50) { expr1##3; expr2##3; }                            \
    else if (lightMap.a < 0.62) { expr1##4; expr2##4; }                            \
    else if (lightMap.a < 0.75) { expr1##5; expr2##5; }                            \
    else if (lightMap.a < 0.87) { expr1##6; expr2##6; }

#define INIT_CHARACTER_MATERIAL_PROPERTIES_3(lightMap, type1, expr1, type2, expr2, type3, expr3) \
    type1 expr1##7; type2 expr2##7; type3 expr3##7;                                              \
    if      (lightMap.a < 0.12) { expr1##0; expr2##0; expr3##0; }                                \
    else if (lightMap.a < 0.25) { expr1##1; expr2##1; expr3##1; }                                \
    else if (lightMap.a < 0.37) { expr1##2; expr2##2; expr3##2; }                                \
    else if (lightMap.a < 0.50) { expr1##3; expr2##3; expr3##3; }                                \
    else if (lightMap.a < 0.62) { expr1##4; expr2##4; expr3##4; }                                \
    else if (lightMap.a < 0.75) { expr1##5; expr2##5; expr3##5; }                                \
    else if (lightMap.a < 0.87) { expr1##6; expr2##6; expr3##6; }

#define INIT_CHARACTER_MATERIAL_PROPERTIES_4(lightMap, type1, expr1, type2, expr2, type3, expr3, type4, expr4) \
    type1 expr1##7; type2 expr2##7; type3 expr3##7; type4 expr4##7;                                            \
    if      (lightMap.a < 0.12) { expr1##0; expr2##0; expr3##0; expr4##0; }                                    \
    else if (lightMap.a < 0.25) { expr1##1; expr2##1; expr3##1; expr4##1; }                                    \
    else if (lightMap.a < 0.37) { expr1##2; expr2##2; expr3##2; expr4##2; }                                    \
    else if (lightMap.a < 0.50) { expr1##3; expr2##3; expr3##3; expr4##3; }                                    \
    else if (lightMap.a < 0.62) { expr1##4; expr2##4; expr3##4; expr4##4; }                                    \
    else if (lightMap.a < 0.75) { expr1##5; expr2##5; expr3##5; expr4##5; }                                    \
    else if (lightMap.a < 0.87) { expr1##6; expr2##6; expr3##6; expr4##6; }

#define INIT_CHARACTER_MATERIAL_PROPERTIES_5(lightMap, type1, expr1, type2, expr2, type3, expr3, type4, expr4, type5, expr5) \
    type1 expr1##7; type2 expr2##7; type3 expr3##7; type4 expr4##7; type5 expr5##7;                                          \
    if      (lightMap.a < 0.12) { expr1##0; expr2##0; expr3##0; expr4##0; expr5##0; }                                        \
    else if (lightMap.a < 0.25) { expr1##1; expr2##1; expr3##1; expr4##1; expr5##1; }                                        \
    else if (lightMap.a < 0.37) { expr1##2; expr2##2; expr3##2; expr4##2; expr5##2; }                                        \
    else if (lightMap.a < 0.50) { expr1##3; expr2##3; expr3##3; expr4##3; expr5##3; }                                        \
    else if (lightMap.a < 0.62) { expr1##4; expr2##4; expr3##4; expr4##4; expr5##4; }                                        \
    else if (lightMap.a < 0.75) { expr1##5; expr2##5; expr3##5; expr4##5; expr5##5; }                                        \
    else if (lightMap.a < 0.87) { expr1##6; expr2##6; expr3##6; expr4##6; expr5##6; }

#define INIT_CHARACTER_MATERIAL_PROPERTIES_6(lightMap, type1, expr1, type2, expr2, type3, expr3, type4, expr4, type5, expr5, type6, expr6) \
    type1 expr1##7; type2 expr2##7; type3 expr3##7; type4 expr4##7; type5 expr5##7; type6 expr6##7;                                        \
    if      (lightMap.a < 0.12) { expr1##0; expr2##0; expr3##0; expr4##0; expr5##0; expr6##0; }                                            \
    else if (lightMap.a < 0.25) { expr1##1; expr2##1; expr3##1; expr4##1; expr5##1; expr6##1; }                                            \
    else if (lightMap.a < 0.37) { expr1##2; expr2##2; expr3##2; expr4##2; expr5##2; expr6##2; }                                            \
    else if (lightMap.a < 0.50) { expr1##3; expr2##3; expr3##3; expr4##3; expr5##3; expr6##3; }                                            \
    else if (lightMap.a < 0.62) { expr1##4; expr2##4; expr3##4; expr4##4; expr5##4; expr6##4; }                                            \
    else if (lightMap.a < 0.75) { expr1##5; expr2##5; expr3##5; expr4##5; expr5##5; expr6##5; }                                            \
    else if (lightMap.a < 0.87) { expr1##6; expr2##6; expr3##6; expr4##6; expr5##6; expr6##6; }

#define INIT_CHARACTER_MATERIAL_PROPERTIES_7(lightMap, type1, expr1, type2, expr2, type3, expr3, type4, expr4, type5, expr5, type6, expr6, type7, expr7)  \
    type1 expr1##7; type2 expr2##7; type3 expr3##7; type4 expr4##7; type5 expr5##7; type6 expr6##7; type7 expr7##7;                                       \
    if      (lightMap.a < 0.12) { expr1##0; expr2##0; expr3##0; expr4##0; expr5##0; expr6##0; expr7##0; }                                                 \
    else if (lightMap.a < 0.25) { expr1##1; expr2##1; expr3##1; expr4##1; expr5##1; expr6##1; expr7##1; }                                                 \
    else if (lightMap.a < 0.37) { expr1##2; expr2##2; expr3##2; expr4##2; expr5##2; expr6##2; expr7##2; }                                                 \
    else if (lightMap.a < 0.50) { expr1##3; expr2##3; expr3##3; expr4##3; expr5##3; expr6##3; expr7##3; }                                                 \
    else if (lightMap.a < 0.62) { expr1##4; expr2##4; expr3##4; expr4##4; expr5##4; expr6##4; expr7##4; }                                                 \
    else if (lightMap.a < 0.75) { expr1##5; expr2##5; expr3##5; expr4##5; expr5##5; expr6##5; expr7##5; }                                                 \
    else if (lightMap.a < 0.87) { expr1##6; expr2##6; expr3##6; expr4##6; expr5##6; expr6##6; expr7##6; }

#define INIT_CHARACTER_MATERIAL_PROPERTIES_8(lightMap, type1, expr1, type2, expr2, type3, expr3, type4, expr4, type5, expr5, type6, expr6, type7, expr7, type8, expr8) \
    type1 expr1##7; type2 expr2##7; type3 expr3##7; type4 expr4##7; type5 expr5##7; type6 expr6##7; type7 expr7##7; type8 expr8##7;                                    \
    if      (lightMap.a < 0.12) { expr1##0; expr2##0; expr3##0; expr4##0; expr5##0; expr6##0; expr7##0; expr8##0; }                                                    \
    else if (lightMap.a < 0.25) { expr1##1; expr2##1; expr3##1; expr4##1; expr5##1; expr6##1; expr7##1; expr8##1; }                                                    \
    else if (lightMap.a < 0.37) { expr1##2; expr2##2; expr3##2; expr4##2; expr5##2; expr6##2; expr7##2; expr8##2; }                                                    \
    else if (lightMap.a < 0.50) { expr1##3; expr2##3; expr3##3; expr4##3; expr5##3; expr6##3; expr7##3; expr8##3; }                                                    \
    else if (lightMap.a < 0.62) { expr1##4; expr2##4; expr3##4; expr4##4; expr5##4; expr6##4; expr7##4; expr8##4; }                                                    \
    else if (lightMap.a < 0.75) { expr1##5; expr2##5; expr3##5; expr4##5; expr5##5; expr6##5; expr7##5; expr8##5; }                                                    \
    else if (lightMap.a < 0.87) { expr1##6; expr2##6; expr3##6; expr4##6; expr5##6; expr6##6; expr7##6; expr8##6; }

#define INIT_CHARACTER_MATERIAL_PROPERTIES_9(lightMap, type1, expr1, type2, expr2, type3, expr3, type4, expr4, type5, expr5, type6, expr6, type7, expr7, type8, expr8, type9, expr9) \
    type1 expr1##7; type2 expr2##7; type3 expr3##7; type4 expr4##7; type5 expr5##7; type6 expr6##7; type7 expr7##7; type8 expr8##7; type9 expr9##7;                                  \
    if      (lightMap.a < 0.12) { expr1##0; expr2##0; expr3##0; expr4##0; expr5##0; expr6##0; expr7##0; expr8##0; expr9##0; }                                                        \
    else if (lightMap.a < 0.25) { expr1##1; expr2##1; expr3##1; expr4##1; expr5##1; expr6##1; expr7##1; expr8##1; expr9##1; }                                                        \
    else if (lightMap.a < 0.37) { expr1##2; expr2##2; expr3##2; expr4##2; expr5##2; expr6##2; expr7##2; expr8##2; expr9##2; }                                                        \
    else if (lightMap.a < 0.50) { expr1##3; expr2##3; expr3##3; expr4##3; expr5##3; expr6##3; expr7##3; expr8##3; expr9##3; }                                                        \
    else if (lightMap.a < 0.62) { expr1##4; expr2##4; expr3##4; expr4##4; expr5##4; expr6##4; expr7##4; expr8##4; expr9##4; }                                                        \
    else if (lightMap.a < 0.75) { expr1##5; expr2##5; expr3##5; expr4##5; expr5##5; expr6##5; expr7##5; expr8##5; expr9##5; }                                                        \
    else if (lightMap.a < 0.87) { expr1##6; expr2##6; expr3##6; expr4##6; expr5##6; expr6##6; expr7##6; expr8##6; expr9##6; }

#endif
