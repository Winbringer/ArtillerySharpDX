using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;
using SharpDX.Windows;
using System.Diagnostics;
using VictoremLibrary;
using SharpDX.D3DCompiler;

namespace DifferedRendering
{
    class AppMy : IDisposable
    {

        Factory _factory;
        //Форма куда будем вставлять наше представление renderTargetView.
        private RenderForm _renderForm = null;
        //Объектное представление нашей видеокарты
        private SharpDX.Direct3D11.Device _dx11Device = null;
        private DeviceContext _dx11DeviceContext = null;
        //Цепочка замены заднего и отображаемого буфера
        private SwapChain _swapChain = null;
        //Представление куда мы выводим картинку.
        RenderTargetView _renderView = null;
        DepthStencilView _depthView = null;
        //Управление через клавиатуру
        DirectInput _directInput;
        Keyboard _keyboard;
        Stopwatch _stopWatch = new Stopwatch();
        //Шейдеры
        VertexShader fillGBufferVS;
        PixelShader fillGBufferPS;
        private Model model;
        //Свойства
        public float ViewRatio { get; private set; }
        public DeviceContext DeviceContext { get { return _dx11DeviceContext; } }
        public SharpDX.Windows.RenderForm Form { get { return _renderForm; } }
        public SwapChain SwapChain { get { return _swapChain; } }
        public int Width { get { return _renderForm.Width; } }
        public int Height { get { return _renderForm.Height; } }
        public Color Color { get; set; }
        public RenderTargetView RenderView { get { return _renderView; } }
        public DepthStencilView DepthView { get { return _depthView; } }


        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="renderForm">Форма в котору будем рисовать наши объекты</param>
        public AppMy(RenderForm renderForm)
        {
            Color = new Color(0, 0, 128);

            _renderForm = renderForm;

            ViewRatio = (float)_renderForm.Width / _renderForm.Height;

            InitializeDeviceResources();

            model = new Model(_dx11Device, "sponza.obj", "textures\\", Width, Height);
            model._world = Matrix.Scaling(1);

            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();
            _stopWatch.Reset();
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
                         _renderForm.Width,
                         _renderForm.Height,
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
                      Width = _renderForm.Width,
                      Height = _renderForm.Height,
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
            _dx11DeviceContext.Rasterizer.SetViewport(0, 0, _renderForm.Width, _renderForm.Height);
            _dx11DeviceContext.OutputMerger.SetTargets(_depthView, _renderView);


            ShaderFlags flags = ShaderFlags.None;
#if DEBUG
            flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            using (var bytecode = ShaderBytecode.CompileFromFile(@"Shaders\FillGBuffer.hlsl", "VSFillGBuffer", "vs_5_0", flags))
                fillGBufferVS = new VertexShader(_dx11Device, bytecode);

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Shaders\FillGBuffer.hlsl", "PSFillGBuffer", "ps_5_0", flags))
            {
                fillGBufferPS = new PixelShader(_dx11Device, bytecode);
            }

            gbuffer = new GBuffer(this.Width, this.Height, new SampleDescription(1, 0), _dx11Device, Format.R8G8B8A8_UNorm, Format.R32_UInt, Format.R8G8B8A8_UNorm);

        }


        private void Update(float time)
        {
            var m = _keyboard.GetCurrentState();
            // if (m.PressedKeys.Count > 0)
            model.Update(time, true);
        }

        private void Draw(float time)
        {
            _dx11DeviceContext.ClearRenderTargetView(_renderView, Color);
            _dx11DeviceContext.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            //_dx11DeviceContext.VertexShader.Set(fillGBufferVS);
            //_dx11DeviceContext.PixelShader.Set(fillGBufferPS);
            //gbuffer.Clear(_dx11DeviceContext, new Color(0, 0, 0, 0));
            //gbuffer.Bind(_dx11DeviceContext);
            ////meshes.ForEach((m) => {
            ////   perObject.View = viewMatrix;
            ////    perObject.InverseView = Matrix.Invert(viewMatrix);
            ////    perObject.Projection = projectionMatrix;
            ////    perObject.InverseProjection = Matrix.Invert(projectionMatrix);
            ////    m.Render();
            ////}
            //gbuffer.Unbind(_dx11DeviceContext);
            //_dx11DeviceContext.OutputMerger.SetRenderTargets(this._depthView, this._renderView);
            ////... use G-Buffer for screen - space rendering
            model.Draw(_dx11DeviceContext);
            _swapChain.Present(0, PresentFlags.None);
        }

        /// <summary>
        /// Запускает бесконечный цикл игры
        /// </summary>
        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }

        double totalTime = 0;
        private GBuffer gbuffer;

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
            Utilities.Dispose(ref _factory);
            Utilities.Dispose(ref _depthView);
            Utilities.Dispose(ref _dx11DeviceContext);
            Utilities.Dispose(ref fillGBufferPS);
            Utilities.Dispose(ref fillGBufferVS);
            Utilities.Dispose(ref gbuffer);
            model?.Dispose();
            Utilities.Dispose(ref _swapChain);
            Utilities.Dispose(ref _dx11Device);
            _swapChain?.Dispose();
            _dx11Device?.Dispose();
        }

    }
}
