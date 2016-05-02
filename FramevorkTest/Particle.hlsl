
// Particle system constants 
cbuffer ParticleConstants : register(b0)
{
    float3 DomainBoundsMin;
    float ForceStrength;
    float3 DomainBoundsMax;
    float MaxLifetime;
    float3 ForceDirection;
    uint MaxParticles;
    float3 Attractor;
    float Radius;
}; 
// Particles per frame constant buffer 
cbuffer ParticleFrame : register(b1)
{
    float Time;
    float FrameTime;
    uint RandomSeed;
    uint ParticleCount; // consume buffer count 
}
// Represents a single particle
struct Particle
{
    float3 Position;
    float Radius;
    float3 OldPosition;
    float Energy;
};
 // Pixel shader input 
struct PS_Input
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
    float Energy : ENERGY;
};

// Access to the particle buffer
StructuredBuffer<Particle> particles : register(t0);
  // Append and consume buffers for particles 
AppendStructuredBuffer<Particle> NewState : register(u0);
ConsumeStructuredBuffer<Particle> CurrentState : register(u1);

// Computes the vertex position
float4 ComputePosition(in float3 pos, in float size, in float2 vPos)
{
        // Create billboard (quad always facing the camera)   
    float3 toEye = normalize(CameraPosition.xyz - pos);
    float3 up = float3(0.0f, 1.0f, 0.0f);
    float3 right = cross(toEye, up);
    up = cross(toEye, right);
    pos += (right * size * vPos.x) + (up * size * vPos.y);
    return mul(float4(pos, 1), WorldViewProjection);
}

PS_Input VSMain(in uint vertexID : SV_VertexID, in uint instanceID : SV_InstanceID)
{
    PS_Input result = (PS_Input) 0;
    // Load particle using instance Id 
    Particle p = particles[instanceID];
     // 0-1 Vertex strip layout    //  /      // 2-3   
    result.UV = float2(vertexID & 1, (vertexID & 2) >> 1);
    result.Position = ComputePosition(p.Position, p.Radius, result.UV * float2(2, -2) + float2(-1, 1));
    result.Energy = p.Energy;
    return result;
}

float4 PSMain(PS_Input pixel) : SV_Target
{
    float4 result = ParticleTexture.Sample(linearSampler, pixel.TextureUV);
     // Fade-out as approaching the near clip plane  
      // and as a particle loses energy between 1->0    
    return float4(result.xyz, saturate(pixel.Energy) * result.w * pixel.Position.z * pixel.Position.z);
}

  
// Apply ForceDirection with ForceStrength to particle
void ApplyForces(inout Particle particle)
{
  // Forces  
    float3 force = (float3) 0;
    // Directional force 
    force += normalize(ForceDirection) * ForceStrength;
       // Damping   
    float windResist = 0.9;
    force *= windResist;
    particle.OldPosition = particle.Position;
       // Integration step   
    particle.Position += force * FrameTime;
}
 // Random Number Generator methods
uint rand_lcg(inout uint rng_state)
{
    // Linear congruential generator  
    rng_state = 1664525 * rng_state + 1013904223;
    return rng_state;
}
uint wang_hash(uint seed)
{
    // Initialize a random seed  
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

[numthreads(THREADSX, 1, 1)]
void Generator(uint groupIndex : SV_GroupIndex,
 uint3 groupId : SV_GroupID,
uint3 groupThreadId : SV_GroupThreadID,
uint3 threadId : SV_DispatchThreadID)
{
    uint indx = threadId.x + threadId.y * THREADSX;
    Particle p = (Particle) 0;
    // Initialize random seed
    uint rng_state = wang_hash(RandomSeed + indx);
       // Random float between [0, 1]  
    float f0 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
      
    float f1 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
    float f2 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
    // Set properties of new particle
    p.Radius = Radius;
    p.Position.x = DomainBoundsMin.x + f0 * ((DomainBoundsMax.x - DomainBoundsMin.x) + 1);
    p.Position.z = DomainBoundsMin.z + f1 * ((DomainBoundsMax.z - DomainBoundsMin.z) + 1);
    p.Position.y = (DomainBoundsMax.y - 6) + f2 * ((DomainBoundsMax.y - (DomainBoundsMax.y - 6)) + 1);
    p.OldPosition = p.Position;
    p.Energy = MaxLifetime;
  // Append the new particle to the output buffer
    NewState.Append(p);
}
[numthreads(THREADSX, THREADSY, 1)]
  void Snowfall(uint groupIndex : SV_GroupIndex,
 uint3 groupId : SV_GroupID,
uint3 groupThreadId : SV_GroupThreadID,
 uint3 threadId : SV_DispatchThreadID)
{
    uint indx = threadId.x + threadId.y * THREADSX;
     // Skip out of bounds threads 
    if (indx >= ParticleCount)
        return;
     // Load/Consume particle 
    Particle p = CurrentState.Consume();
    ApplyForces(p);
    // Ensure the particle does not fall endlessly  
    p.Position.y = max(p.Position.y, DomainBoundsMin.y);
    // Count down time to live    p.Energy -= FrameTime;
    // If no longer falling only let sit for a second
           
    if (p.Position.y == p.OldPosition.y && p.Energy > 1.0f)
        p.Energy = 1.0f;
    if (p.Energy > 0)
    { // If particle is alive add back to append buffer  
        NewState.Append(p);
    }
}