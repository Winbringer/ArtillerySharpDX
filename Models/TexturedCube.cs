using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX11GameByWinbringer.Models;
using SharpDX;

namespace SharpDX11GameByWinbringer.Models
{
    class TexturedCube : DrawableGameObject<Vertex, Data>
    {        
        Vertex[] verts;
        uint[] textureIndices;
        private Data _data;
        float size = 100;
        public TexturedCube(SharpDX.Direct3D11.Device device)
        {
            textureIndices =new uint[]
                {
                0,1,2, // передняя сторона
                2,3,0,

                6,5,4, // задняя сторона
                4,7,6,

                4,0,3, // левый бок
                3,7,4,

                1,5,6, // правый бок
                6,2,1,

                4,5,1, // вверх
                1,0,4,

                3,2,6, // низ
                6,7,3,
         };

            verts = new Vertex[8];
            verts[0] = new Vertex(new Vector3(-size, size, size), new Vector2(0, 0));
            verts[1] = new Vertex(new Vector3(size, size, size), new Vector2(1, 0));
            verts[2] = new Vertex(new Vector3(size, -size, size), new Vector2(1, 1));
            verts[3] = new Vertex(new Vector3(-size, -size, size), new Vector2(0, 1));
            verts[4] = new Vertex(new Vector3(-size, size, -size), new Vector2(0, 0));
            verts[5] = new Vertex(new Vector3(size, size, -size), new Vector2(1, 0));
            verts[6] = new Vertex(new Vector3(size, -size, -size), new Vector2(1, 1));
            verts[7] = new Vertex(new Vector3(-size, -size, -size), new Vector2(0, 1));

            CreateBuffers(device, verts, textureIndices);           
        }
        public override void Update(Matrix world, Matrix view, Matrix proj)
        {
            _data.World =Matrix.Translation(0,100,0) * world;
            _data.View = view;
            _data.Proj = proj;
            _data.World.Transpose();
            _data.View.Transpose();
            _data.Proj.Transpose();
        }
        public void Draw()
        {
            Draw(_data, textureIndices.Count(), SharpDX.Direct3D.PrimitiveTopology.TriangleList);
        }

    }
}
