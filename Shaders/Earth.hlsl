cbuffer data : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
    float4x4 WorldInverseTranspose;
};

cbuffer data1 : register(b1)
{
    float Ns_SpecularPower;
    float Ni_OpticalDensity;
    float d_Transparency;
    float Tr_Transparency;
    float3 Tf_TransmissionFilter;  
    float4 Ka_AmbientColor;
    float4 Kd_DiffuseColor;
    float4 Ks_SpecularColor;
    float4 Ke_EmissiveColor;
};

cbuffer data2 : register(b2)
{
    float4 Color;
   float3 Direction;
};  

struct VS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float3 textureUV : TEXCOORD;
};

struct PS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float3 textureUV : TEXCOORD;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;   
    output.position = mul(input.position, WorldViewProjection);
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