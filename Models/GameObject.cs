using SharpDX;

namespace SharpDX11GameByWinbringer.Models
{
    public abstract class GameObject<V, B>
    {
        public V[] Verteces;
        public B ConstantBufferData;
        public uint[] Indeces;
        public Matrix World;
        public abstract void Update(Matrix world, Matrix view, Matrix proj);
    }
}
