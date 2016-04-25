Texture2D<float4> input : register(t0);
RWTexture2D<float4> output : register(u0);
cbuffer ComputeConstants : register(b0)
{
    float Intensity;
};
// Lerp helper functions 
float4 lerpKeepAlpha(float4 source, float3 target, float T)
{
    return float4(lerp(source.rgb, target, T), source.a);
}
float4 lerpKeepAlpha(float3 source, float4 target, float T)
{
    return float4(lerp(source, target.rgb, T), target.a);
}

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
    //........Делает черно белым ...............................................................
    //float4 sample = input[dispatchThreadId.xy];
    // // Calculate the relative luminance  
    //float3 target = (float3) LUMINANCE(sample.rgb);
    //output[dispatchThreadId.xy] = float4(lerp(target, sample.rgb, Intensity), sample.a);
    
    //....................Меняет контрастность.
    //float4 sample = input[dispatchThreadId.xy]; 
    //// Adjust contrast by moving towards or away from gray 
    // // Note: if LerpT == -1, we achieve a negative image 
    // //          LerpT == 0.0 will result in gray  
    ////          LerpT == 1.0 will result in no change 
    // //          LerpT >  1.0 will increase contrast 
    // float3 target = float3(0.5,0.5,0.5); 
    //output[dispatchThreadId.xy] = lerpKeepAlpha(target, sample, Intensity);

    //.............Меняет яркость.
    //// Adjust brightness by adding or removing Black
    // // LerpT == 1.0 original image 
    //// LerpT > 1.0 brightens 
    //// LerpT < 1.0 darkens
    //float4 sample = input[dispatchThreadId.xy];
    //float3 target = float3(0, 0, 0);
    //output[dispatchThreadId.xy] = lerpKeepAlpha(target, sample, Intensity);

    //................Добавляет sepia tone (Светло коричневый тон)
    float4 sample = input[dispatchThreadId.xy];
    float3 target;
    target.r = saturate(dot(sample.rgb, float3(0.393, 0.769, 0.189)));
    target.g = saturate(dot(sample.rgb, float3(0.349, 0.686, 0.168)));
    target.b = saturate(dot(sample.rgb, float3(0.272, 0.534, 0.131)));
    output[dispatchThreadId.xy] = lerpKeepAlpha(sample, target, Intensity);
}