using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sculpting
{
    [BurstCompile]
    public struct MeshIntersectionJob : IJob
    {
        /// <summary>
        /// Vertices of the mesh to voxelize. 3 consecutive vertices form one triangle.
        /// </summary>
        [ReadOnly] public NativeArray<float3> scaledVertices;

        [ReadOnly] public int width, height, depth;

        [ReadOnly] public int px, py, pz;

        [ReadOnly] public int axis;

        [ReadOnly] public int index;

        public NativeList<float4> meshIntersections;

        private struct IntersectionSorter : IComparer<float4>
        {
            public int Compare(float4 x, float4 y)
            {
                return (int)math.sign(x.w - y.w);
            }
        }

        public void Execute()
        {
            var numVerts = scaledVertices.Length;

            var sorter = new IntersectionSorter();

            float3 ray, pos;

            switch (axis)
            {
                case 0:
                    //Intersect X axis
                    ray = new float3(width, 0, 0);

                    pos = new float3(0, py, pz + 0.0001f);

                    //Find all intersections and normals
                    for (int v = 0; v < numVerts; v += 3)
                    {
                        if (IntersectTriangle(pos, ray, scaledVertices[v], scaledVertices[v + 1], scaledVertices[v + 2], out float t, out float bu, out float bv))
                        {
                            //Use flat normal
                            var normal = math.normalize(math.cross(scaledVertices[v + 2] - scaledVertices[v + 1], scaledVertices[v] - scaledVertices[v + 1]));

                            meshIntersections.Add(new float4(normal, t * width));
                        }
                    }

                    //Sort intersections
                    meshIntersections.Sort(sorter);

                    break;

                case 1:

                    //Intersect Y axis
                    ray = new float3(0, height, 0);

                    pos = new float3(px + 0.0001f, 0, pz);

                    //Find all intersections and normals
                    for (int v = 0; v < numVerts; v += 3)
                    {
                        if (IntersectTriangle(pos, ray, scaledVertices[v], scaledVertices[v + 1], scaledVertices[v + 2], out float t, out float bu, out float bv))
                        {
                            //Use flat normal
                            var normal = math.normalize(math.cross(scaledVertices[v + 2] - scaledVertices[v + 1], scaledVertices[v] - scaledVertices[v + 1]));

                            meshIntersections.Add(new float4(normal, t * height));
                        }
                    }

                    //Sort intersections
                    meshIntersections.Sort(sorter);

                    break;

                case 2:
                    //Intersect Z axis
                    ray = new float3(0, 0, depth);
                    pos = new float3(px, py + 0.0001f, 0);

                    //Find all intersections and normals
                    for (int v = 0; v < numVerts; v += 3)
                    {
                        if (IntersectTriangle(pos, ray, scaledVertices[v], scaledVertices[v + 1], scaledVertices[v + 2], out float t, out float bu, out float bv))
                        {
                            //Use flat normal
                            var normal = math.normalize(math.cross(scaledVertices[v + 2] - scaledVertices[v + 1], scaledVertices[v] - scaledVertices[v + 1]));

                            meshIntersections.Add(new float4(normal, t * depth));
                        }
                    }

                    //Sort intersections
                    meshIntersections.Sort(sorter);

                    break;
            }
        }

        /// <summary>
        /// Triangle ray intersection: http://fileadmin.cs.lth.se/cs/personal/tomas_akenine-moller/raytri/.
        /// Returns whether the ray intersections, intersection distance and barycentric coordinates u and v.
        /// </summary>
        private bool IntersectTriangle(float3 orig, float3 dir,
            float3 vert0, float3 vert1, float3 vert2,
            out float t, out float u, out float v)
        {
            t = 0;
            u = 0;
            v = 0;

            const float EPSILON = 0.00001f;

            float3 edge1, edge2, tvec, pvec, qvec;
            float det, inv_det;

            /* find vectors for two edges sharing vert0 */
            //SUB(edge1, vert1, vert0);
            edge1 = vert1 - vert0;
            //SUB(edge2, vert2, vert0);
            edge2 = vert2 - vert0;

            /* begin calculating determinant - also used to calculate U parameter */
            //CROSS(pvec, dir, edge2);
            pvec = math.cross(dir, edge2);

            /* if determinant is near zero, ray lies in plane of triangle */
            //det = DOT(edge1, pvec);
            det = math.dot(edge1, pvec);

            /* calculate distance from vert0 to ray origin */
            //SUB(tvec, orig, vert0);
            tvec = orig - vert0;
            inv_det = 1.0f / det;

            //CROSS(qvec, tvec, edge1);
            //qvec = tvec - edge1;
            qvec = math.cross(tvec, edge1);

            if (det > EPSILON)
            {
                /**u = DOT(tvec, pvec);
                if (*u < 0.0 || *u > det)
                    return 0;*/
                u = math.dot(tvec, pvec);
                if (u < 0.0f || u > det)
                {
                    return false;
                }

                /* calculate V parameter and test bounds */
                /**v = DOT(dir, qvec);
                if (*v < 0.0 || *u + *v > det)
                    return 0;*/
                v = math.dot(dir, qvec);
                if (v < 0.0f || u + v > det)
                {
                    return false;
                }
            }
            else if (det < -EPSILON)
            {
                /* calculate U parameter and test bounds */
                /**u = DOT(tvec, pvec);
                if (*u > 0.0 || *u < det)
                    return 0;*/
                u = math.dot(tvec, pvec);
                if (u > 0.0f || u < det)
                {
                    return false;
                }

                /* calculate V parameter and test bounds */
                /**v = DOT(dir, qvec);
                if (*v > 0.0 || *u + *v < det)
                    return 0;*/
                v = math.dot(dir, qvec);
                if (v > 0.0f || u + v < det)
                {
                    return false;
                }
            }
            else return false;  /* ray is parallell to the plane of the triangle */

            /**t = DOT(edge2, qvec) * inv_det;
            (*u) *= inv_det;
            (*v) *= inv_det;*/
            t = math.dot(edge2, qvec) * inv_det;
            u *= inv_det;
            v *= inv_det;

            return true;
        }
    }
}
