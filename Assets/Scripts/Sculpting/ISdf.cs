using Unity.Mathematics;

namespace Sculpting
{
    public interface ISdf
    {
        float Eval(float3 pos);
        float3 Min();
        float3 Max();
    }
}