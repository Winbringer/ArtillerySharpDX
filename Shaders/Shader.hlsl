Texture2D textureMap : register(t0);
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
    float4 Time;
};
struct VS_IN
{
    float3 position : POSITION0;
};

struct PS_IN
{
    float4 position : SV_Position0;
    float2 color : TEXCOORD0;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
    float4 pos = float4(input.position, (float) 1);
    float height = (sin(pos.z) + sin(pos.x)) / 2; //Time.x / 1000 +
    pos.y = height*2;
    pos = mul(World, pos);
    pos = mul(View, pos);
    pos = mul(Proj, pos);
    output.position = pos;
    output.color = float2(height, (float)1);
    return output;
}

float4 PS(PS_IN input) : SV_Target0
{
    return float4(input.color.x+0.5F,0,0,1); //float4(1, 0, 0, 1);
   // return textureMap.Sample(textureSampler, input.texcoord);
}
