using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sculpting
{
    //TODO Give one voxel padding in +X/+Y/+Z that mirrors the voxels of the adjacent chunks.
    //This will be necessary to make chunk mesh building independent and to jobify SDF modifications because applying an intersection change requires
    //both materials of each edge.
    public class SculptureChunk : IDisposable
    {
        private readonly int chunkSize;
        private readonly int chunkSizeSq;
        public int ChunkSize
        {
            get
            {
                return chunkSize;
            }
        }

        private NativeArray3D<Voxel> voxels;

        //TODO Cleanup, separate mesh from chunk
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
            this.chunkSizeSq = chunkSize * chunkSize;
            this.Pos = pos;

            voxels = new NativeArray3D<Voxel>(chunkSize + 1, chunkSize + 1, chunkSize + 1, Allocator.Persistent);
            //voxels = new Voxel[chunkSize * chunkSize * chunkSize];
            //voxels = new NativeArray<Voxel>(chunkSize * chunkSize * chunkSize, Allocator.Persistent); //TODO Dispose
        }

        public int GetMaterial(int x, int y, int z)
        {
            return voxels[x, y, z].Material;
        }

        public delegate void FinalizeChange();
        public FinalizeChange ScheduleSdf<TSdf>(float ox, float oy, float oz, TSdf sdf, int material, bool replace)
            where TSdf : struct, ISdf
        {
            var changed = new NativeArray<bool>(1, Allocator.TempJob);
            var outVoxels = new NativeArray3D<Voxel>(voxels.Length(0), voxels.Length(1), voxels.Length(2), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var sdfJob = new ChunkSdfJob<TSdf>
            {
                origin = new float3(ox, oy, oz),
                sdf = sdf,
                material = material,
                replace = replace,
                snapshot = voxels,
                changed = changed,
                outVoxels = outVoxels
            };

            var handle = sdfJob.Schedule();

            return () =>
            {
                handle.Complete();

                voxels.Dispose();
                voxels = outVoxels;

                if (sdfJob.changed[0])
                {
                    NeedsRebuild = true;
                }
                changed.Dispose();

                //Update the padding of all -X/-Y/-Z adjacent chunks
                //TODO Only propagate those sides that have changed
                PropagatePadding();
            };
        }

        /// <summary>
        /// Propagates the -X/-Y/-Z border voxels to the padding of the -X/-Y/-Z adjacent chunks
        /// </summary>
        private void PropagatePadding()
        {
            sculpture.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y, Pos.z))?.UpdatePadding(this);
            sculpture.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y - 1, Pos.z))?.UpdatePadding(this);
            sculpture.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y, Pos.z - 1))?.UpdatePadding(this);

            sculpture.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y - 1, Pos.z))?.UpdatePadding(this);
            sculpture.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y, Pos.z - 1))?.UpdatePadding(this);
            sculpture.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y - 1, Pos.z - 1))?.UpdatePadding(this);

            sculpture.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y - 1, Pos.z - 1))?.UpdatePadding(this);
        }

        /// <summary>
        /// Updates the padding of this chunk to the -X/-Y/-Z border voxels of the specified neighbor chunk
        /// </summary>
        /// <param name="neighbor"></param>    
        private void UpdatePadding(SculptureChunk neighbor)
        {
            var xOff = neighbor.Pos.x - Pos.x;
            var yOff = neighbor.Pos.y - Pos.y;
            var zOff = neighbor.Pos.z - Pos.z;

            if (xOff < 0 || xOff > 1 || yOff < 0 || yOff > 1 || zOff < 0 || zOff > 1 || xOff + yOff + zOff == 0)
            {
                throw new ArgumentException("Chunk is not a -X/-Y/-Z neighbor!");
            }

            //xOff == 0 ==> xStart =         0, xEnd = chunkSize
            //xOff == 1 ==> xStart = chunkSize, xEnd = chunkSize + 1 
            var xStart = xOff * chunkSize;
            var xEnd = xStart + 1 + (1 - xOff) * (chunkSize - 1);
            var yStart = yOff * chunkSize;
            var yEnd = yStart + 1 + (1 - yOff) * (chunkSize - 1);
            var zStart = zOff * chunkSize;
            var zEnd = zStart + 1 + (1 - zOff) * (chunkSize - 1);

            //Debug.Log("Update padding: " + xOff + "/" + yOff + "/" + zOff + " " + xStart + "-" + xEnd + " " + yStart + "-" + yEnd + " " + zStart + "-" + zEnd);

            for (int z = zStart; z < zEnd; z++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    for (int x = xStart; x < xEnd; x++)
                    {
                        voxels[x, y, z] = neighbor.voxels[x % chunkSize, y % chunkSize, z % chunkSize];
                    }
                }
            }
        }

        public void FillCell(int x, int y, int z, int cellIndex, NativeArray<int> materials, NativeArray<float> intersections, NativeArray<float3> normals)
        {
            ChunkBuildJob.FillCell(voxels, x, y, z, cellIndex, materials, intersections, normals);
        }

        public delegate void FinalizeBuild();
        public FinalizeBuild ScheduleBuild()
        {
            NeedsRebuild = false;

            var meshVertices = new NativeList<float3>(Allocator.TempJob);
            var meshNormals = new NativeList<float3>(Allocator.TempJob);
            var meshTriangles = new NativeList<int>(Allocator.TempJob);
            var meshColors = new NativeList<Color32>(Allocator.TempJob);
            var meshMaterials = new NativeList<int>(Allocator.TempJob);

            ChunkBuildJob polygonizerJob = new ChunkBuildJob
            {
                Voxels = voxels,
                PolygonizationProperties = sculpture.CMSProperties.Data,
                MeshVertices = meshVertices,
                MeshNormals = meshNormals,
                MeshTriangles = meshTriangles,
                MeshColors = meshColors,
                MeshMaterials = meshMaterials,
            };

            var handle = polygonizerJob.Schedule();

            return () =>
            {
                handle.Complete();

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

                meshVertices.Dispose();
                meshNormals.Dispose();
                meshTriangles.Dispose();
                meshColors.Dispose();
                meshMaterials.Dispose();

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
            };
        }

        public void Dispose()
        {
            voxels.Dispose();
        }
    }
}