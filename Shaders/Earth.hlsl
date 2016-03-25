cbuffer data : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
    float4x4 WorldInverseTranspose;
};

cbuffer data1 : register(b1)
{
    float Ns_SpecularPower;
    float Ni_OpticalDensity;
    float d_Transparency;
    float Tr_Transparency;
    float3 Tf_TransmissionFilter;
    float4 Ka_AmbientColor;
    float4 Kd_DiffuseColor;
    float4 Ks_SpecularColor;
    float4 Ke_EmissiveColor;
};

cbuffer data2 : register(b2)
{
    float4 Color;
    float3 Direction;
    float3 CameraPosition;
};  

struct VS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float3 textureUV : TEXCOORD;
};

struct PS_IN
{
    float4 Position : SV_Position;    
    float3 TextureUV : TEXCOORD;
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
};

float3 SpecularBlinnPhong(float3 normal, float3 toLight, float3 toEye)
{ 
    float3 halfway = normalize(toLight + toEye);     
    float specularAmount = pow(saturate(dot(normal, halfway)), max(Ns_SpecularPower, 0.00001f));
    return Ks_SpecularColor.rgb * specularAmount;
}

float3 Lambert(float4 pixelDiffuse, float3 normal, float3 toLight)
{
    float3 diffuseAmount = Kd_DiffuseColor * saturate(dot(normal, toLight));
    return pixelDiffuse.rgb * diffuseAmount.r;
}

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;  
    
    output.Position = mul(input.position, WorldViewProjection);
    output.TextureUV = input.textureUV;
    output.WorldNormal = mul(input.normal, (float3x3) WorldInverseTranspose);
    output.WorldPosition = mul(input.position, World).xyz;

    return output;
}

Texture2D textureMap : register(t0);
Texture2D specularMap : register(t1);
SamplerState textureSampler : register(s0);

float4 PS(PS_IN input) : SV_Target
{
    float4 sample = textureMap.Sample(textureSampler, input.TextureUV);
    float4 specularColor = specularMap.Sample(textureSampler, input.TextureUV);

    float3 normal = normalize(input.WorldNormal);
    float3 toLight = normalize(-Direction);
    float3 toEye = normalize(CameraPosition - input.WorldPosition);

    float3 H = normalize(toLight + toEye);
    float D = saturate(dot(normal, H));
    float3 emissive = Ke_EmissiveColor.rgb;
    float3 ambient = sample * Ka_AmbientColor.r;
    float3 diffuse = sample * Kd_DiffuseColor.r * saturate(dot(normal, toLight));  
    float3 specular = specularColor * (Ks_SpecularColor.r * D / (Ns_SpecularPower - D * Ns_SpecularPower + D));

    float3 color = ambient + diffuse + specular+emissive;
    float alpha =sample.a;

    return float4(color, alpha);
}