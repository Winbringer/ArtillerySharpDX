Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);
TextureCube Reflection : register(t1);

cbuffer data : register(b0)
{
    float4x4 WVP;
    uint hasBones;
    uint hasDif;
    float4x4 world;
    float4 dif;
    bool IsReflective;
    float ReflectionAmount;
    float3 CameraPosition;    
};

cbuffer data2 : register(b1)
{
    float4x4 Bones[1024];
}

cbuffer cube : register(b2)
{
    float4x4 CubeFaceViewProj[6];
}

static const float3 ToL = normalize(float3(-1, 1, -1));

struct VS_IN
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD;
    float3 tangent : TANGENT;
    float3 biTangent : BINORMAL;
    float4 boneID : BLENDINDICES;
    float4 wheights : BLENDWEIGHT;
};

struct GS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD;
    float3 tangent : TANGENT;
    float3 biTangent : BINORMAL;
    float3 WorldPosition : WORLDPOS;
};

struct PS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD;
    float3 tangent : TANGENT;
    float3 biTangent : BINORMAL;
    float3 WorldPosition : WORLDPOS;
    // Allows us to write to multiple render targets
    uint RTIndex : SV_RenderTargetArrayIndex;
};

void SkinVertex(float4 weights, float4 bones, inout float4 position, inout float3 normal, inout float3 tangent, inout float3 biTangent)
{
    float4x4 skinTransform = Bones[bones.x] * weights.x +
     Bones[bones.y] * weights.y +
     Bones[bones.z] * weights.z +
     Bones[bones.w] * weights.w;
           
    position = mul(position, skinTransform);
    normal = mul(normal, (float3x3) skinTransform);
    tangent = mul(tangent, (float3x3) skinTransform);
    biTangent = mul(biTangent, (float3x3) skinTransform);
    
}

GS_IN VS(VS_IN input)
{
    GS_IN vertex = (GS_IN) 0;
    float4 position = input.position;
    if (hasBones)
        SkinVertex(input.wheights, input.boneID, position, input.normal, input.tangent, input.biTangent);
    vertex.position = mul(position, world);
    vertex.normal = mul(input.normal, (float3x3) world);
    vertex.tangent = mul(input.tangent, (float3x3) world);
    vertex.biTangent = mul(input.biTangent, (float3x3) world);
    vertex.uv = input.uv;
    vertex.WorldPosition = vertex.position.xyz;
    return vertex;
}

float4 PS(PS_IN input) : SV_Target0
{
    float4 color = dif;
    float3 normal = normalize(input.normal);
    float3 toEye = normalize(CameraPosition - input.WorldPosition);
    if (hasDif)
        color = textureMap.Sample(textureSampler, input.uv);
    float3 amb = color.rgb * 0.3;
    float3 dif = color.rgb * saturate(dot(normal, ToL)) * 0.8;
    float3 colorAD = amb + dif;

    if (IsReflective)
    {
        float3 reflection = reflect(-toEye, normal);
        colorAD = lerp(colorAD, Reflection.Sample(textureSampler, reflection).rgb, ReflectionAmount);
    }

    return float4(colorAD.x, colorAD.y, colorAD.z, color.w);
}

[maxvertexcount(3)] // Outgoing vertex count (1 triangle)
[instance(6)] // Number of times to execute for each input
void GS(triangle GS_IN input[3], uint instanceId : SV_GSInstanceID, inout TriangleStream<PS_IN> stream)
{
    // Output the input triangle using the  View/Projection 
    // of the cube face identified by instanceId
    float4x4 viewProj = CubeFaceViewProj[instanceId];
    PS_IN output;

    // Assign the render target instance
    // i.e. 0 = +X face, 1 = -X face and so on
    output.RTIndex = instanceId;
    
    // In order to render correctly into a TextureCube we
    // must either:
    // 1) using a left-handed view/projection; OR
    // 2) using a right-handed view/projection with -1 X-
    //    axis scale
    // Our meshes assume a right-handed coordinate system
    // therefore both cases above require vertex winding
    // to be switched.
    uint3 indx = uint3(0, 2, 1);
    [unroll]
    for (int v = 0; v < 3; v++)
    {
        // Apply cube face view/projection
        output.position = mul(input[indx[v]].position, viewProj);
        // Copy other vertex properties as is
        output.WorldPosition = input[indx[v]].WorldPosition;
        output.normal = input[indx[v]].normal;
        output.uv = input[indx[v]].uv;
        output.tangent = input[indx[v]].tangent;
        output.biTangent = input[indx[v]].biTangent;

        // Append to the stream
        stream.Append(output);
    }
    stream.RestartStrip();
}