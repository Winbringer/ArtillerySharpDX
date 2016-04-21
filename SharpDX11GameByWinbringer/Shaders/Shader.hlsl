cbuffer data : register(b0)
{
    float Time;
    float4x4 WVP;
};
struct VS_IN
{
    float4 position : POSITION;
    float2 TextureUV : TEXCOORD0;
};

struct PS_IN
{
    float4 position : SV_Position;
    float2 TextureUV : TEXCOORD0;
    float height : FOG;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
    //Расчет высоты точки
    float height = (sin( input.position.z / 10)
    + sin(input.position.x / 10)) / 2;
    input.position.y = height;
    height = (height + (float) 1) / (float) 2;
    height = lerp(0.5, 1.1F, height);
    output.position = mul(input.position, WVP);
    output.height = height;
    output.TextureUV = input.TextureUV;
    return output;
}

Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);

float4 PS(PS_IN input) : SV_Target0
{
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    color = color * input.height;
    color.w = 1;
    return color;
}
