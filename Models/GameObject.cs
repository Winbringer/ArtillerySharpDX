
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    public abstract class GameObject<V> : System.IDisposable where V : struct
    {
        V[] Verteces;
        uint[] Indeces;
        Buffer _triangleVertexBuffer;
        public Buffer _indexBuffer;
        public VertexBufferBinding _vertexBinging;
        public void InitBuffers(Device Device)
        {
            //Создаем буфферы для видеокарты
            _triangleVertexBuffer = Buffer.Create<V>(Device, BindFlags.VertexBuffer, Verteces);
            _indexBuffer = Buffer.Create(Device, BindFlags.IndexBuffer, Indeces);
            _vertexBinging = new VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<V>(), 0);
        }
        public void Dispose()
        {
            Utilities.Dispose(ref _triangleVertexBuffer);
            Utilities.Dispose(ref _indexBuffer);
        }
    }
}
