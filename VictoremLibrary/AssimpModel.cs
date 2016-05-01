using Assimp;
using Assimp.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpDX;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace VictoremLibrary
{
    struct JointBone
    {
        public string ParentName;
        public Matrix Transform;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AssimpVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 uv;
        public Vector3 tangent;
        public Vector3 biTangent;
        public Vector4 BoneID;
        public Vector4 BoneWheight;
    }

    public class AssimpMesh
    {
        public bool HasBones { get; set; }
        public string Texture { get; set; } = null;
        public AssimpVertex[] Veteces { get; set; } = null;
        public uint[] Indeces { get; set; } = null;
        public string NormalMap { get; set; } = null;
        public string DiplacementMap { get; set; } = null;
        public string SpecularMap { get; set; } = null;
    }

    struct Joint
    {
        public string PName;
        public string Name;
        public Vector3D Pos;
        public Assimp.Quaternion Quat;
        public Matrix matrix;
    }

    class AssimpAnimation
    {
        public int numFrames { get; private set; }
        public double framesPerSecond { get; private set; }
        public Matrix[][] Frames { get; private set; }
       

        public AssimpAnimation(Animation animation, Dictionary<string, JointBone> Hierarhy, Matrix inv)
        {
            Dictionary<string, JointBone> hier = new Dictionary<string, JointBone>();
            for (int i = 0; i < Hierarhy.Count; i++)
            {
                hier.Add(Hierarhy.ElementAt(i).Key, Hierarhy.ElementAt(i).Value);
            }
            numFrames = (int)animation.DurationInTicks + 1;
            framesPerSecond = animation.TicksPerSecond != 0 ? animation.TicksPerSecond : 25d;
            // Frames =  // GetFrames(animation, Hierarhy, inv);
            Dictionary<string, Joint>[]  Transforms = GetTransforms(animation, hier, numFrames);
            Frames = ToFrames(Transforms);
        }

        Matrix[][] ToFrames(Dictionary<string, Joint>[] tr)
        {
            List<Matrix[]> m = new List<Matrix[]>();
            for (int i = 0; i < numFrames; i++)
            {
               m.Add( tr[i].Select(j => j.Value.matrix).ToArray());
            }
            return m.ToArray();
        }

        Dictionary<string, Joint>[] GetTransforms(Animation a, Dictionary<string, JointBone> h, int NumFrames)
        {
            List<Dictionary<string, Joint>> tr = new List<Dictionary<string, Joint>>();
            for (int i = 0; i < numFrames; i++)
            {
                tr.Add(buildJoints(GetJoints(a, i, h)));
            }
            return tr.ToArray();

        }

        Dictionary<string, Joint> GetJoints(Animation a, int f, Dictionary<string, JointBone> h)
        {
            Dictionary<string, Joint> j = new Dictionary<string, Joint>();
            foreach (var node in a.NodeAnimationChannels)
            {
                j.Add(node.NodeName, new Joint()
                {
                    Name = node.NodeName,
                    PName = h[node.NodeName].ParentName,
                    Pos = node.PositionKeys.Any(pky => pky.Time == f) ? node.PositionKeys.First(pk => pk.Time == f).Value : node.PositionKeys.First(pk => pk.Time == node.PositionKeys.Max(m => m.Time)).Value,
                    Quat = node.RotationKeys.Any(pky => pky.Time == f) ? node.RotationKeys.First(pk => pk.Time == f).Value : node.RotationKeys.First(pk => pk.Time == node.RotationKeys.Max(m => m.Time)).Value
                });
            }
            return j;
        }

        Dictionary<string, Joint> buildJoints(Dictionary<string, Joint> joints)
        {
            for (int i = 0; i < joints.Count; i++)
            {
                var j = joints.ElementAt(i);
                if (j.Value.PName != null)
                {
                    Joint b = j.Value;
                    b.Pos = joints[b.PName].Pos + Assimp.Quaternion.Rotate(b.Pos, joints[b.PName].Quat);
                    b.Quat = joints[b.PName].Quat * b.Quat;
                    b.matrix = Matrix.AffineTransformation(1, ToQuat(b.Quat), ToVector3( b.Pos));
                    joints[b.Name] = b;
                }
                if(string.IsNullOrEmpty(j.Value.PName))
                {
                    Joint b = j.Value;
                    b.matrix = Matrix.AffineTransformation(1, ToQuat(b.Quat), ToVector3(b.Pos));
                    joints[b.Name] = b;
                }
            }
            return joints;
        }

        //Matrix[][] GetFrames(Animation animation, Dictionary<string, JointBone> Hierarhy, Matrix inverse)
        //{
        //    List<Matrix[]> mat = new List<Matrix[]>();
        //    for (int i = 0; i < numFrames; i++)
        //    {
        //        mat.Add(buildJoints(SetHierarhiMatices(animation.NodeAnimationChannels.ToArray(), Hierarhy, i), inverse));
        //    }
        //    return mat.ToArray();
        //}

        //Dictionary<string, JointBone> SetHierarhiMatices(NodeAnimationChannel[] nodeAnimCannels, Dictionary<string, JointBone> Hierarhy, int i)
        //{
        //    foreach (var nodeAnim in nodeAnimCannels)
        //    {
        //        var j = Hierarhy[nodeAnim.NodeName];
        //        j.Transform = GetFramMatrix(nodeAnim, i);
        //        Hierarhy[nodeAnim.NodeName] = j;
        //    }
        //    return Hierarhy;
        //}

        //Matrix GetFramMatrix(NodeAnimationChannel nodeAnim, int i)
        //{
        //    Matrix matrix;
        //    if (nodeAnim.HasPositionKeys)
        //    {
        //        var pPosition = i < nodeAnim.PositionKeyCount ? nodeAnim.PositionKeys[i].Value : nodeAnim.PositionKeys.Last().Value;

        //        var pRot = i < nodeAnim.RotationKeyCount ? nodeAnim.RotationKeys[i].Value : nodeAnim.RotationKeys.Last().Value;

        //        var pscale = i < nodeAnim.ScalingKeyCount ? nodeAnim.ScalingKeys[i].Value : nodeAnim.ScalingKeys.Last().Value;

        //        // create the combined transformation matrix
        //        pRot.Normalize();
        //        var mat = new Assimp.Matrix4x4(pRot.GetMatrix());
        //        mat.A1 *= pscale.X; mat.B1 *= pscale.X; mat.C1 *= pscale.X;
        //        mat.A2 *= pscale.Y; mat.B2 *= pscale.Y; mat.C2 *= pscale.Y;
        //        mat.A3 *= pscale.Z; mat.B3 *= pscale.Z; mat.C3 *= pscale.Z;
        //        mat.A4 = pPosition.X; mat.B4 = pPosition.Y; mat.C4 = pPosition.Z;
        //        matrix = ToMatrix(mat);
        //    }
        //    else { matrix = Matrix.Identity; }

        //    return matrix;
        //}

        //Matrix[] buildJoints(Dictionary<string, JointBone> hierarhy, Matrix inverse)
        //{
        //    var m = new List<Matrix>();
        //    for (int i = 0; i < hierarhy.Count; i++)
        //    {
        //        var j = hierarhy.ElementAt(i).Value;

        //        if (j.ParentName != null)
        //        {
        //            j.Transform = inverse * j.Transform * hierarhy[j.ParentName].Transform;
        //        }
        //        m.Add(j.Transform);
        //    }
        //    return m.ToArray();
        //}

        //Matrix[] CalculateTransforms(Dictionary<string, JointBone> hierarhy)
        //{
        //    var m = new List<Matrix>();
        //    foreach (var j in hierarhy)
        //    {
        //        m.Add(CalculatPartnTransform(j.Value, hierarhy));
        //    }
        //    return m.ToArray();
        //}

        //Matrix CalculatPartnTransform(JointBone jB, Dictionary<string, JointBone> hierarhy)
        //{
        //    if (jB.ParentName == null) return jB.Transform;
        //    return  jB.Transform* CalculatPartnTransform(hierarhy[jB.ParentName], hierarhy);
        //}

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

    /// <summary>
    /// Класс для загрузки 3Д моделей и скелейтной анимации из файлов
    /// </summary>
    public class AssimpModel
    {
        #region Инпут элементы
        public static readonly InputElement[] SkinnedPosNormalTexTanBi = {
    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
    new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
    new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
    new InputElement("TANGENT", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
     new InputElement("BINORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
      new InputElement("BLENDINDICES", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
    new InputElement("BLENDWEIGHT", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),

};
        public static readonly InputElement[] PosNormalTexTanBi = {
    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
    new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
    new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
    new InputElement("TANGENT", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
     new InputElement("BINORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),

};
        public static readonly InputElement[] PosNormalTex = {
    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
    new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
    new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
};

        public static readonly InputElement[] PosNormal = {
    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
    new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
};
        #endregion

        public bool HasAnimations { get; private set; }
        public AssimpMesh[] Meshes { get { return _meshes; } }
        public int AnimationsCount { get { return _animatons.Length; } }
        AssimpMesh[] _meshes;
        Dictionary<string, JointBone> _boneHierarhy;
        AssimpAnimation[] _animatons;

        /// <summary>
        /// Сколько фреймов есть у анимации
        /// </summary>
        /// <param name="animaton">Номер анимации для которой нужно получить данные</param>
        /// <returns>Сколько фреймов есть у анимации</returns>
        public int AnimationNumFrames(int animaton)
        {
            return _animatons[animaton].numFrames;
        }

        /// <summary>
        /// Количество фреймов в секунду
        /// </summary>
        /// <param name="animaton">Номер анимации для которой нужно получить данные</param>
        /// <returns>Количество фреймов в секунду</returns>
        public float AnimFramesPerSecond(int animaton)
        {
            return (float)_animatons[animaton].framesPerSecond;
        }

        /// <summary>
        /// Возврашает массив Костей для скелетной анимации.
        /// </summary>
        /// <param name="animaton">Номер анимации кторую нужно применить.</param>
        /// <param name="frame">Нормер фрейма этой анимации</param>
        /// <returns>Матрицу с преобразований для скелета</returns>
        public Matrix[] GetAnimationFrame(int animaton, int frame)
        {
            return _animatons[animaton].Frames[0];
        }

        /// <summary>
        /// Загружает модель из файла
        /// </summary>
        /// <param name="File">Локальный путь к файлу модели</param>
        public AssimpModel(string File)
        {
            String fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), File);

            using (AssimpContext importer = new AssimpContext())
            {
                NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
                importer.SetConfig(config);
#if DEBUG
                using (LogStream logstream = new LogStream(delegate (String msg, String userData)
                 {

                     Console.WriteLine(msg);


                 }))
                    logstream.Attach();
#endif
                var Model = importer.ImportFile(fileName, PostProcessPreset.ConvertToLeftHanded | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.Triangulate | PostProcessSteps.JoinIdenticalVertices);

                var m_GlobalInverseTransform = Model.RootNode.Transform;
                m_GlobalInverseTransform.Inverse();

                //TODO: Загрузить данные в мои собственные классы и структуры.  
                HasAnimations = false;
                if (Model.Meshes[0].HasBones)
                    _boneHierarhy = GetHierarhy(Model);
                _meshes = GetMeshes(Model);
                if (Model.HasAnimations && Model.Animations[0].HasNodeAnimations)
                {
                    _animatons = GetAnimation(Model, ToMatrix(m_GlobalInverseTransform));
                    HasAnimations = true;
                }

            }
        }

        AssimpMesh[] GetMeshes(Scene model)
        {
            var m = new List<AssimpMesh>();
            foreach (var mesh in model.Meshes)
            {
                m.Add(new AssimpMesh()
                {
                    Indeces = mesh.GetIndices().Select(i => (uint)i).ToArray(),
                    Texture = model.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath,
                    Veteces = GetVertex(mesh).ToArray(),
                    NormalMap = model.Materials[mesh.MaterialIndex].TextureNormal.FilePath,
                    SpecularMap = model.Materials[mesh.MaterialIndex].TextureSpecular.FilePath,
                    DiplacementMap = model.Materials[mesh.MaterialIndex].TextureDisplacement.FilePath,
                    HasBones = mesh.HasBones
                });
            }
            return m.ToArray();
        }

        Dictionary<string, JointBone> GetHierarhy(Scene model)
        {
            Dictionary<string, JointBone> jb = new Dictionary<string, JointBone>();
            GetChildren(model.RootNode, ref jb);
            var m = jb.Where(j => j.Key.Contains("<")).Select(jj => jj.Key).ToArray();
            foreach (var item in m)
            {
                jb.Remove(item);
            }
          var n = jb.Where(ji => ji.Value.ParentName.Contains("<")).Select(jt=>jt.Key).ToArray();
            foreach (var i in n)
            {
                var jbone = jb[i];
                jbone.ParentName= null;
                jb[i] = jbone;
            }
            return jb;
        }

        AssimpAnimation[] GetAnimation(Scene model, Matrix inv)
        {
            var a = new List<AssimpAnimation>();

            foreach (var item in model.Animations)
            {
                a.Add(new AssimpAnimation(item, _boneHierarhy, inv));
            }
            return a.ToArray();
        }

        void GetChildren(Node n, ref Dictionary<string, JointBone> jb)
        {
            jb.Add(n.Name, new JointBone()
            {
                ParentName = n.Parent?.Name,
                Transform = ToMatrix(n.Transform)
            });
            if (n.HasChildren)
                for (int i = 0; i < n.ChildCount; ++i)
                {
                    GetChildren(n.Children[i], ref jb);
                }
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
                    tangent = ToVector3(m.Tangents[i]),
                    biTangent = ToVector3(m.BiTangents[i]),
                    normal = ToVector3(m.Normals[i]),
                    BoneWheight = GetWheightID(m, i),
                    BoneID = GetBoneID(m, i)
                });
            }

            return v;
        }

        Vector4 GetBoneID(Assimp.Mesh m, int i)
        {
            Vector4 ret = new Vector4();
            var my = m.Bones
                     .Where(bb => bb.HasVertexWeights && bb.VertexWeights.Any(tt => tt.VertexID == i))
                     .Select(ib => _boneHierarhy.Values.ToList().IndexOf(_boneHierarhy[ib.Name])).ToArray();
            ret.X = my[0];
            ret.Y = my.Length > 1 ? my[1] : 0;
            ret.Z = my.Length > 2 ? my[2] : 0;
            ret.W = my.Length > 3 ? my[3] : 0;
            return ret;
        }

        Vector4 GetWheightID(Assimp.Mesh m, int i)
        {
            Vector4 ret = new Vector4();
            var my = m.Bones.SelectMany(b => b.VertexWeights).Where(w => w.VertexID == i).ToArray();
            var sum = my.Sum(x => x.Weight);
            ret.X = my[0].Weight;
            ret.Y = my.Length > 1 ? my[1].Weight : 0;
            ret.Z = my.Length > 2 ? my[2].Weight : 0;
            ret.W = my.Length > 3 ? my[3].Weight : 0;
            return ret;
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

