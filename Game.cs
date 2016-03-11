using DX11 = SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;

namespace SharpDX11GameByWinbringer
{
    /// <summary>
    /// Основной класс игры. Наша Вьюшка. Отвечает за инициализацию графики и игровой цкил отображения данных на экран.
    /// </summary>
    public class Game : System.IDisposable
    {
        //События
        public delegate void UpdateDraw(double t);
        public event UpdateDraw OnUpdate = null;
        public event UpdateDraw OnDraw = null;
        //Поля
        Factory _factory;
        //Форма куда будем вставлять наше представление renderTargetView.
        private SharpDX.Windows.RenderForm _renderForm = null;      
        //Объектное представление нашей видеокарты
        private DX11.Device _dx11Device = null;
        private DX11.DeviceContext _dx11DeviceContext = null;
        //Цепочка замены заднего и отображаемого буфера
        private SwapChain _swapChain;
        //Представление куда мы выводим картинку.
        private DX11.RenderTargetView _renderView = null;
        private DX11.DepthStencilView _depthView = null;
        //Параметры отображения
        private DX11.RasterizerState _rasterizerState = null;
        private DX11.DepthStencilState _DState = null;
        private DX11.BlendState _blendState = null;
        private Presenter _presenter = null;
        //Свойства
        public float ViewRatio { get; set; }
        public DX11.DeviceContext DeviceContext { get { return _dx11DeviceContext; } }
        public SharpDX.Windows.RenderForm Form { get { return _renderForm; } }
        public SwapChain SwapChain { get { return _swapChain; } }
        public int Width { get { return _renderForm.ClientSize.Width; } }
        public int Height { get { return _renderForm.ClientSize.Height; } }

