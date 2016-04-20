cbuffer data : register(b0)
{
    float4x4 WVP;
    float4x4 World;
    float4x4 WorldIT;
};

struct VS_IN
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float2 TextureUV : TEXCOORD;
 //   float4 Tangent : TANGENT;
  
};

struct PS_IN
{
    float4 position : SV_Position;   
    float2 TextureUV : TEXCOORD;
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
   // float4 WorldTangent : TANGENT;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
    output.position = mul(input.position, WVP); 
    output.WorldNormal = normalize(mul(input.normal, (float3x3) WorldIT));
    //output.WorldPosition = mul(input.position, World).xyz;
    output.TextureUV = input.TextureUV;
   // output.WorldTangent = float4(mul(input.Tangent.xyz, (float3x3) WorldIT), input.Tangent.w);
    return output;
}

Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);

float4 PS(PS_IN input) : SV_Target
{
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    float4 amb = color * 0.2f;   
    float4 diff = color * saturate(dot(normalize(input.WorldNormal), normalize(float3(-1, 1, -1)))) * 0.8f;
    return amb + diff;
    //float3 normal = normalize(input.WorldNormal);
    //float3 toLight = normalize(LightPosition - input.WorldPosition);

    //float3 Abm = color.rgb * 0.2f;
    //float3 Dif = color.rgb * 0.8f * saturate(dot(normal, toLight));
    //float3 c = Abm + Dif;

    //return float4(c,1.0f);
}
