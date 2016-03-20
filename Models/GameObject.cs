using SharpDX;
using SharpDX.Direct3D11;
using SharpDX11GameByWinbringer.ViewModels;
using System.Linq;

namespace SharpDX11GameByWinbringer.Models
{
    public abstract class GameObject<V, B> : System.IDisposable where V : struct where B : struct
    {
        private Buffer _triangleVertexBuffer;
        protected V[] Verteces;
        protected uint[] Indeces;
        protected Matrix World;
        protected B ConstantBufferData;
        protected Buffer _indexBuffer;
        protected Buffer _constantBuffer;
        protected VertexBufferBinding _vertexBinging;
        Device _device;
        protected int IndexCount { get { return Indeces.Count(); } }
        protected void CreateBuffers(Device Device)
        {
            _device = Device;
            //Создаем буфферы для видеокарты
            _triangleVertexBuffer = Buffer.Create<V>(Device, BindFlags.VertexBuffer, Verteces);
            _indexBuffer = Buffer.Create(Device, BindFlags.IndexBuffer, Indeces);
            _constantBuffer = new Buffer(Device, Utilities.SizeOf<B>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, Utilities.SizeOf<B>());
            _vertexBinging = new VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<V>(), 0);
        }
        public void FillViewModel( ViewModel<B> VM)
        {
            _device.ImmediateContext.UpdateSubresource(ref ConstantBufferData, _constantBuffer);    
            VM.ConstantBuffer = _constantBuffer;
            VM.IndexBuffer = _indexBuffer;
            VM.IndexCount = IndexCount;
            VM.VertexBinging = _vertexBinging;
        }
        public abstract void Update(Matrix world, Matrix view, Matrix proj);

        public void Dispose()
        {
            _vertexBinging.Buffer.Dispose();
            Utilities.Dispose(ref _triangleVertexBuffer);
            Utilities.Dispose(ref _indexBuffer);
            Utilities.Dispose(ref _constantBuffer);
        }
    }
}
