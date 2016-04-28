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
    public struct AssimpVertex
    {
        public Vector3 position;
        public Vector3 uv;
        public Vector3 normal;
        public Vector3 tangent;
        public Vector3 biTangent;
    }

    public class AssimpMesh
    {
        public string Texture { get; set; } = null;
        public AssimpVertex[] Veteces { get; set; } = null;
        public int[] Indeces { get; set; } = null;
        public string NormalMap { get; set; } = null;
        public string DiplacementMap { get; set; }=null;
        public string SpecularMap { get; set; }= null;
    }

    public class AssimpModel
    {
        Scene model;
        public List<AssimpMesh> Meshes { get;}
        public AssimpModel(string File)
        { 
            String fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), File);

            using (AssimpContext importer = new AssimpContext())
            {
                NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
                importer.SetConfig(config);
                using (LogStream logstream = new LogStream(delegate (String msg, String userData)
                 {
                    
                     Console.WriteLine(msg);

                 }))
                {
                    logstream.Attach();

                    model = importer.ImportFile(fileName, PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.GenerateNormals | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.MakeLeftHanded | PostProcessSteps.RemoveComponent);

                    //TODO: Загрузить данные в мои собственные классы и структуры.  
                    Meshes = GetMeshes();
                }
            }
        }

        public List<AssimpMesh> GetMeshes()
        {
            var m = new List<AssimpMesh>();
            foreach (var mesh in model.Meshes)
            {
                m.Add(new AssimpMesh()
                {
                    Indeces = mesh.GetIndices(),
                    Texture = model.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath,
                    Veteces = GetVertex(mesh).ToArray(),
                    NormalMap = model.Materials[mesh.MaterialIndex].TextureNormal.FilePath,
                    SpecularMap = model.Materials[mesh.MaterialIndex].TextureSpecular.FilePath,
                    DiplacementMap = model.Materials[mesh.MaterialIndex].TextureDisplacement.FilePath
                });
            }
            return m;
        }


        public List<AssimpVertex> GetVertex(Assimp.Mesh m)
        {
            List<AssimpVertex> v = new List<AssimpVertex>();

            for (int i = 0; i < m.VertexCount; ++i)
            {
                v.Add(new AssimpVertex()
                {
                    position = new Vector3(m.Vertices[i].X, m.Vertices[i].Y, m.Vertices[i].Z),
                    uv = new Vector3(m.TextureCoordinateChannels[0][i].X, m.TextureCoordinateChannels[0][i].Y, m.TextureCoordinateChannels[0][i].Z),
                    normal = new Vector3(m.Normals[i].X, m.Normals[i].Y, m.Normals[i].Z),
                    tangent = new Vector3(m.Tangents[i].X, m.Tangents[i].Y, m.Tangents[i].Z),
                    biTangent =ToVector3( m.BiTangents[i] )
                });
            }

            return v;
        }


         Vector3 ToVector3( Vector3D v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

    }
}

