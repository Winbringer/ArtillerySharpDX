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
        public event Drawing OnDraw;
        Vertex[] verts;
        uint[] textureIndices;
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
            verts[0] = new Vertex(new Vector3(-1, 1, 1), new Vector2(0, 0));
            verts[1] = new Vertex(new Vector3(1, 1, 1), new Vector2(1, 0));
            verts[2] = new Vertex(new Vector3(1, -1, 1), new Vector2(1, 1));
            verts[3] = new Vertex(new Vector3(-1, -1, 1), new Vector2(0, 1));
            verts[4] = new Vertex(new Vector3(-1, 1, -1), new Vector2(0, 0));
            verts[5] = new Vertex(new Vector3(1, 1, -1), new Vector2(1, 0));
            verts[6] = new Vertex(new Vector3(1, -1, -1), new Vector2(1, 1));
            verts[7] = new Vertex(new Vector3(-1, -1, -1), new Vector2(0, 1));
            CreateBuffers(device, verts, textureIndices);

        }
        public void Move()
        {

        }
        public override void Draw()
        {

        }

    }
}
