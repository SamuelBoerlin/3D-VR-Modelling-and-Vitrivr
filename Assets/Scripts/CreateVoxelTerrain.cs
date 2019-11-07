using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelPolygonizer;
using VoxelPolygonizer.CMS;

public struct MaterialColors : VoxelMeshTessellation.IMaterialColorMap
{
    public Color32 GetColor(int material)
    {
        switch (material)
        {
            default:
                return Color.white;
            case 1:
                return Color.red;
            case 2:
                return Color.green;
            case 3:
                return Color.blue;
        }
    }
}

[BurstCompile]
public struct PolygonizeJob : IJob
{
    public NativeMemoryCache MemoryCache;

    [ReadOnly] public NativeArray<float3> Cells;

    [ReadOnly] public NativeArray<int> Materials;

    [ReadOnly] public NativeArray<float> Intersections;

    [ReadOnly] public NativeArray<float3> Normals;

    public NativeList<VoxelMeshComponent> Components;

    public NativeList<PackedIndex> Indices;

    public NativeList<VoxelMeshComponentVertex> Vertices;

    public VoxelMeshTessellation.NativeDeduplicationCache DedupeCache;
    public NativeList<float3> MeshVertices;
    public NativeList<float3> MeshNormals;
    public NativeList<int> MeshTriangles;
    public NativeList<Color32> MeshColors;
    public NativeList<int> MeshMaterials;

    public void Execute()
    {
        var solver = new SvdQefSolver<TestArrayVoxelCell>();
        solver.Clamp = false;
        var polygonizer = new CMSVoxelPolygonizer<TestArrayVoxelCell, CMSStandardProperties, SvdQefSolver<TestArrayVoxelCell>, IntersectionSharpFeatureSolver<TestArrayVoxelCell>>(new CMSStandardProperties(), solver, new IntersectionSharpFeatureSolver<TestArrayVoxelCell>(), MemoryCache);
        //var polygonizer = new CMSVoxelPolygonizer<TestArrayVoxelCell, CMSStandardProperties, MeanQefSolver<TestArrayVoxelCell>>(new CMSStandardProperties(), new MeanQefSolver<TestArrayVoxelCell>(), MemoryCache);

        for (int i = 0; i < Cells.Length; i++)
        {
            TestArrayVoxelCell cell = new TestArrayVoxelCell(i, Cells[i], Materials, Intersections, Normals);

            polygonizer.Polygonize(cell, Components, Indices, Vertices);
        }

        VoxelMeshTessellation.Tessellate<MaterialColors>(Components, Indices, Vertices, Matrix4x4.identity, MeshVertices, MeshTriangles, MeshNormals, MeshMaterials, new MaterialColors(), MeshColors, DedupeCache);
    }
}

public readonly struct JobCell
{
    public readonly TestArrayVoxelCell cell;
    public readonly Vector3 pos;

    public JobCell(TestArrayVoxelCell cell, Vector3 pos)
    {
        this.cell = cell;
        this.pos = pos;
    }
}


[RequireComponent(typeof(MeshFilter))]
public class CreateVoxelTerrain : MonoBehaviour
{
    [SerializeField] private MeshCollider meshCollider = null;

    //private readonly CMSVoxelPolygonizer<TestArrayVoxelCell> polygonizer = new CMSVoxelPolygonizer<TestArrayVoxelCell>();
    //private readonly SvdQefSolver qefSolver = new SvdQefSolver();

    private Mesh voxelMesh = null;

    [SerializeField] private float scale = 1.0f;

    [SerializeField] private bool regenerate = false;

    [SerializeField] private bool placeSdf = false;

    [SerializeField] private bool sphereSdf = true;

    [SerializeField] private bool replaceSdfMaterial = false;

    [SerializeField] [Range(0, 3)] private int sdfMaterial = 1;

    [SerializeField] private bool generateEachFrame = false;
    private int run = 0;

    [SerializeField] private bool lockSelection = false;
    [SerializeField] Vector3Int lockedSelection = Vector3Int.zero;

    [SerializeField] private bool renderLockedSelectionOnly = false;

    [SerializeField] [Range(0.0f, 30.0f)] private float fixedTime = 0.0f;

    private Vector3 gizmoPosition = Vector3.zero;
    private Matrix4x4 gizmoTransform = Matrix4x4.identity;
    private TestArrayVoxelCell gizmoCell = new TestArrayVoxelCell();
    private List<VoxelMeshComponent> gizmoComponents = null;
    private List<PackedIndex> gizmoComponentIndices = null;
    private List<VoxelMeshComponentVertex> gizmoComponentVertices = null;

