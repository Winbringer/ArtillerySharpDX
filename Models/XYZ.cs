
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    sealed class XYZ : GameObject<ColoredVertex, Data>
    {
        public XYZ(Device device)
        {
            Verteces = new ColoredVertex[]
            {
                new ColoredVertex(new Vector3(0,0,0) ,new Vector4(1,1,1,1)),
                new ColoredVertex(new Vector3(400, 0, 0), new Vector4(1, 0, 0, 1)),
                new ColoredVertex(new Vector3(0, 400, 0), new Vector4(0, 1, 0, 1)),
                new ColoredVertex(new Vector3(0, 0, 400), new Vector4(0, 0, 1, 1))
            };
            Indeces = new uint[]
                {
                    0,1,
                    0,2,
                    0,3
                };
            CreateBuffers(device);
        }

        public override void Update(Matrix World, Matrix View, Matrix Proj)
        {
            ConstantBufferData.WVP = Matrix.Identity * World * View * Proj;
            ConstantBufferData.WVP.Transpose();
        }
    }
}
