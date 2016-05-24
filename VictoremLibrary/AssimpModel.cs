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
    public struct JointBone
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

    public class AssimpAnimation
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
                    scaling = Matrix4x4.FromScaling(node.ScalingKeys.Any(pky => pky.Time == f) ? node.ScalingKeys.First(pk => pk.Time == f).Value : node.ScalingKeys.First(pk => pk.Time == node.ScalingKeys.Max(m => m.Time)).Value).ToMatrix()
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
                    b.matrix = b.scaling * Matrix.AffineTransformation(1, b.Quat, b.Pos);
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

    public class Mesh3D : Component<AssimpVertex>
    {
        ShaderResourceView _textures;
        public ShaderResourceView Texture { get { return _textures; } set { Utilities.Dispose(ref _textures); _textures = value; } }
        ShaderResourceView _normalMap;
        public ShaderResourceView NormalMap { get { return _normalMap; } set { Utilities.Dispose(ref _normalMap); _normalMap = value; } }
        ShaderResourceView _specularMap;
        public ShaderResourceView SpecularMap { get { return _specularMap; } set { Utilities.Dispose(ref _specularMap); _specularMap = value; } }
        ShaderResourceView _displacementMap;
        public ShaderResourceView DisplacementMap { get { return _displacementMap; } set { Utilities.Dispose(ref _displacementMap); _displacementMap = value; } }

        public Mesh3D(SharpDX.Direct3D11.Device device, AssimpMesh mesh, string texturFolder)
        {
            this._indeces = mesh.Indeces;
            this._veteces = mesh.Veteces;
            InitBuffers(device);
            if (!string.IsNullOrEmpty(mesh.Texture))
                _textures = StaticMetods.LoadTextureFromFile(device.ImmediateContext, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, texturFolder + mesh.Texture)));
            if (!string.IsNullOrEmpty(mesh.NormalMap))
                _normalMap = StaticMetods.LoadTextureFromFile(device.ImmediateContext, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, texturFolder + mesh.NormalMap)));
            if (!string.IsNullOrEmpty(mesh.SpecularMap))
                _specularMap = StaticMetods.LoadTextureFromFile(device.ImmediateContext, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, texturFolder + mesh.SpecularMap)));
            if (!string.IsNullOrEmpty(mesh.DiplacementMap))
                _displacementMap = StaticMetods.LoadTextureFromFile(device.ImmediateContext, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, texturFolder + mesh.DiplacementMap)));
        }

        public override void Dispose()
        {
            base.Dispose();
            Utilities.Dispose(ref _textures);
            Utilities.Dispose(ref _normalMap);
            Utilities.Dispose(ref _displacementMap);
            Utilities.Dispose(ref _specularMap);
        }
    }

    /// <summary>
    /// Класс для загрузки 3Д моделей и скелейтной анимации из файлов
    /// </summary>
    public class AssimpModel : IDisposable
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


        AssimpAnimation[] _animatons;
        List<Mesh3D> _3dMeshes;
        /// <summary>
        /// Есть ли у модели анимация
        /// </summary>
        public bool HasAnimations { get; private set; }
        /// <summary>
        /// Количество анимаций модели
        /// </summary>
        public int AnimationsCount { get { return _animatons.Length; } }
        /// <summary>
        /// Меши нашей анимации с буферами данных и ресурсами текстур
        /// </summary>
        public List<Mesh3D> Meshes3D { get { return _3dMeshes; } }
        /// <summary>
        /// Анимация содержащая готовые матрицы преобразований скелета
        /// </summary>
        public AssimpAnimation[] Animatons { get { return _animatons; } }
        /// <summary>
        /// Загружает 3Д модель из файла
        /// </summary>
        /// <param name="dc">Устройстов для рендеринга модели</param>
        /// <param name="Folder">Папка с моделью</param>
        /// <param name="File">Файлы модели</param>
        public AssimpModel(DeviceContext dc, string Folder, string File)
        {
            string fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Folder + File);

            using (AssimpContext importer = new AssimpContext())
            {
                NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
                importer.SetConfig(config);
                var Model = importer.ImportFile(fileName, PostProcessPreset.ConvertToLeftHanded | PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace);

                //TODO: Загрузить данные в мои собственные классы и структуры.  
                Dictionary<string, JointBone> _boneHierarhy = new Dictionary<string, JointBone>();

                HasAnimations = false;
                if (Model.Meshes[0].HasBones)
                    _boneHierarhy = GetHierarhy(Model);
                AssimpMesh[] _meshes = GetMeshes(Model, _boneHierarhy);
                if (Model.HasAnimations && Model.Animations[0].HasNodeAnimations)
                {
                    _animatons = GetAnimation(Model, _boneHierarhy);
                    HasAnimations = true;
                }
                _3dMeshes = new List<Mesh3D>();
                foreach (var item in _meshes)
                {
                    _3dMeshes.Add(new Mesh3D(dc.Device, item, Folder));
                }

            }
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
                    Veteces = GetVertex(mesh, _boneHierarhy).ToArray(),
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
            var n = jb.Where(ji => ji.Value.ParentName != null && ji.Value.ParentName.Contains("<")).Select(jt => jt.Key).ToArray();
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
                    uv = m.TextureCoordinateChannelCount > 0 ? new Vector3(m.TextureCoordinateChannels[0][i].X, m.TextureCoordinateChannels[0][i].Y, m.TextureCoordinateChannels[0][i].Z) : new Vector3(),
                    tangent = m.Tangents.Count > 0 ? m.Tangents[i].ToVector3() : new Vector3(),
                    biTangent = m.BiTangents.Count > 0 ? m.BiTangents[i].ToVector3() : new Vector3(),
                    normal = m.HasNormals ? m.Normals[i].ToVector3() : new Vector3(),
                    BoneWheight = m.HasBones ? GetWheightID(m, i) : new Vector4(),
                    BoneID = m.HasBones ? GetBoneID(m, i, _boneHierarhy) : new Vector4()
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

        public void Dispose()
        {
            for (int i = 0; i < _3dMeshes.Count; i++)
            {
                _3dMeshes?[i]?.Dispose();
            }
        }
    }
}

