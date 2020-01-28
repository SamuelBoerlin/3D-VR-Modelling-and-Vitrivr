using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sculpting
{
    [BurstCompile]
    public struct MeshScaleJob : IJob
    {
        /// <summary>
        /// Vertices of the mesh to voxelize. 3 consecutive vertices form one triangle.
        /// </summary>
        [ReadOnly] public NativeArray<float3> vertices;

        [ReadOnly] public int width, height, depth;

        [WriteOnly] public NativeArray<float3> outVertices;

        public void Execute()
        {
            var numVerts = vertices.Length;

            var padding = 2.5f;
            var center = new float3(width / 2.0f, height / 2.0f, depth / 2.0f);

            //Scale model and position such that it fits into the grid
            float3 maxBounds = 0.0f;
            float3 minBounds = 0.0f;
            for (int l = vertices.Length, i = 0; i < l; i++)
            {
                maxBounds = math.max(vertices[i], maxBounds);
                minBounds = math.min(vertices[i], minBounds);
            }
            float3 midBounds = new float3((maxBounds.x + minBounds.x) / 2.0f, (maxBounds.y + minBounds.y) / 2.0f, (maxBounds.z + minBounds.z) / 2.0f);

            float3 maxDist = 0.0f;
            for (int l = vertices.Length, i = 0; i < l; i++)
            {
                var dif = vertices[i] - midBounds;
                maxDist = math.max(new float3(dif.x * dif.x, dif.y * dif.y, dif.z * dif.z), maxDist);
            }
            maxDist = math.sqrt(maxDist);
            float3 scales = new float3((width / 2.0f - padding) / maxDist.x, (height / 2.0f - padding) / maxDist.y, (depth / 2.0f - padding) / maxDist.z);
            float scale = math.cmin(scales);

            for (int l = vertices.Length, i = 0; i < l; i++)
            {
                outVertices[i] = (vertices[i] - midBounds) * scale + center;
            }
        }
    }
}
