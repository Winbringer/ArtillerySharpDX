﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX11GameByWinbringer.Models;
using SharpDX;

namespace SharpDX11GameByWinbringer.Models
{
    class TexturedCube:GameObject<Vertex,Data>
    {        
        float size = 100;
        public TexturedCube()
        {           
            Indeces = new uint[]
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

            Verteces = new Vertex[8];
            Verteces[0] = new Vertex(new Vector3(-size, size, size), new Vector2(0, 0));
            Verteces[1] = new Vertex(new Vector3(size, size, size), new Vector2(1, 0));
            Verteces[2] = new Vertex(new Vector3(size, -size, size), new Vector2(1, 1));
            Verteces[3] = new Vertex(new Vector3(-size, -size, size), new Vector2(0, 1));
            Verteces[4] = new Vertex(new Vector3(-size, size, -size), new Vector2(0, 0));
            Verteces[5] = new Vertex(new Vector3(size, size, -size), new Vector2(1, 0));
            Verteces[6] = new Vertex(new Vector3(size, -size, -size), new Vector2(1, 1));
            Verteces[7] = new Vertex(new Vector3(-size, -size, -size), new Vector2(0, 1));

        }       
    }
}
