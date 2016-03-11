using SharpDX;
using System.Runtime.InteropServices;

namespace SharpDX11GameByWinbringer.Models
{   
    [StructLayout(LayoutKind.Sequential)]
    struct Data
    {
        public Matrix World;
        public Matrix View;
        public Matrix Proj;        
        public Vector4 Time;            
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

}
