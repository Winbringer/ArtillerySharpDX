using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SharpDX;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using SharpDX.DXGI;

namespace VictoremLibrary
{

    #region Структуры
    
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
        public int ID;
        public Vector2 textureUV;
        public int startWeight;
        public int numWeights;

    }

    struct Weight
    {
        public int ID;
        public int JointID;
        public float bias;
        public Vector3 position;
        public Vector3 normal;
    }

    struct Joint
    {
        public string name;
        public int parentID;
        public Vector3 position;
        public Quaternion orientation;
        public Vector3 transform(Vector3 v)
        {
            return Vector3.Transform(v, orientation) + position;
        }

    }

    #endregion

    class MD5Mesh
    {
        public string shader;
        public List<MD5Vertex> verts = new List<MD5Vertex>();
        public List<uint> tris = new List<uint>();
        public List<Weight> weights = new List<Weight>();
    }

    class MD5Anim
    {
        public string name;
        int frame = 0;
        public readonly int frameRate;
        public readonly int numFrames;
        public readonly int numJoints;
        public readonly float frameTime;
        public readonly float totalAnimTime;
        public float currAnimTime;
        public int FrameNo { get { return frame; } }
        public List<HierarchyItem> hierarchy;
        public List<BaseFrameJoint> baseFrame;
        public List<float[]> frames;
        public List<Joint[]> Animations;

        public MD5Anim(string path)
        {
            Animations = new List<Joint[]>();
            List<string> lines = ReadMD5File(path);
            frameRate = (int)FParse(lines.First(l => l.Contains("frameRate ")).Split(' ')[1]);
            numFrames = (int)FParse(lines.First(l => l.Contains("numFrames ")).Split(' ')[1]);
            numJoints = (int)FParse(lines.First(l => l.Contains("numJoints ")).Split(' ')[1]);
            frameTime = 1000f / frameRate;
            totalAnimTime = frameTime * numFrames;
            currAnimTime = 0;
            hierarchy = GetHierarchy(lines);
            baseFrame = GetBaseFrame(lines);
            frames = GetFrames(lines);
            for (int i = 0; i < numFrames; i++)
            {
                Animations.Add(setFrame(i));
            }
        }

        public Joint[] Animate(float time)
        {
            currAnimTime += time;

            while (currAnimTime > totalAnimTime)
            {
                currAnimTime -= totalAnimTime;
            }

            float currentFrame = currAnimTime / frameTime;
            int frame0 = (int)System.Math.Floor(currentFrame);
            int frame1 = frame0 + 1;

            if (frame1 > numFrames - 1)
                frame1 = 0;

            float interpolation = currentFrame - frame0;
            return GetLerpJoints(Animations[frame0], Animations[frame1], interpolation);
        }

        Joint GetLerpJoint(Joint jointA, Joint jointB, float interp)
        {
            Joint finalJoint = new Joint();

            finalJoint.position = jointA.position + interp * (jointB.position - jointA.position);

            Quaternion.Slerp(ref jointA.orientation, ref jointB.orientation, interp, out finalJoint.orientation);

            return finalJoint;
        }

        Joint[] GetLerpJoints(Joint[] jA, Joint[] jB, float interp)
        {
            List<Joint> j = new List<Joint>();
            for (int i = 0; i < jA.Length; i++)
            {
                j.Add(GetLerpJoint(jA[i], jB[i], interp));
            }
            return j.ToArray();
        }

        Joint[] setFrame(int no)
        {
            if (no < 0)
                no = numFrames - 1;

            if (no >= numFrames)
                no = 0;

            frame = no;

            Joint[] joints = resetJoints();
            for (int i = 0; i < numJoints; i++)
            {
                int flags = hierarchy[i].flags;
                int pos = hierarchy[i].startIndex;

                if ((flags & 1) != 0)
                    joints[i].position.X = frames[no][pos];

                if ((flags & 2) != 0)
                    joints[i].position.Y = frames[no][pos + 1];

                if ((flags & 4) != 0)
                    joints[i].position.Z = frames[no][pos + 2];

                if ((flags & 8) != 0)
                    joints[i].orientation.X = frames[no][pos + 3];

                if ((flags & 16) != 0)
                    joints[i].orientation.Y = frames[no][pos + 4];

                if ((flags & 32) != 0)
                    joints[i].orientation.Z = frames[no][pos + 5];

                joints[i].orientation = Quaternion.Normalize(joints[i].orientation);
            }

            joints = buildJoints(joints);
            return joints;
        }

        Joint[] buildJoints(Joint[] joints)
        {
            for (int i = 0; i < numJoints; i++)
                if (joints[i].parentID >= 0)
                {
                    int id = joints[i].parentID;
                    joints[i].position = joints[id].position +
                                    Vector3.Transform(joints[i].position, joints[id].orientation);
                    joints[i].orientation = Quaternion.Normalize(Quaternion.Multiply(joints[id].orientation, joints[i].orientation));
                }
            return joints;
        }

        Joint[] resetJoints()
        {
            List<Joint> joints = new List<Joint>();
            Joint joint = new Joint();
            for (int i = 0; i < numJoints; ++i)
            {
                joint.name = hierarchy[i].name;
                joint.parentID = hierarchy[i].parent;
                joint.position = baseFrame[i].pos;
                joint.orientation = baseFrame[i].orient;
                joints.Add(joint);
            }
            return joints.ToArray();
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
                    isF = false;
                    f.Add(ff.ToArray());
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
                    h.startIndex = (int)FParse(Regex.Match(m[2], "[0-9]+").Groups[0].Value);
                    hi.Add(h);
                }
            }
            return hi;
        }

        float FParse(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }

        List<string> ReadMD5File(string obj)
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

    class MD5Model : System.IDisposable
    {
        readonly public string path;
        public Matrix World;
        public List<Mesh> MD5Meshes = new List<Mesh>();
        Joint[] joints;
        MD5Mesh[] meshes;
        List<MD5Anim> animations = new List<MD5Anim>();
        Device _dv;
        public MD5Model(string path, string name, Device dv)
        {
            _dv = dv;
            this.path = path;
            World = Matrix.Identity;
            List<string> lines = ReadMD5File(path + name);
            joints = GetJoints(lines);
            meshes = GetMeshes(lines);
            string mtlFile = lines.First(x => x.Contains("mtllib ")).Replace("mtllib ", "").Trim();
            for (int i = 0; i < meshes.Length; ++i)
            {
                List<Vertex> vertexec = GetVerteces(meshes[i], joints);
                SetNormals(ref vertexec, ref meshes[i], joints);
                MD5Meshes.Add(new Mesh(dv,vertexec.ToArray(), meshes[i].tris.ToArray(), meshes[i].shader,path+mtlFile));            
            }
        }

        public void AddAnimation(string pathToFile, string name)
        {
            animations.Add(new MD5Anim(pathToFile) { name = name });
        }

        public void Animate(float time, int animNamber)
        {
            Joint[] j = animations[animNamber].Animate(time);
            for (int i = 0; i < MD5Meshes.Count; i++)
            {
                MD5Meshes[i].UpdateVertBuffers(_dv.ImmediateContext, MoveJoints(meshes[i], j).ToArray());

            }
        }

        public void Animate(float time, string name)
        {
            Joint[] j = animations.First(x => x.name.Trim() == name.Trim()).Animate(time);
            for (int i = 0; i < MD5Meshes.Count; i++)
            {
                MD5Meshes[i].UpdateVertBuffers(_dv.ImmediateContext, MoveJoints(meshes[i], j).ToArray());
            }
        }

        List<Vertex> MoveJoints(MD5Mesh subsets, Joint[] joints)
        {
            List<Vertex> verteces = new List<Vertex>();
            for (int i = 0; i < subsets.verts.Count; ++i)
            {
                var tempVert = subsets.verts[i];
                Vector3 position = new Vector3(0);
                Vector3 normal = new Vector3(0);
                for (int j = 0; j < tempVert.numWeights; ++j)
                {
                    Weight tempWeight = subsets.weights[tempVert.startWeight + j];
                    Joint tempJoint = joints[tempWeight.JointID];
                    position += (Vector3.Transform(tempWeight.position, tempJoint.orientation) + tempJoint.position) * tempWeight.bias;
                    normal += Vector3.Transform(tempWeight.normal, tempJoint.orientation) * tempWeight.bias;
                }
                Vertex v = new Vertex();
                v.position = position;
                v.normal = normal;
                verteces.Add(v);
            }

            return verteces;
        }

        void SetNormals(ref List<Vertex> verteces, ref MD5Mesh subset, Joint[] joint)
        {
            List<Vector3> tempNormal = new List<Vector3>();
            Vector3 unnormalized = Vector3.Zero;
            Vector3 edge1;
            Vector3 edge2;

            for (int i = 0; i < subset.tris.Count / 3; ++i)
            {
                int in1 = (int)subset.tris[i * 3];
                int in2 = (int)subset.tris[(i * 3) + 1];
                int in3 = (int)subset.tris[(i * 3) + 2];
                Vector3 v0 = verteces[in1].position;
                Vector3 v1 = verteces[in2].position;
                Vector3 v2 = verteces[in3].position;
                edge1 = v0 - v2;
                edge2 = v2 - v1;
                unnormalized = Vector3.Cross(edge1, edge2);
                tempNormal.Add(unnormalized);
            }

            Vector3 normalSum = new Vector3(0.0f);
            int facesUsing = 0;

            for (int i = 0; i < subset.verts.Count; ++i)
            {
                for (int j = 0; j < subset.tris.Count / 3; ++j)
                {
                    if (subset.tris[(j * 3)] == i || subset.tris[(j * 3) + 1] == i || subset.tris[(j * 3) + 2] == i)
                    {
                        normalSum += tempNormal[j];
                        ++facesUsing;
                    }
                }

                normalSum = normalSum / facesUsing;

                normalSum = Vector3.Normalize(normalSum);
                Vertex v = verteces[i];
                v.normal = normalSum;
                verteces[i] = v;

                MD5Vertex tempVert = subset.verts[i];

                for (int k = 0; k < tempVert.numWeights; k++)
                {
                    Joint tempJoint = joint[subset.weights[tempVert.startWeight + k].JointID];
                    Quaternion jointOrientation = tempJoint.orientation;


                    Vector3 normal = Vector3.Transform(normalSum, jointOrientation);
                    Weight w = subset.weights[tempVert.startWeight + k];
                    w.normal = normal;
                    subset.weights[tempVert.startWeight + k] = w;
                }

                normalSum = new Vector3(0.0f);
                facesUsing = 0;

            }

        }

        List<Vertex> GetVerteces(MD5Mesh subsets, Joint[] joints)
        {
            List<Vertex> verteces = new List<Vertex>();
            for (int i = 0; i < subsets.verts.Count; ++i)
            {
                var tempVert = subsets.verts[i];
                Vector3 position = new Vector3(0);
                for (int j = 0; j < tempVert.numWeights; ++j)
                {
                    Weight tempWeight = subsets.weights[tempVert.startWeight + j];
                    Joint tempJoint = joints[tempWeight.JointID];
                    position += (Vector3.Transform(tempWeight.position, tempJoint.orientation) + tempJoint.position) * tempWeight.bias;
                }
                Vertex v = new Vertex();
                v.textureUV = tempVert.textureUV;
                v.position = position;
                verteces.Add(v);
            }
            return verteces;
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

                if (item.Contains("shader ")) m[index].shader = path + item.Split(' ')[1].Replace("\"", "");

                if (item.Contains("vert "))
                {
                    var s = item.Replace("vert ", "").Split(' ');
                    MD5Vertex v = new MD5Vertex();
                    v.ID = (int)FParse(s[0]);
                    v.textureUV = new Vector2(FParse(s[2]), FParse(s[3]));
                    v.startWeight = (int)FParse(s[5]);
                    v.numWeights = (int)FParse(s[6]);

                    m[index].verts.Add(v);
                }

                if (item.Contains("tri "))
                {
                    var tris = item.Split(' ');
                    m[index].tris.Add((uint)FParse(tris[2]));
                    m[index].tris.Add((uint)FParse(tris[3]));
                    m[index].tris.Add((uint)FParse(tris[4]));
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
                    j.name = Regex.Match(l, pattern).Groups[0].Value.Replace("\"", "");
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
            foreach (var item in MD5Meshes)
            {
                item?.Dispose();
            }
        }
    }

}
