
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.ViewModels
{
   public class ViewModel<T> where T:struct
    {       
        public T ConstantBufferData { get; set; }
        public Buffer IndexBuffer { get; set; }
        public Buffer ConstantBuffer { get; set; }
        public VertexBufferBinding VertexBinging { get; set; }
        public int IndexCount { get; set; }                 
    }
}
