﻿using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VictoremLibrary;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace CubeReflection
{
    public struct CubeFaceCamera
    {
        public Matrix View;
        public Matrix Projection;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerFrame
    {
        public Matrix WVP;
        public Matrix World;
        public Vector3 CameraPosition;
        float _padding0;
        public void Trn()
        {
            WVP.Transpose();
            World.Transpose();

        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerMaterial
    {
        public uint HasTexture;
        public uint IsReflective;
        public float ReflectionAmount;
        float pd0;
    }

    class App : IDisposable
    {
        #region Fields
        Factory _factory;
        private RenderForm _renderForm = null;
        private SharpDX.Direct3D11.Device _dx11Device = null;
        private DeviceContext _dx11DeviceContext = null;
        private SwapChain _swapChain = null;
        RenderTargetView _renderView = null;
        DepthStencilView _depthView = null;
        DirectInput _directInput;
        Keyboard _keyboard;
        Stopwatch _stopWatch = new Stopwatch();
        double totalTime = 0;
        private ModelSDX _model0;
        private ModelSDX _model1;
        Buffer _c0;
        Buffer _c1;
        Buffer _c2;
        private ShaderSignature _inputSignature;
        private VertexShader _VS0;
        private VertexShader _VS1;
        private PixelShader _PS0;
        private PixelShader _PS1;
        private GeometryShader _GS0;
        private InputLayout _layout;
        private Viewport _viewPort;
        private SamplerState _samler;
        private DepthStencilState _depth;
        private RasterizerState _rasterizer;
        private PerFrame _pf;
        private PerMaterial _pm;
        Matrix World = Matrix.Identity;

        #endregion

        #region Propertis
        public float ViewRatio { get; private set; }
        public DeviceContext DeviceContext { get { return _dx11DeviceContext; } }
        public SharpDX.Windows.RenderForm Form { get { return _renderForm; } }
        public SwapChain SwapChain { get { return _swapChain; } }
        public int Width { get { return _renderForm.ClientSize.Width; } }
        public int Height { get { return _renderForm.ClientSize.Height; } }
        public Color Color { get; set; }
        public RenderTargetView RenderView { get { return _renderView; } }
        public DepthStencilView DepthView { get { return _depthView; } }
        InputElement[] inputElements ={
                new InputElement("SV_Position", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            };
        #endregion

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="renderForm">Форма в котору будем рисовать наши объекты</param>
        public App(RenderForm renderForm)
        {
            Color = new Color(0, 0, 128);

            _renderForm = renderForm;

            ViewRatio = (float)_renderForm.ClientSize.Width / _renderForm.ClientSize.Height;

            InitializeDeviceResources();



            _model0 = new ModelSDX(_dx11Device, "Wm\\", "Female.md5mesh");
            _model1 = new ModelSDX(_dx11Device, "Wm\\", "Female.md5mesh");

            _c0 = CreateConstantBuffer(Utilities.SizeOf<PerFrame>());
            _c1 = CreateConstantBuffer(Utilities.SizeOf<PerMaterial>());
            _c2 = CreateConstantBuffer(Utilities.SizeOf<Matrix>() * 6);

            CreateShaders();

            _layout = new InputLayout(_dx11Device, _inputSignature, inputElements);
            _viewPort = new Viewport(0, 0, Width, Height);

            CreateStates();
            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();
            _stopWatch.Reset();
        }

        #region Metods
        public SharpDX.Direct3D11.Buffer CreateConstantBuffer(int size)
        {
            return new SharpDX.Direct3D11.Buffer(_dx11Device,
                size,
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);
        }

        /// <summary>
        /// Инициализирует объекты связанные с графическим устройство - Девайс его контекст и Свапчейн
        /// </summary>
        private void InitializeDeviceResources()
        {
            var creationFlags = DeviceCreationFlags.None;
#if DEBUG
            creationFlags = DeviceCreationFlags.Debug;
#endif

            //Создаем объектное преставление нашего GPU, его контекст и класс который будет менят местами буфферы в которые рисует наша GPU
            SharpDX.Direct3D11.Device.CreateWithSwapChain(
                 SharpDX.Direct3D.DriverType.Hardware,
                 creationFlags | DeviceCreationFlags.BgraSupport,
                 new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 },
                  new SwapChainDescription()
                  {
                      ModeDescription = new ModeDescription(
                         _renderForm.ClientSize.Width,
                         _renderForm.ClientSize.Height,
                          new Rational(60, 1),
                          Format.R8G8B8A8_UNorm),
                      SampleDescription = new SampleDescription(4, 0),
                      Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                      BufferCount = 2,
                      OutputHandle = _renderForm.Handle,
                      IsWindowed = true,
                      SwapEffect = SwapEffect.Discard,
                      Flags = SwapChainFlags.None
                  },
                 out _dx11Device,
                 out _swapChain);
            //Игноровать все события видновс
            _factory = _swapChain.GetParent<SharpDX.DXGI.Factory>();
            _factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);
            // Создаем буффер и вьюшку глубины
            using (var _depthBuffer = new Texture2D(
                  _dx11Device,
                  new Texture2DDescription()
                  {
                      Format = Format.D32_Float_S8X24_UInt,
                      ArraySize = 1,
                      MipLevels = 1,
                      Width = _renderForm.ClientSize.Width,
                      Height = _renderForm.ClientSize.Height,
                      SampleDescription = _swapChain.Description.SampleDescription,
                      Usage = ResourceUsage.Default,
                      BindFlags = BindFlags.DepthStencil,
                      CpuAccessFlags = CpuAccessFlags.None,
                      OptionFlags = ResourceOptionFlags.None
                  }))
                _depthView = new DepthStencilView(_dx11Device, _depthBuffer, new SharpDX.Direct3D11.DepthStencilViewDescription()
                {
                    Dimension = (SwapChain.Description.SampleDescription.Count > 1 ||
                     SwapChain.Description.SampleDescription.Quality > 0) ?
                     DepthStencilViewDimension.Texture2DMultisampled :
                     DepthStencilViewDimension.Texture2D,
                    Flags = DepthStencilViewFlags.None
                });
            //Создаем буффер и вьюшку для рисования
            using (Texture2D backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
                _renderView = new RenderTargetView(_dx11Device, backBuffer);
            //Создаем контекст нашего GPU
            _dx11DeviceContext = _dx11Device.ImmediateContext;
            //Устанавливаем размер конечной картинки            
            _dx11DeviceContext.Rasterizer.SetViewport(0, 0, _renderForm.ClientSize.Width, _renderForm.ClientSize.Height);
            _dx11DeviceContext.OutputMerger.SetTargets(_depthView, _renderView);
        }

        void CreateShaders()
        {
            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif
            var shadersFile = "CubeMap.hlsl";
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "VS0", "vs_5_0", shaderFlags))
            {
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _VS0 = new VertexShader(_dx11Device, vertexShaderByteCode);
            }
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "VS1", "vs_5_0", shaderFlags))
                _VS1 = new VertexShader(_dx11Device, vertexShaderByteCode);
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "PS0", "ps_5_0", shaderFlags))
                _PS0 = new PixelShader(_dx11Device, vertexShaderByteCode);
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "PS1", "ps_5_0", shaderFlags))
                _PS1 = new PixelShader(_dx11Device, vertexShaderByteCode);
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "GS0", "gs_5_0", shaderFlags))
                _GS0 = new GeometryShader(_dx11Device, vertexShaderByteCode);
        }



        private void Update(float time)
        {
            var m = _keyboard.GetCurrentState();
        }

        private void Draw(float time)
        {
            DrawMesh(Matrix.Identity, Matrix.Identity, _renderView, _depthView, _viewPort);
            _swapChain.Present(0, PresentFlags.None);
        }

        void DrawMesh(Matrix v, Matrix p, RenderTargetView rv, DepthStencilView dv, Viewport vp)
        {
            _dx11DeviceContext.InputAssembler.InputLayout = _layout;
            _dx11DeviceContext.OutputMerger.SetRenderTargets(dv, rv);
            _dx11DeviceContext.Rasterizer.SetViewport(vp);
            _dx11DeviceContext.ClearRenderTargetView(rv, Color);
            _dx11DeviceContext.ClearDepthStencilView(dv,
                DepthStencilClearFlags.Depth |
                DepthStencilClearFlags.Stencil,
                1.0f,
                0);

            SetStates();
            _pf.World = World;
            _pf.WVP = World * v * p;
            _pf.Trn();
            _dx11DeviceContext.UpdateSubresource(ref _pf, _c0);

            _dx11DeviceContext.PixelShader.Set(_PS1);
            _dx11DeviceContext.VertexShader.Set(_VS1);
            _dx11DeviceContext.VertexShader.SetConstantBuffer(0, _c0);
            _dx11DeviceContext.VertexShader.SetConstantBuffer(2, _c2);

            _dx11DeviceContext.PixelShader.SetConstantBuffer(0, _c0);

            _dx11DeviceContext.PixelShader.SetConstantBuffer(2, _c2);

            _dx11DeviceContext.GeometryShader.Set(null);
            _dx11DeviceContext.HullShader.Set(null);
            _dx11DeviceContext.DomainShader.Set(null);
            _dx11DeviceContext.ComputeShader.Set(null);

            foreach (var m in _model0.Meshes3D)
            {
                _pm.HasTexture = m?.Texture == null ? 0u : 1u;
                _dx11DeviceContext.UpdateSubresource(ref _pm, _c1);
                _dx11DeviceContext.PixelShader.SetConstantBuffer(1, _c1);
                _dx11DeviceContext.VertexShader.SetConstantBuffer(1, _c1);
                _dx11DeviceContext.PixelShader.SetShaderResource(0, m.Texture);
            }


        }

        void DrawRfCube(Matrix v, Matrix p, RenderTargetView rv, DepthStencilView dv, Viewport vp)
        {
            _dx11DeviceContext.InputAssembler.InputLayout = _layout;
            _dx11DeviceContext.OutputMerger.SetRenderTargets(dv, rv);
            _dx11DeviceContext.Rasterizer.SetViewport(vp);
            _dx11DeviceContext.ClearRenderTargetView(rv, Color);
            _dx11DeviceContext.ClearDepthStencilView(dv,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
                1.0f, 0);


        }

        /// <summary>
        /// Запускает бесконечный цикл игры
        /// </summary>
        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }

        void CreateStates()
        {
            var d = DepthStencilStateDescription.Default();
            d.IsDepthEnabled = true;
            d.IsStencilEnabled = false;

            var r = RasterizerStateDescription.Default();
            r.FillMode = SharpDX.Direct3D11.FillMode.Solid;
            r.CullMode = CullMode.None;

            var b = BlendStateDescription.Default();
            b.AlphaToCoverageEnable = new RawBool(true);
            var s = new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = new Color4(0, 0, 0, 0),
                ComparisonFunction = Comparison.Never,
                Filter = Filter.Anisotropic,
                MaximumAnisotropy = 16,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0.0f
            };
            _samler = new SamplerState(_dx11Device, s);
            _depth = new DepthStencilState(_dx11Device, d);
            _rasterizer = new RasterizerState(_dx11Device, r);
            _dx11DeviceContext.OutputMerger.DepthStencilState = _depth;
        }

        void SetStates()
        {
            _dx11DeviceContext.Rasterizer.State = _rasterizer;
            _dx11DeviceContext.PixelShader.SetSampler(0, _samler);
        }

        private void RenderCallback()
        {
            var elapsed = _stopWatch.ElapsedMilliseconds;
            totalTime += elapsed;
            _stopWatch.Reset();
            _stopWatch.Start();

            if (totalTime > 30)
            {
                Update((float)totalTime);
                totalTime = 0;
            }
            Draw(elapsed);
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _keyboard);
            Utilities.Dispose(ref _directInput);
            Utilities.Dispose(ref _renderView);
            Utilities.Dispose(ref _swapChain);
            Utilities.Dispose(ref _factory);
            Utilities.Dispose(ref _depthView);
            Utilities.Dispose(ref _c0);
            Utilities.Dispose(ref _c1);
            Utilities.Dispose(ref _c2);
            Utilities.Dispose(ref _layout);
            Utilities.Dispose(ref _inputSignature);
            Utilities.Dispose(ref _VS0);
            Utilities.Dispose(ref _VS1);
            Utilities.Dispose(ref _PS0);
            Utilities.Dispose(ref _PS1);
            Utilities.Dispose(ref _GS0);
            Utilities.Dispose(ref _samler);
            Utilities.Dispose(ref _depth);
            Utilities.Dispose(ref _rasterizer);


            _model0?.Dispose();
            _model1?.Dispose();

            Utilities.Dispose(ref _dx11Device);
            Utilities.Dispose(ref _dx11DeviceContext);
            _swapChain?.Dispose();
            _dx11Device?.Dispose();
        }
        #endregion
    }

}
