cbuffer data : register(b0)
{
    float4x4 WVP;
    uint hasBones;   
};

cbuffer data2 : register(b1)
{
    float4x4 Bones[1024];
}
static const float3 ToL = float3(-1, 1, -1);

struct VS_IN
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD;
    float3 tangent : TANGENT;
    float3 biTangent : BINORMAL;
    uint4 boneID : BLENDINDICES;
    float4 wheights : BLENDWEIGHT;
};

struct PS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
};

void SkinVertex(float4 weights, uint4 bones, inout float4 position, inout float3 normal, inout float3 tangent, inout float3 biTangent)
{
    float4x4 skinTransform = Bones[bones.x] * weights.x + Bones[bones.y] * weights.y + Bones[bones.z] * weights.z + Bones[bones.w] * weights.w;
           
    position = mul(position, skinTransform);
    normal = mul(normal, (float3x3) skinTransform);
    tangent = mul(tangent, (float3x3) skinTransform);
    biTangent = mul(biTangent, (float3x3) skinTransform);
    
}

PS_IN VS(VS_IN input)
{
    PS_IN vertex = (PS_IN) 0;
    float4 position = input.position;
  if(hasBones)  SkinVertex(input.wheights, input.boneID, position, input.normal, input.tangent, input.biTangent);
    vertex.position = mul(position, WVP);
    vertex.normal = mul(input.normal, (float3x3) WVP);
    return vertex;
}

float4 PS(PS_IN input) : SV_Target0
{
    float4 color = float4(1, 1, 1, 1);
    float3 amb = color.rgb * 0.2;
    float3 dif = color.rgb * saturate(dot(input.normal, ToL)) * 0.8;
    return float4(dif+amb,color.w);
}