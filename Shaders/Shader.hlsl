Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0)
{
    Filter = MIN_MAG_MIP_POINT;
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
    float3 position : SV_Position0;
    float2 TextureUV : TEXCOORD0;
};

struct PS_IN
{
    float4 position : SV_Position0;
    float2 TextureUV : TEXCOORD0;
    float hight : FOG;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
    float4 pos = float4(input.position, (float) 1);
    float height = (sin(Time.x / 1000 + pos.z / 3) + sin(Time.x / 3000 + pos.x / 5)) / 2;
    pos.y = height * 5;
    output.hight = height;
    output.TextureUV = input.TextureUV;
    //Расчет позиции точки на экране
    pos = mul(World, pos);
    pos = mul(View, pos);
    pos = mul(Proj, pos);
    output.position = pos;
    return output;
}

float4 PS(PS_IN input) : SV_Target0
{
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    float shadou = (input.hight+(float)1.5)/2;
    return color * shadou;
}
