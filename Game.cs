using DX11 = SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX11GameByWinbringer.Models;
using System.Diagnostics;
using SharpDX;

namespace SharpDX11GameByWinbringer
{
    /// <summary>
    /// Основной класс игры. Наша Вьюшка. Отвечает за инициализацию графики и игровой цкил отображения данных на экран.
    /// </summary>
    public class Game : System.IDisposable
    {
        public delegate void UpdateDraw(double t);
        public event UpdateDraw OnUpdate = null;
        public event UpdateDraw OnDraw = null;
        Factory _factory;
        //Форма куда будем вставлять наше представление renderTargetView.
        private SharpDX.Windows.RenderForm _renderForm = null;
        private readonly int _Width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        private readonly int _Height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
        //Объектное представление нашей видеокарты
        private DX11.Device _dx11Device = null;
        private DX11.DeviceContext _dx11DeviceContext = null;
        //Цепочка замены заднего и отображаемого буфера
        private SwapChain _swapChain;
        //Представление куда мы выводим картинку.
        private DX11.RenderTargetView _renderTargetView = null;       
        //Буффер и представление глубины
        DX11.Texture2D _depthBuffer = null;
        DX11.DepthStencilView _depthView = null;
        //Параметры растеризации
        DX11.RasterizerState _rasterizerState = null;
        DX11.DepthStencilState _DState = null;
        DX11.RasterizerStateDescription _rasterizerStateDescription;
        Presenter _presenter = null;
        public float ViewRatio { get; set; }
        public DX11.DeviceContext DeviceContext { get { return _dx11DeviceContext; } }
        public SharpDX.Windows.RenderForm Form { get { return _renderForm; } }
        
        public Game()
        {
            ViewRatio = (float)_Width / _Height;
            _renderForm = new SharpDX.Windows.RenderForm("SharpDXGameByWinbringer")
            {
                AllowUserResizing = false,
                IsFullscreen = false,
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                ClientSize = new System.Drawing.Size(_Width, _Height),
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
            };
            _renderForm.Shown += (sender, e) => { _renderForm.Activate(); };
            InitializeDeviceResources();
        }

        private void InitializeDeviceResources()
        {
            //Параметры отоборжарежния
            ModeDescription backBufferDesc = new ModeDescription(_Width, _Height, new Rational(60, 1), Format.R8G8B8A8_UNorm);
            SwapChainDescription swapChainDesc = new SwapChainDescription()
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(2, 2),
                Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                BufferCount = 2,
                OutputHandle = _renderForm.Handle,
                IsWindowed = true,
                SwapEffect = SwapEffect.Discard,
                Flags = SwapChainFlags.AllowModeSwitch
            };
            //Создаем Девайс, Цепочку обмена и Девайс контекст
            DX11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DX11.DeviceCreationFlags.None | DX11.DeviceCreationFlags.BgraSupport, new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 }, swapChainDesc, out _dx11Device, out _swapChain);
            _dx11DeviceContext = _dx11Device.ImmediateContext;
            //Игноровать все события видновс
            _factory = _swapChain.GetParent<Factory>();
            _factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);
            // Создаем буффер глубины
            _depthBuffer = new DX11.Texture2D(_dx11Device, new DX11.Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = _renderForm.ClientSize.Width,
                Height = _renderForm.ClientSize.Height,
                SampleDescription = new SampleDescription(2, 2),
                Usage = DX11.ResourceUsage.Default,
                BindFlags = DX11.BindFlags.DepthStencil,
                CpuAccessFlags = DX11.CpuAccessFlags.None,
                OptionFlags = DX11.ResourceOptionFlags.None
            });
            // Создавем Отображение глубины
            _depthView = new DX11.DepthStencilView(_dx11Device, _depthBuffer);
            //Создаем цель куда будем рисовать
            using (DX11.Texture2D backBuffer = _swapChain.GetBackBuffer<DX11.Texture2D>(0))
            {               
                _renderTargetView = new DX11.RenderTargetView(_dx11Device, backBuffer);                
                _presenter = new Presenter(this, new TextWirter(backBuffer, _Width, _Height));
            }
            _dx11DeviceContext.OutputMerger.SetTargets(_depthView, _renderTargetView);
            //Устанавливаем размер конечной картинки            
            _dx11DeviceContext.Rasterizer.SetViewport(0, 0, (float)_Width, (float)_Height);
            //Устанавливаем параметры рисования.
            _rasterizerStateDescription = DX11.RasterizerStateDescription.Default();
            _rasterizerStateDescription.CullMode = DX11.CullMode.None;
            _rasterizerState = new DX11.RasterizerState(_dx11Device, _rasterizerStateDescription);
            _dx11DeviceContext.Rasterizer.State = _rasterizerState;
            //Устанавливаем параметры буффера глубины
            DX11.DepthStencilStateDescription DStateDescripshion = DX11.DepthStencilStateDescription.Default();
            DStateDescripshion.DepthWriteMask = DX11.DepthWriteMask.Zero;
            _DState = new DX11.DepthStencilState(_dx11Device, DStateDescripshion);
            _dx11DeviceContext.OutputMerger.DepthStencilState = _DState;

        }

        private void Update(double time)
        {
            OnUpdate?.Invoke(time);
        }

        private void Draw()
        {
            _dx11DeviceContext.ClearDepthStencilView(_depthView, DX11.DepthStencilClearFlags.Depth | DX11.DepthStencilClearFlags.Stencil, 1.0f, 0);
            _dx11DeviceContext.ClearRenderTargetView(_renderTargetView, new SharpDX.Color(0, 0, 128));            
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
                    Utilities.Dispose(ref _DState);
                    Utilities.Dispose(ref _renderTargetView);
                    Utilities.Dispose(ref _swapChain);
                    Utilities.Dispose(ref _renderForm);
                    Utilities.Dispose(ref _factory);
                    Utilities.Dispose(ref _depthBuffer);
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
    }
}
// _timer.Tick();
//const int FPS = 25;
//const int FrameDuration = 1000 / FPS;
//const int MaxFrameSkip = 10;
//double nextFrameTime = Environment.TickCount;
//// счетчик итераций игрового цикла, произведенных до первого рендера
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