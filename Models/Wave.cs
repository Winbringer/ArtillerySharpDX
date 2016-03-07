
using SharpDX;
using DX11 = SharpDX.Direct3D11;
using System;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.DXGI;

namespace SharpDX11GameByWinbringer.Models
{
    class Wave : IDisposable
    {
        private Vector3[] _vertices;
        private DX11.Buffer _triangleVertexBuffer;
        private DX11.Buffer _indexBuffer;
        private DX11.Buffer _constantBuffer;
        private DX11.Device _dx11Device;
        private DX11.DeviceContext _dx11DeviceContext;
        private DX11.VertexShader _vertexShader;
        private DX11.PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private DX11.InputLayout _inputLayout;
        int[] _indeces;
        Color world = new Color(1f,0,1f,1f);

        public Wave(DX11.Device dv, DX11.DeviceContext dc)
        {
            this._dx11Device = dv;
            _dx11DeviceContext = dc;
            InitializeTriangle();
            InitializeShaders();

        }

        public void Draw()
        {
            SetResourses();      
            _dx11DeviceContext.DrawIndexed(_indeces.Length,0,0);
        }
        public void Update()
        {
            
        }
        private void InitializeTriangle()
        {
            int N = 2;
            //Создание верщин
            _vertices = new Vector3[N * N];
            float delta = 1f / (N - 1);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    int index = i * N + j;
                    _vertices[index] = new Vector3(delta * i, 0, -delta * j);
                }
            }
            _triangleVertexBuffer = DX11.Buffer.Create<Vector3>(_dx11Device, DX11.BindFlags.VertexBuffer, _vertices);
            //Создание индексов
            _indeces = new int[(N - 1) * (N - 1) * 6];
            uint counter = 0;
            for (int z = 0; z < (N - 1); z++)
            {
                for (int X = 0; X < (N - 1); X++)
                {
                    int lowerLeft = (z * N + X);
                    int lowerRight = lowerLeft + 1;
                    int upperLeft = lowerLeft + N;
                    int upperRight = upperLeft + 1;
                    _indeces[counter++] = lowerLeft;
                    _indeces[counter++] = upperLeft;
                    _indeces[counter++] = upperRight;
                    _indeces[counter++] = lowerLeft;
                    _indeces[counter++] = upperRight;
                    _indeces[counter++] = lowerRight;
                }
            }
            _indexBuffer = DX11.Buffer.Create(_dx11Device, DX11.BindFlags.IndexBuffer, _indeces);
            //Создание буфера для передачи данных в шейдеры
            _constantBuffer = new DX11.Buffer(_dx11Device, Utilities.SizeOf<Color4>(), DX11.ResourceUsage.Default, DX11.BindFlags.ConstantBuffer, DX11.CpuAccessFlags.None, DX11.ResourceOptionFlags.None, 0);
        }

        private void InitializeShaders()
        {
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Shaders\\Shader.hlsl", "VS", "vs_4_0", ShaderFlags.Debug))
            {
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _vertexShader = new DX11.VertexShader(_dx11Device, vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders\\Shader.hlsl", "PS", "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new DX11.PixelShader(_dx11Device, pixelShaderByteCode);
            }
            DX11.InputElement[] inputElements = new DX11.InputElement[]
            {
                new DX11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0)
            };            
            _inputLayout = new DX11.InputLayout(_dx11Device, _inputSignature, inputElements);           
        }
        private void SetResourses()
        {  
            //Обновляем данные в буфере переменных шейдера
            _dx11DeviceContext.UpdateSubresource(ref world, _constantBuffer);           
            //Установка шейдеров
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);
            //Установка буфера через который мы будем передавать значения для глобальных переменных шейдера.           
            _dx11DeviceContext.VertexShader.SetConstantBuffer(0, _constantBuffer);
            _dx11DeviceContext.PixelShader.SetConstantBuffer(0, _constantBuffer);
            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            //Устанавливаем макет для входных данных видеокарты. В нем указано какие данные ожидает шейдер
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;
            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, new DX11.VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<Vector3>(), 0));
            _dx11DeviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);  
        }
        public void Dispose()
        {
            _triangleVertexBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
            _inputSignature.Dispose();
        }
    }
}
