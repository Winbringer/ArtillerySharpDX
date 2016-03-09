using SharpDX.Windows;
using DX11 = SharpDX.Direct3D11;
using System;
using System.Drawing;
using SharpDX.DXGI;
using SharpDX11GameByWinbringer.Models;
using SharpDX.Direct3D;
using SharpDX;

namespace SharpDX11GameByWinbringer
{
    class Game : IDisposable
    {
        Factory _factory;
        //Форма куда будем вставлять наше представление renderTargetView.
        private RenderForm _renderForm = null;
        private readonly int _Width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        private readonly int _Height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
        //Таймер который будет счиать время
        private static readonly GameTimer _timer = new GameTimer();
        //Объектное представление нашей видеокарты
        private DX11.Device _dx11Device = null;
        private DX11.DeviceContext _dx11DeviceContext = null;
        //Цепочка замены заднего и отображаемого буфера
        private SwapChain _swapChain;
        //Представление куда мы выводим картинку.
        private DX11.RenderTargetView _renderTargetView = null;
        //Координатная сетка 
        private Viewport _viewport;
        //Буффер и представление глубины
        DX11.Texture2D depthBuffer = null;
        DX11.DepthStencilView depthView = null;
        Wave _waves = null;
        public Game()
        {
            _timer.Reset();
            _renderForm = new RenderForm("SharpDXGameByWinbringer")
            {
                AllowUserResizing = false,
                IsFullscreen = false,
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                ClientSize = new Size(_Width, _Height),
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
            };
            _renderForm.Shown += (sender, e) => { _renderForm.Activate(); };
            InitInput();
            InitializeDeviceResources();
            float ratio = (float)_Width / (float)_Height;
            _waves = new Wave(_dx11DeviceContext, ratio);

        }

        private void InitializeDeviceResources()
        {
            ModeDescription backBufferDesc = new ModeDescription(_Width, _Height, new Rational(60, 1), Format.R8G8B8A8_UNorm);
            SwapChainDescription swapChainDesc = new SwapChainDescription()
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(4, 4),
                Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                BufferCount = 2,
                OutputHandle = _renderForm.Handle,
                IsWindowed = true,
                SwapEffect = SwapEffect.Discard,                 
                Flags = SwapChainFlags.AllowModeSwitch
            };
            //Создаем Девайс, Цепочку обмена и Девайс контекст
            DX11.Device.CreateWithSwapChain(DriverType.Hardware, DX11.DeviceCreationFlags.None, new[] { FeatureLevel.Level_11_0 }, swapChainDesc, out _dx11Device, out _swapChain);
            _dx11DeviceContext = _dx11Device.ImmediateContext;
            //Игноровать все события видновс
            _factory = _swapChain.GetParent<Factory>();
            _factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);
            // Создаем буффер глубины
            depthBuffer = new DX11.Texture2D(_dx11Device, new DX11.Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = _renderForm.ClientSize.Width,
                Height = _renderForm.ClientSize.Height,
                SampleDescription = new SampleDescription(4, 4),
                Usage = DX11.ResourceUsage.Default,
                BindFlags = DX11.BindFlags.DepthStencil,
                CpuAccessFlags = DX11.CpuAccessFlags.None,
                OptionFlags = DX11.ResourceOptionFlags.None
            });
            // Создавем Отображение глубины
            depthView = new DX11.DepthStencilView(_dx11Device, depthBuffer);
            //Создаем цель куда будем рисовать
            using (DX11.Texture2D backBuffer = _swapChain.GetBackBuffer<DX11.Texture2D>(0))
            {
                _renderTargetView = new DX11.RenderTargetView(_dx11Device, backBuffer);
            }
            _viewport = new Viewport(0, 0, _Width, _Height);
            _dx11DeviceContext.OutputMerger.SetTargets(depthView, _renderTargetView);
            _dx11DeviceContext.Rasterizer.SetViewport(_viewport);
        }

        private void Update(double time)
        {
            
        }

        private void Draw()
        {
            _dx11DeviceContext.ClearDepthStencilView(depthView, DX11.DepthStencilClearFlags.Depth, 1.0f, 0);
            _dx11DeviceContext.ClearRenderTargetView(_renderTargetView, new SharpDX.Color(0, 0,128));
            //Рисование объектов 
            _waves.Draw();
            _swapChain.Present(1, PresentFlags.None);
        }
        

        private void InitInput()
        {
            _renderForm.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == System.Windows.Forms.Keys.Escape) _renderForm.Close();
            };
        }

        public void Dispose()
        {
            _waves.Dispose();
            _renderTargetView.Dispose();
            _swapChain.Dispose();
            _dx11Device.Dispose();
            _dx11DeviceContext.Dispose();
            _renderForm.Dispose();
            _factory.Dispose();
            depthBuffer.Dispose();
            depthView.Dispose();
        }

        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }
        double nextFrameTime = Environment.TickCount;
        private void RenderCallback()
        {          
            double lag = Environment.TickCount - nextFrameTime;
            if (lag > 30) { nextFrameTime = Environment.TickCount; Update(lag); }
            Draw();
        }
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