    private NativeArray<int> gizmoCellMaterials;
    private NativeArray<float3> gizmoCellNormals;
    private NativeArray<float> gizmoCellIntersections;

    private Vector3Int? prevSelectedCell = null;
    private Vector3Int? selectedCell = null;

    const int fieldSize = 16;

    private TestVoxelField field;

    private class PlacedSdf
    {
        internal readonly ISdf sdf;
        internal readonly int material;
        internal readonly bool replace;

        internal PlacedSdf(ISdf sdf, int material, bool replace)
        {
            this.sdf = sdf;
            this.material = material;
            this.replace = replace;
        }
    }

    private List<PlacedSdf> placedSdfs = new List<PlacedSdf>();


    private void ApplySdfWithScale(TestVoxelField field, float scale, float x, float y, float z, ISdf sdf, int material, bool replace)
    {
        field.ApplySdf(x * scale, y * scale, z * scale, new ScaleSDF(scale, sdf), material, replace);
    }

    private void GenerateScene(TestVoxelField field)
    {
        //ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 0.5f + 8), new SphereSDF(8.5f)), 1, false);
        // ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 0.5f + 3), new SphereSDF(4f)), 0, false);

        /*ApplySdfWithScale(field, scale, 8, 4, 10, new BoxSDF(8.5f), 3, false);
        ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 0.5f + 3), new SphereSDF(4.5f)), 0, false);
        ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 8.5f), new SphereSDF(5.6f)), 1, true);
        */

        /*
        ApplySdfWithScale(field, scale, 8, 4, 10, new BoxSDF(8.5f), 3, false);

        ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 0.5f), new BoxSDF(6)), 1, false);

        ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 0.5f + 3), new SphereSDF(4.5f)), 0, false);

        ApplySdfWithScale(field, scale, 8, 8, 9, new OffsetSDF(new Vector3(0.5f, 0.5f, 8.5f), new BoxSDF(4.75f)), 3, true);

        ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 8.5f), new SphereSDF(5.6f)), 1, true);

        ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 0.5f), new BoxSDF(2.4f)), 2, false);
        */


        float time = fixedTime;// Time.time;

        /*ApplySdfWithScale(field, scale, 8, 8, 8, new PerlinSDF(new Vector3(-8, -8, -8), new Vector3(8, 8, 8), new Vector2(time, time * 0.5f), new Vector2(1, 1) * 0.1f, 6f, 4, 2.0f, 0.25f), 1, false);

        ApplySdfWithScale(field, scale, 8, 10, 8, new TransformSDF(Matrix4x4.Rotate(Quaternion.Euler(time * 12, time * 45, time * 60)), new BoxSDF(4.8f)), 3, false);

        ApplySdfWithScale(field, scale, 8, 12, 8, new SphereSDF(3.75f), 0, false);

        ApplySdfWithScale(field, scale, 8, 10, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 0.5f), new BoxSDF(1.4f)), 0, false);*/

        /*ApplySdfWithScale(field, scale, 8, 8, 8, new PerlinSDF(new Vector3(-8, -8, -8), new Vector3(8, 8, 8), new Vector2(time, time * 0.5f), new Vector2(1, 1) * 0.1f, 6f, 4, 2.0f, 0.25f), 1, false);

        ApplySdfWithScale(field, scale, 8, 10, 8, new TransformSDF(Matrix4x4.Rotate(Quaternion.Euler(0, time * 45, 0)), new BoxSDF(4.8f)), 3, true);

        ApplySdfWithScale(field, scale, 8, 12, 8, new SphereSDF(3.75f), 2, true);

        ApplySdfWithScale(field, scale, 8, 10, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 0.5f), new BoxSDF(2)), 0, false);*/

        /*ApplySdfWithScale(field, scale, 8, 11, 8, new TransformSDF(Matrix4x4.Rotate(Quaternion.Euler(Time.time * 15, -Time.time * 45, Time.time * 18)), new BoxSDF(4.1f)), 3, false);
        ApplySdfWithScale(field, scale, 8, 11, 8, new TransformSDF(Matrix4x4.Rotate(Quaternion.Euler(-Time.time * 15, Time.time * 45, Time.time * 18)), new BoxSDF(1.8f)), 0, false);*/

        foreach (PlacedSdf placedSdf in placedSdfs)
        {
            ApplySdfWithScale(field, scale, 0, 0, 0, placedSdf.sdf, placedSdf.material, placedSdf.replace);
        }
    }




