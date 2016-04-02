using SharpDX;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

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
        public Matrix World;
        Joint[] joints;
        MD5Mesh[] subsets;
        List<ShaderResourceView> textures;
        List<string> textureFiles;

        public MD5Model()
        {
            const string path = "3DModelsFiles\\Human\\";
            List<string> lines = ReadMD5File(path + "boy.md5mesh");
            joints = new Joint[lines.Where(l => l.Contains("numJoints")).Select(l => int.Parse(l.Replace("numJoints", "").Trim())).First()];
            subsets = new MD5Mesh[lines.Where(l => l.Contains("numMeshes")).Select(l => int.Parse(l.Replace("numMeshes", "").Trim())).First()];
            joints = GetJoints(lines);
           

            int jj = 0;

        }

        Joint[] GetJoints(List<string> lines)
        {
            string pattern = "\".+\"";


            Joint[] m = lines.Where(l => Regex.IsMatch(l, pattern) && !Regex.IsMatch(l, "shader"))
                .Select(l =>
                {
                    Joint j = new Joint();
                    j.Name = Regex.Match(l, pattern).Groups[0].Value.Replace("\"", "");
                    var r = l.Replace(Regex.Match(l, pattern).Groups[0].Value, "").Trim().Split(' ');
                    j.parentID = int.Parse(r[0], CultureInfo.InvariantCulture);
                    float x = FParse(r[2]);
                    float y = FParse(r[3]);
                    float z = FParse(r[4]);
                    j.position = new Vector3(x, y, z);
                    x = FParse(r[7]);
                    y = FParse(r[8]);
                    z = FParse(r[9]);
                    float w = 0;
                    float t = 1.0f - (x * x)
                        - (y * y)
                        - (z * z);
                    if (t < 0.0f)
                    {
                        w = 0.0f;
                    }
                    else
                    {
                        w = -(float)System.Math.Sqrt(t);
                    }

                    j.orientation = new Vector4(x, y, z, w);
                    return j;
                })
                .ToArray();
            return m;
        }

        private float FParse(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }
        private List<string> ReadMD5File(string obj)
        {
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(obj))
            {
                while (true)
                {
                    string l = reader.ReadLine();
                    if (reader.EndOfStream)
                        break;

                    if (string.IsNullOrEmpty(l.Trim()))
                        continue;

                    lines.Add(l);
                }
            }
            return lines;
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
