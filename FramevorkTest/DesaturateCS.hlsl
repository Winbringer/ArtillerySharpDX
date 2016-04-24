﻿Texture2D<float4> input : register(t0);
RWTexture2D<float4> output : register(u0);
cbuffer ComputeConstants : register(b0)
{
    float Intensity;
};
// used for RGB/sRGB color models
#define LUMINANCE_RGB float3(0.2125, 0.7154, 0.0721)
#define LUMINANCE(_V) dot(_V.rgb, LUMINANCE_RGB)
// Desaturate the input, the result is returned in output
[numthreads(THREADSX, THREADSY, 1)]
void DesaturateCS(uint groupIndex : SV_GroupIndex,
 uint3 groupId : SV_GroupID,
 uint3 groupThreadId : SV_GroupThreadID,
 uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
     // Calculate the relative luminance  
    float3 target = (float3) LUMINANCE(sample.rgb);
    output[dispatchThreadId.xy] = float4(lerp(target, sample.rgb, Intensity), sample.a);
}