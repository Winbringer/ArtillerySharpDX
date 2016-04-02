using SharpDX;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SharpDX11GameByWinbringer.Models
{
    struct MD5Vertex
    {
        public int ID;
        public Vector3 position;
        public Vector2 textureUV;
        public Vector3 normal;
        public int startWeight;
        public int numWeights;
    }
    struct Weight
    {
        public int ID;
        public int JointID;
        public float bias;
        public Vector3 position;
    }
    struct Joint
    {
        public string Name;
        public int parentID;
        public Vector3 position;
        public Vector4 orientation;
    }

    class MD5Mesh : System.IDisposable
    {
        string texArrayIndex;
        int numTriangles;
        List<MD5Vertex> vertices;
        List<uint> indices;
        List<Weight> weights;

        List<Vector3> positions;

        Buffer vertBuff;
        Buffer indexBuff;
        public void Dispose()
        {
            Utilities.Dispose(ref vertBuff);
            Utilities.Dispose(ref indexBuff);
        }
    }

    class MD5Model : System.IDisposable
    {
        int numSubsets;
        int numJoints;
        public Matrix World;
        List<Joint> joints;
        List<MD5Mesh> subsets;
        List<ShaderResourceView> textures;
        List<string> textureFiles;

        public MD5Model()
        {

        }

        public void Dispose()
        {
            foreach (var item in subsets)
            {
                item.Dispose();
            }
        }
    }
}
