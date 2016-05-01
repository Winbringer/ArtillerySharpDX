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
        public string Name;
        public Matrix Transform;
        public Matrix Offset;
    }

    struct Joint
    {
        public string PName;
        public string Name;
        public Vector3 Pos;
        public SharpDX.Quaternion Quat;
        public Matrix scaling;
        public Matrix matrix;
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

    class AssimpAnimation
    {
        public int numFrames { get; private set; }
        public double framesPerSecond { get; private set; }
        public Matrix[][] Frames { get; private set; }


        public AssimpAnimation(Animation animation, Dictionary<string, JointBone> Hierarhy)
        {
            Dictionary<string, JointBone> hier = new Dictionary<string, JointBone>();
            for (int i = 0; i < Hierarhy.Count; i++)
            {
                hier.Add(Hierarhy.ElementAt(i).Key, Hierarhy.ElementAt(i).Value);
            }
            numFrames = (int)animation.DurationInTicks + 1;
            framesPerSecond = animation.TicksPerSecond != 0 ? animation.TicksPerSecond : 25d;
            Dictionary<string, Joint>[] Transforms = GetTransforms(animation, hier, numFrames);
            Frames = ToFrames(Transforms, hier);
        }

        Matrix[][] ToFrames(Dictionary<string, Joint>[] tr, Dictionary<string, JointBone> hier)
        {
            List<Matrix[]> m = new List<Matrix[]>();
            for (int i = 0; i < numFrames; i++)
            {
                m.Add(tr[i].Select(j => hier[j.Value.Name].Offset * j.Value.matrix).ToArray());
            }
            return m.ToArray();
        }

        Dictionary<string, Joint>[] GetTransforms(Animation a, Dictionary<string, JointBone> h, int NumFrames)
        {
            List<Dictionary<string, Joint>> tr = new List<Dictionary<string, Joint>>();
            for (int i = 0; i < numFrames; i++)
            {
                tr.Add(GetJoints(a, i, h));
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
                    Pos = (node.PositionKeys.Any(pky => pky.Time == f) ? node.PositionKeys.First(pk => pk.Time == f).Value : node.PositionKeys.First(pk => pk.Time == node.PositionKeys.Max(m => m.Time)).Value).ToVector3(),
                    Quat = (node.RotationKeys.Any(pky => pky.Time == f) ? node.RotationKeys.First(pk => pk.Time == f).Value : node.RotationKeys.First(pk => pk.Time == node.RotationKeys.Max(m => m.Time)).Value).ToQuat(),
                    scaling =Matrix4x4.FromScaling(node.ScalingKeys.Any(pky => pky.Time == f) ? node.ScalingKeys.First(pk => pk.Time == f).Value : node.ScalingKeys.First(pk => pk.Time == node.ScalingKeys.Max(m => m.Time)).Value).ToMatrix()
                });
            }

            Dictionary<string, Joint> jbilded = buildJoints(j);
            return jbilded;
        }

        Dictionary<string, Joint> buildJoints(Dictionary<string, Joint> joints)
        {
            for (int i = 0; i < joints.Count; i++)
            {
                var j = joints.ElementAt(i);
                if (j.Value.PName != null)
                {
                    Joint b = j.Value;
                    b.Pos = joints[b.PName].Pos + Vector3.Transform(b.Pos, joints[b.PName].Quat);
                    b.Quat = joints[b.PName].Quat * b.Quat;
                    b.scaling = joints[b.PName].scaling * b.scaling;
                    b.matrix =b.scaling* Matrix.AffineTransformation(1, b.Quat, b.Pos);
                    joints[b.Name] = b;
                }
                if (string.IsNullOrEmpty(j.Value.PName))
                {
                    Joint b = j.Value;
                    b.matrix = Matrix.AffineTransformation(1, b.Quat, b.Pos);
                    joints[b.Name] = b;
                }
            }
            return joints;
        }

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
        AssimpAnimation[] _animatons;


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
                var Model = importer.ImportFile(fileName, PostProcessPreset.ConvertToLeftHanded | PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace );

                //TODO: Загрузить данные в мои собственные классы и структуры.  
                Dictionary<string, JointBone> _boneHierarhy = new Dictionary<string, JointBone>();

                HasAnimations = false;
                if (Model.Meshes[0].HasBones)
                   _boneHierarhy = GetHierarhy(Model);
                _meshes = GetMeshes(Model, _boneHierarhy);
                if (Model.HasAnimations && Model.Animations[0].HasNodeAnimations)
                {
                    _animatons = GetAnimation(Model, _boneHierarhy);
                    HasAnimations = true;
                }

            }
        }



        #region Пример из Гита
        private void ComputeBoundingBox(Scene m_model)
        {
            var m_sceneMin = new Vector3(1e10f, 1e10f, 1e10f);
            var m_sceneMax = new Vector3(-1e10f, -1e10f, -1e10f);
            var m_sceneCenter = new Vector3(0);
            Matrix identity = Matrix.Identity;

            ComputeBoundingBox(m_model, m_model.RootNode, ref m_sceneMin, ref m_sceneMax, ref identity);

            m_sceneCenter.X = (m_sceneMin.X + m_sceneMax.X) / 2.0f;
            m_sceneCenter.Y = (m_sceneMin.Y + m_sceneMax.Y) / 2.0f;
            m_sceneCenter.Z = (m_sceneMin.Z + m_sceneMax.Z) / 2.0f;
        }

        private void ComputeBoundingBox(Scene m_model, Node node, ref Vector3 min, ref Vector3 max, ref Matrix trafo)
        {
            Matrix prev = trafo;
            trafo = Matrix.Multiply(prev, FromMatrix(node.Transform));

            if (node.HasMeshes)
            {
                foreach (int index in node.MeshIndices)
                {
                    Assimp.Mesh mesh = m_model.Meshes[index];
                    for (int i = 0; i < mesh.VertexCount; i++)
                    {
                        Vector3 tmp = FromVector(mesh.Vertices[i]);
                        Vector3.Transform(ref tmp, ref trafo, out tmp);

                        min.X = Math.Min(min.X, tmp.X);
                        min.Y = Math.Min(min.Y, tmp.Y);
                        min.Z = Math.Min(min.Z, tmp.Z);

                        max.X = Math.Max(max.X, tmp.X);
                        max.Y = Math.Max(max.Y, tmp.Y);
                        max.Z = Math.Max(max.Z, tmp.Z);
                    }
                }
            }

            for (int i = 0; i < node.ChildCount; i++)
            {
                ComputeBoundingBox(m_model, node.Children[i], ref min, ref max, ref trafo);
            }
            trafo = prev;
        }

        private Matrix FromMatrix(Matrix4x4 mat)
        {
            Matrix m = new Matrix();
            m.M11 = mat.A1;
            m.M12 = mat.A2;
            m.M13 = mat.A3;
            m.M14 = mat.A4;
            m.M21 = mat.B1;
            m.M22 = mat.B2;
            m.M23 = mat.B3;
            m.M24 = mat.B4;
            m.M31 = mat.C1;
            m.M32 = mat.C2;
            m.M33 = mat.C3;
            m.M34 = mat.C4;
            m.M41 = mat.D1;
            m.M42 = mat.D2;
            m.M43 = mat.D3;
            m.M44 = mat.D4;
            return m;
        }

        private Vector3 FromVector(Vector3D vec)
        {
            Vector3 v;
            v.X = vec.X;
            v.Y = vec.Y;
            v.Z = vec.Z;
            return v;
        }

        private Color4 FromColor(Color4D color)
        {
            Color4 c;
            c.Red = color.R;
            c.Green = color.G;
            c.Blue = color.B;
            c.Alpha = color.A;
            return c;
        }
        #endregion

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
            if (!HasAnimations) throw new Exception("У этой модели нет скелетной анимации!");
            return _animatons[animaton].Frames[frame];
        }

        AssimpMesh[] GetMeshes(Scene model, Dictionary<string, JointBone> _boneHierarhy)
        {
            var m = new List<AssimpMesh>();
            foreach (var mesh in model.Meshes)
            {
                m.Add(new AssimpMesh()
                {
                    Indeces = mesh.GetIndices().Select(i => (uint)i).ToArray(),
                    Texture = model.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath,
                    Veteces = GetVertex(mesh,_boneHierarhy).ToArray(),
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
            GetChildren(model.RootNode, ref jb, ref model);
            var m = jb.Where(j => j.Key.Contains("<")).Select(jj => jj.Key).ToArray();
            foreach (var item in m)
            {
                jb.Remove(item);
            }
            var n = jb.Where(ji => ji.Value.ParentName.Contains("<")).Select(jt => jt.Key).ToArray();
            foreach (var i in n)
            {
                var jbone = jb[i];
                jbone.ParentName = null;
                jb[i] = jbone;
            }
            return jb;
        }

        AssimpAnimation[] GetAnimation(Scene model, Dictionary<string, JointBone> _boneHierarhy)
        {

            var a = new List<AssimpAnimation>();

            foreach (var item in model.Animations)
            {
                a.Add(new AssimpAnimation(item, _boneHierarhy));
            }
            return a.ToArray();
        }

        void GetChildren(Node n, ref Dictionary<string, JointBone> jb, ref Scene model)
        {
            jb.Add(n.Name, new JointBone()
            {
                ParentName = n.Parent?.Name,
                Transform = n.Transform.ToMatrix(),
                Name = n.Name,
                Offset = model.Meshes.SelectMany(m => m.Bones).FirstOrDefault(b => b.Name == n.Name)?.OffsetMatrix.ToMatrix() ?? Matrix.Identity

            });
            if (n.HasChildren)
                for (int i = 0; i < n.ChildCount; ++i)
                {
                    GetChildren(n.Children[i], ref jb, ref model);
                }
        }

        List<AssimpVertex> GetVertex(Assimp.Mesh m, Dictionary<string, JointBone> _boneHierarhy)
        {
            List<AssimpVertex> v = new List<AssimpVertex>();

            for (int i = 0; i < m.VertexCount; ++i)
            {
                Vector3 cc = new Vector3(m.Vertices[i].X, m.Vertices[i].Y, m.Vertices[i].Z);
                v.Add(new AssimpVertex()
                {
                    position = cc,
                    uv =m.TextureCoordinateChannelCount>0? new Vector3(m.TextureCoordinateChannels[0][i].X, m.TextureCoordinateChannels[0][i].Y, m.TextureCoordinateChannels[0][i].Z): new Vector3(),
                    tangent =m.Tangents.Count>0? m.Tangents[i].ToVector3(): new Vector3(),
                    biTangent =m.BiTangents.Count>0? m.BiTangents[i].ToVector3():new Vector3(),
                    normal =m.HasNormals? m.Normals[i].ToVector3():new Vector3(),
                    BoneWheight =m.HasBones? GetWheightID(m, i): new Vector4(),
                    BoneID = m.HasBones ? GetBoneID(m, i, _boneHierarhy): new Vector4()
                });
            }

            return v;
        }

        Vector4 GetBoneID(Assimp.Mesh m, int i, Dictionary<string, JointBone> _boneHierarhy)
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

            ret.X = my[0].Weight;
            ret.Y = my.Length > 1 ? my[1].Weight : 0;
            ret.Z = my.Length > 2 ? my[2].Weight : 0;
            ret.W = my.Length > 3 ? my[3].Weight : 0;
            return ret;
        }
    }
}

