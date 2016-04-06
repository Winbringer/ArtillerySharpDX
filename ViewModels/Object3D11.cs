using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.ViewModels
{
   abstract public class Object3D11:System.IDisposable
    {
        public Buffer _indexBuffer;
        protected Buffer _vertexBuffer;
        public InputElement[] _inputElements;
        public VertexBufferBinding _vertexBinding;
        public Matrix ObjWorld;

        public void Dispose()
        {
            Utilities.Dispose(ref _indexBuffer);
            Utilities.Dispose(ref _vertexBuffer);
        }
    }
}
