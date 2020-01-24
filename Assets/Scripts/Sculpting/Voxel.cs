using Unity.Mathematics;

namespace Sculpting
{
    public struct Voxel
    {
        public readonly int Material;
        public readonly QuantizedHermiteData Data;

        //TODO Temporary for testing
        public float3 Intersections
        {
            get
            {
                return new float3(Data[0].w, Data[1].w, Data[2].w);
            }
        }
        public float3x3 Normals
        {
            get
            {
                return new float3x3(Data[0].xyz, Data[1].xyz, Data[2].xyz);
            }
        }

        public Voxel(int material, float3 intersections, float3x3 normals)
        {
            Material = material;
            Data = new QuantizedHermiteData(intersections, normals);
        }

        public Voxel(int material, QuantizedHermiteData data)
        {
            Material = material;
            Data = data;
        }

        public Voxel ModifyMaterial(int material)
        {
            return new Voxel(material, Data);
        }

        public Voxel ModifyEdge(int edge, float intersection, float3 normal)
        {
            float4x3 newData = (float4x3)Data;
            newData[edge] = new float4(normal, intersection);
            return new Voxel(Material, new float3(newData[0].w, newData[1].w, newData[2].w), new float3x3(newData[0].xyz, newData[1].xyz, newData[2].xyz));
        }
    }

    /*public readonly struct Voxel
    {
        public readonly int Material;
        public readonly float3 Intersections;
        public readonly float3x3 Normals;

        public Voxel(int material, float3 intersections, float3x3 normals)
        {
            this.Material = material;
            this.Intersections = intersections;
            this.Normals = normals;
        }

        public Voxel ModifyMaterial(int material)
        {
            return new Voxel(material, Intersections, Normals);
        }

        public Voxel ModifyEdge(int edge, float intersection, float3 normal)
        {
            float3 newIntersections = Intersections;
            newIntersections[edge] = intersection;

            float3x3 newNormals = Normals;
            newNormals[edge] = normal;

            return new Voxel(Material, newIntersections, newNormals);
        }
    }*/
}
