using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sculpting
{
    public class Voxelizer
    {
        private struct IntersectionJobHandle
        {
            public JobHandle handle;
            public int axis;
            public int colIndex;
            public NativeList<float4> intersections;
        }

        public readonly struct Column
        {
            public readonly int index;
            public readonly int length;

            public Column(int index, int length)
            {
                this.index = index;
                this.length = length;
            }
        }

        public static void Voxelize(NativeArray<float3> vertices, NativeArray3D<Voxel> grid, int material)
        {
            var width = grid.Length(0);
            var height = grid.Length(1);
            var depth = grid.Length(2);

            NativeArray<float3> scaledVertices = new NativeArray<float3>(vertices.Length, Allocator.TempJob);

            var scaleJob = new MeshScaleJob
            {
                vertices = vertices,
                outVertices = scaledVertices,
                width = grid.Length(0),
                height = grid.Length(1),
                depth = grid.Length(2)
            };

            scaleJob.Schedule().Complete();

            var intersectionJobs = new List<IntersectionJobHandle>();

            //Intersect X axis
            var colIndex = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    var intersections = new NativeList<float4>(Allocator.TempJob);

                    var intersectionJob = new MeshIntersectionJob
                    {
                        scaledVertices = scaledVertices,
                        width = width,
                        height = height,
                        depth = depth,
                        px = 0,
                        py = y,
                        pz = z,
                        axis = 0,
                        index = colIndex,
                        meshIntersections = intersections
                    };

                    intersectionJobs.Add(new IntersectionJobHandle
                    {
                        handle = intersectionJob.Schedule(),
                        axis = 0,
                        colIndex = colIndex,
                        intersections = intersections
                    });

                    colIndex++;
                }
            }

            //Intersect Y axis
            colIndex = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    var intersections = new NativeList<float4>(Allocator.TempJob);

                    var intersectionJob = new MeshIntersectionJob
                    {
                        scaledVertices = scaledVertices,
                        width = width,
                        height = height,
                        depth = depth,
                        px = x,
                        py = 0,
                        pz = z,
                        axis = 1,
                        index = colIndex,
                        meshIntersections = intersections
                    };

                    intersectionJobs.Add(new IntersectionJobHandle
                    {
                        handle = intersectionJob.Schedule(),
                        axis = 1,
                        colIndex = colIndex,
                        intersections = intersections
                    });

                    colIndex++;
                }
            }

            //Intersect Z axis
            colIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var intersections = new NativeList<float4>(Allocator.TempJob);

                    var intersectionJob = new MeshIntersectionJob
                    {
                        scaledVertices = scaledVertices,
                        width = width,
                        height = height,
                        depth = depth,
                        px = x,
                        py = y,
                        pz = 0,
                        axis = 2,
                        index = colIndex,
                        meshIntersections = intersections
                    };

                    intersectionJobs.Add(new IntersectionJobHandle
                    {
                        handle = intersectionJob.Schedule(),
                        axis = 2,
                        colIndex = colIndex,
                        intersections = intersections
                    });

                    colIndex++;
                }
            }

            var colsX = new NativeArray<Column>(height * depth, Allocator.TempJob);
            var colsY = new NativeArray<Column>(width * depth, Allocator.TempJob);
            var colsZ = new NativeArray<Column>(width * height, Allocator.TempJob);
            var meshIntersections = new NativeList<float4>(Allocator.TempJob);

            foreach (var handle in intersectionJobs)
            {
                handle.handle.Complete();

                var col = new Column(meshIntersections.Length, handle.intersections.Length);

                meshIntersections.AddRange(handle.intersections);

                handle.intersections.Dispose();

                switch (handle.axis)
                {
                    case 0:
                        colsX[handle.colIndex] = col;
                        break;
                    case 1:
                        colsY[handle.colIndex] = col;
                        break;
                    case 2:
                        colsZ[handle.colIndex] = col;
                        break;
                }
            }

            var voxelizeJob = new VoxelizerJob
            {
                colsX = colsX,
                colsY = colsY,
                colsZ = colsZ,
                intersections = meshIntersections,
                material = material,
                grid = grid
            };

            voxelizeJob.Schedule().Complete();

            colsX.Dispose();
            colsY.Dispose();
            colsZ.Dispose();

            meshIntersections.Dispose();

            scaledVertices.Dispose();
        }
    }
}
