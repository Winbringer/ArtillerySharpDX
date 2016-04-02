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

namespace SharpDX11GameByWinbringer.Models
{
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
        public Vector4 orientation;
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
        public  VertexBufferBinding vb;
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
            dr = new Drawer("Shaders\\Boy.hlsl", inputElements,dv.ImmediateContext);
            tex = dv.ImmediateContext.LoadTextureFromFile(texture);
            var s = SamplerStateDescription.Default();
            s.AddressU = TextureAddressMode.Wrap;
            s.AddressV = TextureAddressMode.Wrap;
            RasterizerStateDescription rasterizerStateDescription = RasterizerStateDescription.Default();
            rasterizerStateDescription.CullMode = CullMode.None;
            rasterizerStateDescription.FillMode = FillMode.Solid;

            DepthStencilStateDescription DStateDescripshion = DepthStencilStateDescription.Default();
            DStateDescripshion.IsDepthEnabled = true;

            dr.Samplerdescription = s;
            dr.RasterizerDescription = rasterizerStateDescription;
            dr.DepthStencilDescripshion = DStateDescripshion;
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
        public Matrix World;
        Joint[] joints;
        MD5Mesh[] subsets;

        const string path = "3DModelsFiles\\Human\\";
        private Buffer _constantBuffer;
        DeviceContext _dx11Context;
        public MD5Model(DeviceContext dc)
        {
            _dx11Context = dc;
            World =  Matrix.RotationX(-MathUtil.PiOverTwo);
            List<string> lines = ReadMD5File(path + "boy.md5mesh");
            joints = GetJoints(lines);
            subsets = GetMeshes(lines);
            subsets = SetPositions(subsets, joints);
           subsets = SetNormals(subsets);
            _constantBuffer = new Buffer(_dx11Context.Device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            foreach (var item in subsets)
            {
                item.InitBuffers(_dx11Context.Device);
            }

          }

        public void Draw(Matrix world, Matrix view, Matrix proj)
        {
            Matrix MVP = World * world * view * proj;
            MVP.Transpose();
            _dx11Context.UpdateSubresource(ref MVP, _constantBuffer);
            foreach (var item in subsets)
            {
                item.Draw(new[] { _constantBuffer});
            }
        }

        MD5Mesh[] SetNormals(MD5Mesh[] subset)
        {
            List<Vector3> tempNormal = new List<Vector3>();
            
            Vector3 unnormalized = new Vector3(0.0f, 0.0f, 0.0f);

            float vecX, vecY, vecZ;

            Vector3 edge1;
            Vector3 edge2;

            for (int k = 0; k < subset.Length; k++)
            {
                for (int i = 0; i < subset[k].indices.Count / 3; ++i)
                {
                    vecX = subset[k].vertices[(int)subset[k].indices[(i * 3)]].position.X - subset[k].vertices[(int)subset[k].indices[(i * 3) + 2]].position.X;
                    vecY = subset[k].vertices[(int)subset[k].indices[(i * 3)]].position.Y - subset[k].vertices[(int)subset[k].indices[(i * 3) + 2]].position.Y;
                    vecZ = subset[k].vertices[(int)subset[k].indices[(i * 3)]].position.Z - subset[k].vertices[(int)subset[k].indices[(i * 3) + 2]].position.Z;
                    edge1 = new Vector3(vecX, vecY, vecZ);  
                    vecX = subset[k].vertices[(int)subset[k].indices[(i * 3) + 2]].position.X - subset[k].vertices[(int)subset[k].indices[(i * 3) + 1]].position.X;
                    vecY = subset[k].vertices[(int)subset[k].indices[(i * 3) + 2]].position.Y - subset[k].vertices[(int)subset[k].indices[(i * 3) + 1]].position.Y;
                    vecZ = subset[k].vertices[(int)subset[k].indices[(i * 3) + 2]].position.Z - subset[k].vertices[(int)subset[k].indices[(i * 3) + 1]].position.Z;
                    edge2 = new Vector3(vecX, vecY, vecZ);   
                    unnormalized = Vector3.Cross(edge1, edge2);

                    tempNormal.Add(unnormalized);
                }

                //Compute vertex normals (normal Averaging)
                Vector4 normalSum = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                int facesUsing = 0;
                float tX, tY, tZ;    //temp axis variables

                //Go through each vertex
                for (int i = 0; i < subset[k].vertices.Count; ++i)
                {
                    //Check which triangles use this vertex
                    for (int j = 0; j < subset[k].numTriangles; ++j)
                    {
                        if (subset[k].indices[j * 3] == i ||
                            subset[k].indices[(j * 3) + 1] == i ||
                            subset[k].indices[(j * 3) + 2] == i)
                        {
                            tX = normalSum.X + tempNormal[j].X;
                            tY = normalSum.Y + tempNormal[j].Y;
                            tZ = normalSum.Z + tempNormal[j].Z;

                            normalSum = new Vector4(tX, tY, tZ, 0.0f);   

                            facesUsing++;
                        }
                    }

                    
                    normalSum = normalSum / facesUsing;
                    
                    normalSum =Vector4.Normalize(normalSum);
                    var mt = subset[k].vertices[i];
                    mt.normal= new Vector3( -normalSum.X, -normalSum.Y,-normalSum.Z);
                    subset[k].vertices[i] = mt;
                    //Clear normalSum, facesUsing for next vertex
                    normalSum = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
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

                        var tempJointOrientation =new Quaternion( tempJoint.orientation);
                        var tempWeightPos = new Quaternion(tempWeight.position, 0.0f);
                        var tempJointOrientationConjugate = new Quaternion(-tempJoint.orientation.X,
                                                                         -tempJoint.orientation.Y,
                                                                         -tempJoint.orientation.Z,
                                                                         tempJoint.orientation.W);
                        Vector3 rotatedPoint;
                      var  rot = Quaternion.Multiply(Quaternion.Multiply(tempJointOrientation,tempWeightPos), tempJointOrientationConjugate);                       
                        rotatedPoint = new Vector3(rot.X, rot.Y, rot.Z);                        
                        tempVert.position += (tempJoint.position + rotatedPoint) * tempWeight.bias;                        
                    }
                   
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
            Utilities.Dispose(ref _constantBuffer);
            foreach (var item in subsets)
            {
                item.Dispose();
            }
        }
    }
}
