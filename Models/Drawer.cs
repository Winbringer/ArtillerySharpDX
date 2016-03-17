using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace SharpDX11GameByWinbringer.Models
{
    /// <summary>
    /// Рисует 3D примитивы на экране
    /// </summary>   
    public class Drawer : System.IDisposable
    {
        private DeviceContext _dx11DeviceContext;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private InputLayout _inputLayout;
        private ShaderResourceView _textureResourse;
        //Параметры отображения
        private RasterizerState _rasterizerState = null;
        private BlendState _blendState = null;
        private SamplerState _samplerState = null;
        private DepthStencilState _DState = null;
        RawColor4? blendFactor =null;
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="shadersFile">Путь к файлу с шейдерами PS и VS</param>
        /// <param name="inputElements">Какие входные данные ожидает шейдер</param>
        /// <param name="dvContext">Контекст видеокарты</param>
        /// <param name="texture">Путь к текстуре</param>
        /// <param name="description">Самплер текстуры</param>
        public Drawer(
            string shadersFile,
            InputElement[] inputElements,
            DeviceContext dvContext,
            string texture,
            SamplerStateDescription description,
            DepthStencilStateDescription DStateDescripshion,
            RasterizerStateDescription rasterizerStateDescription,
            BlendStateDescription blendDescription,
             RawColor4? blendFactor = null)
        {

            _dx11DeviceContext = dvContext;
            this.blendFactor = blendFactor;
            //Загружаем шейдеры из файлов
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
            //Загружаем текстуру           
            _textureResourse = CreateTextureFromFile(texture);
            //Создание самплера для текстуры
            _samplerState = new SamplerState(_dx11DeviceContext.Device, description);
            _DState = new DepthStencilState(_dx11DeviceContext.Device, DStateDescripshion);
            _rasterizerState = new RasterizerState(_dx11DeviceContext.Device, rasterizerStateDescription);
            _blendState = new BlendState(_dx11DeviceContext.Device, blendDescription);

        }    
        /// <summary>
        /// Рисует наши примитивы на экран.
        /// </summary>
        /// <typeparam name="T">Тип данных передаваемых в констант буффер</typeparam>
        /// <param name="data">Данные для шейдера которые будут записаны в констант буффер</param>
        /// <param name="indexBuffer">Буффер с индексами</param>
        /// <param name="constantBuffer">Буффер с данными шейдера вроде мировой матрицы</param>
        /// <param name="vertexBufferBinding">Описание буффера индексов и его данные</param>
        /// <param name="indexCount">Количество индексов которые будем рисовать</param>
        /// <param name="PTolology">Тип рисуемых примитивов: линии, треугольники, точки</param>
        public void Draw<T>(T data, Buffer indexBuffer, Buffer constantBuffer, VertexBufferBinding vertexBufferBinding, int indexCount, PrimitiveTopology PTolology, bool isBlending=false) where T : struct
        {
            //Установка шейдеров
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);
            //Устанавливаем самплер текстуры для шейдера
            _dx11DeviceContext.PixelShader.SetSampler(0, _samplerState);
            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = PTolology;
            //Устанавливаем макет для входных данных видеокарты. В нем указано какие данные ожидает шейдер
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;
            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            _dx11DeviceContext.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
            _dx11DeviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            _dx11DeviceContext.UpdateSubresource(ref data, constantBuffer);           
            //Отправляем текстуру в шейдер
            _dx11DeviceContext.PixelShader.SetShaderResource(0, _textureResourse);
            _dx11DeviceContext.Rasterizer.State = _rasterizerState;
            _dx11DeviceContext.OutputMerger.DepthStencilState = _DState;
            _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            if(isBlending)_dx11DeviceContext.OutputMerger.SetBlendState(_blendState, blendFactor);
            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.DrawIndexed(indexCount, 0, 0);
        }
        /// <summary>
        /// Создает Ресурс текстуры для шейдера из картинки
        /// </summary>
        /// <param name="filename">Путь к картинке</param>
        /// <returns></returns>
        private ShaderResourceView CreateTextureFromFile(string filename)
        {
            ShaderResourceView SRV;
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(filename))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                // Определить и создать Texture2D.
                Texture2DDescription textureDesc = new Texture2DDescription()
                {
                    MipLevels = 1,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    Width = width,
                    Height = height,
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource,
                    Usage = ResourceUsage.Default,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
                };
                System.Drawing.Imaging.BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                DataRectangle dataRectangle = new DataRectangle(data.Scan0, data.Stride);
                using (var buffer = new Texture2D(_dx11DeviceContext.Device, textureDesc, dataRectangle))
                {
                    bitmap.UnlockBits(data);
                    SRV = new ShaderResourceView(_dx11DeviceContext.Device, buffer);
                }
            }
            return SRV;
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
                    Utilities.Dispose(ref _rasterizerState);
                    Utilities.Dispose(ref _blendState);
                    Utilities.Dispose(ref _DState);
                    Utilities.Dispose(ref _samplerState);
                    Utilities.Dispose(ref _textureResourse);
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

    }
}
