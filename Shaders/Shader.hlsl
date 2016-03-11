﻿Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s1)
{
    Textur = <textureMap>;
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
    AddresW = Wrap;
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
    float height : FOG;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
   
    float4 pos = float4(input.position, (float) 1);
    //Расчет высоты точки
    float height = (sin(Time.x / 1000 + pos.z / (float) 10) + sin(Time.x / 1000 + pos.x / (float) 10)) / 2;
    pos.y = height * 5;
    //Расчет позиции точки на экране
    float4 posW = mul(pos,World);
    float4 posV = mul(posW,View);
    float4 posP = mul(posV,Proj);
    //Установка выходных значений
    height = (height + (float) 1) / (float) 2;
    output.height = lerp(0.7, 1.1F, height);
    output.position = posP;   
    output.TextureUV = input.TextureUV;
    return output;
}

float4 PS(PS_IN input) : SV_Target0
{
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    return color * input.height;
}
