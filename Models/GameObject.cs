using SharpDX;
using SharpDX.Direct3D11;
using System.Linq;

namespace SharpDX11GameByWinbringer.Models
{
    public abstract class GameObject<V, B> : System.IDisposable where V : struct where B : struct
    {
        private Buffer _triangleVertexBuffer;
        protected V[] Verteces;
        protected uint[] Indeces;
        public Matrix World;
        public B ConstantBufferData;
        public Buffer _indexBuffer;
        public Buffer _constantBuffer;
        public VertexBufferBinding _vertexBinging;
        public int IndexCount { get { return Indeces.Count(); } }
        protected void CreateBuffers(Device Device)
        {
            //Создаем буфферы для видеокарты
            _triangleVertexBuffer = Buffer.Create<V>(Device, BindFlags.VertexBuffer, Verteces);
            _indexBuffer = Buffer.Create(Device, BindFlags.IndexBuffer, Indeces);
            _constantBuffer = new Buffer(Device, Utilities.SizeOf<B>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, Utilities.SizeOf<B>());
            _vertexBinging = new VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<V>(), 0);
        }
        public abstract void Update(Matrix world, Matrix view, Matrix proj);

        public void Dispose()
        {
            Utilities.Dispose(ref _triangleVertexBuffer);
            Utilities.Dispose(ref _indexBuffer);
            Utilities.Dispose(ref _constantBuffer);
        }
    }
}
