using SharpDX;

namespace SharpDX11GameByWinbringer.Models
{

    class Wave:GameObject<Vertex,DataT>
    {   
        public float Size = 500F;
        public int N = 500;
        public Wave()
        {

            Verteces = new Vertex[N * N];
            //Создание верщин           
            float delta = Size / (N - 1);
            float deltaT = 1f / (N - 1);
            float deltaTexture = 1f / (N - 1);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    int index = i * N + j;
                    Verteces[index].Position = new Vector3(delta * i, 0, -delta * j);
                    Verteces[index].TextureUV = new Vector2(deltaT * i, deltaT * j);
                }
            }
            //Создание индексов
            Indeces = new uint[(N - 1) * (N - 1) * 6];
            uint counter = 0;
            for (int z = 0; z < (N - 1); z++)
            {
                for (int X = 0; X < (N - 1); X++)
                {
                    uint lowerLeft = (uint)(z * N + X);
                    uint lowerRight = lowerLeft + 1;
                    uint upperLeft = lowerLeft + (uint)N;
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
    }
}
