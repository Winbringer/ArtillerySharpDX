using SharpDX;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using VictoremLibrary;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;

namespace ConsoleApplication2
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrixes
    {
        public Matrix World;
        public Matrix View;
        public Matrix Proj;
        public void Trans()
        {
            World.Transpose();
            View.Transpose();
            Proj.Transpose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GPUParticleData
    {
        public Vector3 Position;
        public Vector3 Velocity;
    }

    class Logic : LogicBase
    {
        const int PARTICLES_COUNT = 1000000;
        private Buffer _perFrame;
        private Effect _effect;
        private Buffer _particlesBuffer;
        private ShaderResourceView _SRV;
        private UnorderedAccessView _UAV;
        private SamplerState _particleSampler;
        private ShaderResourceView _texture;
        private DepthStencilState _DState;
        private BlendState _blendState;
        private int _groupSizeX;
        private int _groupSizeY;
        private Effect _solver;

        public Matrix World { get { return worldMatrix; } set { worldMatrix = value; } }

        public Logic(Game game) : base(game)
        {
            game.Color = Color.Black;
            System.Random random = new System.Random();

            GPUParticleData[] initialParticles = new GPUParticleData[PARTICLES_COUNT];
            Vector3 min = new Vector3(-30f, -30f, -30f);
            Vector3 max = new Vector3(30f, 30f, 30f);

            for (int i = 0; i < PARTICLES_COUNT; i++)
            {
                initialParticles[i].Position = random.NextVector3(min, max);

                float angle = -(float)System.Math.Atan2(initialParticles[i].Position.X, initialParticles[i].Position.Z);
                initialParticles[i].Velocity = new Vector3((float)System.Math.Cos(angle), 0f, (float)System.Math.Sin(angle)) * 5f;
            }

            _particlesBuffer = new Buffer(game.DeviceContext.Device,
               Utilities.SizeOf<GPUParticleData>() * PARTICLES_COUNT,
               ResourceUsage.Default,
               BindFlags.ShaderResource | BindFlags.UnorderedAccess,
               CpuAccessFlags.None,
               ResourceOptionFlags.BufferStructured,
               Utilities.SizeOf<GPUParticleData>());
            game.DeviceContext.UpdateSubresource(initialParticles, _particlesBuffer);

            #region Blend and Depth States
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

            blendDesc.RenderTarget[0].DestinationBlend = BlendOption.One;

            BlendStateDescription blendDescription = BlendStateDescription.Default();
            blendDescription.RenderTarget[0].IsBlendEnabled = true;
            blendDescription.RenderTarget[0].SourceBlend = BlendOption.One;
            blendDescription.RenderTarget[0].DestinationBlend = BlendOption.One;
            blendDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;


            var depthDesc = new DepthStencilStateDescription
            {
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.Zero,
                IsDepthEnabled = true,
                IsStencilEnabled = false
            };

            _DState = new DepthStencilState(game.DeviceContext.Device, depthDesc);
            _blendState = new BlendState(game.DeviceContext.Device, blendDescription);
            #endregion

            worldMatrix = Matrix.Identity;
            viewMatrix = Matrix.LookAtLH(new Vector3(100,100, 100), Vector3.Zero, Vector3.Up);
            projectionMatrix = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.ViewRatio, 1f, 1000);
            Matrixes m = new Matrixes();
            m.World = worldMatrix;
            m.View = viewMatrix;
            m.Proj = projectionMatrix;
            m.Trans();

            _perFrame = new Buffer(game.DeviceContext.Device,
              Utilities.SizeOf<Matrixes>(),
              ResourceUsage.Default,
              BindFlags.ConstantBuffer,
              CpuAccessFlags.None,
              ResourceOptionFlags.None,
              0);
            game.DeviceContext.UpdateSubresource(ref m, _perFrame);

            using (var effectByteCode = ShaderBytecode.CompileFromFile("Particle.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None))
                _effect = new Effect(game.DeviceContext.Device, effectByteCode);

            using (var effectByteCode = ShaderBytecode.CompileFromFile("Solver.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None))
                _solver = new Effect(game.DeviceContext.Device, effectByteCode);

            _SRV = new ShaderResourceView(game.DeviceContext.Device, _particlesBuffer);

            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Buffer = new UnorderedAccessViewDescription.BufferResource { FirstElement = 0 }
            };
            uavDesc.Format = Format.Unknown;
            uavDesc.Buffer.Flags = UnorderedAccessViewBufferFlags.None;
            uavDesc.Buffer.ElementCount = _particlesBuffer.Description.SizeInBytes / _particlesBuffer.Description.StructureByteStride;
            _UAV = new UnorderedAccessView(game.DeviceContext.Device, _particlesBuffer, uavDesc);

            SamplerStateDescription samplerDecription = SamplerStateDescription.Default();
            {
                samplerDecription.AddressU = TextureAddressMode.Clamp;
                samplerDecription.AddressV = TextureAddressMode.Clamp;
                samplerDecription.Filter = Filter.MinMagMipLinear;
            };

            _particleSampler = new SamplerState(game.DeviceContext.Device, samplerDecription);
            _texture = StaticMetods.LoadTextureFromFile(game.DeviceContext, "Particle.png");


            int numGroups = (PARTICLES_COUNT % 768 != 0) ? ((PARTICLES_COUNT / 768) + 1) : (PARTICLES_COUNT / 768);
            double secondRoot = System.Math.Pow((double)numGroups, (double)(1.0 / 2.0));
            secondRoot = System.Math.Ceiling(secondRoot);
            _groupSizeX = _groupSizeY = (int)secondRoot;

            game.DeviceContext.OutputMerger.DepthStencilState = _DState;

            _effect.GetConstantBufferByName("Params").AsConstantBuffer().SetConstantBuffer(_perFrame);
            _effect.GetVariableByName("ParticleSampler").AsSampler().SetSampler(0, _particleSampler);
            _effect.GetVariableByName("ParticleTexture").AsShaderResource().SetResource(_texture);
            _effect.GetVariableByName("Size").AsScalar().Set(0.1f);
            _effect.GetVariableByName("Particles").AsShaderResource().SetResource(_SRV);
            _solver.GetVariableByName("GroupDim").AsScalar().Set(_groupSizeX);
            _solver.GetVariableByName("MaxParticles").AsScalar().Set(PARTICLES_COUNT);            
        }

        public override void Dispose()
        {
            Utilities.Dispose(ref _effect);
            Utilities.Dispose(ref _perFrame);
            Utilities.Dispose(ref _particlesBuffer);
            Utilities.Dispose(ref _SRV);
            Utilities.Dispose(ref _UAV);
            Utilities.Dispose(ref _particleSampler);
            Utilities.Dispose(ref _texture);
            Utilities.Dispose(ref _blendState);
            Utilities.Dispose(ref _DState);
            Utilities.Dispose(ref _solver);

        }

        protected override void Draw(float time)
        {
            _effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(game.DeviceContext);
            game.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.PointList;
            game.DeviceContext.OutputMerger.BlendState = _blendState;           
            game.DeviceContext.Draw(PARTICLES_COUNT, 0);
        }

        protected override void KeyKontroller(float time, SharpDX.DirectInput.KeyboardState kState)
        {
        }

        protected override void Upadate(float time)
        {
            float angle = (float)time / 2000;
            Vector3 attractor = new Vector3((float)System.Math.Cos(angle), (float)System.Math.Cos(angle) * (float)System.Math.Sin(angle), (float)System.Math.Sin(angle));
            _solver.GetVariableByName("Attractor").AsVector().Set(attractor*2);
            _solver.GetVariableByName("DeltaTime").AsScalar().Set(time/1000);
            _solver.GetVariableByName("Particles").AsUnorderedAccessView().Set(_UAV);
            _solver.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(game.DeviceContext);
            game.DeviceContext.Dispatch(_groupSizeX, _groupSizeY, 1);
            game.DeviceContext.ComputeShader.SetUnorderedAccessView(0, null);
        }
    }
}
