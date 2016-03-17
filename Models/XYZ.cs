
using SharpDX;

namespace SharpDX11GameByWinbringer.Models
{
    class XYZ : GameObject<ColoredVertex, Data>
    {
        public XYZ()
        {
            Verteces = new ColoredVertex[]
            {
                new ColoredVertex(new Vector3(0,0,0) ,new Vector4(1,1,1,1)),
                new ColoredVertex(new Vector3(500, 0, 0), new Vector4(1, 0, 0, 1)),
                new ColoredVertex(new Vector3(0, 500, 0), new Vector4(0, 1, 0, 1)),
                new ColoredVertex(new Vector3(0, 0, 500), new Vector4(0, 0, 1, 1))
            };
            Indeces = new uint[]
                {
                    0,1,
                    0,2,
                    0,3
                };
        }

        public override void Update(Matrix World, Matrix View, Matrix Proj)
        {
            ConstantBufferData.World = Matrix.Identity;
            ConstantBufferData.View = View;
            ConstantBufferData.Proj = Proj;
            ConstantBufferData.World.Transpose();
            ConstantBufferData.View.Transpose();
            ConstantBufferData.Proj.Transpose();
        }
    }
}
