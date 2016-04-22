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
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 textureUV;
    }

    public struct Mtl
    {
        public float Ns_SpecularPower;
        public float Ni_OpticalDensity;
        public float d_Transparency;
        public float Tr_Transparency;
        public Vector3 Tf_TransmissionFilter;
        public Color4 Ka_AmbientColor;
        public Color4 Kd_DiffuseColor;
        public Color4 Ks_SpecularColor;
        public Color4 Ke_EmissiveColor;
        public string Amb_Map;
        public string Dif_Map;
        public string Spec_Map;
        public string Norm_Map;
        public string Disp_Map;
    }

    public class Mesh : Component<Vertex>
    {
        public Mtl Material;
       
        public Mesh(Vertex[] ver, uint[] ind, Device dv)
        {
            this._indeces = ind;
            this._veteces = ver;
            Material = new Mtl();
           InitBuffers(dv);
        }

        public void UpdateVertBuffers(DeviceContext dt,Vertex[] vert)
        {
            dt.UpdateSubresource(vert, _vertexBuffer);
        }

        public void SetMaterial(string mtlFile)
        {
            CultureInfo infos = CultureInfo.InvariantCulture;           
            using (StreamReader reader = new StreamReader(mtlFile))
            {
                while (true)
                {
                    string l = reader.ReadLine();
                    if (reader.EndOfStream) break;
                    if (l.Contains("map_Ka "))
                        Material.Amb_Map= l.Replace("map_Ka ", "").Trim();
                    if (l.Contains("map_Kd "))
                        Material.Dif_Map = l.Replace("map_Ka ", "").Trim();
                    if (l.Contains("Ns "))
                        Material.Ns_SpecularPower = float.Parse(l.Replace("Ns ", "").Trim(), infos);
                    if (l.Contains("Ni "))
                        Material.Ni_OpticalDensity = float.Parse(l.Replace("Ni ", "").Trim(), infos);

                    if (l.Contains("\td "))
                        Material.d_Transparency = float.Parse(l.Replace("d ", "").Trim(), infos);

                    if (l.Contains("Tr "))
                        Material.Tr_Transparency = float.Parse(l.Replace("Tr ", "").Trim(), infos);

                    if (l.Contains("Tf "))
                    {
                        var val = l.Replace("Tf ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        Material.Tf_TransmissionFilter = new Vector3(val[0], val[1], val[2]);
                    }
                    if (l.Contains("Ka "))
                    {
                        var val = l.Replace("Ka ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        Material.Ka_AmbientColor = new Color4(val[0], val[1], val[2], 1);
                    }
                    if (l.Contains("Kd "))
                    {
                        var val = l.Replace("Kd ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        Material.Kd_DiffuseColor = new Color4(val[0], val[1], val[2], 1);
                    }
                    if (l.Contains("Ks "))
                    {
                        var val = l.Replace("Ks ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        Material.Ks_SpecularColor = new Color4(val[0], val[1], val[2], 1);
                    }
                    if (l.Contains("Ke "))
                    {
                        var val = l.Replace("Ke ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        Material.Ke_EmissiveColor = new Color4(val[0], val[1], val[2], 1);
                    }
                }
            }
        }
    }
}
