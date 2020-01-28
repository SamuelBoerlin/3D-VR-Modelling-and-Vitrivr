using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Sculpting.Voxelizer;

namespace Sculpting
{
    [BurstCompile]
    public struct VoxelizerJob : IJob
    {
        [ReadOnly] public NativeArray<Column> colsX, colsY, colsZ;

        [ReadOnly] public NativeList<float4> intersections;

        [ReadOnly] public int material;

        /// <summary>
        /// Output voxel grid. Determines resolution.
        /// </summary>
        public NativeArray3D<Voxel> grid;

        private struct IntersectionSorter : IComparer<float4>
        {
            public int Compare(float4 x, float4 y)
            {
                return (int)math.sign(x.w - y.w);
            }
        }

        public void Execute()
        {
            int width = grid.Length(0);
            int height = grid.Length(1);
            int depth = grid.Length(2);

            var colIndex = 0;

            //Intersect X axis
            colIndex = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    var col = colsX[colIndex];

                    if (col.length > 0)
                    {
                        //Alternate between inside/outside
                        bool inside = false;
                        int pix = 0;
                        for (int i = 0; i < col.length; i++)
                        {
                            var intersection = intersections[col.index + i];
                            var ix = (int)math.floor(intersection.w);

                            //Set intersection and normal
                            //grid[ix, y, z] = grid[ix, y, z].ModifyEdge(0, intersection.w - ix, intersection.xyz);

                            if (inside)
                            {
                                //Fill voxel materials
                                for (int x = pix + 1; x <= ix; x++)
                                {
                                    grid[x, y, z] = grid[x, y, z].ModifyMaterial(material);
                                }
                            }

                            pix = ix;
                            inside = !inside;
                        }
                    }

                    colIndex++;
                }
            }

            //Intersect Y axis
            colIndex = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    var col = colsY[colIndex];

                    if (col.length > 0)
                    {
                        //Alternate between inside/outside
                        bool inside = false;
                        int piy = 0;
                        for (int i = 0; i < col.length; i++)
                        {
                            var intersection = intersections[col.index + i];
                            var iy = (int)math.floor(intersection.w);

                            if (inside)
                            {
                                //Fill voxel materials
                                for (int y = piy + 1; y <= iy; y++)
                                {
                                    grid[x, y, z] = grid[x, y, z].ModifyMaterial(material);
                                }
                            }

                            piy = iy;
                            inside = !inside;
                        }
                    }

                    colIndex++;
                }
            }

            //Intersect Z axis
            colIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var col = colsZ[colIndex];

                    if (col.length > 0)
                    {
                        //Alternate between inside/outside
                        bool inside = false;
                        int piz = 0;
                        for (int i = 0; i < col.length; i++)
                        {
                            var intersection = intersections[col.index + i];
                            var iz = (int)math.floor(intersection.w);

                            if (inside)
                            {
                                //Fill voxel materials
                                for (int z = piz + 1; z <= iz; z++)
                                {
                                    grid[x, y, z] = grid[x, y, z].ModifyMaterial(material);
                                }
                            }

                            piz = iz;
                            inside = !inside;
                        }
                    }

                    colIndex++;
                }
            }

            //Populate X axis
            colIndex = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    var col = colsX[colIndex];

                    if (col.length > 0)
                    {
                        bool prevSolid = grid[0, y, z].Material == material;

                        for (int x = 1; x < width; x++)
                        {
                            bool solid = grid[x, y, z].Material == material;

                            if (solid != prevSolid)
                            {
                                float4 closestIntersection = 0;

                                for (int i = 0; i < col.length; i++)
                                {
                                    var intersection = intersections[col.index + i];
                                    if (math.abs(intersection.w - x + 1) < math.abs(closestIntersection.w - x + 1))
                                    {
                                        closestIntersection = intersection;
                                    }
                                }

                                grid[x - 1, y, z] = grid[x - 1, y, z].ModifyEdge(0, math.clamp(closestIntersection.w - x + 1, 0, 1), closestIntersection.xyz);
                            }

                            prevSolid = solid;
                        }
                    }

                    colIndex++;
                }
            }

            //Populate Y axis
            colIndex = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    var col = colsY[colIndex];

                    if (col.length > 0)
                    {
                        bool prevSolid = grid[x, 0, z].Material == material;

                        for (int y = 1; y < height; y++)
                        {
                            bool solid = grid[x, y, z].Material == material;

                            if (solid != prevSolid)
                            {
                                float4 closestIntersection = 0;

                                for (int i = 0; i < col.length; i++)
                                {
                                    var intersection = intersections[col.index + i];
                                    if (math.abs(intersection.w - y + 1) < math.abs(closestIntersection.w - y + 1))
                                    {
                                        closestIntersection = intersection;
                                    }
                                }

                                grid[x, y - 1, z] = grid[x, y - 1, z].ModifyEdge(1, math.clamp(closestIntersection.w - y + 1, 0, 1), closestIntersection.xyz);
                            }

                            prevSolid = solid;
                        }
                    }

                    colIndex++;
                }
            }

            //Populate Z axis
            colIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var col = colsZ[colIndex];

                    if (col.length > 0)
                    {
                        bool prevSolid = grid[x, y, 0].Material == material;

                        for (int z = 1; z < depth; z++)
                        {
                            bool solid = grid[x, y, z].Material == material;

                            if (solid != prevSolid)
                            {
                                float4 closestIntersection = 0;

                                for (int i = 0; i < col.length; i++)
                                {
                                    var intersection = intersections[col.index + i];
                                    if (math.abs(intersection.w - z + 1) < math.abs(closestIntersection.w - z + 1))
                                    {
                                        closestIntersection = intersection;
                                    }
                                }

                                grid[x, y, z - 1] = grid[x, y, z - 1].ModifyEdge(2, math.clamp(closestIntersection.w - z + 1, 0, 1), closestIntersection.xyz);
                            }

                            prevSolid = solid;
                        }
                    }

                    colIndex++;
                }
            }
        }
    }
}
