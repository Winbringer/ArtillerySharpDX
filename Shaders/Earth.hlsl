cbuffer data : register(b0)
{
    float4x4 WVP;
};
struct VS_IN
{
    float4 position : SV_Position;
    float4 normal : NORMAL;
    float4 textureUV : TEXCOORD;
};

struct PS_IN
{
    float4 position : SV_Position;
    float4 normal : NORMAL;
    float4 textureUV : TEXCOORD;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
    output.position = mul(input.position, WVP);   
    output.normal = input.normal;
    output.textureUV = input.normal;

    return output;
}

Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);

float4 PS(PS_IN input) : SV_Target
{
    
    return textureMap.Sample(textureSampler, input.textureUV);
}