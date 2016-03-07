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
        //Форма куда будем вставлять наше представление renderTargetView.
        private RenderForm _renderForm;
        private readonly int _Width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        private readonly int _Height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
        //Таймер который будет счиать время
        private static readonly GameTimer _timer = new GameTimer();
        //Объектное представление нашей видеокарты
        private DX11.Device _dx11Device;
        private DX11.DeviceContext _dx11DeviceContext;
        //Цепочка замены заднего и отображаемого буфера
        private SwapChain _swapChain;
        //Представление куда мы выводим картинку.
        private DX11.RenderTargetView _renderTargetView;
        //Координатная сетка 
        private Viewport _viewport;
        Wave _waves;
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
            _waves = new Wave(_dx11Device, _dx11DeviceContext, ratio);

        }

        private void InitializeDeviceResources()
        {
            ModeDescription backBufferDesc = new ModeDescription(_Width, _Height, new Rational(60, 1), Format.R8G8B8A8_UNorm);
            SwapChainDescription swapChainDesc = new SwapChainDescription()
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 2,
                OutputHandle = _renderForm.Handle,
                IsWindowed = true,
                SwapEffect = SwapEffect.Discard
            };
            //Создаем Девайс, Цепочку обмена и Девайс контекст
            DX11.Device.CreateWithSwapChain(DriverType.Hardware, DX11.DeviceCreationFlags.None, swapChainDesc, out _dx11Device, out _swapChain);
            _dx11DeviceContext = _dx11Device.ImmediateContext;
            //Игноровать все события видновс
            var factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);

            using (DX11.Texture2D backBuffer = _swapChain.GetBackBuffer<DX11.Texture2D>(0))
            {
                _renderTargetView = new DX11.RenderTargetView(_dx11Device, backBuffer);
            }
            _viewport = new Viewport(0, 0, _Width, _Height);
            _dx11DeviceContext.OutputMerger.SetRenderTargets(_renderTargetView);
            _dx11DeviceContext.Rasterizer.SetViewport(_viewport);
        }
        private void Draw(double time)
        {
            _dx11DeviceContext.ClearRenderTargetView(_renderTargetView, new SharpDX.Color(32, 103, 178));
            //Рисование объектов 
            _waves.Draw();
            _swapChain.Present(0, PresentFlags.None);
        }
        private void Update(double time)
        {


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
        }

        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }

        private void RenderCallback()
        {
            _timer.Tick();
            Update(_timer.DeltaTime);
            Draw(_timer.DeltaTime);

        }
    }
}
