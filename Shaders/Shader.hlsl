float4 Color;
float4 VS(float4 position : POSITION) : SV_POSITION
{
    return position;
}
float4 PS(float4 position : SV_POSITION) : SV_TARGET
{
    return Color;
}