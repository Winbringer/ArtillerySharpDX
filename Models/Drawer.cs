using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDX11GameByWinbringer.ViewModels;

namespace SharpDX11GameByWinbringer.Models
{
    /// <summary>
    /// Рисует 3D примитивы на экране
    /// </summary>   
    public sealed class Drawer : System.IDisposable
    {
        #region Поля и свойства

        private DeviceContext _dx11DeviceContext;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private InputLayout _inputLayout;
        //Параметры отображения
        private RasterizerState _rasterizerState = null;
        private BlendState _blendState = null;
        private SamplerState _samplerState = null;
        private DepthStencilState _DState = null;

        public RawColor4? BlendFactor { get; set; } = null;

        public SamplerStateDescription Samplerdescription { set { _samplerState = new SamplerState(_dx11DeviceContext.Device, value); } }
        public DepthStencilStateDescription DepthStencilDescripshion { set { _DState = new DepthStencilState(_dx11DeviceContext.Device, value); } }
        public RasterizerStateDescription RasterizerDescription { set { _rasterizerState = new RasterizerState(_dx11DeviceContext.Device, value); } }
        public BlendStateDescription BlendDescription { set { _blendState = new BlendState(_dx11DeviceContext.Device, value); } }

        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="shadersFile">Путь к файлу с шейдерами PS и VS</param>
        /// <param name="inputElements">Какие входные данные ожидает шейдер</param>
        /// <param name="dvContext">Контекст видеокарты</param>
        /// <param name="texture">Путь к текстуре</param>
        public Drawer(string shadersFile,
                      InputElement[] inputElements,
                      DeviceContext dvContext)
        {

            _dx11DeviceContext = dvContext;

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

            //Создаем шаблон ввода данных для шейдера
            _inputLayout = new InputLayout(_dx11DeviceContext.Device, _inputSignature, inputElements);           
            Samplerdescription = SamplerStateDescription.Default();
            DepthStencilDescripshion = DepthStencilStateDescription.Default();
            RasterizerDescription = RasterizerStateDescription.Default();
            BlendDescription = BlendStateDescription.Default();

        }

        #region Методы

        public void Draw(ViewModel VM, PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList, bool isBlending = false, int startIndex = 0, int baseVetex = 0)
        {
            //Установка шейдеров
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);

            //Устанавливаем самплер текстуры для шейдера
            _dx11DeviceContext.PixelShader.SetSampler(0, _samplerState);

            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = primitiveTopology;

            //Устанавливаем макет для входных данных видеокарты. В нем указано какие данные ожидает шейдер
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;

            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, VM.VertexBinging);
            _dx11DeviceContext.InputAssembler.SetIndexBuffer(VM.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

            if (VM.ConstantBuffers != null)
                for (int i = 0; i < VM.ConstantBuffers.Length; ++i)
                {
                    _dx11DeviceContext.VertexShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                    _dx11DeviceContext.PixelShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                }
            if (VM.Textures != null)
                for (int i = 0; i < VM.Textures.Length; ++i)
                {
                    //Отправляем текстуру в шейдер
                    _dx11DeviceContext.PixelShader.SetShaderResource(i, VM.Textures?[i]);
                }

            _dx11DeviceContext.Rasterizer.State = _rasterizerState;
            _dx11DeviceContext.OutputMerger.DepthStencilState = _DState;

            _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            if (isBlending) _dx11DeviceContext.OutputMerger.SetBlendState(_blendState, BlendFactor);

            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.DrawIndexed(VM.IndexCount, startIndex, baseVetex);
        }


        #region IDisposable Support
        private bool disposedValue = false;
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).                   
                    Utilities.Dispose(ref _rasterizerState);
                    Utilities.Dispose(ref _blendState);
                    Utilities.Dispose(ref _DState);
                    Utilities.Dispose(ref _samplerState);
                    Utilities.Dispose(ref _vertexShader);
                    Utilities.Dispose(ref _pixelShader);
                    Utilities.Dispose(ref _inputLayout);
                    Utilities.Dispose(ref _inputSignature);

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

        #endregion
    }
}
