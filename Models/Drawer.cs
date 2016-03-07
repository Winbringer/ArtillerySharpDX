using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDX11GameByWinbringer.Models
{
    class Drawer<T> : System.IDisposable where T : struct
    {
        private Buffer _triangleVertexBuffer;
        private Buffer _indexBuffer;
        private Buffer _constantBuffer;
        private Device _dx11Device;
        private DeviceContext _dx11DeviceContext;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private InputLayout _inputLayout;
        private int[] _indeces;
        private Vector3[] _vertices;
        T _world;
        PrimitiveTopology _pt;

        public Drawer(T constBufferData, Vector3[] vetexes, int[] indexes, string shadersFile, InputElement[] inputElements, Device dv, DeviceContext dvContext, PrimitiveTopology pt)
        {
            _indeces = indexes;
            _vertices = vetexes;
            _world = constBufferData;
            _dx11Device = dv;
            _dx11DeviceContext = dvContext;
            _pt = pt;
            //Создаем буфферы для видеокарты
            _triangleVertexBuffer = Buffer.Create<Vector3>(_dx11Device, BindFlags.VertexBuffer, _vertices);
            _indexBuffer = Buffer.Create(_dx11Device, BindFlags.IndexBuffer, _indeces);
            _constantBuffer = new Buffer(_dx11Device, Utilities.SizeOf<T>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            //Загружаем шейдеры из файлов
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "VS", "vs_4_0", ShaderFlags.Debug))
            {
                //Синатура храянящая сведения о том какие входные переменные есть у шейдера
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _vertexShader = new VertexShader(_dx11Device, vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "PS", "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new PixelShader(_dx11Device, pixelShaderByteCode);
            }
            //Создаем шаблон ввода данных для шейдера
            _inputLayout = new InputLayout(_dx11Device, _inputSignature, inputElements);
        }

        public void Draw()
        {
            //Обновляем данные в буфере переменных шейдера
            _dx11DeviceContext.UpdateSubresource(ref _world, _constantBuffer);
            //Установка шейдеров
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);           
            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = _pt;
            //Устанавливаем макет для входных данных видеокарты. В нем указано какие данные ожидает шейдер
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;
            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<Vector3>(), 0));
            _dx11DeviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);                     
            _dx11DeviceContext.VertexShader.SetConstantBuffer(0, _constantBuffer);
            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.DrawIndexed(_indeces.Length, 0, 0);
        }

        public void Dispose()
        {
            _triangleVertexBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
            _inputSignature.Dispose();
            _indexBuffer.Dispose();
            _constantBuffer.Dispose();
        }
    }
}
