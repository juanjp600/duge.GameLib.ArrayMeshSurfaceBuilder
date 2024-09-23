/*
    duge.GameLib.ArrayMeshSurfaceBuilder

    Copyright (C) 2024 Juan Pablo Arce

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using GodotArray = Godot.Collections.Array;
using GodotMaterial = Godot.Material;

namespace duge.GameLib.ArrayMeshSurfaceBuilder;

public sealed partial class ArrayMeshSurfaceBuilder : IDisposable
{
    public sealed class Surface
    {
        public readonly record struct Vertex(
            Vector3 Position,
            Vector3 Normal,
            Color Color,
            Vector2 Uv);
        public readonly record struct Triangle(
            int Index0,
            int Index1,
            int Index2);

        public string Name = "";
        public List<Vertex> Vertices = new();
        public List<Triangle> Triangles = new();
        public GodotMaterial? Material = null;

        private static void ResizeList<T>(List<T?> list, int newSize)
        {
            if (newSize < 0) { return; }

            if (newSize < list.Count)
            {
                list.RemoveRange(newSize, list.Count - newSize);
            }
            else if (newSize > list.Count)
            {
                list.AddRange(Enumerable.Repeat(default(T), newSize - list.Count));
            }
        }

        public void PopulateDotnetLists(
            List<Vector3> vertexPositionsList,
            List<Vector3> vertexNormalsList,
            List<Color> vertexColorsList,
            List<Vector2> vertexUvsList,
            List<int> indicesList)
        {
            PopulateVertexList(list: vertexPositionsList, getter: static vertex => vertex.Position);
            PopulateVertexList(list: vertexNormalsList, getter: static vertex => vertex.Normal);
            PopulateVertexList(list: vertexColorsList, getter: static vertex => vertex.Color);
            PopulateVertexList(list: vertexUvsList, getter: static vertex => vertex.Uv);

            ResizeList(
                list: indicesList,
                newSize: Triangles.Count * 3);
            for (int index = 0; index < Triangles.Count; index++)
            {
                Triangle triangle = Triangles[index];
                indicesList[(index * 3) + 0] = triangle.Index0;
                indicesList[(index * 3) + 1] = triangle.Index1;
                indicesList[(index * 3) + 2] = triangle.Index2;
            }

            void PopulateVertexList<T>(List<T?> list, Func<Vertex, T> getter)
            {
                ResizeList(
                    list: list,
                    newSize: Vertices.Count);
                for (int index = 0; index < Vertices.Count; index++)
                {
                    list[index] = getter(Vertices[index]);
                }
            }
        }

        public static Surface? CreateFromCompositeArray(GodotArray compositeArray, Mesh.PrimitiveType primitiveType)
        {
            if (!TryConvertVariantToDotnetArray(
                    variant: compositeArray[(int)Mesh.ArrayType.Vertex],
                    variantType: Variant.Type.PackedVector3Array,
                    conversionFunction: static variant => variant.AsVector3Array(),
                    out Vector3[]? vertexPositionsDotnet))
            {
                return null;
            }
            if (!TryConvertVariantToDotnetArray(
                    variant: compositeArray[(int)Mesh.ArrayType.Normal],
                    variantType: Variant.Type.PackedVector3Array,
                    conversionFunction: static variant => variant.AsVector3Array(),
                    out Vector3[]? vertexNormalsDotnet))
            {
                return null;
            }
            if (!TryConvertVariantToDotnetArray(
                    variant: compositeArray[(int)Mesh.ArrayType.Color],
                    variantType: Variant.Type.PackedColorArray,
                    conversionFunction: static variant => variant.AsColorArray(),
                    out Color[]? vertexColorsDotnet))
            {
                vertexColorsDotnet = Enumerable.Repeat(Color.FromHtml("#ffffff"), vertexPositionsDotnet.Length).ToArray();
            }
            if (!TryConvertVariantToDotnetArray(
                    variant: compositeArray[(int)Mesh.ArrayType.TexUV],
                    variantType: Variant.Type.PackedVector2Array,
                    conversionFunction: static variant => variant.AsVector2Array(),
                    out Vector2[]? vertexUvsDotnet))
            {
                return null;
            }
            if (!TryConvertVariantToDotnetArray(
                    variant: compositeArray[(int)Mesh.ArrayType.Index],
                    variantType: Variant.Type.PackedInt32Array,
                    conversionFunction: static variant => variant.AsInt32Array(),
                    out int[]? indicesDotnet))
            {
                if (primitiveType == Mesh.PrimitiveType.Triangles)
                {
                    indicesDotnet = Enumerable.Range(0, vertexPositionsDotnet.Length / 3)
                        .SelectMany(i => new[] { i * 3 + 0, i * 3 + 1, i * 3 + 2 })
                        .ToArray();
                }
                else
                {
                    return null;
                }
            }
            if (indicesDotnet.Length % 3 != 0) { return null; }
            if (vertexPositionsDotnet.Length != vertexNormalsDotnet.Length) { return null; }
            if (vertexPositionsDotnet.Length != vertexColorsDotnet.Length) { return null; }
            if (vertexPositionsDotnet.Length != vertexUvsDotnet.Length) { return null; }

            Surface newSurface = new Surface();
            ResizeList(
                list: newSurface.Vertices,
                newSize: vertexPositionsDotnet.Length);
            for (int i = 0; i < vertexPositionsDotnet.Length; i++)
            {
                newSurface.Vertices[i] = new Vertex(
                    Position: vertexPositionsDotnet[i],
                    Normal: vertexNormalsDotnet[i],
                    Color: vertexColorsDotnet[i],
                    Uv: vertexUvsDotnet[i]);
            }
            ResizeList(
                list: newSurface.Triangles,
                newSize: indicesDotnet.Length / 3);
            for (int i = 0; i < indicesDotnet.Length; i += 3)
            {
                newSurface.Triangles[i / 3] = new Triangle(
                    Index0: indicesDotnet[i + 0],
                    Index1: indicesDotnet[i + 1],
                    Index2: indicesDotnet[i + 2]);
            }

            return newSurface;

            static bool TryConvertVariantToDotnetArray<T>(
                Variant variant,
                Variant.Type variantType,
                Func<Variant, T[]> conversionFunction,
                [NotNullWhen(returnValue: true)]out T[]? outDotnetArray)
            {
                outDotnetArray = null;
                if (variant.VariantType != variantType) { return false; }

                outDotnetArray = conversionFunction(variant);
                return true;
            }
        }

        public static Surface? CreateFromPrimitiveMesh(PrimitiveMesh primitiveMesh)
            => CreateFromCompositeArray(primitiveMesh.GetMeshArrays(), Mesh.PrimitiveType.Triangles);

        public static Surface? CreateFromArrayMeshSurface(ArrayMesh arrayMesh, int surfaceIndex)
            => CreateFromCompositeArray(arrayMesh.SurfaceGetArrays(surfaceIndex), arrayMesh.SurfaceGetPrimitiveType(surfaceIndex));

        public ConcavePolygonShape3D ToShape(Vector3 scale)
            => new ConcavePolygonShape3D
            {
                Data = Triangles.SelectMany(triangle => new[]
                {
                    Vertices[triangle.Index0].Position * scale,
                    Vertices[triangle.Index1].Position * scale,
                    Vertices[triangle.Index2].Position * scale
                }).ToArray()
            };

        public void AutoCalculateNormals()
        {
            Dictionary<int, List<Triangle>> vertexIndexToTriangles = Enumerable.Range(0, Vertices.Count)
                .Select(i => (i, new List<Triangle>()))
                .ToDictionary();
            foreach (Triangle triangle in Triangles)
            {
                vertexIndexToTriangles[triangle.Index0].Add(triangle);
                vertexIndexToTriangles[triangle.Index1].Add(triangle);
                vertexIndexToTriangles[triangle.Index2].Add(triangle);
            }
            foreach (KeyValuePair<int, List<Triangle>> kvp in vertexIndexToTriangles)
            {
                Vector3 normal = kvp.Value
                    .Select(tri
                        => (Vertices[tri.Index2].Position - Vertices[tri.Index0].Position)
                        .Cross(Vertices[tri.Index1].Position - Vertices[tri.Index0].Position)
                        .Normalized())
                    .Aggregate(Vector3.Zero, static (v0, v1) => v0 + v1)
                        .Normalized();
                Vertices[kvp.Key] = Vertices[kvp.Key] with { Normal = normal };
            }
        }

        public void RemoveOrphanVertices()
        {
            bool[] referenced = new bool[Vertices.Count];
            foreach (Triangle triangle in Triangles)
            {
                referenced[triangle.Index0] = true;
                referenced[triangle.Index1] = true;
                referenced[triangle.Index2] = true;
            }

            List<int> removed = new List<int>();
            for (int i = referenced.Length - 1; i >= 0; i--)
            {
                if (referenced[i]) { continue; }

                removed.Add(i);
                Vertices.RemoveAt(i);
            }

            for (int i = 0; i < Triangles.Count; i++)
            {
                int FixIndex(int index)
                {
                    int decrements = 0;
                    for (int j = removed.Count - 1; j >= 0; j--)
                    {
                        if (index < removed[j]) { break; }
                        decrements++;
                    }
                    return index - decrements;
                }

                Triangles[i] = new Triangle(
                    Index0: FixIndex(Triangles[i].Index0),
                    Index1: FixIndex(Triangles[i].Index1),
                    Index2: FixIndex(Triangles[i].Index2));
            }
        }
    }
}