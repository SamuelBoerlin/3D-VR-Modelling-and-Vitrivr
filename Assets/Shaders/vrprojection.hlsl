void PixelShaderFunction_float(float2 uv, float w, out float2 vruv)

{

float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];

vruv.xy = uv.xy * scaleOffset.xy + scaleOffset.zw; // * w;

}