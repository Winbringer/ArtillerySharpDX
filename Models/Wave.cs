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
        private Matrix _world;
        private Drawer<Data, Vertex> _drawer;
        private Vertex[] _vertices;
        private int[] _indeces;
        private Data _data;
        private readonly float _size = 500F;
        readonly int _N = 500;

        public Wave(DeviceContext dc)
        {
            _world = Matrix.Translation(-_size / 2, 0, _size / 2) * Matrix.RotationY(MathUtil.PiOverFour);
            InitializeTriangle();
            InputElement[] inputElements = new InputElement[]
            {
                new InputElement("SV_Position", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("TEXCOORD",0,SharpDX.DXGI.Format.R32G32_Float,12,0)
            };            
            _data = new Data()
            {                
                Time = new Vector4(1)
            };
            //Установка Сампрелар для текстуры.
            SamplerStateDescription description = SamplerStateDescription.Default();
            description.Filter = Filter.MinMagMipLinear;
            description.AddressU = TextureAddressMode.Wrap;
            description.AddressV = TextureAddressMode.Wrap;

            _drawer = new Drawer<Data, Vertex>(
                _vertices,
                _indeces,
                "Shaders\\Shader.hlsl",
                inputElements, dc,
                "Textures\\grass.jpg",
                description);
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
                    _vertices[index].TextureUV = new Vector2(deltaT *i, deltaT* j);
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
        }

        public void Update(Matrix world, Matrix view, Matrix proj)
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
            _drawer.PTolology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            _data.Time = new Vector4(System.Environment.TickCount);
            _drawer.Draw(_data);
        }
        
       
        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).
                    _drawer.Dispose();
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
