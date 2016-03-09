using SharpDX;
using SharpDX.Direct3D11;
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

    class Wave : System.IDisposable
    {
        private Drawer<Data, Vertex> _drawer;
        private Vertex[] _vertices;
        private int[] _indeces;
        private Data _data;
        private readonly float _size = 500F;
        readonly int _N = 500;

        public Wave(DeviceContext dc, float ratio)
        {
            InitializeTriangle();
            InputElement[] inputElements = new InputElement[]
            {
                new InputElement("SV_Position", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("TEXCOORD",0,SharpDX.DXGI.Format.R32G32_Float,12,0)
            };
            Matrix w =Matrix.Translation(-_size / 2, 0, _size / 2) * Matrix.RotationY(MathUtil.PiOverFour);
            Matrix v = Matrix.LookAtLH(new Vector3(0, 50f, -400f), new Vector3(0, 0, 0), Vector3.Up);
            Matrix p = Matrix.PerspectiveFovLH(MathUtil.Pi / 3, ratio, 1f, 2000f);

            w.Transpose();
            v.Transpose();
            p.Transpose();

            _data = new Data()
            {
                World = w,
                View = v,
                Proj = p,
                Time = new Vector4(1)
            };

            _drawer = new Drawer<Data, Vertex>(
                _vertices,
                _indeces,
                "Shaders\\Shader.hlsl",
                inputElements, dc,
                "Textures\\grass.jpg");
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
                  _vertices[index].TextureUV = new Vector2(deltaT*i, deltaT* j);
                }
            }
            //Создание индексов
            _indeces = new int[(_N - 1) * (_N - 1) * 6];
            uint counter = 0;
            for (int z = 0; z < (_N - 1); z++)
            {
                for (int X = 0; X < (_N - 1); X++)
                {
                    int lowerLeft = (z * _N + X);
                    int lowerRight = lowerLeft + 1;
                    int upperLeft = lowerLeft + _N;
                    int upperRight = upperLeft + 1;
                    _indeces[counter++] = lowerLeft;
                    _indeces[counter++] = upperLeft;
                    _indeces[counter++] = upperRight;
                    _indeces[counter++] = lowerLeft;
                    _indeces[counter++] = upperRight;
                    _indeces[counter++] = lowerRight;
                }
            }
            //Создание координат текстуры            
            //for (int k = 0; k < _vertices.Length; k++)
            //{
            //    int m = 0;
            //    for (int i = 0; i < 2; i++)
            //    {
            //        for (int j = 0; j < 2; j++)
            //        {
            //            _vertices[k + m].TextureUV = new Vector2(i,j);
            //            ++m;
            //        }
            //    }
            //    k += m - 1;
            //}

        }

        public void Draw()
        {
            _drawer.PTolology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            _data.Time = new Vector4(System.Environment.TickCount);
            _drawer.Draw(_data);
        }

        public void Update()
        {

        }
        public void Dispose()
        {
            _drawer.Dispose();
        }
    }
}
