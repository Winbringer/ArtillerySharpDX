using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using VictoremLibrary;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Linq;
using SharpDX.Mathematics.Interop;

namespace FramevorkTest
{

    // Structure for particle 
    public struct Particle
    {
        public Vector3 Position;
        public float Radius;
        public Vector3 OldPosition;
        public float Energy;
    }
    // Particle constants (updated on initialization) 
    public struct ParticleConstants
    {
        public Vector3 DomainBoundsMin;
        public float ForceStrength;
        public Vector3 DomainBoundsMax;
        public float MaxLifetime;
        public Vector3 ForceDirection;
        public int MaxParticles;
        public Vector3 Attractor;
        public float Radius;
    }
    // particle constant buffer updated per frame 
    public struct ParticleFrame
    {
        public float Time; public float FrameTime; public uint RandomSeed;
        // use CopyStructureCount for last component   
        uint _padding0;
    }

    class ParticleRenderer
    {
        private BlendState blendState;
        private BlendState blendStateLight;
        private DepthStencilState disableDepthWrite;
        private ShaderResourceView particleTextureSRV;
        private Buffer perComputeBuffer;
        private Buffer perFrame;
        // Private member fields 
        Buffer indirectArgsBuffer;
        List<Buffer> particleBuffers = new List<Buffer>();
        List<ShaderResourceView> particleSRVs = new List<ShaderResourceView>();
        List<UnorderedAccessView> particleUAVs = new List<UnorderedAccessView>();
        public int ParticlesPerBatch = 16;
        float limiter = 0f;
        Random random = new Random();

        public ParticleConstants Constants;
        public ParticleFrame Frame;
        private Device device;
        float genTime = 0f;

        public ParticleRenderer(Game game)
        {
            device = game.DeviceContext.Device;
            context = device.ImmediateContext;
            #region Blend States
            var blendDesc = new BlendStateDescription()
            {
                IndependentBlendEnable = false,
                AlphaToCoverageEnable = false,
            };
            // Additive blend state that darkens when overlapped
            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                BlendOperation = BlendOperation.Add,
                AlphaBlendOperation = BlendOperation.Add,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };
            blendState = new BlendState(device, blendDesc);
            // Additive blend state that lightens when overlapped
            // (needs a dark background) 
            blendDesc.RenderTarget[0].DestinationBlend = BlendOption.One;
            blendStateLight = new BlendState(device, blendDesc);
            #endregion

            // depth stencil state to disable Z-buffer write
            disableDepthWrite = new DepthStencilState(device,
                new DepthStencilStateDescription
                {
                    DepthComparison = Comparison.Less,
                    DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.Zero,
                    IsDepthEnabled = true,
                    IsStencilEnabled = false
                });

            // Create the per compute shader constant buffer
            perComputeBuffer = new Buffer(device,
                Utilities.SizeOf<ParticleConstants>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);
            // Create the particle frame buffer
            perFrame = new Buffer(device,
                Utilities.SizeOf<ParticleFrame>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);
            particleTextureSRV = StaticMetods.LoadTextureFromFile(device.ImmediateContext, "Particle.png");
        }

