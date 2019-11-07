using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using VoxelPolygonizer;

public struct TestVoxelCell : IVoxelCell
{
    private static readonly Dictionary<int, CellEdges> Edges = new Dictionary<int, CellEdges>();
    private static readonly Dictionary<int, CellMaterials> Materials = new Dictionary<int, CellMaterials>();
    private static readonly Dictionary<int, float> Intersections = new Dictionary<int, float>();
    private static readonly Dictionary<int, Vector3> Normals = new Dictionary<int, Vector3>();

    static TestVoxelCell()
    {
        Edges.Add((int)VoxelCellFace.XNeg, new CellEdges(0, 1, 2, 3));
        Edges.Add((int)VoxelCellFace.XPos, new CellEdges(4, 5, 6, 7));
        Edges.Add((int)VoxelCellFace.YNeg, new CellEdges(8, 9, 10, 11));
        Edges.Add((int)VoxelCellFace.YPos, new CellEdges(12, 13, 14, 15));
        Edges.Add((int)VoxelCellFace.ZNeg, new CellEdges(16, 17, 18, 19));
        Edges.Add((int)VoxelCellFace.ZPos, new CellEdges(20, 21, 22, 23));

        int otherMat = 2;
        Materials.Add((int)VoxelCellFace.XNeg, new CellMaterials(1, 0, 0, otherMat));
        Materials.Add((int)VoxelCellFace.XPos, new CellMaterials(0, 1, otherMat, 0));
        Materials.Add((int)VoxelCellFace.YNeg, new CellMaterials(1, 1, 0, 0));
        Materials.Add((int)VoxelCellFace.YPos, new CellMaterials(0, 0, otherMat, otherMat));
        Materials.Add((int)VoxelCellFace.ZNeg, new CellMaterials(0, 0, 0, 0));
        Materials.Add((int)VoxelCellFace.ZPos, new CellMaterials(1, 1, otherMat, otherMat));

        //XNeg
        Intersections.Add(0, 0.5F);
        Intersections.Add(2, 0.5F);
        Intersections.Add(3, 0.5F); //transition
        //XPos
        Intersections.Add(4, 0.5F);
        Intersections.Add(5, 0.5F); //transition
        Intersections.Add(6, 0.5F);
        //YNeg
        Intersections.Add(9, 0.5F);
        Intersections.Add(11, 0.5F);
        //YPos
        Intersections.Add(13, 0.5F);
        Intersections.Add(15, 0.5F);
        //ZNeg none
        //ZPos
        Intersections.Add(21, 0.5F); //transition
        Intersections.Add(23, 0.5F); //transition

        //XNeg
        Normals.Add(0, -Vector3.forward);
        Normals.Add(2, -Vector3.forward);
        Normals.Add(3, Vector3.up); //transition
        //XPos
        Normals.Add(4, -Vector3.forward);
        Normals.Add(5, Vector3.up); //transition
        Normals.Add(6, -Vector3.forward);
        //YNeg
        Normals.Add(9, -Vector3.forward);
        Normals.Add(11, -Vector3.forward);
        //YPos
        Normals.Add(13, -Vector3.forward);
        Normals.Add(15, -Vector3.forward);
        //ZNeg none
        //ZPos
        Normals.Add(21, Vector3.up); //transition
        Normals.Add(23, Vector3.up); //transition

        /*Materials.Add((int)VoxelCellFace.XNeg, new CellMaterials( 0, 1, 0, 0));
        Materials.Add((int)VoxelCellFace.XPos, new CellMaterials( 0, 0, 0, 1));
        Materials.Add((int)VoxelCellFace.YNeg, new CellMaterials( 0, 0, 0, 1));
        Materials.Add((int)VoxelCellFace.YPos, new CellMaterials( 0, 1, 0, 0));
        Materials.Add((int)VoxelCellFace.ZNeg, new CellMaterials( 1, 0, 1, 0));
        Materials.Add((int)VoxelCellFace.ZPos, new CellMaterials( 0, 0, 0, 0));

        Intersections.Add(0, 0.5F);
        Intersections.Add(1, 0.5F);
        Intersections.Add(6, 0.5F);
        Intersections.Add(7, 0.5F);
        Intersections.Add(10, 0.5F);
        Intersections.Add(11, 0.5F);
        Intersections.Add(12, 0.5F);
        Intersections.Add(13, 0.5F);
        Intersections.Add(16, 0.5F);
        Intersections.Add(17, 0.5F);
        Intersections.Add(18, 0.5F);
        Intersections.Add(19, 0.5F);

        Normals.Add(0, new Vector3(1f, 1f, 4f).normalized);
        Normals.Add(1, new Vector3(0.25f, 1f, -0.45f).normalized);
        Normals.Add(6, new Vector3(-1f, -1f, 4f).normalized);
        Normals.Add(7, new Vector3(-0.25f, -1f, -0.45f).normalized);
        Normals.Add(10, new Vector3(1f, 0.25f, -0.45f).normalized);
        Normals.Add(11, new Vector3(1f, 1f, 4f).normalized);
        Normals.Add(12, new Vector3(-1f, -0.25f, -0.45f).normalized);
        Normals.Add(13, new Vector3(-1f, -1f, 4f).normalized);
        Normals.Add(16, new Vector3(1f, 0.25f, -0.45f).normalized);
        Normals.Add(17, new Vector3(-0.25f, -1f, -0.45f).normalized);
        Normals.Add(18, new Vector3(-1f, -0.25f, -0.45f).normalized);
        Normals.Add(19, new Vector3(0.25f, 1f, -0.45f).normalized);*/
    }

    public int GetCellFaceCount(VoxelCellFace face)
    {
        return 1;
    }

    public int GetCellFace(VoxelCellFace face, int index)
    {
        return (int)face;
    }

    public float GetWidth()
    {
        return 1;
    }

    public float GetDepth()
    {
        return 1;
    }

    public float GetHeight()
    {
        return 1;
    }

    public CellEdges GetEdges(int cell)
    {
        return Edges[cell];
    }

    public CellInfo GetInfo(int cell)
    {
        Vector3 cellPos;
        switch (cell)
        {
            default:
            case 0:
                cellPos = new Vector3(0, 0, 0);
                break;
            case 1:
                cellPos = new Vector3(1, 0, 0);
                break;
            case 2:
                cellPos = new Vector3(1, 0, 1);
                break;
            case 3:
                cellPos = new Vector3(0, 0, 1);
                break;
            case 4:
                cellPos = new Vector3(0, 0, 1);
                break;
            case 5:
                cellPos = new Vector3(0, 1, 0);
                break;
        }
        return new CellInfo(cellPos, 1, 1);
    }

    public bool HasIntersection(int cell, int edge)
    {
        return Intersections.ContainsKey(edge);
    }

    public float GetIntersection(int cell, int edge)
    {
        if (Intersections.ContainsKey(edge))
        {
            return Intersections[edge];
        }
        return 0;
    }

    public CellMaterials GetMaterials(int cell)
    {
        return Materials[cell];
    }

    public int GetNeighboringEdge(int cell, int edge)
    {
        return -1;
    }

    public float3 GetNormal(int cell, int edge)
    {
        if (Normals.ContainsKey(edge))
        {
            return Normals[edge];
        }
        return Vector3.zero;
    }
}
