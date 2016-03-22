cbuffer PerObject : register(b0)
{ 
    float4x4 WorldViewProjection;
    float4x4 World;
    float4x4 WorldInverseTranspose;
}; 

struct DirectionalLight
{
    float4 Color;
    float3 Direction;
};

cbuffer PerFrame : register(b1)
{
    DirectionalLight Light;
    float3 CameraPosition;
}; 

cbuffer PerMaterial : register(b2)
{
    float4 MaterialAmbient;
    float4 MaterialDiffuse;
    float4 MaterialSpecular;
    float MaterialSpecularPower;
    bool HasTexture;
    float4 MaterialEmissive;
    float4x4 UVTransform;
};

struct VertexShaderInput
{
    float4 Position : SV_Position;
    float3 Normal : NORMAL;       
    float4 Color : COLOR0;        
    float2 TextureUV : TEXCOORD; 
};

struct PixelShaderInput
{
    float4 Position : SV_Position;
    float4 Diffuse : COLOR;
    float2 TextureUV : TEXCOORD;
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
};

PixelShaderInput VS(VertexShaderInput vertex)
{
    PixelShaderInput result = (PixelShaderInput) 0;

    result.Diffuse = vertex.Color * MaterialDiffuse;    
    result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2) UVTransform).xy;    
    result.Position = mul(vertex.Position, WorldViewProjection);   
    result.TextureUV = vertex.TextureUV;     
    result.WorldNormal = mul(vertex.Normal, (float3x3) WorldInverseTranspose);   
    result.WorldPosition = mul(vertex.Position, World).xyz;

    return result;
}

float4 PS(PixelShaderInput pixel) : SV_Target
{
    return pixel.Diffuse;
}

