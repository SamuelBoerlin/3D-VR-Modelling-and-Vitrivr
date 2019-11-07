using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public class TestVoxelField
{
    private readonly int[][][] materials;
    private readonly float[][][][] intersections;
    private readonly Vector3[][][][] normals;

    public readonly int sizeX, sizeY, sizeZ;

    public TestVoxelField(int sizeX, int sizeY, int sizeZ)
    {
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;

        materials = new int[sizeX][][];
        for (int x = 0; x < sizeX; x++)
        {
            materials[x] = new int[sizeY][];
            for (int y = 0; y < sizeY; y++)
            {
                materials[x][y] = new int[sizeZ];
            }
        }

        intersections = new float[sizeX][][][];
        for (int x = 0; x < sizeX; x++)
        {
            intersections[x] = new float[sizeY][][];
            for (int y = 0; y < sizeY; y++)
            {
                intersections[x][y] = new float[sizeZ][];
                for (int z = 0; z < sizeZ; z++)
                {
                    intersections[x][y][z] = new float[3];
                }
            }
        }

        normals = new Vector3[sizeX][][][];
        for (int x = 0; x < sizeX; x++)
        {
            normals[x] = new Vector3[sizeY][][];
            for (int y = 0; y < sizeY; y++)
            {
                normals[x][y] = new Vector3[sizeZ][];
                for (int z = 0; z < sizeZ; z++)
                {
                    normals[x][y][z] = new Vector3[3];
                }
            }
        }
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

        if (x < 0 || y < 0 || z < 0 || x >= sizeX || y >= sizeY || z >= sizeZ)
        {
            return;
        }

        //Remove intersections and normals from edges that do not have
        //a material change
        if (x + xo < sizeX && y + yo < sizeY && z + zo < sizeZ && materials[x][y][z] == materials[x + xo][y + yo][z + zo])
        {
            //intersections[x][y][z][edge] = 0;
            //normals[x][y][z][edge] = Vector3.zero;
            //return;
        }

        bool isIgnoredReplacement = replace && (materials[x][y][z] == 0 || x + xo >= sizeX || y + yo >= sizeY || z + zo >= sizeZ || materials[x + xo][y + yo][z + zo] == 0);

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
            if ((x + xo < sizeX && y + yo < sizeY && z + zo < sizeZ) && materials[x][y][z] == materials[x + xo][y + yo][z + zo])
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
                    if (x >= 0 && x < sizeX && y >= 0 && y < sizeY && z >= 0 && z < sizeZ)
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
                    if (x >= 0 && x < sizeX && y >= 0 && y < sizeY && z >= 0 && z < sizeZ)
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

    public void FillCell(int x, int y, int z, int cellIndex, NativeArray<int> materials, NativeArray<float> intersections, NativeArray<float3> normals)
    {
        materials[cellIndex * 8 + 0] = this.materials[x][y][z];
        materials[cellIndex * 8 + 1] = this.materials[x + 1][y][z];
        materials[cellIndex * 8 + 2] = this.materials[x + 1][y][z + 1];
        materials[cellIndex * 8 + 3] = this.materials[x][y][z + 1];
        materials[cellIndex * 8 + 4] = this.materials[x][y + 1][z];
        materials[cellIndex * 8 + 5] = this.materials[x + 1][y + 1][z];
        materials[cellIndex * 8 + 6] = this.materials[x + 1][y + 1][z + 1];
        materials[cellIndex * 8 + 7] = this.materials[x][y + 1][z + 1];

        intersections[cellIndex * 12 + 0] = this.intersections[x][y][z][0];
        intersections[cellIndex * 12 + 1] = this.intersections[x + 1][y][z][2];
        intersections[cellIndex * 12 + 2] = this.intersections[x][y][z + 1][0];
        intersections[cellIndex * 12 + 3] = this.intersections[x][y][z][2];
        intersections[cellIndex * 12 + 4] = this.intersections[x][y + 1][z][0];
        intersections[cellIndex * 12 + 5] = this.intersections[x + 1][y + 1][z][2];
        intersections[cellIndex * 12 + 6] = this.intersections[x][y + 1][z + 1][0];
        intersections[cellIndex * 12 + 7] = this.intersections[x][y + 1][z][2];
        intersections[cellIndex * 12 + 8] = this.intersections[x][y][z][1];
        intersections[cellIndex * 12 + 9] = this.intersections[x + 1][y][z][1];
        intersections[cellIndex * 12 + 10] = this.intersections[x + 1][y][z + 1][1];
        intersections[cellIndex * 12 + 11] = this.intersections[x][y][z + 1][1];

        normals[cellIndex * 12 + 0] = this.normals[x][y][z][0];
        normals[cellIndex * 12 + 1] = this.normals[x + 1][y][z][2];
        normals[cellIndex * 12 + 2] = this.normals[x][y][z + 1][0];
        normals[cellIndex * 12 + 3] = this.normals[x][y][z][2];
        normals[cellIndex * 12 + 4] = this.normals[x][y + 1][z][0];
        normals[cellIndex * 12 + 5] = this.normals[x + 1][y + 1][z][2];
        normals[cellIndex * 12 + 6] = this.normals[x][y + 1][z + 1][0];
        normals[cellIndex * 12 + 7] = this.normals[x][y + 1][z][2];
        normals[cellIndex * 12 + 8] = this.normals[x][y][z][1];
        normals[cellIndex * 12 + 9] = this.normals[x + 1][y][z][1];
        normals[cellIndex * 12 + 10] = this.normals[x + 1][y][z + 1][1];
        normals[cellIndex * 12 + 11] = this.normals[x][y][z + 1][1];
    }

    public readonly struct RayCastResult
    {
        public readonly Vector3 pos;
        public readonly Vector3 sidePos;

        public RayCastResult(Vector3 pos, Vector3 sidePos)
        {
            this.pos = pos;
            this.sidePos = sidePos;
        }
    }

    public bool RayCast(Vector3 pos, Vector3 dir, float dst, out RayCastResult result)
    {
        const float step = 0.1f;

        int prevX = int.MaxValue;
        int prevY = int.MaxValue;
        int prevZ = int.MaxValue;

        Vector3 stepOffset = dir.normalized * step;

        for (int i = 0; i < dst / step; i++)
        {
            int x = (int)Mathf.Floor(pos.x);
            int y = (int)Mathf.Floor(pos.y);
            int z = (int)Mathf.Floor(pos.z);

            if (x != prevX || y != prevY || z != prevZ)
            {
                if (x >= 0 && y >= 0 && z >= 0 && x < sizeX - 1 && y < sizeY - 1 && z < sizeZ - 1)
                {
                    for (int zo = 0; zo < 2; zo++)
                    {
                        for (int yo = 0; yo < 2; yo++)
                        {
                            for (int xo = 0; xo < 2; xo++)
                            {
                                int material = materials[x + xo][y + yo][z + zo];

                                if (material != 0)
                                {
                                    result = new RayCastResult(new Vector3(x, y, z), new Vector3(prevX, prevY, prevZ));
                                    return true;
                                }
                            }
                        }
                    }
                }

                prevX = x;
                prevY = y;
                prevZ = z;
            }

            pos += stepOffset;
        }

        result = new RayCastResult(Vector3.zero, Vector3.zero);
        return false;
    }
}
