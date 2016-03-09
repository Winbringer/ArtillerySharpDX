﻿using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Linq;

namespace SharpDX11GameByWinbringer.Models
{
    class Drawer<T,T1> : System.IDisposable where T1 : struct where T : struct
    {
        private Buffer _triangleVertexBuffer;
        private Buffer _indexBuffer;
        private Buffer _constantBuffer;
        private DeviceContext _dx11DeviceContext;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private InputLayout _inputLayout;
        private ShaderResourceView _textureResourse;
        private SamplerState _samplerState = null;
        //Используемые данные
        private int[] _indeces;
        private T1[] _vertices;      
        /// <summary>
        /// Задает тип рисуемых примитивов
        /// </summary>
        public PrimitiveTopology PTolology { get; set; }

        public Drawer(T1[] vetexes, int[] indexes, string shadersFile, InputElement[] inputElements, DeviceContext dvContext, string texture, SamplerStateDescription description)
        {
            _indeces = indexes;
            _vertices = vetexes;         
            _dx11DeviceContext = dvContext;
            PTolology = PrimitiveTopology.TriangleList;
            //Создаем буфферы для видеокарты
            _triangleVertexBuffer = Buffer.Create<T1>(_dx11DeviceContext.Device, BindFlags.VertexBuffer, _vertices);
            _indexBuffer = Buffer.Create(_dx11DeviceContext.Device, BindFlags.IndexBuffer, _indeces);
            int size = Utilities.SizeOf<T>();
            _constantBuffer = new Buffer(_dx11DeviceContext.Device, size, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            //Загружаем шейдеры из файлов
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "VS", "vs_5_0", ShaderFlags.Debug))
            {
                //Синатура храянящая сведения о том какие входные переменные есть у шейдера
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _vertexShader = new VertexShader(_dx11DeviceContext.Device, vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "PS", "ps_5_0", ShaderFlags.Debug))
            {
                _pixelShader = new PixelShader(_dx11DeviceContext.Device, pixelShaderByteCode);
            }
            //Создаем шаблон ввода данных для шейдера
            _inputLayout = new InputLayout(_dx11DeviceContext.Device, _inputSignature, inputElements);
            //Загружаем текстуру           
            _textureResourse = CreateTextureFromFile(texture);
            //Создание самплера для текстуры
            _samplerState = new SamplerState(_dx11DeviceContext.Device, description);           
        }

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

        public void Draw(T shaderData) 
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
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<T1>(), 0));
            _dx11DeviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
            //Обновляем данные в буфере переменных шейдера
            _dx11DeviceContext.UpdateSubresource(ref shaderData, _constantBuffer);
            _dx11DeviceContext.VertexShader.SetConstantBuffer(0, _constantBuffer);
            //Отправляем текстуру в шейдер
            _dx11DeviceContext.PixelShader.SetShaderResource(0, _textureResourse);           
            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.DrawIndexed(_indeces.Count(), 0, 0);
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
                    _samplerState.Dispose();
                    _textureResourse.Dispose();
                    _triangleVertexBuffer.Dispose();
                    _vertexShader.Dispose();
                    _pixelShader.Dispose();
                    _inputLayout.Dispose();
                    _inputSignature.Dispose();
                    _indexBuffer.Dispose();
                    _constantBuffer.Dispose();
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
