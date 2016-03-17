﻿using SharpDX;

namespace SharpDX11GameByWinbringer.Models
{

    class Wave:GameObject<Vertex,DataT>
    {   
        private readonly float _size = 500F;
        readonly int _N = 500;

        public Wave()
        {
            World = Matrix.Translation(-_size / 2, 0, _size / 2) * Matrix.RotationY(MathUtil.PiOverFour);
            InitializeTriangle();
            ConstantBufferData = new DataT();          
        }

        private void InitializeTriangle()
        {
           Verteces = new Vertex[_N * _N];
            //Создание верщин           
            float delta = _size / (_N - 1);
            float deltaT = 1f / (_N - 1);
            float deltaTexture = 1f / (_N - 1);
            for (int i = 0; i < _N; i++)
            {
                for (int j = 0; j < _N; j++)
                {
                    int index = i * _N + j;
                    Verteces[index].Position = new Vector3(delta * i, 0, -delta * j);
                    Verteces[index].TextureUV = new Vector2(deltaT * i, deltaT * j);
                }
            }
            //Создание индексов
            Indeces = new uint[(_N - 1) * (_N - 1) * 6];
            uint counter = 0;
            for (int z = 0; z < (_N - 1); z++)
            {
                for (int X = 0; X < (_N - 1); X++)
                {
                    uint lowerLeft = (uint)(z * _N + X);
                    uint lowerRight = lowerLeft + 1;
                    uint upperLeft = lowerLeft + (uint)_N;
                    uint upperRight = upperLeft + 1;
                    Indeces[counter++] = lowerLeft;
                   Indeces[counter++] = upperLeft;
                   Indeces[counter++] = upperRight;
                   Indeces[counter++] = lowerLeft;
                   Indeces[counter++] = upperRight;
                    Indeces[counter++] = lowerRight;
                }
            }
        }

        public override  void Update(Matrix world, Matrix view, Matrix proj)
        {          
            ConstantBufferData.World =World * world;
            ConstantBufferData.View = view;
            ConstantBufferData.Proj = proj;
            ConstantBufferData.World.Transpose();
            ConstantBufferData.View.Transpose();
            ConstantBufferData.Proj.Transpose();
            ConstantBufferData.Time = new Vector4(System.Environment.TickCount);
        }       
    }
}
