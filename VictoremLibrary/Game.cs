using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;

namespace VictoremLibrary
{  /// <summary>
   /// Класс для передачи данны из Событий
   /// </summary>
    public class UpdateArgs : EventArgs
    {
        public float Time { get; set; }
        public KeyboardState KeyboardState { get; set; }
    }

    /// <summary>
    /// Основной класс игры.
    /// </summary>
    public class Game : IDisposable
    {
        public delegate void KeyPressHandler(float time, KeyboardState kState);
        public delegate void UpdateHandler(float time);
        /// <summary>
        /// Происходит при нажатии клавиатуры. Тип данных передоваемых в переменную e - UpdateArgs.
        /// </summary>
        public event KeyPressHandler OnKeyPressed = null;
        /// <summary>
        /// Вызываеться при обновлении логики игры.Тип данных передоваемых в переменную  e - UpdateArgs.
        /// </summary>
        public event UpdateHandler OnUpdate = null;
        /// <summary>
        /// Вызываеться при рендеринге игры
        /// </summary>
        public event UpdateHandler OnDraw = null;

        SharpDX.DXGI.Factory _factory;
        //Форма куда будем вставлять наше представление renderTargetView.
        private RenderForm _renderForm = null;
        //Объектное представление нашей видеокарты
        private SharpDX.Direct3D11.Device _dx11Device = null;
        private DeviceContext _dx11DeviceContext = null;
        //Цепочка замены заднего и отображаемого буфера
        private SwapChain _swapChain = null;
        //Представление куда мы выводим картинку.
        private RenderTargetView _renderView = null;
        private DepthStencilView _depthView = null;
        //Управление через клавиатуру
        DirectInput _directInput;
        Keyboard _keyboard;
        DX11Drawer _drawer = null;
        TextWirter _texWriter = null;
        FilterCS _filter = null;

        //Свойства
        public float ViewRatio { get; private set; }
        public DeviceContext DeviceContext { get { return _dx11DeviceContext; } }
        public SharpDX.Windows.RenderForm Form { get { return _renderForm; } }
        public SwapChain SwapChain { get { return _swapChain; } }
        public int Width { get { return _renderForm.ClientSize.Width; } }
        public int Height { get { return _renderForm.ClientSize.Height; } }
        public Color Color { get; set; }
        /// <summary>
        /// Выводит 3Д объекты на экран
        /// </summary>
        public DX11Drawer Drawer { get { return _drawer; } }
        /// <summary>
        /// Выводит 2Д объекты и Текст на экран
        /// </summary>
        public TextWirter Drawer2D { get { return _texWriter; } }
        /// <summary>
        /// Придоставляет методы для наложения различных эффектов ( размытия, яркости и т. д.) на текстуру.
        /// </summary>
        public FilterCS FilterFoTexture { get { return _filter; } }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="renderForm">Форма в котору будем рисовать наши объекты</param>
        public Game(RenderForm renderForm)
        {
            Color = new Color(0, 0, 128);

            _renderForm = renderForm;

            ViewRatio = (float)_renderForm.ClientSize.Width / _renderForm.ClientSize.Height;

            InitializeDeviceResources();

            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();
            _drawer = new DX11Drawer(_dx11DeviceContext);
            _filter = new FilterCS(this);
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
            _texWriter = new TextWirter(this.SwapChain.GetBackBuffer<Texture2D>(0), _renderForm.ClientSize.Width, _renderForm.ClientSize.Height);
        }
        float Time = 0;
        private void Update(double time)
        {
            Time = (float)time;
            var m = _keyboard.GetCurrentState();
            if (m.PressedKeys.Count > 0)
                OnKeyPressed?.Invoke(Time, m);
            OnUpdate?.Invoke(Time);
        }

        private void Draw()
        {
            _dx11DeviceContext.ClearRenderTargetView(_renderView, Color);
            _dx11DeviceContext.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            OnDraw?.Invoke(Time);
            _swapChain.Present(0, PresentFlags.None);
        }

        /// <summary>
        /// Запускает бесконечный цикл игры
        /// </summary>
        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }

        double nextFrameTime = Environment.TickCount;
        private void RenderCallback()
        {
            double lag = Environment.TickCount - nextFrameTime;
            if (lag > 30)
            {
                nextFrameTime = Environment.TickCount;
                Update(lag);
            }
            Draw();
        }

        public void Dispose()
        {
            OnKeyPressed = null;
            OnUpdate = null;
            OnDraw = null;
            Utilities.Dispose(ref _keyboard);
            Utilities.Dispose(ref _directInput);
            Utilities.Dispose(ref _renderView);
            Utilities.Dispose(ref _swapChain);
            Utilities.Dispose(ref _factory);
            Utilities.Dispose(ref _depthView);
            Utilities.Dispose(ref _dx11Device);
            Utilities.Dispose(ref _dx11DeviceContext);
            _swapChain?.Dispose();
            _dx11Device?.Dispose();
            _drawer.Dispose();
            _texWriter.Dispose();
        }

    }
}
