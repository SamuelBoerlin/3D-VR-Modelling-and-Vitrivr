using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelPolygonizer;
using VoxelPolygonizer.CMS;
using static VoxelPolygonizer.VoxelMeshTessellation;

namespace Sculpting
{
    [BurstCompile]
    public struct ChunkBuildJob : IJob
    {
        [ReadOnly] public CMSProperties.DataStruct PolygonizationProperties;

        [ReadOnly] public NativeArray3D<Voxel> Voxels;

        public NativeList<float3> MeshVertices;
        public NativeList<float3> MeshNormals;
        public NativeList<int> MeshTriangles;
        public NativeList<Color32> MeshColors;
        public NativeList<int> MeshMaterials;

        private struct MaterialColors : IMaterialColorMap
        {
            public Color32 GetColor(int material)
            {
                switch(material)
                {
                    default:
                    case 1:
                        return Color.white;
                    case 2:
                        //red
                        return new Color(255 / 255.0f, 116 / 255.0f, 112 / 255.0f);
                    case 3:
                        //blue
                        return new Color(112 / 255.0f, 119 / 255.0f, 255 / 255.0f);
                    case 4:
                        //green
                        return new Color(112 / 255.0f, 255 / 255.0f, 115 / 255.0f);
                    case 5:
                        //violet
                        return new Color(173 / 255.0f, 112 / 255.0f, 255 / 255.0f);
                    case 6:
                        //yellow
                        return new Color(255 / 255.0f, 255 / 255.0f, 112 / 255.0f);
                }
            }
        }

        public void Execute()
        {
            int nVoxels = Voxels.Length(0) * Voxels.Length(1) * Voxels.Length(2);
            int nCells = (Voxels.Length(0) - 1) * (Voxels.Length(1) - 1) * (Voxels.Length(2) - 1);

            var MemoryCache = new NativeMemoryCache(Allocator.Temp);
            var DedupeCache = new VoxelMeshTessellation.NativeDeduplicationCache(Allocator.Temp);

            var Components = new NativeList<VoxelMeshComponent>(Allocator.Temp);
            var Indices = new NativeList<PackedIndex>(Allocator.Temp);
            var Vertices = new NativeList<VoxelMeshComponentVertex>(Allocator.Temp);

            var Materials = new NativeArray<int>(8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var Intersections = new NativeArray<float>(12, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var Normals = new NativeArray<float3>(12, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var solver = new SvdQefSolver<RawArrayVoxelCell>
            {
                Clamp = false
            };
            var polygonizer = new CMSVoxelPolygonizer<RawArrayVoxelCell, CMSProperties.DataStruct, SvdQefSolver<RawArrayVoxelCell>, IntersectionSharpFeatureSolver<RawArrayVoxelCell>>(PolygonizationProperties, solver, new IntersectionSharpFeatureSolver<RawArrayVoxelCell>(), MemoryCache);

            for (int z = 0; z < Voxels.Length(2) - 1; z++)
            {
                for (int y = 0; y < Voxels.Length(1) - 1; y++)
                {
                    for (int x = 0; x < Voxels.Length(0) - 1; x++)
                    {
                        FillCell(Voxels, x, y, z, 0, Materials, Intersections, Normals);

                        //TODO Directly operate on voxel array
                        RawArrayVoxelCell cell = new RawArrayVoxelCell(0, new float3(x, y, z), Materials, Intersections, Normals);

                        polygonizer.Polygonize(cell, Components, Indices, Vertices);
                    }
                }
            }

            VoxelMeshTessellation.Tessellate(Components, Indices, Vertices, Matrix4x4.identity, MeshVertices, MeshTriangles, MeshNormals, MeshMaterials, new MaterialColors(), MeshColors, DedupeCache);

            MemoryCache.Dispose();
            DedupeCache.Dispose();

            Materials.Dispose();
            Intersections.Dispose();
            Normals.Dispose();

            Components.Dispose();
            Indices.Dispose();
            Vertices.Dispose();

            //Cells.Dispose();
        }

        public static void FillCell(NativeArray3D<Voxel> voxels, int x, int y, int z, int cellIndex, NativeArray<int> materials, NativeArray<float> intersections, NativeArray<float3> normals)
        {
            materials[cellIndex * 8 + 0] = voxels[x, y, z].Material;
            materials[cellIndex * 8 + 1] = voxels[x + 1, y, z].Material;
            materials[cellIndex * 8 + 2] = voxels[x + 1, y, z + 1].Material;
            materials[cellIndex * 8 + 3] = voxels[x, y, z + 1].Material;
            materials[cellIndex * 8 + 4] = voxels[x, y + 1, z].Material;
            materials[cellIndex * 8 + 5] = voxels[x + 1, y + 1, z].Material;
            materials[cellIndex * 8 + 6] = voxels[x + 1, y + 1, z + 1].Material;
            materials[cellIndex * 8 + 7] = voxels[x, y + 1, z + 1].Material;

            intersections[cellIndex * 12 + 0] = voxels[x, y, z].Intersections[0];
            intersections[cellIndex * 12 + 1] = voxels[x + 1, y, z].Intersections[2];
            intersections[cellIndex * 12 + 2] = voxels[x, y, z + 1].Intersections[0];
            intersections[cellIndex * 12 + 3] = voxels[x, y, z].Intersections[2];
            intersections[cellIndex * 12 + 4] = voxels[x, y + 1, z].Intersections[0];
            intersections[cellIndex * 12 + 5] = voxels[x + 1, y + 1, z].Intersections[2];
            intersections[cellIndex * 12 + 6] = voxels[x, y + 1, z + 1].Intersections[0];
            intersections[cellIndex * 12 + 7] = voxels[x, y + 1, z].Intersections[2];
            intersections[cellIndex * 12 + 8] = voxels[x, y, z].Intersections[1];
            intersections[cellIndex * 12 + 9] = voxels[x + 1, y, z].Intersections[1];
            intersections[cellIndex * 12 + 10] = voxels[x + 1, y, z + 1].Intersections[1];
            intersections[cellIndex * 12 + 11] = voxels[x, y, z + 1].Intersections[1];

            normals[cellIndex * 12 + 0] = voxels[x, y, z].Normals[0];
            normals[cellIndex * 12 + 1] = voxels[x + 1, y, z].Normals[2];
            normals[cellIndex * 12 + 2] = voxels[x, y, z + 1].Normals[0];
            normals[cellIndex * 12 + 3] = voxels[x, y, z].Normals[2];
            normals[cellIndex * 12 + 4] = voxels[x, y + 1, z].Normals[0];
            normals[cellIndex * 12 + 5] = voxels[x + 1, y + 1, z].Normals[2];
            normals[cellIndex * 12 + 6] = voxels[x, y + 1, z + 1].Normals[0];
            normals[cellIndex * 12 + 7] = voxels[x, y + 1, z].Normals[2];
            normals[cellIndex * 12 + 8] = voxels[x, y, z].Normals[1];
            normals[cellIndex * 12 + 9] = voxels[x + 1, y, z].Normals[1];
            normals[cellIndex * 12 + 10] = voxels[x + 1, y, z + 1].Normals[1];
            normals[cellIndex * 12 + 11] = voxels[x, y, z + 1].Normals[1];
        }
    }
}