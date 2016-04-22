using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VictoremLibrary
{

    /// <summary>
    /// Класс для рисования объектов в буфеер свапчейна.
    /// </summary>
    public class DX11Drawer : System.IDisposable
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
            r.FillMode = SharpDX.Direct3D11.FillMode.Solid;
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

}
