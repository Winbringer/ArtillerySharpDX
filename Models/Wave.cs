using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    struct Data
    {
        public Matrix World;
        public Matrix View;
        public Matrix Proj;
        public Vector4 Time;
    }
    class Wave : System.IDisposable
    {
        private Drawer<Data> _drawer;
        private Vector3[] _vertices;
        private Vector2[] _textureC;
        private int[] _indeces;
        private Data _data;

        public Wave(Device dv, DeviceContext dc, float ratio)
        {
            InitializeTriangle();
            InputElement[] inputElements = new InputElement[]
            {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0)
            };

            _data = new Data()
            {
                World = Matrix.Identity,// * Matrix.LookAtLH(new Vector3(0f, 6f, -5f), Vector3.Zero, Vector3.Up)* Matrix.PerspectiveFovLH(MathUtil.PiOverFour, ratio, 0.1f, 100.0f),
                View = Matrix.LookAtLH(new Vector3(0, 100f, -200f), new Vector3(0,0,0), Vector3.Up),
                Proj = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, ratio, 1f, 1000f),
                Time = new Vector4(1)
            };
            _drawer = new Drawer<Data>(_data,
                _vertices,
                _indeces,
                "Shaders\\Shader.hlsl",
                inputElements, dv, dc,
                "Textures\\venus.png");
        }

        private void InitializeTriangle()
        {
            //Количество точек вдоль одной стороны куба
            int N = 200;
            //Создание верщин
            _vertices = new Vector3[N * N];
            float size = 100f;
            float delta = size / (N - 1);
            float deltaTexture = 1f / (N - 1);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    int index = i * N + j;
                    _vertices[index] = new Vector3(delta * i - size / 2, 0 , -delta * j);                    
                }
            }
            //Создание индексов
            _indeces = new int[(N - 1) * (N - 1) * 6];
            uint counter = 0;
            for (int z = 0; z < (N - 1); z++)
            {
                for (int X = 0; X < (N - 1); X++)
                {
                    int lowerLeft = (z * N + X);
                    int lowerRight = lowerLeft + 1;
                    int upperLeft = lowerLeft + N;
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
            _textureC = new Vector2[N * N];
            for (int k = 0; k < _textureC.Length; k++)
            {
                int m = 0;
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        _textureC[k + m] = new Vector2(i, j);
                        ++m;
                    }
                }
                k += m - 1;
            }
        }

        public void Draw()
        {
            _drawer.PTolology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            _drawer.ShaderData.Time = new Vector4(1);
            _drawer.Draw();
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
