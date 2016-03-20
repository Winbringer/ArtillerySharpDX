cbuffer PerObject : register(b0)
{ // WorldViewProjection matrix  
    float4x4 WorldViewProjection;
     // We need the world matrix so that we can   
     // calculate the lighting in world space  
    float4x4 World;
      // Inverse transpose of world, used for  
      // bringing normals into world space, especially  
      // necessary where non-uniform scaling has been applied   
    float4x4 WorldInverseTranspose;
}; 

cbuffer PerFrame : register(b1)
{
    float3 CameraPosition;
}; 

struct VertexShaderInput
{
    float4 Position : SV_Position; // Position  
    float3 Normal : NORMAL; // Normal - for lighting 
    float4 Color : COLOR0; // Vertex color  
    float2 TextureUV : TEXCOORD; // Texture UV coordinate 
};

struct PixelShaderInput
{
    float4 Position : SV_Position;
    // Interpolation of vertex * material diffuse   
    float4 Diffuse : COLOR;
      // Interpolation of vertex UV texture coordinate   
    float2 TextureUV : TEXCOORD;
    // We need the World Position and normal for lighting 
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
};

PixelShaderInput VS(VertexShaderInput vertex)
{
    PixelShaderInput result = (PixelShaderInput) 0;
    // Apply WVP matrix transformation 
    result.Position = mul(vertex.Position, WorldViewProjection);
    result.Diffuse = vertex.Color;
    result.TextureUV = vertex.TextureUV;
    // transform normal to world space   
    result.WorldNormal = mul(vertex.Normal, (float3x3) WorldInverseTranspose);
    // transform input position to world   
    result.WorldPosition = mul(vertex.Position, World).xyz;
    return result;
}

float4 PS(PixelShaderInput pixel) : SV_Target
{
    return pixel.Diffuse;
}

