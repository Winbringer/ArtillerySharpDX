struct Particle // описание структуры на GPU
{
    float3 Position;
    float3 Velocity;
};

StructuredBuffer<Particle> Particles : register(t0); // буфер частиц
Texture2D ParticleTexture : register(t1);
SamplerState ParticleSampler : register(s0);
 float Size; // размер конченого квадрата 
cbuffer Params : register(b0) // матрицы вида и проекции
{
    float4x4 World;
    float4x4 View;
    float4x4 Projection;
};

// т.к. вертексов у нас нет, мы можем получить текущий ID вертекса при рисовании без использования Vertex Buffer
struct VertexInput
{
    uint VertexID : SV_VertexID;
};

struct PixelInput // описывает вертекс на выходе из Vertex Shader
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD;
};

struct PixelOutput // цвет результирующего пикселя
{
    float4 Color : SV_TARGET0;
};

PixelInput VS(VertexInput input)
{
    PixelInput output = (PixelInput) 0;

    Particle particle = Particles[input.VertexID];

    float4 worldPosition = mul(float4(particle.Position, 1), World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = viewPosition;
    output.UV = 0;

    return output;
}

PixelOutput PS(PixelInput input)
{
    PixelOutput output = (PixelOutput) 0;
    output.Color = ParticleTexture.Sample(ParticleSampler, input.UV);
    output.Color.b = 0;
    return output;
}

// функция изменения вертекса и последующая проекция его в Projection Space
PixelInput _offsetNprojected(PixelInput data, float2 offset, float2 uv)
{
    data.Position.xy += offset;
    data.Position = mul(data.Position, Projection);
    data.UV = uv;

    return data;
}

[maxvertexcount(4)] // результат работы GS – 4 вертекса, которые образуют TriangleStrip
void TriangleGS(point PixelInput input[1], inout TriangleStream<PixelInput> stream)
{
    PixelInput pointOut = input[0];
  
	// описание квадрата
    stream.Append(_offsetNprojected(pointOut, float2(-1, -1) * Size, float2(0, 0)));
    stream.Append(_offsetNprojected(pointOut, float2(-1, 1) * Size, float2(0, 1)));
    stream.Append(_offsetNprojected(pointOut, float2(1, -1) * Size, float2(1, 0)));
    stream.Append(_offsetNprojected(pointOut, float2(1, 1) * Size, float2(1, 1)));

	// создать TriangleStrip
    stream.RestartStrip();
}

technique11 Render
{
    pass P0
    {
        SetGeometryShader(CompileShader(gs_5_0, TriangleGS()));
        SetComputeShader(0);
        SetHullShader(0);
        SetDomainShader(0);
        SetVertexShader(CompileShader(vs_5_0, VS()));
        SetPixelShader(CompileShader(ps_5_0, PS()));
    }
}