
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
        private DX11.Device _dx11Device;
        private DX11.DeviceContext _dx11DeviceContext;
        private DX11.VertexShader _vertexShader;
        private DX11.PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private DX11.InputLayout _inputLayout;

        public Wave(DX11.Device dv, DX11.DeviceContext dc)
        {
            this._dx11Device = dv;
            _dx11DeviceContext = dc;
            InitializeTriangle();
            InitializeShaders();      
          
        }

        public void Draw()
        {
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, new DX11.VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<Vector3>(), 0));
            _dx11DeviceContext.Draw(_vertices.Length, 0);
        }
        private void InitializeTriangle()
        {
            _vertices = new Vector3[] { new Vector3(-0.5f, 0.5f, 0.0f), new Vector3(0.5f, 0.5f, 0.0f), new Vector3(0.0f, -0.5f, 0.0f) };
            _triangleVertexBuffer = DX11.Buffer.Create<Vector3>(_dx11Device, DX11.BindFlags.VertexBuffer, _vertices);
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
            DX11.InputElement[] inputElements = new DX11.InputElement[] { new DX11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0) };
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _inputLayout = new DX11.InputLayout(_dx11Device, _inputSignature, inputElements);
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;
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
