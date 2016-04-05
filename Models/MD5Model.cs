using SharpDX;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace SharpDX11GameByWinbringer.Models
{
    [StructLayout(LayoutKind.Sequential)]
    struct cbuffer
    {
        public Matrix MVP;
        public Matrix World;
        public Matrix WorldIT;
        public void Transpose()
        {
            MVP.Transpose();
            World.Transpose();
            WorldIT.Transpose();
        }
    }

    struct HierarchyItem
    {
        public string name;
        public int parent;
        public int flags;
        public int startIndex;
    };

    struct BaseFrameJoint
    {
        public Vector3 pos;
        public Quaternion orient;
    };

    struct MD5Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 textureUV;
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
        public Quaternion orientation;
        public Vector3 transform(Vector3 v)
        {
            return Vector3.Transform(v, orientation) + position;
        }

    }

    class MD5Anim
    {
        public int numFrames;
        public List<HierarchyItem> hierarchy;
        public List<BaseFrameJoint> baseFrame;
        public List<float[]> frames;

        public MD5Anim(string path)
        {
            //hierarchy = new List<HierarchyItem>();
            //baseFrame = new List<BaseFrameJoint>();
            //frames = new List<float[]>();
            //numFrames = 0;
            List<string> lines = ReadMD5File(path);
            numFrames = (int)FParse(lines.First(l => l.Contains("numFrames ")).Split(' ')[1]);
            hierarchy = GetHierarchy(lines);
            baseFrame = GetBaseFrame(lines);
            frames = GetFrames(lines);
        }

        List<float[]> GetFrames(List<string> lines)
        {
            List<float[]> f = new List<float[]>();
            List<float> ff = new List<float>();
            bool isF = false;
            foreach (var item in lines)
            {
                if (!item.Contains("baseframe {") && item.Contains("frame "))
                {
                    isF = true;
                    continue;
                }
                if (item.Contains("}"))
                {
                    isF = false; f.Add(ff.ToArray());
                    ff = new List<float>();
                    continue;
                }
                if (isF)
                {
                    var m = item.Split(' ').Select(l => FParse(l));
                    ff.AddRange(m.ToArray());
                }
            }
            return f.Skip(3).ToList();
        }

        List<BaseFrameJoint> GetBaseFrame(List<string> lines)
        {
            List<BaseFrameJoint> bf = new List<BaseFrameJoint>();
            bool isBF = false;
            foreach (var item in lines)
            {
                if (item.Contains("baseframe {")) { isBF = true; continue; }
                if (item.Contains("}")) { isBF = false; continue; }
                if (isBF)
                {
                    BaseFrameJoint h = new BaseFrameJoint();
                    var m = item.Split(' ');
                    h.pos = new Vector3(FParse(m[1]), FParse(m[2]), FParse(m[3]));
                    float x = FParse(m[6]);
                    float y = FParse(m[7]);
                    float z = FParse(m[8]);
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

                    h.orient = new Quaternion(x, y, z, w);
                    bf.Add(h);
                }
            }
            return bf;
        }

        List<HierarchyItem> GetHierarchy(List<string> lines)
        {
            string pattern = "\".+\"";
            List<HierarchyItem> hi = new List<HierarchyItem>();
            bool isH = false;
            foreach (var item in lines)
            {
                if (item.Contains("hierarchy {")) { isH = true; continue; }
                if (item.Contains("}")) { isH = false; continue; }
                if (isH)
                {
                    HierarchyItem h = new HierarchyItem();
                    h.name = Regex.Match(item, pattern).Groups[0].Value.Replace("\"", "");
                    var m = item.Replace(Regex.Match(item, pattern).Groups[0].Value, "").Trim().Split(' ');
                    h.parent = (int)FParse(m[0]);
                    h.flags = (int)FParse(m[1]);
                    h.startIndex = (int)FParse(Regex.Match(m[2], "[0-9]").Groups[0].Value);
                    hi.Add(h);
                }
            }
            return hi;
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
    }

    class MD5Mesh : System.IDisposable
    {
        public string texture;
        public int numTriangles;
        public List<MD5Vertex> vertices = new List<MD5Vertex>();
        public List<uint> indices = new List<uint>();
        public List<Weight> weights = new List<Weight>();

        public Buffer vertBuff = null;
        public Buffer indexBuff = null;
        public VertexBufferBinding vb;
        ViewModels.ViewModel VM = new ViewModels.ViewModel();
        Drawer dr;
        ShaderResourceView tex;

        public void InitBuffers(Device dv)
        {
            vertBuff = Buffer.Create(dv, BindFlags.VertexBuffer, vertices.ToArray());
            indexBuff = Buffer.Create(dv, BindFlags.IndexBuffer, indices.ToArray());
            vb = new VertexBufferBinding(vertBuff, Utilities.SizeOf<MD5Vertex>(), 0);
            InputElement[] inputElements = new InputElement[]
       {
             new InputElement("SV_Position",0,Format.R32G32B32_Float,0,0),
             new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
             new InputElement("TEXCOORD", 0, Format.R32G32B32_Float, 24, 0)
       };
            dr = new Drawer("Shaders\\Boy.hlsl", inputElements, dv.ImmediateContext);
            tex = dv.ImmediateContext.LoadTextureFromFile(texture);

        }
        public void Draw(Buffer[] cb)
        {
            VM.ConstantBuffers = cb;
            VM.DrawedVertexCount = indices.Count;
            VM.IndexBuffer = indexBuff;
            VM.Textures = new[] { tex };
            VM.VertexBinging = vb;
            dr.Draw(VM);
        }
        public void Dispose()
        {
            Utilities.Dispose(ref vertBuff);
            Utilities.Dispose(ref indexBuff);
            Utilities.Dispose(ref tex);
            Utilities.Dispose(ref dr);
            Utilities.Dispose(ref VM);
        }
    }

    class MD5Model : System.IDisposable
    {
        const string path = "3DModelsFiles\\Human\\";
        public Matrix World;
        Joint[] joints;
        MD5Mesh[] subsets;
        private Buffer _constantBuffer;
        DeviceContext _dx11Context;
        MD5Anim anim;

        public MD5Model(DeviceContext dc)
        {
            _dx11Context = dc;
            World = Matrix.Identity;// Matrix.RotationX(-MathUtil.PiOverTwo);
            List<string> lines = ReadMD5File(path + "boy.md5mesh");
            joints = GetJoints(lines);
            subsets = GetMeshes(lines);
            subsets = SetPositions(subsets, joints);
            subsets = SetNormals(subsets);
            _constantBuffer = new Buffer(_dx11Context.Device, Utilities.SizeOf<cbuffer>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            foreach (var item in subsets)
            {
                item.InitBuffers(_dx11Context.Device);
            }

            this.anim = new MD5Anim(path + "boy.md5anim");
        }

        public void Draw(Matrix world, Matrix view, Matrix proj)
        {
            cbuffer b = new cbuffer();
            b.MVP = World * world * view * proj;
            b.WorldIT = Matrix.Invert(World * world);
            b.World = World * world;
            b.Transpose();
            _dx11Context.UpdateSubresource(ref b, _constantBuffer);
            foreach (var item in subsets)
            {
                item.Draw(new[] { _constantBuffer });
            }
        }

        MD5Mesh[] SetNormals(MD5Mesh[] subset)
        {
            List<Vector3> tempNormal = new List<Vector3>();
            Vector3 unnormalized = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 edge1;
            Vector3 edge2;

            for (int k = 0; k < subset.Length; k++)
            {
                for (int i = 0; i < subset[k].indices.Count / 3; ++i)
                {
                    edge1 = subset[k].vertices[(int)subset[k].indices[(i * 3)]].position - -subset[k].vertices[(int)subset[k].indices[(i * 3) + 2]].position;
                    edge2 = subset[k].vertices[(int)subset[k].indices[(i * 3) + 2]].position - subset[k].vertices[(int)subset[k].indices[(i * 3) + 1]].position;
                    unnormalized = Vector3.Cross(edge1, edge2);
                    tempNormal.Add(unnormalized);
                }

                Vector3 normalSum = new Vector3(0.0f);
                int facesUsing = 0;

                for (int i = 0; i < subset[k].vertices.Count; ++i)
                {
                    for (int j = 0; j < subset[k].numTriangles; ++j)
                    {
                        if (subset[k].indices[j * 3] == i ||
                            subset[k].indices[(j * 3) + 1] == i ||
                            subset[k].indices[(j * 3) + 2] == i)
                        {
                            normalSum += tempNormal[j];
                            ++facesUsing;
                        }
                    }

                    normalSum = normalSum / facesUsing;

                    normalSum = Vector3.Normalize(normalSum);

                    var mt = subset[k].vertices[i];
                    mt.normal = -normalSum;
                    subset[k].vertices[i] = mt;

                    normalSum = new Vector3(0.0f);
                    facesUsing = 0;
                }
            }
            return subset;
        }

        MD5Mesh[] SetPositions(MD5Mesh[] subsets, Joint[] joints)

        {
            for (int k = 0; k < subsets.Length; ++k)
                for (int i = 0; i < subsets[k].vertices.Count; ++i)
                {
                    var tempVert = subsets[k].vertices[i];
                    tempVert.position = new Vector3(0);
                    for (int j = 0; j < tempVert.numWeights; ++j)
                    {
                        Weight tempWeight = subsets[k].weights[tempVert.startWeight + j];
                        Joint tempJoint = joints[tempWeight.JointID];
                        tempVert.position += (Vector3.Transform(tempWeight.position, tempJoint.orientation) + tempJoint.position) * tempWeight.bias;
                    }
                    var tempPos = tempVert.position;
                    tempVert.position = new Vector3(tempPos.X, tempPos.Z, tempPos.Y);
                    subsets[k].vertices[i] = tempVert;
                }
            return subsets;
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

                    j.orientation = new Quaternion(x, y, z, w);
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
            Utilities.Dispose(ref _constantBuffer);
            foreach (var item in subsets)
            {
                item.Dispose();
            }
        }
    }
}
