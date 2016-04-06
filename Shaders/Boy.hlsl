cbuffer data : register(b0)
{
    float4x4 WVP;
    float4x4 World;
    float4x4 WorldIT;
};

//float3 LightPosition = float3(1, -1, 1);
//float3 KS = float3(1, 1, 1);
//float SP = 12;
//float3 CameraPosition = float3(10, 10, -100);

struct VS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float2 TextureUV : TEXCOORD;
  
};

struct PS_IN
{
    float4 position : SV_Position;   
    float2 TextureUV : TEXCOORD;
    float3 WorldNormal : TEXCOORD1;
    float3 WorldPosition : WORLDPOS;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
    output.position = mul(input.position, WVP); 
    //output.WorldNormal = normalize(mul(input.normal, (float3x3) WorldIT));
    //output.WorldPosition = mul(input.position, World).xyz;
    output.TextureUV = input.TextureUV;
    return output;
}

Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);
float4 PS(PS_IN input) : SV_Target
{
    return  textureMap.Sample(textureSampler, input.TextureUV);
    //float3 normal = normalize(input.WorldNormal);
    //float3 toLight = normalize(LightPosition - input.WorldPosition);

    //float3 Abm = color.rgb * 0.2f;
    //float3 Dif = color.rgb * 0.8f * saturate(dot(normal, toLight));
    //float3 c = Abm + Dif;

    //return float4(c,1.0f);
}
