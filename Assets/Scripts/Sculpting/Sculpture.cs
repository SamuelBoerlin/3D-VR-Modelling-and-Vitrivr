using System.Collections.Generic;
using UnityEngine;

namespace Sculpting
{
    [RequireComponent(typeof(Transform))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Sculpture : MonoBehaviour
    {
        [SerializeField] private int chunkSize = 16;
        public int ChunkSize
        {
            get
            {
                return chunkSize;
            }
        }

        [SerializeField] private CMSProperties cmsProperties = null;
        public CMSProperties CMSProperties
        {
            get
            {
                return cmsProperties;
            }
        }

        private readonly Dictionary<ChunkPos, SculptureChunk> chunks = new Dictionary<ChunkPos, SculptureChunk>();

        private MeshRenderer meshRenderer;

        private Vector3 TransformPointToLocalSpace(Vector3 vec)
        {
            return transform.InverseTransformPoint(vec);
        }

        private Vector3 TransformDirToLocalSpace(Vector3 vec)
        {
            return transform.InverseTransformDirection(vec);
        }

        private Quaternion TransformQuatToLocalSpace(Quaternion rot)
        {
            return Quaternion.Inverse(transform.rotation) * rot;
        }

        public SculptureChunk GetChunk(ChunkPos pos)
        {
            chunks.TryGetValue(pos, out SculptureChunk chunk);
            return chunk;
        }

        public Dictionary<ChunkPos, SculptureChunk>.ValueCollection GetChunks()
        {
            return chunks.Values;
        }

        public void Clear()
        {
            foreach(var chunk in chunks.Values)
            {
                chunk.Dispose();
            }
            chunks.Clear();
        }

        public void ApplyGrid(int x, int y, int z, NativeArray3D<Voxel> grid)
        {
            int minX = Mathf.FloorToInt(x / chunkSize);
            int minY = Mathf.FloorToInt(y / chunkSize);
            int minZ = Mathf.FloorToInt(z / chunkSize);

            int maxX = Mathf.FloorToInt((x + grid.Length(0)) / chunkSize);
            int maxY = Mathf.FloorToInt((y + grid.Length(1)) / chunkSize);
            int maxZ = Mathf.FloorToInt((z + grid.Length(2)) / chunkSize);

            var handles = new List<SculptureChunk.FinalizeChange>();

            var watch = System.Diagnostics.Stopwatch.StartNew();

            //Schedule all jobs
            for (int cx = minX; cx <= maxX; cx++)
            {
                for (int cy = minY; cy <= maxY; cy++)
                {
                    for (int cz = minZ; cz <= maxZ; cz++)
                    {
                        ChunkPos chunkPos = ChunkPos.FromChunk(cx, cy, cz);

                        chunks.TryGetValue(chunkPos, out SculptureChunk chunk);
                        if (chunk == null)
                        {
                            chunks[chunkPos] = chunk = new SculptureChunk(this, chunkPos, chunkSize);
                        }

                        var sx = Mathf.Max(0, x - cx * chunkSize);
                        var sy = Mathf.Max(0, y - cy * chunkSize);
                        var sz = Mathf.Max(0, z - cz * chunkSize);

                        var gx = cx * chunkSize - x;
                        var gy = cy * chunkSize - y;
                        var gz = cz * chunkSize - z;

                        chunk.ScheduleGrid(sx, sy, sz, gx, gy, gz, grid);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the specified signed distance field function to the sculpture.
        /// The SDF is applied in local space, i.e. 1 voxel = 1 unit on the signed distance field!
        /// </summary>
        /// <param name="pos">World position</param>
        /// <param name="rot">World rotation</param>
        /// <param name="sdf">Signed distance field function</param>
        /// <param name="material">Material to be added</param>
        /// <param name="replace">Whether </param>
        public void ApplySdf<TSdf>(Vector3 pos, Quaternion rot, TSdf sdf, int material, bool replace)
            where TSdf : struct, ISdf
        {
            pos = TransformPointToLocalSpace(pos);
            rot = TransformQuatToLocalSpace(rot);

            Debug.Log("Apply sdf at: " + pos);

            var transformedSdf = new TransformSDF<TSdf>(Matrix4x4.TRS(pos, rot, Vector3.one), sdf);

            Vector3 minBound = transformedSdf.Min();
            Vector3 maxBound = transformedSdf.Max();

            int minX = Mathf.FloorToInt(minBound.x / chunkSize);
            int minY = Mathf.FloorToInt(minBound.y / chunkSize);
            int minZ = Mathf.FloorToInt(minBound.z / chunkSize);

            int maxX = Mathf.FloorToInt(maxBound.x / chunkSize);
            int maxY = Mathf.FloorToInt(maxBound.y / chunkSize);
            int maxZ = Mathf.FloorToInt(maxBound.z / chunkSize);

            var handles = new List<SculptureChunk.FinalizeChange>();

            var watch = System.Diagnostics.Stopwatch.StartNew();

            //Schedule all jobs
            for (int cx = minX; cx <= maxX; cx++)
            {
                for (int cy = minY; cy <= maxY; cy++)
                {
                    for (int cz = minZ; cz <= maxZ; cz++)
                    {
                        ChunkPos chunkPos = ChunkPos.FromChunk(cx, cy, cz);

                        chunks.TryGetValue(chunkPos, out SculptureChunk chunk);
                        if (chunk == null)
                        {
                            chunks[chunkPos] = chunk = new SculptureChunk(this, chunkPos, chunkSize);
                        }

                        handles.Add(chunk.ScheduleSdf(-cx * chunkSize, -cy * chunkSize, -cz * chunkSize, transformedSdf, material, replace));
                    }
                }
            }

            //Wait and finalize jobs
            foreach(var handle in handles)
            {
                handle();
            }

            watch.Stop();

            string text = "Applied SDF to " + handles.Count + " voxel chunks in " + watch.ElapsedMilliseconds + "ms. Avg: " + (watch.ElapsedMilliseconds / (float)handles.Count) + "ms.";
            Debug.Log(text);
        }

        public readonly struct RayCastResult
        {
            public readonly Vector3 pos;
            public readonly Vector3 sidePos;
            public readonly SculptureChunk chunk;

            public RayCastResult(Vector3 pos, Vector3 sidePos, SculptureChunk chunk)
            {
                this.pos = pos;
                this.sidePos = sidePos;
                this.chunk = chunk;
            }
        }

        public bool RayCast(Vector3 pos, Vector3 dir, float dst, out RayCastResult result)
        {
            pos = TransformPointToLocalSpace(pos);
            dir = TransformDirToLocalSpace(dir);

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
                    for (int zo = 0; zo < 2; zo++)
                    {
                        for (int yo = 0; yo < 2; yo++)
                        {
                            for (int xo = 0; xo < 2; xo++)
                            {
                                int bx = x + xo;
                                int by = y + yo;
                                int bz = z + zo;

                                var chunk = GetChunk(ChunkPos.FromVoxel(bx, by, bz, chunkSize));

                                if (chunk != null)
                                {
                                    int material = chunk.GetMaterial(((bx % chunkSize) + chunkSize) % chunkSize, ((by % chunkSize) + chunkSize) % chunkSize, ((bz % chunkSize) + chunkSize) % chunkSize);
                                    if (material != 0)
                                    {
                                        result = new RayCastResult(new Vector3(x, y, z), new Vector3(prevX, prevY, prevZ), chunk);
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

            result = new RayCastResult(Vector3.zero, Vector3.zero, null);
            return false;
        }

        void Start()
        {
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
        }

        void Update()
        {
            var handles = new List<SculptureChunk.FinalizeBuild>();

            System.Diagnostics.Stopwatch watch = null;

            //Schedule all rebuild jobs
            foreach (ChunkPos pos in chunks.Keys)
            {
                SculptureChunk chunk = chunks[pos];

                if (chunk.mesh == null || chunk.NeedsRebuild)
                {
                    if(watch == null)
                    {
                        watch = System.Diagnostics.Stopwatch.StartNew();
                    }
                    handles.Add(chunk.ScheduleBuild());
                }

                if(chunk.mesh != null)
                {
                    Graphics.DrawMesh(chunk.mesh, Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale) * Matrix4x4.Translate(new Vector3(pos.x * chunkSize, pos.y * chunkSize, pos.z * chunkSize)), meshRenderer.material, 0);
                }
            }

            //Wait and finalize jobs
            foreach (var handle in handles)
            {
                handle();
            }

            if(watch != null) { 
                watch.Stop();

                string text = "Polygonized " + handles.Count + " voxel chunks in " + watch.ElapsedMilliseconds + "ms. Avg: " + (watch.ElapsedMilliseconds / (float)handles.Count) + "ms.";
                Debug.Log(text);
            }
        }

        void OnApplicationQuit()
        {
            foreach(var chunk in chunks.Values)
            {
                chunk.Dispose();
            }
            chunks.Clear();
        }
    }
}