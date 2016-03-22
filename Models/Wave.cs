using System;
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    sealed class Wave:GameObject<Vertex,DataT>
    {   
        private readonly float _size = 500F;
        readonly int _N = 1000;

        public Wave(Device device)
        {
            _world = Matrix.Translation(-_size / 2, 0, _size / 2) * Matrix.RotationY(MathUtil.PiOverFour);
            CreateVerteces();
            _constantBufferData = new DataT();
            CreateBuffers(device);     
        }


        public override  void Update(Matrix world, Matrix view, Matrix proj)
        {
            _constantBufferData.WVP = _world * world * view * proj;
            _constantBufferData.WVP.Transpose();
            _constantBufferData.Time = System.Environment.TickCount;
        }

        protected override void CreateVerteces()
        {
            _verteces = new Vertex[_N * _N];
            //Создание верщин           
            float delta = _size / (_N - 1);
            float deltaT = 1f / (_N - 1);
            float deltaTexture = 1f / (_N - 1);
            for (int i = 0; i < _N; i++)
            {
                for (int j = 0; j < _N; j++)
                {
                    int index = i * _N + j;
                    _verteces[index].Position = new Vector3(delta * i, 0, -delta * j);
                    _verteces[index].TextureUV = new Vector2(deltaT * i, deltaT * j);
                }
            }
            //Создание индексов
            _indeces = new uint[(_N - 1) * (_N - 1) * 6];
            uint counter = 0;
            for (int z = 0; z < (_N - 1); z++)
            {
                for (int X = 0; X < (_N - 1); X++)
                {
                    uint lowerLeft = (uint)(z * _N + X);
                    uint lowerRight = lowerLeft + 1;
                    uint upperLeft = lowerLeft + (uint)_N;
                    uint upperRight = upperLeft + 1;
                    _indeces[counter++] = lowerLeft;
                    _indeces[counter++] = upperLeft;
                    _indeces[counter++] = upperRight;
                    _indeces[counter++] = lowerLeft;
                    _indeces[counter++] = upperRight;
                    _indeces[counter++] = lowerRight;
                }
            }
        }
    }
}
