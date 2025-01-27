﻿Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);

struct VS_INPUT
{
    float4 Pos : POSITION;
};

struct HS_INPUT
{
    float4 Pos : POSITION;
};

struct DS_INPUT
{
    float4 Pos : POSITION;
};

struct HS_CONSTANT_DATA
{
    float Edges[3] : SV_TessFactor;
    float Inside[1] : SV_InsideTessFactor;
};

struct GS_INPUT
{
    float4 Pos : SV_POSITION;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Nrm : NORMAL;
};

cbuffer data : register(b0)
{
    float4x4 world;
    float4x4 viewProj;
    float4 factor;
}

HS_INPUT VS(VS_INPUT input)
{
    HS_INPUT output = (HS_INPUT) 0;
    output.Pos = mul(input.Pos,world);
    return output;
}

float4 PS(PS_INPUT input) : SV_Target
{
    return float4(1, 0, 0, 1); //* dot(input.Nrm, normalize(float3(1, 1, 0)));
}


HS_CONSTANT_DATA SampleHSFunction(InputPatch<HS_INPUT, 3> ip, uint PatchID : SV_PrimitiveID)
{
    HS_CONSTANT_DATA Output;
    float tessFactor = factor.x;

    float TessAmount = factor.y;

    Output.Edges[0] = Output.Edges[1] = Output.Edges[2] = tessFactor;
    Output.Inside[0] = TessAmount;

    return Output;
}

[domain("tri")]
[partitioning("pow2")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(3)]
[patchconstantfunc("SampleHSFunction")]
DS_INPUT HS(InputPatch<HS_INPUT, 3> p,  uint i : SV_OutputControlPointID, uint PatchID : SV_PrimitiveID)
{


    DS_INPUT Output;
    Output.Pos = p[i].Pos;
    return Output;
}


[domain("tri")]
GS_INPUT DS(HS_CONSTANT_DATA input, float3 UV : SV_DomainLocation, const OutputPatch<DS_INPUT, 3> TrianglePatch)
{
    GS_INPUT outP;
    outP.Pos = UV.x * TrianglePatch[0].Pos + UV.y * TrianglePatch[1].Pos + UV.z * TrianglePatch[2].Pos;
    outP.Pos /= outP.Pos.w;
	
  //  outP.Pos.y = 2.0F * sin(outP.Pos.x / 4.0F) * cos(outP.Pos.z / 4.0F);
		
    outP.Pos = mul(outP.Pos,viewProj);
    return outP;
}

[maxvertexcount(72)]
void GS(triangle GS_INPUT input[3], inout TriangleStream<PS_INPUT> TriStream)
{
    float3 P1 = input[0].Pos.xyz;
    float3 P2 = input[1].Pos.xyz;
    float3 P3 = input[2].Pos.xyz;
	
    float3 Nrm = normalize(cross(P2 - P1, P3 - P1));
	
    PS_INPUT A;
    A.Pos = input[0].Pos;
    A.Nrm = Nrm;
	
    PS_INPUT B;
    B.Pos = input[1].Pos;
    B.Nrm = Nrm;
	
    PS_INPUT C;
    C.Pos = input[2].Pos;
    C.Nrm = Nrm;
	
    TriStream.Append(A);
    TriStream.Append(B);
    TriStream.Append(C);
    TriStream.RestartStrip();
}

