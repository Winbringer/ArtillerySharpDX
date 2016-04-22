using SharpDX;
using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.D3DCompiler;

namespace VictoremLibrary
{

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
        public Shader(DeviceContext dC, string shadersFile, SharpDX.Direct3D11.InputElement[] inputElements, bool hasGeom = false, bool hasTes = false)
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

}
