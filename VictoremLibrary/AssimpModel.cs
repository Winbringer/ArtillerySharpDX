using Assimp;
using Assimp.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace VictoremLibrary
{
    internal struct JointBone
    {
        public string ParentName;
        public string Name;
        public Matrix Transform;
        public Matrix GlobalTransform;
    }

    public struct VertexBone
    {
        public string Name;
        public float Wheight;

    }
    public struct AssimpVertex
    {
        public Vector3 position;
        public Vector3 uv;
        public Vector3 normal;
        public Vector3 tangent;
        public Vector3 biTangent;
        public List<VertexBone> Bones;
    }

    public class AssimpMesh
    {
        public string Texture { get; set; } = null;
        public AssimpVertex[] Veteces { get; set; } = null;
        public uint[] Indeces { get; set; } = null;
        public string NormalMap { get; set; } = null;
        public string DiplacementMap { get; set; } = null;
        public string SpecularMap { get; set; } = null;
    }

    class AssimpAnimation
    {
        public int numFrames { get; set; }
        public List<List<JointBone>> Frames { get; set; }
    }

    public class AssimpModel
    {
        public List<AssimpMesh> Meshes { get { return _meshes; } }
        List<AssimpMesh> _meshes;
        List<JointBone> _boneHierarhy;
        List<AssimpAnimation> _animatons;

        public AssimpModel(string File)
        {
            String fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), File);

            using (AssimpContext importer = new AssimpContext())
            {
                NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
                importer.SetConfig(config);
                using (LogStream logstream = new LogStream(delegate (String msg, String userData)
                 {
#if DEBUG
                     Console.WriteLine(msg);
#endif

                 }))
                {
                    logstream.Attach();

                    var Model = importer.ImportFile(fileName, PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.GenerateNormals | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.MakeLeftHanded | PostProcessSteps.RemoveComponent);

                    //TODO: Загрузить данные в мои собственные классы и структуры.  
                    _meshes = GetMeshes(Model);
                    if (Model.Meshes[0].HasBones)
                        _boneHierarhy = GetHierarhy(Model);
                    if (Model.HasAnimations && Model.Animations[0].HasNodeAnimations)
                        _animatons = GetAnimation(Model);
                }
            }
        }

        public void AplyAnimashonFrame(int animaton, int frame)
        {
            var a = _animatons[animaton].Frames[frame];
            var aa = _boneHierarhy.Select(j => new JointBone() { Name = j.Name, ParentName =j.ParentName , Transform=a.Any(y => y.Name == j.Name)? a.First(y => y.Name == j.Name).Transform: j.Transform }).ToList();
            var m = CalculateBoneToWorldTransform(aa);

            Matrix transform = new Matrix();
            foreach (var mesh in _meshes)
            {
                for (int i = 0; i < mesh.Veteces.Count(); ++i)
                {
                    foreach (var bone in mesh.Veteces[i].Bones)
                    {
                        transform += m.First(mm => mm.Name == bone.Name).GlobalTransform * bone.Wheight;
                    }
                    var vec = new Vector3();

                    Vector3.Transform(ref mesh.Veteces[i].position, ref transform, out vec);
                    mesh.Veteces[i].position = vec;
                }
            }

        }

        List<JointBone> CalculateBoneToWorldTransform(List<JointBone> jb)
        {
            List<JointBone> bones = new List<JointBone>();
            for (int i = 0; i < jb.Count; i++)
            {
                var child = jb[i];
                child.GlobalTransform = child.Transform;
                var parent = child.ParentName;
                while (parent != null)
                {
                    JointBone p = jb.First(b => b.Name == parent);
                    child.GlobalTransform *= p.Transform;
                    parent = p.ParentName;
                }
                bones.Add(child);
            }
            return bones;
        }

        List<JointBone> GetHierarhy(Scene model)
        {
            List<JointBone> jb = new List<JointBone>();
            GetChildren(model.RootNode, ref jb);
            return jb;
        }

        void GetChildren(Node n, ref List<JointBone> jb)
        {
            jb.Add(new JointBone()
            {
                Name = n.Name,
                ParentName = n.Parent?.Name,
                Transform = ToMatrix(n.Transform)
            });
            if (n.HasChildren)
                for (int i = 0; i < n.ChildCount; ++i)
                {
                    GetChildren(n.Children[i], ref jb);
                }
        }

        List<AssimpAnimation> GetAnimation(Scene model)
        {
            var a = new List<AssimpAnimation>();

            foreach (var item in model.Animations)
            {
                a.Add(new AssimpAnimation()
                {
                    numFrames = (int)item.DurationInTicks + 1,
                    Frames = GetFrames(item)
                });
            }
            return a.ToList();
        }

        List<List<JointBone>> GetFrames(Assimp.Animation a)
        {
            List<List<JointBone>> f = new List<List<JointBone>>();
            for (int i = 0; i < a.DurationInTicks + 1; i++)
            {
                f.Add(GetAnimFrames(a, i));
            }
            return f.ToList();
        }

        List<JointBone> GetAnimFrames(Assimp.Animation a, int frame)
        {
            List<JointBone> b = new List<JointBone>();
            foreach (var ch in a.NodeAnimationChannels)
            {
                var pos = new Vector3D(0);
                if (ch.HasPositionKeys)
                    pos = frame < ch.PositionKeyCount ? ch.PositionKeys[frame].Value : ch.PositionKeys.Last().Value;
                Matrix4x4 tr = Matrix4x4.FromTranslation(pos);
                var rot = new Assimp.Quaternion();
                if (ch.HasRotationKeys)
                    rot = frame < ch.RotationKeyCount ? ch.RotationKeys[frame].Value : ch.RotationKeys.Last().Value;
                Matrix4x4 r = new Matrix4x4(rot.GetMatrix());
                var scale = new Vector3D(0);
                if (ch.HasScalingKeys)
                    scale = frame < ch.ScalingKeyCount ? ch.ScalingKeys[frame].Value : ch.ScalingKeys.Last().Value;
                Matrix4x4 s = Matrix4x4.FromScaling(scale);
                Matrix4x4 res = tr * r * s;
                b.Add(new JointBone() { Name = ch.NodeName, Transform = ToMatrix(res) });
            }
            return b.ToList();
        }

        List<JointBone> GetBones(Assimp.Mesh m)
        {
            if (!m.HasBones) return null;
            var n = m.Bones.Select(i => new JointBone() { Name = i.Name, Transform = ToMatrix(i.OffsetMatrix) });
            return n.ToList();
        }

        List<AssimpMesh> GetMeshes(Scene model)
        {
            var m = new List<AssimpMesh>();
            foreach (var mesh in model.Meshes)
            {
                m.Add(new AssimpMesh()
                {
                    Indeces = mesh.GetIndices().Select(i=> (uint)i).ToArray(),
                    Texture = model.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath,
                    Veteces = GetVertex(mesh).ToArray(),
                    NormalMap = model.Materials[mesh.MaterialIndex].TextureNormal.FilePath,
                    SpecularMap = model.Materials[mesh.MaterialIndex].TextureSpecular.FilePath,
                    DiplacementMap = model.Materials[mesh.MaterialIndex].TextureDisplacement.FilePath
                });
            }
            return m;
        }


        List<AssimpVertex> GetVertex(Assimp.Mesh m)
        {
            List<AssimpVertex> v = new List<AssimpVertex>();

            for (int i = 0; i < m.VertexCount; ++i)
            {
                v.Add(new AssimpVertex()
                {
                    position = new Vector3(m.Vertices[i].X, m.Vertices[i].Y, m.Vertices[i].Z),
                    uv = new Vector3(m.TextureCoordinateChannels[0][i].X, m.TextureCoordinateChannels[0][i].Y, m.TextureCoordinateChannels[0][i].Z),
                    Bones = m.Bones
                    .Where(bb => bb.HasVertexWeights && bb.VertexWeights.Any(tt => tt.VertexID == i))
                    .Select(ib => new VertexBone()
                    {
                        Name = ib.Name,
                        Wheight = ib.VertexWeights.First(l => l.VertexID == i).Weight
                    })
                        .ToList()
                });
            }

            return v;
        }

        Vector3 ToVector3(Vector3D v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        Matrix ToMatrix(Matrix4x4 m)
        {

            var ret = Matrix.Identity;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    ret[i, j] = m[i + 1, j + 1];
                }
            }
            return ret;
        }

        SharpDX.Quaternion ToQuat(Assimp.Quaternion q)
        {
            return new SharpDX.Quaternion(q.X, q.Y, q.Z, q.W);
        }

    }
}