    private void GenerateMesh()
    {
        int size = (int)Mathf.Ceil(fieldSize * scale);

        field = new TestVoxelField(size, size, size);
        GenerateScene(field);

        var dedupedTable = new Dictionary<int, List<VoxelMeshTessellation.DedupedVertex>>();

        var components = new NativeList<VoxelMeshComponent>(Allocator.Persistent);
        var componentIndices = new NativeList<PackedIndex>(Allocator.Persistent);
        var componentVertices = new NativeList<VoxelMeshComponentVertex>(Allocator.Persistent);

        int voxels = /*1;//*/ (size - 1) * (size - 1) * (size - 1);

        var cellMaterials = new NativeArray<int>(voxels * 8, Allocator.Persistent);
        var cellIntersections = new NativeArray<float>(voxels * 12, Allocator.Persistent);
        var cellNormals = new NativeArray<float3>(voxels * 12, Allocator.Persistent);

        var cells = new NativeArray<float3>(voxels, Allocator.Persistent);

        int voxelIndex = 0;
        for (int z = 0; z < size - 1; z++)
        {
            for (int y = 0; y < size - 1; y++)
            {
                for (int x = 0; x < size - 1; x++)
                {
                    if (renderLockedSelectionOnly && !(x == lockedSelection.x && y == lockedSelection.y && z == lockedSelection.z))
                    {
                        continue;
                    }

                    field.FillCell(x, y, z, voxelIndex, cellMaterials, cellIntersections, cellNormals);

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

        var polygonizerJob = new PolygonizeJob
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

        string text = "Polygonized voxel field in " + watch.ElapsedMilliseconds + "ms. Vertices: " + meshVertices.Length + ". Run: " + run;
        Debug.Log(text);

        var cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            var display = cam.GetComponent<FPSDisplay>();
            if (display != null)
            {
                display.SetInfo(text);
            }
        }

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


        run++;

        voxelMesh.Clear(false);
        voxelMesh.SetVertices(vertices);
        voxelMesh.SetNormals(normals);
        voxelMesh.SetTriangles(indices, 0);
        if (colors.Count > 0)
        {
            voxelMesh.SetColors(colors);
        }

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = voxelMesh;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        gizmoCellMaterials = new NativeArray<int>(8, Allocator.Persistent);
        gizmoCellNormals = new NativeArray<float3>(12, Allocator.Persistent);
        gizmoCellIntersections = new NativeArray<float>(12, Allocator.Persistent);

        voxelMesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = voxelMesh;

        //polygonizer.QefSolver = qefSolver;

        int size = (int)Mathf.Ceil(fieldSize * scale);

        field = new TestVoxelField(size, size, size);
        GenerateScene(field);

        GenerateMesh();
    }

    private void OnApplicationQuit()
    {
        gizmoCellMaterials.Dispose();
        gizmoCellNormals.Dispose();
        gizmoCellIntersections.Dispose();
    }

    private void Update()
    {
        if (!lockSelection)
        {
            Camera camera = Camera.current;
            if (camera != null)
            {
                Vector3 relPos = camera.transform.position - transform.position;
                Vector3 relDir = Quaternion.Inverse(transform.rotation) * camera.transform.forward.normalized;

                //if (field.RayCast(relPos, relDir, 16, out TestVoxelField.RayCastResult result))
                if (gameObject.GetComponent<Sculpture>().RayCast(relPos, relDir, 16, out Sculpture.RayCastResult result))
                {
                    selectedCell = new Vector3Int(Mathf.FloorToInt(result.pos.x), Mathf.FloorToInt(result.pos.y), Mathf.FloorToInt(result.pos.z));
                }
                else
                {
                    selectedCell = null;
                }

                if (selectedCell != null)
                {
                    lockedSelection = selectedCell.Value;
                }
                else
                {
                    lockedSelection = Vector3Int.zero;
                }
            }
            else
            {
                selectedCell = null;
            }
        }
        else
        {
            selectedCell = lockedSelection;
        }

        if (selectedCell != null && prevSelectedCell != selectedCell)
        {
            //field.FillCell(selectedCell.Value.x, selectedCell.Value.y, selectedCell.Value.z, 0, gizmoCellMaterials, gizmoCellIntersections, gizmoCellNormals);
            var sculpture = gameObject.GetComponent<Sculpture>();
            SculptureChunk chunk = sculpture.GetChunk(ChunkPos.FromVoxel(selectedCell.Value, sculpture.ChunkSize));
            if (chunk != null)
            {
                chunk.FillCell(
                    ((selectedCell.Value.x % sculpture.ChunkSize) + sculpture.ChunkSize) % sculpture.ChunkSize,
                    ((selectedCell.Value.y % sculpture.ChunkSize) + sculpture.ChunkSize) % sculpture.ChunkSize,
                    ((selectedCell.Value.z % sculpture.ChunkSize) + sculpture.ChunkSize) % sculpture.ChunkSize,
                    0, gizmoCellMaterials, gizmoCellIntersections, gizmoCellNormals);

                gizmoCell = new TestArrayVoxelCell(0, (Vector3)selectedCell.Value, gizmoCellMaterials, gizmoCellIntersections, gizmoCellNormals);

                NativeMemoryCache memoryCache = new NativeMemoryCache(Allocator.Persistent);

                var polygonizer = new CMSVoxelPolygonizer<TestArrayVoxelCell, CMSStandardProperties, SvdQefSolver<TestArrayVoxelCell>, IntersectionSharpFeatureSolver<TestArrayVoxelCell>>(new CMSStandardProperties(), new SvdQefSolver<TestArrayVoxelCell>(), new IntersectionSharpFeatureSolver<TestArrayVoxelCell>(), memoryCache);

                var components = new NativeList<VoxelMeshComponent>(Allocator.Persistent);
                var componentIndices = new NativeList<PackedIndex>(Allocator.Persistent);
                var componentVertices = new NativeList<VoxelMeshComponentVertex>(Allocator.Persistent);

                polygonizer.Polygonize(gizmoCell, components, componentIndices, componentVertices);

                gizmoComponents = new List<VoxelMeshComponent>(components.Length);
                for (int i = 0; i < components.Length; i++)
                {
                    gizmoComponents.Add(components[i]);
                }

                gizmoComponentIndices = new List<PackedIndex>(componentIndices.Length);
                for (int i = 0; i < componentIndices.Length; i++)
                {
                    gizmoComponentIndices.Add(componentIndices[i]);
                }

                gizmoComponentVertices = new List<VoxelMeshComponentVertex>(componentVertices.Length);
                for (int i = 0; i < componentVertices.Length; i++)
                {
                    gizmoComponentVertices.Add(componentVertices[i]);
                }

                memoryCache.Dispose();
                components.Dispose();
                componentIndices.Dispose();
                componentVertices.Dispose();

                gizmoPosition = selectedCell.Value;
                gizmoTransform = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            }
        }

        prevSelectedCell = selectedCell;

        if (placeSdf)
        {
            placeSdf = false;
            regenerate = true;
            ISdf newSdf;
            if (sphereSdf)
            {
                newSdf = new SphereSDF(2.1f + 20);
            }
            else
            {
                newSdf = new BoxSDF(2.1f + 20);
            }
            placedSdfs.Add(new PlacedSdf(new OffsetSDF(new Vector3(-gizmoPosition.x, -gizmoPosition.y, -gizmoPosition.z), newSdf), sdfMaterial, replaceSdfMaterial));
            Debug.Log("Placed SDF at " + gizmoPosition + " " + placedSdfs.Count);

            gameObject.GetComponent<Sculpture>().ApplySdf(new Vector3(gizmoPosition.x, gizmoPosition.y, gizmoPosition.z), Quaternion.identity, newSdf, sdfMaterial, replaceSdfMaterial);
        }

        if (generateEachFrame || regenerate)
        {
            regenerate = false;
            GenerateMesh();
        }
    }

    private void OnDrawGizmos()
    {
        //ApplySdfWithScale(field, scale, 8, 8, 8, new OffsetSDF(new Vector3(0.5f, 0.5f, 8.5f), new SphereSDF(5.6f)), 1, true);
        /*Gizmos.color = Color.white;
        Gizmos.matrix = Matrix4x4.TRS(new Vector3(8 - 0.5f, 8 - 0.5f + 1, 8 - 8.5f + 3), Quaternion.Euler(0, 0, Time.time * 50), Vector3.one);
        Gizmos.DrawWireSphere(Vector3.zero, 5.6f);*/

        if (gizmoComponents != null && gizmoComponentIndices != null && gizmoComponentVertices != null)
        {
            VoxelMeshTessellation.DrawDebugGizmos<TestArrayVoxelCell, MaterialColors>(gizmoPosition, gizmoTransform, gizmoComponents, gizmoComponentIndices, gizmoComponentVertices, ref gizmoCell, new MaterialColors());
        }
    }
}
