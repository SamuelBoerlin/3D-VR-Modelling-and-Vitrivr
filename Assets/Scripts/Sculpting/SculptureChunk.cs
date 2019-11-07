using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelPolygonizer;
using VoxelPolygonizer.CMS;

public readonly struct ChunkPos
{
    public readonly int x, y, z;

    private ChunkPos(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChunkPos FromChunk(int x, int y, int z)
    {
        return new ChunkPos(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChunkPos FromVoxel(Vector3Int pos, int chunkSize)
    {
        return FromVoxel(pos.x, pos.y, pos.z, chunkSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChunkPos FromVoxel(int x, int y, int z, int chunkSize)
    {
        return new ChunkPos(x < 0 ? (x / chunkSize - 1) : (x / chunkSize), y < 0 ? (y / chunkSize - 1) : (y / chunkSize), z < 0 ? (z / chunkSize - 1) : (z / chunkSize));
    }

    public override bool Equals(object obj)
    {
        return obj is ChunkPos pos &&
               x == pos.x &&
               y == pos.y &&
               z == pos.z;
    }

    public override int GetHashCode()
    {
        var hashCode = 373119288;
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        hashCode = hashCode * -1521134295 + z.GetHashCode();
        return hashCode;
    }

    public override string ToString()
    {
        return "SculptureChunk[x=" + x + ", y=" + y + ", z=" + z + "]";
    }
}

public class SculptureChunk
{
    private readonly int chunkSize;
    public int ChunkSize
    {
        get
        {
            return chunkSize;
        }
    }

    //TODO Flatten all arrays
    private readonly int[][][] materials;
    private readonly float[][][][] intersections;
    private readonly Vector3[][][][] normals;

    //TODO Cleanup
    public Mesh mesh = null;
    public bool NeedsRebuild
    {
        get;
        private set;
    }

    public ChunkPos Pos
    {
        get;
        private set;
    }

    private readonly Sculpture sculpture;

    public SculptureChunk(Sculpture sculpture, ChunkPos pos, int chunkSize)
    {
        this.sculpture = sculpture;
        this.chunkSize = chunkSize;
        this.Pos = pos;

        materials = new int[chunkSize][][];
        for (int x = 0; x < chunkSize; x++)
        {
            materials[x] = new int[chunkSize][];
            for (int y = 0; y < chunkSize; y++)
            {
                materials[x][y] = new int[chunkSize];
            }
        }

        intersections = new float[chunkSize][][][];
        for (int x = 0; x < chunkSize; x++)
        {
            intersections[x] = new float[chunkSize][][];
            for (int y = 0; y < chunkSize; y++)
            {
                intersections[x][y] = new float[chunkSize][];
                for (int z = 0; z < chunkSize; z++)
                {
                    intersections[x][y][z] = new float[3];
                }
            }
        }

        normals = new Vector3[chunkSize][][][];
        for (int x = 0; x < chunkSize; x++)
        {
            normals[x] = new Vector3[chunkSize][][];
            for (int y = 0; y < chunkSize; y++)
            {
                normals[x][y] = new Vector3[chunkSize][];
                for (int z = 0; z < chunkSize; z++)
                {
                    normals[x][y][z] = new Vector3[3];
                }
            }
        }
    }

    public int GetMaterial(int x, int y, int z)
    {
        return materials[x][y][z];
    }

    private Vector3 FindIntersection(Vector3 v1, double d1, Vector3 v2, double d2, ISdf sdf, double epsilon, int maxSteps)
    {
        double dx = v2.x - v1.x;
        double dy = v2.y - v1.y;
        double dz = v2.z - v1.z;
        double len = Mathf.Sqrt((float)(dx * dx + dy * dy + dz * dz));
        dx /= len;
        dy /= len;
        dz /= len;

        double x1 = v1.x;
        double y1 = v1.y;
        double z1 = v1.z;

        double x2 = v2.x;
        double y2 = v2.y;
        double z2 = v2.z;


        double abs1 = Mathf.Abs((float)d1);
        double abs2 = Mathf.Abs((float)d2);

        for (int i = 0; i < maxSteps; i++)
        {
            double x3 = (x1 + dx * abs1 + x2 - dx * abs2) / 2;
            double y3 = (y1 + dy * abs1 + y2 - dy * abs2) / 2;
            double z3 = (z1 + dz * abs1 + z2 - dz * abs2) / 2;
            double d3 = sdf.Eval(x3, y3, z3);

            if ((d3 < 0) == (d1 < 0))
            {
                x1 = x3;
                y1 = y3;
                z1 = z3;
                d1 = d3;
                abs1 = Mathf.Abs((float)d3);
            }
            else
            {
                x2 = x3;
                y2 = y3;
                z2 = z3;
                d2 = d3;
                abs2 = Mathf.Abs((float)d3);
            }

            if (abs1 < epsilon || abs2 < epsilon)
            {
                break;
            }
        }

        if (abs1 < abs2)
        {
            return new Vector3((float)x1, (float)y1, (float)z1);
        }
        else
        {
            return new Vector3((float)x2, (float)y2, (float)z2);
        }
    }

    private void ApplySdfIntersection(double ox, double oy, double oz, int x, int y, int z, int edge, ISdf sdf, Func<Vector3, Vector3> normalFunction, int material, bool replace)
    {
        int xo = 0;
        int yo = 0;
        int zo = 0;

        switch (edge)
        {
            default:
            case 0:
                xo = 1;
                break;
            case 1:
                yo = 1;
                break;
            case 2:
                zo = 1;
                break;
        }

        if (x < 0 || y < 0 || z < 0 || x >= chunkSize || y >= chunkSize || z >= chunkSize)
        {
            return;
        }

        //Remove intersections and normals from edges that do not have
        //a material change
        if (x + xo < chunkSize && y + yo < chunkSize && z + zo < chunkSize && materials[x][y][z] == materials[x + xo][y + yo][z + zo])
        {
            //intersections[x][y][z][edge] = 0;
            //normals[x][y][z][edge] = Vector3.zero;
            //return;
        }

        bool isIgnoredReplacement = replace && (materials[x][y][z] == 0 || x + xo >= chunkSize || y + yo >= chunkSize || z + zo >= chunkSize || materials[x + xo][y + yo][z + zo] == 0);

        double d1 = sdf.Eval(x - ox, y - oy, z - oz);
        double d2 = sdf.Eval(x + xo - ox, y + yo - oy, z + zo - oz);

        if (!isIgnoredReplacement)
        {
            if ((d1 < 0) != (d2 < 0))
            {
                Vector3 intersection = FindIntersection(new Vector3((float)(x - ox), (float)(y - oy), (float)(z - oz)), d1, new Vector3((float)(x + xo - ox), (float)(y + yo - oy), (float)(z + zo - oz)), d2, sdf, 0.0001F, 8);

                intersections[x][y][z][edge] = (intersection - new Vector3((float)(x - ox), (float)(y - oy), (float)(z - oz))).magnitude;
                normals[x][y][z][edge] = normalFunction.Invoke(intersection);
            }
            else if (d1 < 0 && d2 < 0)
            {
                intersections[x][y][z][edge] = 0;
                normals[x][y][z][edge] = Vector3.zero;
            }
        }
        else if ((d1 < 0) != (d2 < 0))
        {
            Vector3 intersection = FindIntersection(new Vector3((float)(x - ox), (float)(y - oy), (float)(z - oz)), d1, new Vector3((float)(x + xo - ox), (float)(y + yo - oy), (float)(z + zo - oz)), d2, sdf, 0.0001F, 8);

            float intersectionDist = (intersection - new Vector3((float)(x - ox), (float)(y - oy), (float)(z - oz))).magnitude;

            //TODO Bounds check
            if ((x + xo < chunkSize && y + yo < chunkSize && z + zo < chunkSize) && materials[x][y][z] == materials[x + xo][y + yo][z + zo])
            {
                intersections[x][y][z][edge] = (intersection - new Vector3((float)(x - ox), (float)(y - oy), (float)(z - oz))).magnitude;
                normals[x][y][z][edge] = normalFunction.Invoke(intersection);
            }

            /*if (d2 < 0 || (x + xo < sizeX && y + yo < sizeY && z + zo < sizeZ))
            {
                if(d1 < 0 && materials[x + xo][y + yo][z + zo] != 0 && intersectionDist > intersections[x][y][z][edge])
                {
                    materials[x + xo][y + yo][z + zo] = material;
                }
                else if(d2 < 0 && materials[x][y][z] != 0 && intersectionDist < intersections[x][y][z][edge])
                {
                    materials[x][y][z] = material;
                }
            }*/
        }

        NeedsRebuild = true;
    }

    public void ApplySdf(float ox, float oy, float oz, ISdf sdf, int material, bool replace)
    {
        Vector3 min = sdf.Min();
        Vector3 max = sdf.Max();

        Vector3Int minBound = new Vector3Int(Mathf.FloorToInt(ox + min.x), Mathf.FloorToInt(oy + min.y), Mathf.FloorToInt(oz + min.z));
        Vector3Int maxBound = new Vector3Int(Mathf.CeilToInt(ox + max.x), Mathf.CeilToInt(oy + max.y), Mathf.CeilToInt(oz + max.z));

        //Apply materials
        for (int z = minBound.z; z <= maxBound.z; z++)
        {
            for (int y = minBound.y; y <= maxBound.y; y++)
            {
                for (int x = minBound.x; x <= maxBound.x; x++)
                {
                    if (x >= 0 && x < chunkSize && y >= 0 && y < chunkSize && z >= 0 && z < chunkSize)
                    {
                        if (replace)
                        {
                            if (sdf.Eval(x - ox, y - oy, z - oz) < 0)
                            {
                                if (materials[x][y][z] != 0)
                                {
                                    materials[x][y][z] = material;
                                }

                                /*if(x - 1 >= 0 && materials[x - 1][y][z] != 0 && sdf.Eval(x - ox - 1, y - oy, z - oz) >= 0)
                                {
                                    materials[x - 1][y][z] = material;
                                }
                                if (y - 1 >= 0 && materials[x][y - 1][z] != 0 && sdf.Eval(x - ox, y - oy - 1, z - oz) >= 0)
                                {
                                    materials[x][y - 1][z] = material;
                                }
                                if (z - 1 >= 0 && materials[x][y][z - 1] != 0 && sdf.Eval(x - ox, y - oy, z - oz - 1) >= 0)
                                {
                                    materials[x][y][z - 1] = material;
                                }*/
                            }
                        }
                        else
                        {
                            if (sdf.Eval(x - ox, y - oy, z - oz) < 0)
                            {
                                materials[x][y][z] = material;
                            }
                        }
                    }
                }
            }
        }

        //Apply intersections and normals in a second pass, such that they aren't unnecessarily
        //asigned to edges with no material change
        for (int z = minBound.z; z <= maxBound.z; z++)
        {
            for (int y = minBound.y; y <= maxBound.y; y++)
            {
                for (int x = minBound.x; x <= maxBound.x; x++)
                {
                    if (x >= 0 && x < chunkSize && y >= 0 && y < chunkSize && z >= 0 && z < chunkSize)
                    {
                        const float epsilon = 0.0001F;

                        Vector3 normalFunc(Vector3 v1)
                        {
                            Vector3 n = new Vector3(
                                    (float)(sdf.Eval(v1.x + epsilon, v1.y, v1.z) - sdf.Eval(v1.x - epsilon, v1.y, v1.z)),
                                        (float)(sdf.Eval(v1.x, v1.y + epsilon, v1.z) - sdf.Eval(v1.x, v1.y - epsilon, v1.z)),
                                        (float)(sdf.Eval(v1.x, v1.y, v1.z + epsilon) - sdf.Eval(v1.x, v1.y, v1.z - epsilon))
                                    );
                            n.Normalize();
                            if (material == 0)
                            {
                                n *= -1;
                            }
                            return n;
                        }

                        ApplySdfIntersection(ox, oy, oz, x, y, z, 0, sdf, normalFunc, material, replace);
                        ApplySdfIntersection(ox, oy, oz, x, y, z, 1, sdf, normalFunc, material, replace);
                        ApplySdfIntersection(ox, oy, oz, x, y, z, 2, sdf, normalFunc, material, replace);
                    }
                }
            }
        }
    }

    private int getMat(int x, int y, int z)
    {
        int cx = x / (chunkSize);
        int cy = y / (chunkSize);
        int cz = z / (chunkSize);
        if(cx == 0 && cy == 0 && cz == 0)
        {
            return this.materials[x][y][z];
        }
        else
        {
            SculptureChunk neighbor = this.sculpture.GetChunk(ChunkPos.FromChunk(this.Pos.x + cx, this.Pos.y + cy, this.Pos.z + cz));
            if(neighbor == null)
            {
                return 0;
            }
            return neighbor.materials[x % (chunkSize)][y % (chunkSize)][z % (chunkSize)];
        }
    }

    private float getInt(int x, int y, int z, int e)
    {
        int cx = x / (chunkSize);
        int cy = y / (chunkSize);
        int cz = z / (chunkSize);
        if (cx == 0 && cy == 0 && cz == 0)
        {
            return this.intersections[x][y][z][e];
        }
        else
        {
            SculptureChunk neighbor = this.sculpture.GetChunk(ChunkPos.FromChunk(this.Pos.x + cx, this.Pos.y + cy, this.Pos.z + cz));
            if(neighbor == null)
            {
                return 0.5f;
            }
            return neighbor.intersections[x % (chunkSize)][y % (chunkSize)][z % (chunkSize)][e];
        }
    }

    private float3 getNorm(int x, int y, int z, int e)
    {
        int cx = x / (chunkSize);
        int cy = y / (chunkSize);
        int cz = z / (chunkSize);
        if (cx == 0 && cy == 0 && cz == 0)
        {
            return this.normals[x][y][z][e];
        }
        else
        {
            SculptureChunk neighbor = this.sculpture.GetChunk(ChunkPos.FromChunk(this.Pos.x + cx, this.Pos.y + cy, this.Pos.z + cz));
            if(neighbor == null)
            {
                return new float3(0, 0, 0);
            }
            return neighbor.normals[x % (chunkSize)][y % (chunkSize)][z % (chunkSize)][e];
        }
    }

    public void FillCell(int x, int y, int z, int cellIndex, NativeArray<int> materials, NativeArray<float> intersections, NativeArray<float3> normals)
    {
        materials[cellIndex * 8 + 0] = getMat(x, y, z);
        materials[cellIndex * 8 + 1] = getMat(x + 1, y, z);
        materials[cellIndex * 8 + 2] = getMat(x + 1, y, z + 1);
        materials[cellIndex * 8 + 3] = getMat(x, y, z + 1);
        materials[cellIndex * 8 + 4] = getMat(x, y + 1, z);
        materials[cellIndex * 8 + 5] = getMat(x + 1, y + 1, z);
        materials[cellIndex * 8 + 6] = getMat(x + 1, y + 1, z + 1);
        materials[cellIndex * 8 + 7] = getMat(x, y + 1, z + 1);

        intersections[cellIndex * 12 + 0] = getInt(x, y, z, 0);
        intersections[cellIndex * 12 + 1] = getInt(x + 1, y, z, 2);
        intersections[cellIndex * 12 + 2] = getInt(x, y, z + 1, 0);
        intersections[cellIndex * 12 + 3] = getInt(x, y, z, 2);
        intersections[cellIndex * 12 + 4] = getInt(x, y + 1, z, 0);
        intersections[cellIndex * 12 + 5] = getInt(x + 1, y + 1, z, 2);
        intersections[cellIndex * 12 + 6] = getInt(x, y + 1, z + 1, 0);
        intersections[cellIndex * 12 + 7] = getInt(x, y + 1, z, 2);
        intersections[cellIndex * 12 + 8] = getInt(x, y, z, 1);
        intersections[cellIndex * 12 + 9] = getInt(x + 1, y, z, 1);
        intersections[cellIndex * 12 + 10] = getInt(x + 1, y, z + 1, 1);
        intersections[cellIndex * 12 + 11] = getInt(x, y, z + 1, 1);

        normals[cellIndex * 12 + 0] = getNorm(x, y, z, 0);
        normals[cellIndex * 12 + 1] = getNorm(x + 1, y, z, 2);
        normals[cellIndex * 12 + 2] = getNorm(x, y, z + 1, 0);
        normals[cellIndex * 12 + 3] = getNorm(x, y, z, 2);
        normals[cellIndex * 12 + 4] = getNorm(x, y + 1, z, 0);
        normals[cellIndex * 12 + 5] = getNorm(x + 1, y + 1, z, 2);
        normals[cellIndex * 12 + 6] = getNorm(x, y + 1, z + 1, 0);
        normals[cellIndex * 12 + 7] = getNorm(x, y + 1, z, 2);
        normals[cellIndex * 12 + 8] = getNorm(x, y, z, 1);
        normals[cellIndex * 12 + 9] = getNorm(x + 1, y, z, 1);
        normals[cellIndex * 12 + 10] = getNorm(x + 1, y, z + 1, 1);
        normals[cellIndex * 12 + 11] = getNorm(x, y, z + 1, 1);
    }

    public void Build()
    {
        NeedsRebuild = false;

        var components = new NativeList<VoxelMeshComponent>(Allocator.Persistent);
        var componentIndices = new NativeList<PackedIndex>(Allocator.Persistent);
        var componentVertices = new NativeList<VoxelMeshComponentVertex>(Allocator.Persistent);

        int voxels = /*1;//*/ (chunkSize - 0) * (chunkSize - 0) * (chunkSize - 0);

        var cellMaterials = new NativeArray<int>(voxels * 8, Allocator.Persistent);
        var cellIntersections = new NativeArray<float>(voxels * 12, Allocator.Persistent);
        var cellNormals = new NativeArray<float3>(voxels * 12, Allocator.Persistent);

        var cells = new NativeArray<float3>(voxels, Allocator.Persistent);

        int voxelIndex = 0;
        for (int z = 0; z < chunkSize - 0; z++)
        {
            for (int y = 0; y < chunkSize - 0; y++)
            {
                for (int x = 0; x < chunkSize - 0; x++)
                {
                    FillCell(x, y, z, voxelIndex, cellMaterials, cellIntersections, cellNormals);

                    cells[voxelIndex] = new float3(x, y, z);

                    voxelIndex++;
                }
            }
        }

        NativeMemoryCache memoryCache = new NativeMemoryCache(Allocator.Persistent);

        VoxelMeshTessellation.NativeDeduplicationCache dedupeCache = new VoxelMeshTessellation.NativeDeduplicationCache(Allocator.Persistent);

        var meshVertices = new NativeList<float3>(Allocator.Persistent);
        var meshNormals = new NativeList<float3>(Allocator.Persistent);
        var meshTriangles = new NativeList<int>(Allocator.Persistent);
        var meshColors = new NativeList<Color32>(Allocator.Persistent);
        var meshMaterials = new NativeList<int>(Allocator.Persistent);

        PolygonizeJob polygonizerJob = new PolygonizeJob
        {
            Cells = cells,
            MemoryCache = memoryCache,
            Materials = cellMaterials,
            Intersections = cellIntersections,
            Normals = cellNormals,
            Components = components,
            Indices = componentIndices,
            Vertices = componentVertices,
            MeshVertices = meshVertices,
            MeshNormals = meshNormals,
            MeshTriangles = meshTriangles,
            MeshColors = meshColors,
            MeshMaterials = meshMaterials,
            DedupeCache = dedupeCache
        };

        var watch = System.Diagnostics.Stopwatch.StartNew();

        polygonizerJob.Schedule().Complete();

        watch.Stop();

        string text = "Polygonized voxel chunk in " + watch.ElapsedMilliseconds + "ms. Vertices: " + meshVertices.Length;
        Debug.Log(text);


        /* for (int i = 0; i < cellPositions.Count; i++)
         {
             //VoxelMeshComponentRenderer.Tessellate(components[i], componentIndices, componentVertices, Matrix4x4.Translate(cellPositions[i]), vertices, indices.Count, indices, normals, materials, colors, mat => GetColorForMaterial(mat), dedupedTable);
             VoxelMeshComponentRenderer.Tessellate(components[i], componentIndices, componentVertices, Matrix4x4.Translate(cellPositions[i]), vertices, indices.Count, indices, normals, materials, dedupedTable);
         }*/
        //VoxelMeshRenderer.Tessellate(components, componentIndices, componentVertices, Matrix4x4.Translate(Vector3.zero), vertices, indices, normals, materials, colors, mat => GetColorForMaterial(mat), dedupedTable);

        var vertices = new List<Vector3>(meshVertices.Length);
        var indices = new List<int>(meshTriangles.Length);
        var materials = new List<int>(meshMaterials.Length);
        var colors = new List<Color32>(meshColors.Length);
        var normals = new List<Vector3>(meshNormals.Length);

        for (int i = 0; i < meshVertices.Length; i++)
        {
            vertices.Add(meshVertices[i]);
        }
        for (int i = 0; i < meshTriangles.Length; i++)
        {
            indices.Add(meshTriangles[i]);
        }
        for (int i = 0; i < meshMaterials.Length; i++)
        {
            materials.Add(meshMaterials[i]);
        }
        for (int i = 0; i < meshColors.Length; i++)
        {
            colors.Add(meshColors[i]);
        }
        for (int i = 0; i < meshNormals.Length; i++)
        {
            normals.Add(meshNormals[i]);
        }

        dedupeCache.Dispose();

        meshVertices.Dispose();
        meshNormals.Dispose();
        meshTriangles.Dispose();
        meshColors.Dispose();
        meshMaterials.Dispose();

        memoryCache.Dispose();

        cells.Dispose();

        cellMaterials.Dispose();
        cellIntersections.Dispose();
        cellNormals.Dispose();

        components.Dispose();
        componentIndices.Dispose();
        componentVertices.Dispose();

        if (mesh == null)
        {
            mesh = new Mesh();
        }

        mesh.Clear(false);
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(indices, 0);
        if (colors.Count > 0)
        {
            mesh.SetColors(colors);
        }
    }
}
