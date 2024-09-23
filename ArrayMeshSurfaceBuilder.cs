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
using System.Runtime.InteropServices;
using Godot;
using GodotArray = Godot.Collections.Array;

namespace duge.GameLib.ArrayMeshSurfaceBuilder;

public sealed partial class ArrayMeshSurfaceBuilder : IDisposable
{
    private readonly List<Vector3> _vertexPositionsDotnet = new();
    private readonly List<Vector3> _vertexNormalsDotnet = new();
    private readonly List<Color> _vertexColorsDotnet = new();
    private readonly List<Vector2> _vertexUvsDotnet = new();
    private readonly List<int> _indicesDotnet = new();

    private readonly GodotArray _compositeGodotArray = CreateCompositeGodotArray();

    private static GodotArray CreateCompositeGodotArray()
    {
        GodotArray returnValue = new GodotArray();
        returnValue.Resize((int)Mesh.ArrayType.Max);
        return returnValue;
    }

    public List<Surface> Surfaces = new();

    public void AddSurfacesToMesh(ArrayMesh arrayMesh)
    {
        foreach (Surface surface in Surfaces)
        {
            surface.PopulateDotnetLists(
                vertexPositionsList: _vertexPositionsDotnet,
                vertexNormalsList: _vertexNormalsDotnet,
                vertexColorsList: _vertexColorsDotnet,
                vertexUvsList: _vertexUvsDotnet,
                indicesList: _indicesDotnet);

            _compositeGodotArray[(int)Mesh.ArrayType.Vertex] = VariantFromVector3List(_vertexPositionsDotnet);
            _compositeGodotArray[(int)Mesh.ArrayType.Normal] = VariantFromVector3List(_vertexNormalsDotnet);
            _compositeGodotArray[(int)Mesh.ArrayType.Color] = VariantFromColorList(_vertexColorsDotnet);
            _compositeGodotArray[(int)Mesh.ArrayType.TexUV] = VariantFromVector2List(_vertexUvsDotnet);
            _compositeGodotArray[(int)Mesh.ArrayType.Index] = VariantFromIntegerList(_indicesDotnet);

            arrayMesh.AddSurfaceFromArrays(
                primitive: Mesh.PrimitiveType.Triangles,
                arrays: _compositeGodotArray);

            int surfaceIndex = arrayMesh.GetSurfaceCount() - 1;

            arrayMesh.SurfaceSetName(surfaceIndex, surface.Name);
            if (surface.Material is not null)
            {
                arrayMesh.SurfaceSetMaterial(surfIdx: surfaceIndex, material: surface.Material);
            }
        }
    }

    public void Dispose()
    {
        _compositeGodotArray.Dispose();
    }

    private static Variant VariantFromVector3List(List<Vector3> list)
    {
        return Variant.CreateFrom(CollectionsMarshal.AsSpan(list));
    }

    private static Variant VariantFromVector2List(List<Vector2> list)
    {
        return Variant.CreateFrom(CollectionsMarshal.AsSpan(list));
    }

    private static Variant VariantFromColorList(List<Color> list)
    {
        return Variant.CreateFrom(CollectionsMarshal.AsSpan(list));
    }

    private static Variant VariantFromIntegerList(List<int> list)
    {
        return Variant.CreateFrom(CollectionsMarshal.AsSpan(list));
    }
}
