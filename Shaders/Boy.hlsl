cbuffer data : register(b0)
{
    float4x4 WVP;
};
struct VS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float2 TextureUV : TEXCOORD;
  
};

struct PS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float2 TextureUV : TEXCOORD;
   
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
    output.position = mul(input.position, WVP); 
    output.TextureUV = input.TextureUV;
    return output;
}

Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);
float4 PS(PS_IN input) : SV_Target
{    
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    return color;
}
