using SharpDX;
using System.Runtime.InteropServices;

namespace SharpDX11GameByWinbringer.Models
{
    [StructLayout(LayoutKind.Sequential,Pack =1)]
  public  struct DataT
    {
        
        public Matrix WVP;        
        public Vector4 Time;           
    }

    [StructLayout(LayoutKind.Sequential,Pack =1)]
  public  struct Data
    {
        public Matrix WVP;              
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
