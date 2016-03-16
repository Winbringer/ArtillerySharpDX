using SharpDX;
using System.Linq;

namespace SharpDX11GameByWinbringer.Models
{

    class Wave : DrawableGameObject<Vertex, Data>
    {          
        private Matrix _world;
        private Vertex[] _vertices;
        private uint[] _indeces;
        private Data _data;
        private readonly float _size = 500F;
        readonly int _N = 500;

        public Wave(SharpDX.Direct3D11.Device Device)
        {
            _world = Matrix.Translation(-_size / 2, 0, _size / 2) * Matrix.RotationY(MathUtil.PiOverFour);
            InitializeTriangle();
            _data = new Data();            
            CreateBuffers(Device, _vertices, _indeces);
        }

        private void InitializeTriangle()
        {
            _vertices = new Vertex[_N * _N];
            //Создание верщин           
            float delta = _size / (_N - 1);
            float deltaT = 1f / (_N - 1);
            float deltaTexture = 1f / (_N - 1);
            for (int i = 0; i < _N; i++)
            {
                for (int j = 0; j < _N; j++)
                {
                    int index = i * _N + j;
                    _vertices[index].Position = new Vector3(delta * i, 0, -delta * j);
                    _vertices[index].TextureUV = new Vector2(deltaT * i, deltaT * j);
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

        public override void Update(Matrix world, Matrix view, Matrix proj)
        {          
            _data.World = _world * world;
            _data.View = view;
            _data.Proj = proj;
            _data.World.Transpose();
            _data.View.Transpose();
            _data.Proj.Transpose();
        }
        public void Draw()
        {
            _data.Time = new Vector4(System.Environment.TickCount);
            Draw(_data, _indeces.Count(), SharpDX.Direct3D.PrimitiveTopology.TriangleList);          
        }
       
    }
}
