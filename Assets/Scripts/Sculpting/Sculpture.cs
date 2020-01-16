using System.Collections.Generic;
using UnityEngine;

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

    [SerializeField] private PolygonizationProperties poligonizationProperties = null;

    private readonly Dictionary<ChunkPos, SculptureChunk> chunks = new Dictionary<ChunkPos, SculptureChunk>();

    private MeshRenderer meshRenderer;

    private Vector3 TransformPointToLocalSpace(Vector3 vec)
    {
        return Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale).inverse.MultiplyPoint(vec);
    }

    private Vector3 TransformDirToLocalSpace(Vector3 vec)
    {
        return Quaternion.Inverse(transform.rotation) * vec;
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

    public ICollection<SculptureChunk> GetChunks()
    {
        return chunks.Values;
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
    public void ApplySdf(Vector3 pos, Quaternion rot, ISdf sdf, int material, bool replace)
    {
        pos = TransformPointToLocalSpace(pos);
        rot = TransformQuatToLocalSpace(rot);

        Debug.Log("Apply sdf at: " + pos);

        sdf = new TransformSDF(Matrix4x4.TRS(pos, rot, Vector3.one), sdf);

        Vector3 minBound = sdf.Min();
        Vector3 maxBound = sdf.Max();

        int minX = Mathf.FloorToInt(minBound.x / chunkSize);
        int minY = Mathf.FloorToInt(minBound.y / chunkSize);
        int minZ = Mathf.FloorToInt(minBound.z / chunkSize);

        int maxX = Mathf.FloorToInt(maxBound.x / chunkSize);
        int maxY = Mathf.FloorToInt(maxBound.y / chunkSize);
        int maxZ = Mathf.FloorToInt(maxBound.z / chunkSize);

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

                    chunk.ApplySdf(-cx * chunkSize, -cy * chunkSize, -cz * chunkSize, sdf, material, replace);
                }
            }
        }
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

                            SculptureChunk chunk = GetChunk(ChunkPos.FromVoxel(bx, by, bz, chunkSize));

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
        foreach (ChunkPos pos in chunks.Keys)
        {
            SculptureChunk chunk = chunks[pos];

            if (chunk.mesh == null || chunk.NeedsRebuild)
            {
                chunk.Build();
            }
            else
            {
                Graphics.DrawMesh(chunk.mesh, Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale) * Matrix4x4.Translate(new Vector3(pos.x * chunkSize, pos.y * chunkSize, pos.z * chunkSize)), meshRenderer.material, 0);
            }
        }
    }
}
