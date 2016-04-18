Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);


cbuffer PerObject : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
    float4x4 WorldInverseTranspose;
    float4x4 ViewProjection;
    float TessellationFactor;
};

struct HS_PNTrianglePatchConstant
{
    float EdgeTessFactor[3] : SV_TessFactor;
    float InsideTessFactor : SV_InsideTessFactor;

    float3 B210 : POSITION3;
    float3 B120 : POSITION4;
    float3 B021 : POSITION5;
    float3 B012 : POSITION6;
    float3 B102 : POSITION7;
    float3 B201 : POSITION8;
    float3 B111 : CENTER;
    
    float3 N200 : NORMAL0;
    float3 N020 : NORMAL1;
    float3 N002 : NORMAL2;

    float3 N110 : NORMAL3;
    float3 N011 : NORMAL4;
    float3 N101 : NORMAL5;
};

float2 BarycentricInterpolate(float2 v0, float2 v1, float2 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float2 BarycentricInterpolate(float2 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

float3 BarycentricInterpolate(float3 v0, float3 v1, float3 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float3 BarycentricInterpolate(float3 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

float4 BarycentricInterpolate(float4 v0, float4 v1, float4 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float4 BarycentricInterpolate(float4 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

float3 ProjectOntoPlane(float3 planeNormal, float3 planePoint, float3 pointToProject)
{
    return pointToProject - dot(pointToProject - planePoint, normalize( planeNormal)) * normalize(planeNormal);
}


struct HS_TrianglePatchConstant
{
    float EdgeTessFactor[3] : SV_TessFactor;
    float InsideTessFactor : SV_InsideTessFactor;
    
    float2 TextureUV[3] : TEXCOORD0;
    float3 WorldNormal[3] : NORMAL3;
};
struct DS_ControlPointInput
{
    float3 Position : BEZIERPOS;
    float4 Diffuse : COLOR0;
};
struct VS_IN
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TextureUV : TEXCOORD;
  
};
struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Nrm : NORMAL;
    float2 TextureUV : TEXCOORD;
};
struct GS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 TextureUV : TEXCOORD;
};

struct HullShaderInput
{
    float3 WorldPosition : POSITION;
    float2 TextureUV : TEXCOORD0;
    float3 WorldNormal : NORMAL;
};

struct DS_PNControlPointInput
{
    float3 Position : POSITION;
    float3 WorldNormal : NORMAL;
    float2 TextureUV : TEXCOORD;
};

HullShaderInput VS(VS_IN vertex)
{
    HullShaderInput result = (HullShaderInput) 0;

    result.TextureUV = vertex.TextureUV;
    result.WorldNormal = mul(vertex.Normal, (float3x3) WorldInverseTranspose);
    result.WorldPosition = mul(vertex.Position, World).xyz;

    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("integer")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_PNTrianglesConstant")] // The constant hull shader function
DS_PNControlPointInput HS_PNTrianglesInteger(InputPatch<HullShaderInput, 3> patch,
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID)
{
    DS_PNControlPointInput result = (DS_PNControlPointInput) 0;
    result.Position = patch[id].WorldPosition;
    result.WorldNormal = patch[id].WorldNormal;
    result.TextureUV = patch[id].TextureUV;
    return result;
}

[domain("tri")]
PS_INPUT DS_PhongTessellation(HS_TrianglePatchConstant constantData, const OutputPatch<DS_PNControlPointInput, 3> patch, float3 barycentricCoords : SV_DomainLocation)
{
    PS_INPUT result = (PS_INPUT) 0;
    float3 position = BarycentricInterpolate(patch[0].Position, patch[1].Position, patch[2].Position, barycentricCoords);
    float2 UV = BarycentricInterpolate(constantData.TextureUV, barycentricCoords);
    float3 normal = BarycentricInterpolate(constantData.WorldNormal, barycentricCoords);

    // BEGIN Phong Tessellation
    float3 posProjectedU = ProjectOntoPlane(constantData.WorldNormal[0], patch[0].Position, position); 
    float3 posProjectedV = ProjectOntoPlane(constantData.WorldNormal[1], patch[1].Position, position);
    float3 posProjectedW = ProjectOntoPlane(constantData.WorldNormal[2], patch[2].Position, position);
    position = BarycentricInterpolate(posProjectedU, posProjectedV, posProjectedW, barycentricCoords);
    // END Phong Tessellation

    result.Pos = mul(float4(position, 1.0f),ViewProjection);
    result.TextureUV = UV;
    result.Nrm = normal;
    return result;
}


float4 PS(PS_INPUT input) : SV_Target
{
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    float4 amb = color * 0.2f;
    float4 diff = color * saturate(dot(normalize(input.Nrm), normalize(float3(-1, 1, -1)))) * 0.8f;
    return amb + diff;
  //  return textureMap.Sample(textureSampler, input.TextureUV) * saturate(dot(normalize(input.Nrm), normalize(float3(-1,1,-1)))); //* dot(input.Nrm, normalize(float3(1, 1, 0)));
}

HS_PNTrianglePatchConstant HS_PNTrianglesConstant(InputPatch<HullShaderInput, 3> patch)
{
    HS_PNTrianglePatchConstant result = (HS_PNTrianglePatchConstant) 0;

  

    float3 roundedEdgeTessFactor;
    float roundedInsideTessFactor, insideTessFactor;
    ProcessTriTessFactorsMax((float3) TessellationFactor, 1.0, roundedEdgeTessFactor, roundedInsideTessFactor, insideTessFactor);

    // Apply the edge and inside tessellation factors
    result.EdgeTessFactor[0] = TessellationFactor;// roundedEdgeTessFactor.x;
                                                 ;
    result.EdgeTessFactor[1] = TessellationFactor;//roundedEdgeTessFactor.y;
                                                 ;
    result.EdgeTessFactor[2] = TessellationFactor;// roundedEdgeTessFactor.z;
                                                 ;
    result.InsideTessFactor = TessellationFactor; // roundedInsideTessFactor;
    

    //************************************************************
    // Calculate PN-Triangle coefficients
    // Refer to Vlachos 2001 for the original formula
    float3 p1 = patch[0].WorldPosition;
    float3 p2 = patch[1].WorldPosition;
    float3 p3 = patch[2].WorldPosition;

    //B300 = p1;
    //B030 = p2;
    //float3 b003 = p3;
    
    float3 n1 = patch[0].WorldNormal;
    float3 n2 = patch[1].WorldNormal;
    float3 n3 = patch[2].WorldNormal;
    
    //N200 = n1;
    //N020 = n2;
    //N002 = n3;

    // Calculate control points
    float w12 = dot((p2 - p1), n1);
    result.B210 = (2.0f * p1 + p2 - w12 * n1) / 3.0f;

    float w21 = dot((p1 - p2), n2);
    result.B120 = (2.0f * p2 + p1 - w21 * n2) / 3.0f;

    float w23 = dot((p3 - p2), n2);
    result.B021 = (2.0f * p2 + p3 - w23 * n2) / 3.0f;
    
    float w32 = dot((p2 - p3), n3);
    result.B012 = (2.0f * p3 + p2 - w32 * n3) / 3.0f;

    float w31 = dot((p1 - p3), n3);
    result.B102 = (2.0f * p3 + p1 - w31 * n3) / 3.0f;
    
    float w13 = dot((p3 - p1), n1);
    result.B201 = (2.0f * p1 + p3 - w13 * n1) / 3.0f;
    
    float3 e = (result.B210 + result.B120 + result.B021 +
                result.B012 + result.B102 + result.B201) / 6.0f;
    float3 v = (p1 + p2 + p3) / 3.0f;
    result.B111 = e + ((e - v) / 2.0f);
    
    // Calculate normals
    float v12 = 2.0f * dot((p2 - p1), (n1 + n2)) /
                          dot((p2 - p1), (p2 - p1));
    result.N110 = normalize((n1 + n2 - v12 * (p2 - p1)));

    float v23 = 2.0f * dot((p3 - p2), (n2 + n3)) /
                          dot((p3 - p2), (p3 - p2));
    result.N011 = normalize((n2 + n3 - v23 * (p3 - p2)));

    float v31 = 2.0f * dot((p1 - p3), (n3 + n1)) /
                          dot((p1 - p3), (p1 - p3));
    result.N101 = normalize((n3 + n1 - v31 * (p1 - p3)));

    return result;
}

