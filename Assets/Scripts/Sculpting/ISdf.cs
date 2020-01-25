using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Sculpting
{
    public interface ISdf
    {
        float Eval(float3 pos);
        float3 Min();
        float3 Max();

        [BurstDiscard]
        Matrix4x4? RenderingTransform();

        [BurstDiscard]
        ISdf RenderChild();
    }
}