        public void InitializeParticles(int maxParticles, float maxLifetime)
        {
            this.Constants.MaxParticles = maxParticles;
            this.Constants.MaxLifetime = maxLifetime;
            // How often and how many particles to generate  
            this.ParticlesPerBatch = (int)(maxParticles * 0.0128f);
            this.limiter = (float)(Math.Ceiling(ParticlesPerBatch / 16.0) * 16.0 * maxLifetime) / (float)maxParticles;

            #region Create Buffers and Views 

            // Create 2 buffers, these are our append/consume 
            // buffers and will be swapped each frame
            particleBuffers.Add(new Buffer(device,
                Utilities.SizeOf<Particle>() * maxParticles, ResourceUsage.Default,
                BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags.None,
                ResourceOptionFlags.BufferStructured,
                Utilities.SizeOf<Particle>()));
            particleBuffers.Add(new Buffer(device,
                Utilities.SizeOf<Particle>() * maxParticles, ResourceUsage.Default,
                BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags.None,
                ResourceOptionFlags.BufferStructured,
                Utilities.SizeOf<Particle>()));
            // Create a UAV and SRV for each particle buffer
            particleUAVs.Add(StaticMetods.CreateBufferUAV(device,
                particleBuffers[0],
                UnorderedAccessViewBufferFlags.Append));
            particleUAVs.Add(StaticMetods.CreateBufferUAV(device,
                particleBuffers[1],
                UnorderedAccessViewBufferFlags.Append));
            particleSRVs.Add(new ShaderResourceView(device, particleBuffers[0]));
            particleSRVs.Add(new ShaderResourceView(device, particleBuffers[1]));
            // Set the starting number of particles to 0
            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, particleUAVs[0], 0);
            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(1, particleUAVs[1], 0);
            // Create particle count buffers: 
            var bufDesc = new BufferDescription
            {
                BindFlags = SharpDX.Direct3D11.BindFlags.ConstantBuffer,
                SizeInBytes = 4 * SharpDX.Utilities.SizeOf<uint>(),
                StructureByteStride = 0,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
            };
            // Used as input to the context.DrawInstancedIndirect 
            // The 4 elements represent the 4 parameters 
            bufDesc.OptionFlags = ResourceOptionFlags.DrawIndirectArguments;
            bufDesc.BindFlags = BindFlags.None;
            indirectArgsBuffer = new Buffer(device, bufDesc); // 4 vertices per instance (i.e. quad) device.ImmediateContext.UpdateSubresource(new uint[4] { 4,      0, 0, 0 }, particleCountIABuffer); 
            #endregion
            // Update the ParticleConstants buffer 
            device.ImmediateContext.UpdateSubresource(ref Constants, perComputeBuffer);
        }


        // time since Generator last run 
        public void Update(string generatorCS, string updaterCS)
        {
            var append = particleUAVs[0];
            var consume = particleUAVs[1];
            // Assign UAV of particles   
            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, append);
            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(1, consume);
            // Update the constant buffers  
            // Generate the next random seed for particle generator 
            Frame.RandomSeed = (uint)random.Next(int.MinValue, int.MaxValue);
            device.ImmediateContext.UpdateSubresource(ref Frame, perFrame);
            // Copy current consume buffer count into perFrame 
            device.ImmediateContext.CopyStructureCount(perFrame, 4 * 3, consume);
            device.ImmediateContext.ComputeShader.SetConstantBuffer(0, perComputeBuffer);
            device.ImmediateContext.ComputeShader.SetConstantBuffer(1, perFrame);

            // Update existing particles   
            UpdateCS(updaterCS, append, consume);
            // Generate new particles (if reached limiter time)   
            genTime += Frame.FrameTime;
            if (genTime > limiter)
            {
                genTime = 0;
                GenerateCS(generatorCS, append);
            }
            // Retrieve the particle count for the render phase 
            device.ImmediateContext.CopyStructureCount(indirectArgsBuffer, 4, append);
            // Clear the shader and resources from pipeline stage 
            device.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, null, null);
            device.ImmediateContext.ComputeShader.SetUnorderedAccessViews(1, null, null, null);
            device.ImmediateContext.ComputeShader.Set(null);
            // Flip UAVs/SRVs  
            particleUAVs[0] = consume;
            particleUAVs[1] = append;
            var s = particleSRVs[0];
            particleSRVs[0] = particleSRVs[1];
            particleSRVs[1] = s;
        }

        private void UpdateCS(string csName, UnorderedAccessView append, UnorderedAccessView consume)
        {
            // Compile the shader if it isn't already    
            if (!computeShaders.ContainsKey(csName)) CompileComputeShader(csName);
            // Set the shader to run 
            context.ComputeShader.Set(computeShaders[csName]);
            // Dispatch the compute shader thread groups  
            context.Dispatch((int)Math.Ceiling(Constants.MaxParticles / (double)ThreadsX), 1, 1);
        }

        public const int GeneratorThreadsX = 16;
        private DeviceContext context;

        private void GenerateCS(string name, UnorderedAccessView append)
        {
            // Compile the shader if it isn't already   
            if (!computeShaders.ContainsKey(name))
            {
                int oldThreadsX = ThreadsX;
                ThreadsX = GeneratorThreadsX;
                CompileComputeShader(name);
                ThreadsX = oldThreadsX;
            }
            // Set the shader to run   
            context.ComputeShader.Set(computeShaders[name]);
            // Dispatch the compute shader thread groups   
            context.Dispatch((int)Math.Ceiling(ParticlesPerBatch / 16.0), 1, 1);
        }

        public Dictionary<String, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();
        public int ThreadsX = 128; // default thread group width
        public int ThreadsY = 1;   // default thread group height 
        private SamplerState linearSampler;
        private PixelShader pixelShader;
        private VertexShader vertexShader;

        public bool UseLightenBlend { get; private set; }

        // Compile compute shader from file
        public void CompileComputeShader(string csFunction,
            string csFile = @"Shaders\ParticleCS.hlsl")
        {
            SharpDX.Direct3D.ShaderMacro[] defines = new[] {
                new SharpDX.Direct3D.ShaderMacro("THREADSX",             ThreadsX),
                new SharpDX.Direct3D.ShaderMacro("THREADSY",             ThreadsY),    };

            using (var bytecode = ShaderBytecode.CompileFromFile(csFile,csFunction,"cs_5_0",ShaderFlags.None,EffectFlags.None, defines ))
            {
                computeShaders[csFunction] = new ComputeShader(device, bytecode);
            }
        }
        protected void DoRender()
        {    // Retrieve existing pipeline states for backup 
           RawColor4 oldBlendFactor;
            int oldSampleMask;
            int oldStencil;
            var oldPSBufs = context.PixelShader.GetConstantBuffers(0, 1);
            using (var oldVS = context.VertexShader.Get())
            using (var oldPS = context.PixelShader.Get())
            using (var oldGS = context.GeometryShader.Get())
            using (var oldSamp = context.PixelShader.GetSamplers(0, 1).FirstOrDefault())
            using (var oldBlendState = context.OutputMerger.GetBlendState(out oldBlendFactor, out oldSampleMask))
            using (var oldIA = context.InputAssembler.InputLayout)
            using (var oldDepth = context.OutputMerger
                 .GetDepthStencilState(out oldStencil))
            {

                // There is no input layout for this renderer
                context.InputAssembler.InputLayout = null;
                // The triangle strip input topology
                context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleStrip;
                // Disable depth write
                context.OutputMerger.SetDepthStencilState(disableDepthWrite);
                // Set the additive blend state
                if (!UseLightenBlend) context.OutputMerger.SetBlendState(blendState, null, 0xFFFFFFFF);
                else context.OutputMerger.SetBlendState(blendStateLight, Color.White, 0xFFFFFFFF);
                // Assign consume particle buffer SRV to vertex shader 
                context.VertexShader.SetShaderResource(0, particleSRVs[1]);
                context.VertexShader.Set(vertexShader);
                // Set pixel shader resources 
                context.PixelShader.SetShaderResource(0, particleTextureSRV);

                context.PixelShader.SetSampler(0, linearSampler);
                context.PixelShader.Set(pixelShader);
                // Draw the number of quad instances stored in the 
                // indirectArgsBuffer. The vertex shader will rely upon 
                // the SV_VertexID and SV_InstanceID input semantics
                context.DrawInstancedIndirect(indirectArgsBuffer, 0);
                // Restore previous pipeline state  
                context.VertexShader.Set(oldVS);
                context.PixelShader.SetConstantBuffers(0, oldPSBufs);
                context.PixelShader.Set(oldPS);
                context.GeometryShader.Set(oldGS);
                context.PixelShader.SetSampler(0, oldSamp);
                context.InputAssembler.InputLayout = oldIA;
                // Restore previous blend and depth state   
                context.OutputMerger.SetBlendState(oldBlendState, oldBlendFactor, oldSampleMask);
                context.OutputMerger.SetDepthStencilState(oldDepth, oldStencil);
            }
        }

    }
}