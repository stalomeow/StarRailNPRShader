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
using System.Linq;
using UnityEngine;

namespace HSR.NPRShader.Utils
{
    public static class NormalUtility
    {
        public enum StoreMode
        {
            Disabled = -1,
            ObjectSpaceTangent = 0,
            ObjectSpaceNormal = 1,
            ObjectSpaceUV7 = 2,
            TangentSpaceUV7 = 3
        }

        public static void SmoothAndStore(GameObject go, StoreMode storeMode, bool upload,
            List<GameObject> outModifiedObjs = null)
        {
            if (storeMode == StoreMode.Disabled)
            {
                return;
            }

            foreach (var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                SmoothAndStore(renderer.sharedMesh, storeMode, upload);
                outModifiedObjs?.Add(renderer.gameObject);
            }

            foreach (var filter in go.GetComponentsInChildren<MeshFilter>(false))
            {
                SmoothAndStore(filter.sharedMesh, storeMode, upload);
                outModifiedObjs?.Add(filter.gameObject);
            }
        }

        public static void SmoothAndStore(Mesh mesh, StoreMode storeMode, bool upload)
        {
            if (storeMode == StoreMode.Disabled)
            {
                return;
            }

            CheckMeshTopology(mesh, MeshTopology.Triangles);

            Vector3[] vertices = mesh.vertices;
            List<int> indices = new();
            Vector3[] normals = new Vector3[vertices.Length];
            Dictionary<Vector3, Vector3> weightedNormals = new();

            // 一些 MMD 模型有背面顶点，如果整个 Mesh 一起计算平滑法线，正反法线会相互抵消，最后变成零向量
            // 有背面顶点是因为材质、法线和正面的不一样，所以背面顶点和对应的正面顶点不在一个 SubMesh 里
            // 下面，以 SubMesh 为单位分开计算
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                // SubMeshDescriptor subMesh = mesh.GetSubMesh(subMeshIndex);
                mesh.GetIndices(indices, subMeshIndex, applyBaseVertex: true); // subMesh.baseVertex

                for (int i = 0; i <= indices.Count - 3; i += 3)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        // Unity 中满足左手定则
                        (int offset1, int offset2) = j switch
                        {
                            0 => (1, 2),
                            1 => (2, 0),
                            2 => (0, 1),
                            _ => throw new NotSupportedException() // Unreachable
                        };

                        Vector3 vertex = vertices[indices[i + j]];
                        Vector3 vec1 = vertices[indices[i + offset1]] - vertex;
                        Vector3 vec2 = vertices[indices[i + offset2]] - vertex;
                        Vector3 normal = GetWeightedNormal(vec1, vec2);

                        // 这里应该可以直接用 Vector3 当 Key
                        // TODO: 如果有精度问题再改
                        weightedNormals.TryAdd(vertex, Vector3.zero);
                        weightedNormals[vertex] += normal;
                    }
                }

                // for (int i = 0; i < subMesh.vertexCount; i++)
                // {
                //     int vertexIndex = subMesh.firstVertex + i;
                //     Vector3 vertex = vertices[vertexIndex];
                //
                //     // 看 Unity 官方文档
                //     // 顶点可能不在当前 SubMesh 里
                //     // 顶点也可能同时在多个 SubMesh 里
                //
                //     if (weightedNormals.TryGetValue(vertex, out Vector3 n))
                //     {
                //         normals[vertexIndex] += n;
                //     }
                // }

                foreach (int vertexIndex in indices.Distinct())
                {
                    Vector3 vertex = vertices[vertexIndex];
                    normals[vertexIndex] += weightedNormals[vertex];
                }

                indices.Clear();
                weightedNormals.Clear();
            }

            for (int i = 0; i < normals.Length; i++)
            {
                // 没必要除以所有权重之和，它不会改变方向。直接归一化就行
                normals[i] = normals[i].normalized;
            }

            StoreNormals(normals, mesh, storeMode, upload);
        }

        private static Vector3 GetWeightedNormal(Vector3 vec1, Vector3 vec2)
        {
            // Vector3 在归一化的时候有做精度限制
            // 模型太小时，直接用 Vector3 算出来会有很多零向量
            // 这里用 double 先放大数倍然后再算
            const double scale = 1e8;

            double x1 = vec1.x * scale;
            double y1 = vec1.y * scale;
            double z1 = vec1.z * scale;
            double len1 = Math.Sqrt(x1 * x1 + y1 * y1 + z1 * z1);

            double x2 = vec2.x * scale;
            double y2 = vec2.y * scale;
            double z2 = vec2.z * scale;
            double len2 = Math.Sqrt(x2 * x2 + y2 * y2 + z2 * z2);

            // normal = cross(vec1, vec2)
            double nx = y1 * z2 - z1 * y2;
            double ny = z1 * x2 - x1 * z2;
            double nz = x1 * y2 - y1 * x2;
            double lenNormal = Math.Sqrt(nx * nx + ny * ny + nz * nz);

            // angle between vec1 and vec2
            double angle = Math.Acos((x1 * x2 + y1 * y2 + z1 * z2) / (len1 * len2));

            // normalize & weight
            nx = nx * angle / lenNormal;
            ny = ny * angle / lenNormal;
            nz = nz * angle / lenNormal;
            return new Vector3((float)nx, (float)ny, (float)nz);
        }

        private static void CheckMeshTopology(Mesh mesh, MeshTopology topology)
        {
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (mesh.GetTopology(i) != topology)
                {
                    throw new InvalidOperationException(
                        $"Invalid mesh topology (SubMesh {i}). Expected is {topology}.");
                }
            }
        }

        private static void StoreNormals(Vector3[] newNormals, Mesh mesh, StoreMode mode, bool upload)
        {
            if (mode == StoreMode.Disabled)
            {
                return;
            }

            switch (mode)
            {
                case StoreMode.ObjectSpaceTangent:
                    mesh.SetTangents(Array.ConvertAll(newNormals, n => (Vector4)n));
                    break;

                case StoreMode.ObjectSpaceNormal:
                    mesh.SetNormals(newNormals);
                    break;

                case StoreMode.ObjectSpaceUV7:
                    mesh.SetUVs(6, newNormals);
                    break;

                case StoreMode.TangentSpaceUV7:
                {
                    Vector4[] tangents = mesh.tangents;
                    Vector3[] normals = mesh.normals;

                    for (int i = 0; i < newNormals.Length; i++)
                    {
                        Vector3 normal = normals[i];
                        Vector3 tangent = tangents[i];
                        Vector3 binormal = (Vector3.Cross(normal, tangent) * tangents[i].w).normalized;

                        // tbn 是正交矩阵
                        Matrix4x4 tbn = new(tangent, binormal, normal, Vector4.zero);
                        newNormals[i] = tbn.transpose.MultiplyVector(newNormals[i]);
                    }

                    goto case StoreMode.ObjectSpaceUV7;
                }

                default:
                    throw new NotImplementedException();
            }

            if (upload)
            {
                mesh.UploadMeshData(false);
            }
        }
    }
}
