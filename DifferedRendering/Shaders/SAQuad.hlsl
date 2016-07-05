struct VertexIn
{
    float4 Position : SV_Position;
};

struct PixelIn
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
};

PixelIn VSMain(VertexIn vertex)
{
    PixelIn result = (PixelIn) 0;
    result.Position = vertex.Position;
    result.Position.w = 1.0f;
    result.UV.x = result.Position.x * 0.5 + 0.5;
    result.UV.y = result.Position.y * -0.5 + 0.5;
    return result;
}