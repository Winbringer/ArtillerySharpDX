using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VictoremLibrary
{
    struct Face
    {
        public int VID;
        public int VnID;
        public int VtID;
    }
    class OBJMesh
    {
        public string MTLFile;
        public string Material;
        public Vector3[] V;
        public Vector3[] Vn;
        public Vector3[] Vt;
        public List<Face> F;
        public OBJMesh()
        {
            F = new List<Face>();
        }
    }
    class OBJModel
    {

        #region Поля и свойства

        public Matrix World { get; set; }       
        public List<Mesh> Meshes = new List<Mesh>();

        #endregion

        public OBJModel(DeviceContext dx11Context, string path, string obj)
        {
           
            World = Matrix.Identity;
            List<OBJMesh> o = ReadOBJFile(path, obj);
            foreach (var item in o)
            {
                Meshes.Add(GetMesh(item, dx11Context.Device, path));
            }
        }

        #region Методы

        Mesh GetMesh(OBJMesh obj, Device dv, string path)
        {

            CultureInfo infos = CultureInfo.InvariantCulture;
            List<Vertex> faces = new List<Vertex>();
            List<uint> index = new List<uint>();
            uint Count = 0;

            var coords = obj.F;
            foreach (var item in coords)
            {


                Vector3 V = obj.V[item.VID];

                Vector3 Vt = obj.Vt[item.VtID];

                Vector3 Vn = obj.Vn[item.VnID];

                Vertex face = new Vertex();
                face.position = V;
                face.textureUV = new Vector2(Vt.X, Vt.Y);
                face.position = Vn;
                int i = faces.FindIndex(t => (t.position == face.position) && (t.normal == face.normal) && (t.textureUV == face.textureUV));
                if (i >= 0)
                {
                    index.Add((uint)i);
                }
                else
                {
                    faces.Add(face);
                    index.Add(Count);
                    ++Count;
                }

            }

            return new Mesh(dv, faces.ToArray(), index.ToArray(), obj.Material, obj.MTLFile + path);
        }

        private List<OBJMesh> ReadOBJFile(string path, string obj)
        {
            var V = new List<Vector3>();
            var Vn = new List<Vector3>();
            var Vt = new List<Vector3>();
            List<OBJMesh> meshes = new List<OBJMesh>();
            string MTLfile = null;
            string material = null;
            meshes.Add(new OBJMesh());
            using (StreamReader reader = new StreamReader(obj))
            {
                while (true)
                {
                    string l = reader.ReadLine();
                    if (reader.EndOfStream)
                        break;
                    if (l.Contains("#") || string.IsNullOrEmpty(l.Trim()))
                        continue;

                    if (l.Contains("v ")) V.Add(GetVector("v ", l));
                    if (l.Contains("vn ")) Vn.Add(GetVector("vn ", l));
                    if (l.Contains("vt ")) Vt.Add(GetVector("vt ", l));

                    if (l.Contains("mtllib ")) MTLfile = l.Replace("mtllib ", "").Trim();
                    if (l.Contains("usemtl ")) material = l.Replace("usemtl ", "").Trim();

                    if (l.Contains("g "))
                    {
                        meshes.Add(new OBJMesh() { V = V.ToArray(), Vn = Vn.ToArray(), Vt = Vt.ToArray(), Material = material, MTLFile = MTLfile });
                        V.Clear();
                        Vt.Clear();
                        Vn.Clear();
                    }
                    if (l.Contains("f "))
                    {
                        l = l.Replace("f ", "").Trim();
                        var nn = l.Split(' ');
                        var f1 = nn[0].Split('/');
                        var f2 = nn[1].Split('/');
                        var f3 = nn[2].Split('/');
                        Face f = new Face();
                        f.VID = IParse(f1[0]) - 1;
                        f.VtID = IParse(f1[1]) - 1;
                        f.VnID = IParse(f1[2]) - 1;
                        meshes.Last().F.Add(f);
                        f.VID = IParse(f2[0]) - 1;
                        f.VtID = IParse(f2[1]) - 1;
                        f.VnID = IParse(f2[2]) - 1;
                        meshes.Last().F.Add(f);
                        f.VID = IParse(f3[0]) - 1;
                        f.VtID = IParse(f3[1]) - 1;
                        f.VnID = IParse(f3[2]) - 1;
                        meshes.Last().F.Add(f);

                    }
                }
            }
            return meshes;
        }

        private Vector3 GetVector(string type, string line)
        {
            CultureInfo infos = CultureInfo.InvariantCulture;
            string[] coords = line.Replace(type, "").Trim().Split(' ');
            Vector3 vector = new Vector3(Convert.ToSingle(coords[0], infos),
                Convert.ToSingle(coords[1], infos),
                Convert.ToSingle(coords[2], infos));
            return vector;
        }

        private int IParse(string s)
        {
            return int.Parse(s, CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
