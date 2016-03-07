
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    class Wave : System.IDisposable
    {
        private Vector3[] _vertices;
        int[] _indeces;
        Color4 _world;
        Drawer<Color4> _drawer;

        public Wave(Device dv, DeviceContext dc)
        {
            InitializeTriangle();
            InputElement[] inputElements = new InputElement[]
            {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0)
            };
            _world = new Color4(1f, 0, 1f, 1f);
            _drawer = new Drawer<Color4>(_world,
                _vertices,
                _indeces,
                "Shaders\\Shader.hlsl",
                inputElements, dv, dc);
        }

        private void InitializeTriangle()
        {
            //Количество точек вдоль одной стороны куба
            int N = 20;
            //Создание верщин
            _vertices = new Vector3[N * N];
            float delta = 0.5f / (N - 1);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    int index = i * N + j;
                    _vertices[index] = new Vector3(delta * i, -delta * j,0);
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
        }

        public void Draw()
        {            
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