        public Game(SharpDX.Windows.RenderForm renderForm)
        {
            _renderForm = renderForm;
            ViewRatio = (float)_renderForm.ClientSize.Width / _renderForm.ClientSize.Height;           
            InitializeDeviceResources();
            _presenter = new Presenter(this);
            _renderForm.Show();
        }

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).                  
                    Utilities.Dispose(ref _presenter);
                    Utilities.Dispose(ref _blendState);
                    Utilities.Dispose(ref _DState);
                    Utilities.Dispose(ref _renderView);
                    Utilities.Dispose(ref _swapChain);
                    Utilities.Dispose(ref _renderForm);
                    Utilities.Dispose(ref _factory);
                    Utilities.Dispose(ref _depthView);
                    Utilities.Dispose(ref _rasterizerState);
                    Utilities.Dispose(ref _dx11Device);
                    Utilities.Dispose(ref _dx11DeviceContext);
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        /// <summary>
        /// Инициализирует объекты связанные с графическим устройство - Девайс его контекст и Свапчейн
        /// </summary>
        private void InitializeDeviceResources()
        {
            //Создаем объектное преставление нашего GPU, его контекст и класс который будет менят местами буфферы в которые рисует наша GPU
            DX11.Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                DX11.DeviceCreationFlags.None | DX11.DeviceCreationFlags.BgraSupport,
                new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 },
                 new SwapChainDescription()
                 {
                     ModeDescription = new ModeDescription(
                        _renderForm.ClientSize.Width,
                        _renderForm.ClientSize.Height,
                         new Rational(60, 1),
                         Format.R8G8B8A8_UNorm),
                     SampleDescription = new SampleDescription(1, 0),
                     Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                     BufferCount = 2,
                     OutputHandle = _renderForm.Handle,
                     IsWindowed = true,
                     SwapEffect = SwapEffect.Discard,
                     Flags = SwapChainFlags.AllowModeSwitch
                 },
                out _dx11Device,
                out _swapChain);
            //Игноровать все события видновс
            _factory = _swapChain.GetParent<Factory>();
            _factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);
            // Создаем буффер и вьюшку глубины
            using (var _depthBuffer = new DX11.Texture2D(
                  _dx11Device,
                  new DX11.Texture2DDescription()
                  {
                      Format = Format.D32_Float_S8X24_UInt,
                      ArraySize = 1,
                      MipLevels = 1,
                      Width = _renderForm.ClientSize.Width,
                      Height = _renderForm.ClientSize.Height,
                      SampleDescription = new SampleDescription(1, 0),
                      Usage = DX11.ResourceUsage.Default,
                      BindFlags = DX11.BindFlags.DepthStencil,
                      CpuAccessFlags = DX11.CpuAccessFlags.None,
                      OptionFlags = DX11.ResourceOptionFlags.None
                  }))
                _depthView = new DX11.DepthStencilView(_dx11Device, _depthBuffer);
            //Создаем буффер и вьюшку для рисования
            using (DX11.Texture2D backBuffer = _swapChain.GetBackBuffer<DX11.Texture2D>(0))
                _renderView = new DX11.RenderTargetView(_dx11Device, backBuffer);
            //Создаем контекст нашего GPU
            _dx11DeviceContext = _dx11Device.ImmediateContext;
            CreateState();
            //Устанавливаем размер конечной картинки            
            _dx11DeviceContext.Rasterizer.SetViewport(0, 0, _renderForm.ClientSize.Width, _renderForm.ClientSize.Height);
            _dx11DeviceContext.Rasterizer.State = _rasterizerState;            
            _dx11DeviceContext.OutputMerger.SetTargets(_depthView, _renderView);

        }
        /// <summary>
        /// Создает параметры рендеринга, глубины и блендинга
        /// </summary>
        private void CreateState()
        {
            //Устанавливаем параметры растеризации так чтобы обратная сторона объекта не спряталась.
            DX11.RasterizerStateDescription rasterizerStateDescription = DX11.RasterizerStateDescription.Default();
            rasterizerStateDescription.CullMode = DX11.CullMode.None;
            rasterizerStateDescription.FillMode = DX11.FillMode.Solid;
            _rasterizerState = new DX11.RasterizerState(_dx11Device, rasterizerStateDescription);
            //Устанавливаем параметры буффера глубины
            DX11.DepthStencilStateDescription DStateDescripshion = DX11.DepthStencilStateDescription.Default();
            _DState = new DX11.DepthStencilState(_dx11Device, DStateDescripshion);
            //TODO: донастроить параметры блендинга для прозрачности.
            #region Формула бледнинга
            //(FC) - Final Color
            //(SP) - Source Pixel
            //(DP) - Destination Pixel
            //(SBF) - Source Blend Factor
            //(DBF) - Destination Blend Factor
            //(FA) - Final Alpha
            //(SA) - Source Alpha
            //(DA) - Destination Alpha
            //(+) - Binaray Operator described below
            //(X) - Cross Multiply Matrices
            //Формула для блендинга
            //(FC) = (SP)(X)(SBF)(+)(DP)(X)(DPF)
            //(FA) = (SA)(SBF)(+)(DA)(DBF)
            //ИСПОЛЬЗОВАНИЕ
            //_dx11DeviceContext.OutputMerger.DepthStencilState = _DState;
            //_dx11DeviceContext.OutputMerger.SetBlendState(
            //    _blendState,
            //     new SharpDX.Mathematics.Interop.RawColor4(0.75f, 0.75f, 0.75f, 1f));
            //ЭТО ДЛЯ НЕПРОЗРАЧНЫХ _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            #endregion
            DX11.RenderTargetBlendDescription targetBlendDescription = new SharpDX.Direct3D11.RenderTargetBlendDescription()
            {
                IsBlendEnabled = new SharpDX.Mathematics.Interop.RawBool(true),
                SourceBlend = DX11.BlendOption.SourceColor,
                DestinationBlend = DX11.BlendOption.BlendFactor,
                BlendOperation = DX11.BlendOperation.Add,
                SourceAlphaBlend = DX11.BlendOption.One,
                DestinationAlphaBlend = DX11.BlendOption.Zero,
                AlphaBlendOperation = DX11.BlendOperation.Add,
                RenderTargetWriteMask = DX11.ColorWriteMaskFlags.All
            };
            DX11.BlendStateDescription blendDescription = DX11.BlendStateDescription.Default();
            blendDescription.AlphaToCoverageEnable = new SharpDX.Mathematics.Interop.RawBool(false);
            blendDescription.RenderTarget[0] = targetBlendDescription;
            _blendState = new DX11.BlendState(_dx11Device, blendDescription);
        }

        private void Update(double time)
        {
            OnUpdate?.Invoke(time);
        }

        private void Draw()
        {
            _dx11DeviceContext.ClearDepthStencilView(_depthView, DX11.DepthStencilClearFlags.Depth | DX11.DepthStencilClearFlags.Stencil, 1.0f, 0);
            _dx11DeviceContext.ClearRenderTargetView(_renderView, new SharpDX.Color(0, 0, 128));
            OnDraw?.Invoke(1);
            _swapChain.Present(0, PresentFlags.None);
        }

        public void Run()
        {
            SharpDX.Windows.RenderLoop.Run(_renderForm, RenderCallback);
        }

        double nextFrameTime = System.Environment.TickCount;

        private void RenderCallback()
        {
            double lag = System.Environment.TickCount - nextFrameTime;
            if (lag > 30)
            {
                nextFrameTime = System.Environment.TickCount;
                Update(lag);
            }
            Draw();
        }

    }
}

//int loops;
//private void RenderCallback()
//{
//    _timer.Tick();
//    loops = 0;
//    while (Environment.TickCount > nextFrameTime && loops < MaxFrameSkip)
//    {
//        Update(_timer.DeltaTime);
//        nextFrameTime += FrameDuration;
//        loops++;
//    }

//    Draw(_timer.DeltaTime);
//}