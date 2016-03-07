Texture2D diffuseMap : register(t0);
SamplerState textureSampler : register(s0)
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

cbuffer data : register(b0)
{
    float4x4 World;
    float4x4 View;
    float4x4 Proj;
    float Time;
};

struct VS_IN
{
    float4 pos : POSITION;
};

struct PS_IN
{
    float4 pos : SV_POSITION;   
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;     
    float4 Pos =input.pos;
    float4 wp = mul(World,Pos );
    float4 vp = mul(View,wp );
    output.pos = mul(Proj,vp);
      return output;
}

float4 PS(PS_IN input) : SV_Target
{
    return diffuseMap.Sample(textureSampler, float2(0, 0));
}