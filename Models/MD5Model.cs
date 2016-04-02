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
        public Vector3 position;
        public Vector2 textureUV;
        public Vector3 normal;
        public int startWeight;
        public int numWeights;
        public int ID;
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
        public string texture;
        public int numTriangles;
        public List<MD5Vertex> vertices = new List<MD5Vertex>();
        public List<uint> indices = new List<uint>();
        public List<Weight> weights = new List<Weight>();
        public List<Vector3> positions = new List<Vector3>();

        public Buffer vertBuff = null;
        public Buffer indexBuff = null;
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
               
        const string path = "3DModelsFiles\\Human\\";
        public MD5Model()
        {
            World = Matrix.Identity;
            List<string> lines = ReadMD5File(path + "boy.md5mesh");
            joints = GetJoints(lines);
            subsets = GetMeshes(lines);      
        }

        MD5Mesh[] GetMeshes(List<string> lines)
        {
            List<MD5Mesh> m = new List<MD5Mesh>();
            int index = -1;
            foreach (var item in lines)
            {
                if (item.Contains("mesh {"))
                {
                    ++index;
                    m.Add(new MD5Mesh());
                }
                if (item.Contains("shader ")) m[index].texture = path + item.Split(' ')[1].Replace("\"", "");
                if (item.Contains("vert "))
                {
                    var s = item.Replace("vert ", "").Split(' ');
                    MD5Vertex v = new MD5Vertex();
                    v.ID = (int)FParse(s[0]);
                    v.textureUV = new Vector2(FParse(s[2]), FParse(s[3]));
                    v.startWeight = (int)FParse(s[5]);
                    v.numWeights = (int)FParse(s[6]);

                    m[index].vertices.Add(v);
                }
                if (item.Contains("numtris ")) m[index].numTriangles = (int)FParse(item.Split(' ')[1]);
                if (item.Contains("tri "))
                {
                    var tri = item.Split(' ');
                    m[index].indices.Add((uint)FParse(tri[2]));
                    m[index].indices.Add((uint)FParse(tri[3]));
                    m[index].indices.Add((uint)FParse(tri[4]));
                }

                if (item.Contains("weight "))
                {
                    Weight w = new Weight();
                    var sw = item.Split(' ');
                    w.ID = (int)FParse(sw[1]);
                    w.JointID = (int)FParse(sw[2]);
                    w.bias = FParse(sw[3]);
                    w.position = new Vector3(FParse(sw[5]), FParse(sw[6]), FParse(sw[7]));
                    m[index].weights.Add(w);
                }
            }

            return m.ToArray();


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
