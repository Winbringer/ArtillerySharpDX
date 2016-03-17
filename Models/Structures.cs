using SharpDX;
using System.Runtime.InteropServices;

namespace SharpDX11GameByWinbringer.Models
{
    [StructLayout(LayoutKind.Explicit, Size = (64 * 3) + 16)]
    struct DataT
    {
        [FieldOffset(0)]
        public Matrix World;
        [FieldOffset(sizeof(float)*4*4)]
        public Matrix View;
        [FieldOffset((sizeof(float) * 4 * 4)*2)]
        public Matrix Proj;
        [FieldOffset((sizeof(float) * 4 * 4) * 3)]
        public Vector4 Time;           
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Data
    {   public Matrix World;
        public Matrix View;
        public Matrix Proj;        
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vertex(Vector3 position, Vector2 textureUV)
        {
            Position = position;
            TextureUV = textureUV;
        }
        public Vector3 Position;
        public Vector2 TextureUV;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ColoredVertex
    {
        public ColoredVertex(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }
        public Vector3 Position;
        public Vector4 Color;
    }

}
