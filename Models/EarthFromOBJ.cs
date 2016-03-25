using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SharpDX11GameByWinbringer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MtlMaterial
    {
        public float Ns_SpecularPower;
        public float Ni_OpticalDensity;
        public float d_Transparency;
        public float Tr_Transparency;
        public Vector3 Tf_TransmissionFilter;
        float padding0;
        public Color4 Ka_AmbientColor;
        public Color4 Kd_DiffuseColor;
        public Color4 Ks_SpecularColor;
        public Color4 Ke_EmissiveColor;
    }

    public struct Face
    {
        public Vector3 V;
        public Vector3 Vn;
        public Vector3 Vt;
    }

    public class EarthFromOBJ : IDisposable
    {
        #region Поля и свойства
        Buffer _vertexBuffer;
        DeviceContext _dx11Context;
        public Matrix World { get; set; }
        int facesCount;
        #endregion

        public EarthFromOBJ(DeviceContext dx11Context)
        {
            _dx11Context = dx11Context;
            const string obj = "3DModelsFiles\\Earth\\earth.obj";
            const string mtl = "3DModelsFiles\\Earth\\earth.mtl";            
            const string jpg = "3DModelsFiles\\Earth\\earthmap.jpg";           

            List<Face> faces = GetFaces(obj);
            facesCount = faces.Count;
            _vertexBuffer = Buffer.Create(dx11Context.Device, BindFlags.VertexBuffer, faces.ToArray());
                 

        }


        #region Методы
        public void Draw()
        {
            _dx11Context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            _dx11Context.Draw(facesCount, 0);
        }
        private List<Face> GetFaces(string objFile)
        {
            CultureInfo infos = CultureInfo.InvariantCulture;
            List<string> lines = ReadOBJFile(objFile);
            List<Vector3> verteces = GetVectors("v ", lines);
            List<Vector3> normals = GetVectors("vn ", lines);
            List<Vector3> textureUVW = GetVectors("vt ", lines);
            List<Face> faces = new List<Face>();
            foreach (string line in lines)
            {
                if (line.Contains("f "))
                {
                    string[] coords = line.Replace("f ", "").Trim().Split(' ');
                    foreach (var item in coords)
                    {
                        string[] indeces = item.Split('/');
                        Face face = new Face();
                        face.V = verteces[int.Parse(indeces[0],infos)-1];
                        face.Vt = textureUVW[int.Parse(indeces[1],infos)-1];
                        face.Vn = normals[int.Parse(indeces[2], infos)-1];
                        faces.Add(face);                       
                    }
                }
                
            }
            return faces;
        }
        private List<string> ReadOBJFile(string obj)
        {
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(obj))
            {
                while (true)
                {
                    string l = reader.ReadLine();
                    if (reader.EndOfStream)
                        break;

                    if (l.Contains("#") || string.IsNullOrEmpty(l.Trim()))
                        continue;

                    lines.Add(l);
                }
            }
            return lines;
        }
        private List<Vector3> GetVectors(string type, List<string> lines)
        {
            CultureInfo infos = CultureInfo.InvariantCulture;
            List<Vector3> vectors = new List<Vector3>();
            foreach (string line in lines)
            {
                if (line.Contains(type))
                {
                    string[] coords = line.Replace(type, "").Trim().Split(' ');
                    vectors.Add(new Vector3(Convert.ToSingle(coords[0], infos), Convert.ToSingle(coords[1], infos), Convert.ToSingle(coords[2], infos)));
                }
            }
            return vectors;
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _vertexBuffer);
        }
        #endregion
    }
}
