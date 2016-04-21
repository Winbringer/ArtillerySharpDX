using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SharpDX;
using SharpDX.DirectInput;
using SharpDX.Windows;
using SharpDX.Mathematics;
using SharpDX.Mathematics.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DXGI;
using System.Windows.Forms;
using SharpDX.Direct3D;

namespace VictoremLibrary
{
    /// <summary>
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
        /// <summary>
        /// Происходит при нажатии клавиатуры. Тип данных передоваемых в переменную e - UpdateArgs.
        /// </summary>
        public event EventHandler OnKeyPressed = null;
        /// <summary>
        /// Вызываеться при обновлении логики игры.Тип данных передоваемых в переменную  e - UpdateArgs.
        /// </summary>
        public event EventHandler OnUpdate = null;
        /// <summary>
        /// Вызываеться при рендеринге игры
        /// </summary>
        public event EventHandler OnDraw = null;

        Factory _factory;
        //Форма куда будем вставлять наше представление renderTargetView.
        private RenderForm _renderForm = null;
        //Объектное представление нашей видеокарты
        private Device _dx11Device = null;
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

        //Свойства
        public float ViewRatio { get; set; }
        public DeviceContext DeviceContext { get { return _dx11DeviceContext; } }
        public SharpDX.Windows.RenderForm Form { get { return _renderForm; } }
        public SwapChain SwapChain { get { return _swapChain; } }
        public int Width { get { return _renderForm.ClientSize.Width; } }
        public int Height { get { return _renderForm.ClientSize.Height; } }
        public Color Color { get; set; }
        public DX11Drawer Drawer { get { return _drawer; } }

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
        }

        /// <summary>
        /// Инициализирует объекты связанные с графическим устройство - Девайс его контекст и Свапчейн
        /// </summary>
        private void InitializeDeviceResources()
        {
            //Создаем объектное преставление нашего GPU, его контекст и класс который будет менят местами буфферы в которые рисует наша GPU
            Device.CreateWithSwapChain(
                 SharpDX.Direct3D.DriverType.Hardware,
                 DeviceCreationFlags.None | DeviceCreationFlags.BgraSupport,
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
            _factory = _swapChain.GetParent<Factory>();
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

        private void Update(double time)
        {
            var m = _keyboard.GetCurrentState();
            if (m.PressedKeys.Count > 0)
                OnKeyPressed?.Invoke(this, new UpdateArgs() { Time = (float)time, KeyboardState = m });
            OnUpdate?.Invoke(this, new UpdateArgs() { Time = (float)time });
        }

        private void Draw()
        {
            _dx11DeviceContext.ClearRenderTargetView(_renderView, Color);
            _dx11DeviceContext.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            OnDraw?.Invoke(this, new EventArgs());
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

        /// <summary>
        /// Создает форму в которую будет происходить рендеринг. На ней нужно обязательно вызвать метод Dispose. Форма закрываеть при нажатии Esc. Форма создаеться по размеру экрана и в его центре.
        /// </summary>
        /// <param name="Text">Текст в заголовке формы</param>
        /// <param name="IconFile">Файл в формает .ico для инконки в заголовке формы</param>
        /// <returns></returns>
        public static RenderForm GetRenderForm(string Text, string IconFile)
        {
            if (!Device.IsSupportedFeatureLevel(FeatureLevel.Level_11_0))
            {
                MessageBox.Show("Для запуска нужен DirectX 11 ОБЯЗАТЕЛЬНО!");
                return null;
            }
#if DEBUG
            SharpDX.Configuration.EnableObjectTracking = true;
#endif
            var _renderForm = new RenderForm(Text)
            {
                AllowUserResizing = false,
                IsFullscreen = false,
                StartPosition = FormStartPosition.CenterScreen,
                ClientSize = new System.Drawing.Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height),
                FormBorderStyle = FormBorderStyle.None,
                Icon = new System.Drawing.Icon(IconFile)
            };

            _renderForm.Shown += (sender, e) => { _renderForm.Activate(); };
            _renderForm.KeyDown += (sender, e) => { if (e.KeyCode == Keys.Escape) _renderForm.Close(); };
            return _renderForm;
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
        }

    }

    /// <summary>
    /// Класс для работы с шейдерами
    /// </summary>
    public class Shader : IDisposable
    {
        private DomainShader _DShader = null;
        private DeviceContext _dx11DeviceContext;
        private GeometryShader _GShader = null;
        private HullShader _HShader = null;
        private ShaderSignature _inputSignature = null;
        private PixelShader _pixelShader = null;
        private VertexShader _vertexShader = null;
        private InputLayout _inputLayout = null;

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="dC">Контекст Директ икс 12</param>
        /// <param name="shadersFile">Путь к файлу в которм описанный шейдеры. Назвалине функций шейредов должно быть VS, PS, GS, HS и DS соответственно.</param>
        ///<param name="inputElements">Входные элементы для Вертексного шейдера</param>
        /// <param name="hasGeom">Используеться ли Геометри шейдер GS</param>
        /// <param name="hasTes">Использовать ли Хулл HS и Домейн DS шейдеры необходимые для тесселяции</param>       
        public Shader(DeviceContext dC, string shadersFile, InputElement[] inputElements, bool hasGeom = false, bool hasTes = false)
        {
            _dx11DeviceContext = dC;
            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif

            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "VS", "vs_5_0", shaderFlags))
            {
                //Синатура храянящая сведения о том какие входные переменные есть у шейдера
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _vertexShader = new VertexShader(_dx11DeviceContext.Device, vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "PS", "ps_5_0", shaderFlags))
            {
                _pixelShader = new PixelShader(_dx11DeviceContext.Device, pixelShaderByteCode);
            }

            if (hasTes)
            {
                using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "HS", "hs_5_0", shaderFlags))
                {
                    _HShader = new HullShader(_dx11DeviceContext.Device, pixelShaderByteCode);
                }
                using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "DS", "ds_5_0", shaderFlags))
                {
                    _DShader = new DomainShader(_dx11DeviceContext.Device, pixelShaderByteCode);
                }
            }

            if (hasGeom)
            {
                using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "GS", "gs_5_0", shaderFlags))
                {
                    _GShader = new GeometryShader(_dx11DeviceContext.Device, pixelShaderByteCode);
                }
            }

            _inputLayout = new InputLayout(_dx11DeviceContext.Device, _inputSignature, inputElements);
        }

        /// <summary>
        /// Устанавливает шейдеры и входные данные для них.
        /// </summary>
        /// <param name="sDesc">Самплеры для текстур</param>
        /// <param name="sResource">Текстуры шейдера</param>
        /// <param name="constBuffer">Буффер констант шейдера</param>
        public void Begin(SamplerState[] sDesc = null, ShaderResourceView[] sResource = null, Buffer[] constBuffer = null)
        {
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);
            _dx11DeviceContext.GeometryShader.Set(_GShader);
            _dx11DeviceContext.HullShader.Set(_HShader);
            _dx11DeviceContext.DomainShader.Set(_DShader);

            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;

            if (sDesc != null)
                for (int i = 0; i < sDesc.Length; ++i)
                {
                    _dx11DeviceContext.VertexShader.SetSampler(i, sDesc[i]);
                    _dx11DeviceContext.PixelShader.SetSampler(i, sDesc[i]);
                    _dx11DeviceContext.GeometryShader.SetSampler(i, sDesc[i]);
                    _dx11DeviceContext.HullShader.SetSampler(i, sDesc[i]);
                    _dx11DeviceContext.DomainShader.SetSampler(i, sDesc[i]);
                }

            if (constBuffer != null)
                for (int i = 0; i < constBuffer.Length; ++i)
                {
                    _dx11DeviceContext.VertexShader.SetConstantBuffer(i, constBuffer[i]);
                    _dx11DeviceContext.PixelShader.SetConstantBuffer(i, constBuffer[i]);
                    _dx11DeviceContext.GeometryShader.SetConstantBuffer(i, constBuffer[i]);
                    _dx11DeviceContext.DomainShader.SetConstantBuffer(i, constBuffer[i]);
                    _dx11DeviceContext.HullShader.SetConstantBuffer(i, constBuffer[i]);
                }
            if (sResource != null)
                for (int i = 0; i < sResource.Length; ++i)
                {
                    _dx11DeviceContext.VertexShader.SetShaderResources(0, sResource);
                    _dx11DeviceContext.PixelShader.SetShaderResources(0, sResource);
                    _dx11DeviceContext.GeometryShader.SetShaderResources(0, sResource);
                    _dx11DeviceContext.DomainShader.SetShaderResources(0, sResource);
                    _dx11DeviceContext.HullShader.SetShaderResources(0, sResource);
                }
        }

        /// <summary>
        /// Отключает шейдер.
        /// </summary>
        public void End()
        {
            _dx11DeviceContext.VertexShader.Set(null);
            _dx11DeviceContext.PixelShader.Set(null);
            _dx11DeviceContext.GeometryShader.Set(null);
            _dx11DeviceContext.HullShader.Set(null);
            _dx11DeviceContext.DomainShader.Set(null);
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            Utilities.Dispose(ref _DShader);
            Utilities.Dispose(ref _GShader);
            Utilities.Dispose(ref _DShader);
            Utilities.Dispose(ref _HShader);
            Utilities.Dispose(ref _inputSignature);
            Utilities.Dispose(ref _pixelShader);
            Utilities.Dispose(ref _vertexShader);
            Utilities.Dispose(ref _inputLayout);

        }

    }

    /// <summary>
    /// Класс для рисования объектов в буфеер свапчейна.
    /// </summary>
    public class DX11Drawer : IDisposable
    {
        #region Поля       
        private DeviceContext _dx11DeviceContext;
        //Параметры отображения
        private RasterizerState _rasterizerState = null;
        private BlendState _blendState = null;
        private DepthStencilState _DState = null;
        #endregion

        #region Свойства
        public RawColor4? BlendFactor { get; set; } = null;
        public DepthStencilStateDescription DepthStencilDescripshion { set { Utilities.Dispose(ref _DState); _DState = new DepthStencilState(_dx11DeviceContext.Device, value); } }
        public RasterizerStateDescription RasterizerDescription { set { Utilities.Dispose(ref _rasterizerState); _rasterizerState = new RasterizerState(_dx11DeviceContext.Device, value); } }
        public BlendStateDescription BlendDescription { set { Utilities.Dispose(ref _blendState); _blendState = new BlendState(_dx11DeviceContext.Device, value); } }
        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="dvContext">Контекст видеокарты</param>
        public DX11Drawer(DeviceContext dvContext)
        {
            _dx11DeviceContext = dvContext;
            var d = DepthStencilStateDescription.Default();
            d.IsDepthEnabled = true;
            d.IsStencilEnabled = false;
            DepthStencilDescripshion = d;
            var r = RasterizerStateDescription.Default();
            r.CullMode = CullMode.None;
            r.FillMode = FillMode.Solid;
            RasterizerDescription = r;
            var b = BlendStateDescription.Default();
            b.AlphaToCoverageEnable = new RawBool(true);
            BlendDescription = b;
        }

        #region Методы

        /// <summary>
        /// Рисует проиндексированные вертексы в буффер свапчейна
        /// </summary>
        /// <param name="vertexBinging">Биндинг с буффером наших вертексов</param>
        /// <param name="indexBuffer">Буффер индексов</param>
        /// <param name="indexCount">Количество индексов которые нужно нарисовать</param>
        /// <param name="primitiveTopology">Топология примитивов т.е. что нужно нарисовать</param>
        /// <param name="isBlending">Используеться ли блендинг</param>
        /// <param name="startIndex">Индеск с которого начинаеться отрисовка</param>
        /// <param name="startVetex">Вертекс с которого начинаеться отриссовка</param>
        public void DrawIndexed(VertexBufferBinding vertexBinging, Buffer indexBuffer, int indexCount, PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList, bool isBlending = false, int startIndex = 0, int startVetex = 0)
        {
            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = primitiveTopology;

            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, vertexBinging);
            _dx11DeviceContext.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

            _dx11DeviceContext.Rasterizer.State = _rasterizerState;
            _dx11DeviceContext.OutputMerger.DepthStencilState = _DState;

            _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            if (isBlending) _dx11DeviceContext.OutputMerger.SetBlendState(_blendState, BlendFactor);

            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.DrawIndexed(indexCount, startIndex, startVetex);
        }

        /// <summary>
        /// Рисует не проиндексированные вертексы в буффер свапчейна
        /// </summary>
        /// <param name="vertexBinging">Биндинг с буффером наших вертексов</param>
        /// <param name="vertexCount">Количество вертексов которые нужно нарисовать</param>
        /// <param name="primitiveTopology">Топология примитивов т.е. что нужно нарисовать</param>
        /// <param name="isBlending">Используеться ли блендинг</param>
        /// <param name="startVetex">Вертекс с которого начинаеться отриссовка</param>
        public void Draw(VertexBufferBinding vertexBinging, int vertexCount, PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList, bool isBlending = false, int startVetex = 0)
        {
            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = primitiveTopology;

            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, vertexBinging);

            _dx11DeviceContext.Rasterizer.State = _rasterizerState;
            _dx11DeviceContext.OutputMerger.DepthStencilState = _DState;

            _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            if (isBlending) _dx11DeviceContext.OutputMerger.SetBlendState(_blendState, BlendFactor);

            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.Draw(vertexCount, startVetex);
        }


        public void Dispose()
        {
            Utilities.Dispose(ref _rasterizerState);
            Utilities.Dispose(ref _blendState);
            Utilities.Dispose(ref _DState);
        }

        #endregion
    }

    /// <summary>
    /// Базовый класс для 3D объектов. Перед рисованием обязательно вызвать метод InitBuffers.
    /// </summary>
    /// <typeparam name="V"> Тип Вертексов для буффера вершин</typeparam>
    abstract public class Component<V> : System.IDisposable where V : struct
    {
        public Matrix ObjWorld = Matrix.Identity;
        protected Buffer _indexBuffer;
        protected Buffer _vertexBuffer;
        protected VertexBufferBinding _vertexBinding;
        protected V[] _veteces;
        protected uint[] _indeces;

        public Buffer IndexBuffer { get { return _indexBuffer; } }
        public VertexBufferBinding VertexBinding { get { return _vertexBinding; } }

        /// <summary>
        /// Создает буфферы Вершин и индексов.
        /// </summary>
        /// <param name="dv">Устройстов в контексте которого происходит рендеринг</param>
        protected virtual void InitBuffers(Device dv)
        {
            _indexBuffer = Buffer.Create(dv, BindFlags.IndexBuffer, _indeces);
            _vertexBuffer = Buffer.Create(dv, BindFlags.VertexBuffer, _veteces);
            _vertexBinding = new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<V>(), 0);
        }

        public virtual void Dispose()
        {
            Utilities.Dispose(ref _indexBuffer);
            Utilities.Dispose(ref _vertexBuffer);
        }
    }

}
