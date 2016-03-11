using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX;
using System.Collections.Generic;

namespace SharpDX11GameByWinbringer.Models
{
    /// <summary>
    /// Базовый класс для рисования объектов обязательно нужно вызать base.Dispose() в наследуемом классе.
    /// </summary>
    /// <typeparam name="V">Тип массива вершин</typeparam>
    /// <typeparam name="CB">Тип структуры передаваемой в костант буффер</typeparam>
    public abstract class DrawableGameObject<V, CB> : IDisposable where V : struct where CB : struct
    {
        public delegate void Drawing(CB data, Buffer indexBuffer, Buffer constantBuffer, VertexBufferBinding vertexBufferBinding, int indexCount, SharpDX.Direct3D.PrimitiveTopology PTolology);
        private Buffer _triangleVertexBuffer;
        protected Buffer _indexBuffer;
        protected Buffer _constantBuffer;
        protected VertexBufferBinding _vertexBinging;
        
        /// <summary>
        /// Создает буфферы через которые будут передаваться данные видеокарте.
        /// </summary>        
        /// <param name="Device">Объектное представление видеокарты D3D11</param>
        /// <param name="vertices">Массив описывающий вертексы</param>
        /// <param name="indeces">Массив индексов</param>
        protected void CreateBuffers(Device Device, V[] vertices, uint[] indeces)
        {
            //Создаем буфферы для видеокарты
            _triangleVertexBuffer = Buffer.Create<V>(Device, BindFlags.VertexBuffer, vertices);
            _indexBuffer = Buffer.Create(Device, BindFlags.IndexBuffer, indeces);
            _constantBuffer = new Buffer(Device, Utilities.SizeOf<CB>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, Utilities.SizeOf<CB>());
            _vertexBinging = new VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<V>(), 0);
        }

        public abstract void Draw();
        public virtual void Dispose()
        {        
            Utilities.Dispose(ref _indexBuffer);
            Utilities.Dispose(ref _constantBuffer);
            Utilities.Dispose(ref _triangleVertexBuffer);
        }
    }
}
