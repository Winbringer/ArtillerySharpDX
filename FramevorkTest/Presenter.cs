using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using VictoremLibrary;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using System.Runtime.InteropServices;

namespace FramevorkTest
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct AnimConst
    {
        public Matrix WVP;
        public uint HasAnimaton;
        public uint HasDiffuseTexture;
        Vector2 padding0;
        public Matrix World;

        public AnimConst(Matrix w, Matrix v, Matrix p, uint HasAnim, uint HasTex)
        {

            HasAnimaton = HasAnim;
            HasDiffuseTexture = HasTex;
            WVP = w * v * p;
            World = w;
            padding0 = new Vector2();
        }

        public void Transpose()
        {
            WVP.Transpose();
        }
    }
    public class BonesConst
    {
        public Matrix[] Bones;

        public BonesConst()
        {
            Bones = new Matrix[1024];

        }
        public void init(Matrix[] bones)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var m = bones[i];
                m.Transpose();
                Bones[i] = m;
            }
        }
        public static int Size()
        {
            return Utilities.SizeOf<Matrix>() * 1024;
        }

    }

    class Presenter : IDisposable
    {
        TextWirter Drawer2d;
        Bitmap bitmap;
        Shader s;
        private AssimpModel m;
        SamplerState samler;
        public Presenter(Game game)
        {
            game.OnDraw += Draw;
            game.OnUpdate += Upadate;
            game.OnKeyPressed += KeyKontroller;
            //var srcTextureSRV = StaticMetods.LoadTextureFromFile(game.DeviceContext, "Village.png");
            //var b = StaticMetods.LoadBytesFormFile(game.DeviceContext, "Village.png");
            //var intt = game.FilterFoTexture.Histogram(srcTextureSRV);
            //game.FilterFoTexture.SobelEdgeColor(ref srcTextureSRV, 0.5f);
            //game.FilterFoTexture.Sepia(ref srcTextureSRV, 0.5f);
            //game.FilterFoTexture.Contrast(ref srcTextureSRV, 2f);
            //Drawer2d = new TextWirter(game.SwapChain.GetBackBuffer<Texture2D>(0), 800, 600);
            //bitmap = StaticMetods.GetBitmapFromSRV(srcTextureSRV, Drawer2d.RenderTarget);
            //Drawer2d.SetTextColor(Color.Red);
            //Drawer2d.SetTextSize(36);
            var sD = SamplerStateDescription.Default();
            sD.AddressU = TextureAddressMode.Wrap;
            sD.AddressV = TextureAddressMode.Wrap;
            sD.AddressW = TextureAddressMode.Wrap;
            sD.MaximumAnisotropy = 16;
            sD.MaximumLod = float.MaxValue;
            sD.MinimumLod = 0;
            sD.Filter = SharpDX.Direct3D11.Filter.MinMagMipLinear;
            samler = new SamplerState(game.DeviceContext.Device, sD);
            s = new Shader(game.DeviceContext, Environment.CurrentDirectory + "\\Assimp.hlsl", AssimpModel.SkinnedPosNormalTexTanBi);
            m = new AssimpModel("3DModelsFiles\\Wm\\Female.md5mesh");
        }

        private void KeyKontroller(object sender, EventArgs e)
        {
            var a = (UpdateArgs)e;
        }

        private void Upadate(object sender, EventArgs e)
        {
            var a = (UpdateArgs)e;

        }
        int frame = 0;
        BonesConst bo = new BonesConst();
        private void Draw(object sender, EventArgs e)
        {
            var a = (UpdateArgs)e;
            var g = (Game)sender;
            var w = Matrix.Identity;//Matrix.RotationX(MathUtil.PiOverTwo);
            var vi = Matrix.LookAtLH(new Vector3(0, 50, -100), Vector3.Zero, Vector3.Up);
            var p = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, g.Form.Width / (float)g.Form.Height, 1f, 1000f);
            var MVP = new AnimConst(w, vi, p, 1, 1);
            MVP.Transpose();
            ++frame;
            if (frame >= m.AnimationNumFrames(0)) frame = 0;
            bo.init(m.GetAnimationFrame(0, frame));
            foreach (var mesh in m.Meshes)
                using(var tex = StaticMetods.LoadTextureFromFile(g.DeviceContext, Environment.CurrentDirectory+"\\3DModelsFiles\\Wm\\" + mesh.Texture))
                using (var v = Buffer.Create(g.DeviceContext.Device, BindFlags.VertexBuffer, mesh.Veteces))
                using (var index = Buffer.Create(g.DeviceContext.Device, BindFlags.IndexBuffer, mesh.Indeces))
                using (var cb = Buffer.Create(g.DeviceContext.Device, ref MVP, new BufferDescription(Utilities.SizeOf<AnimConst>(), BindFlags.ConstantBuffer, ResourceUsage.Default)))
                using (var cb2 = new Buffer(g.DeviceContext.Device, BonesConst.Size(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0))
                {
                    g.DeviceContext.UpdateSubresource(ref MVP, cb);
                   g.DeviceContext.UpdateSubresource(bo.Bones, cb2);
                    s.Begin( new[] { samler }, new[] { tex }, new[] { cb, cb2 });
                    g.Drawer.DrawIndexed(new VertexBufferBinding(v, Utilities.SizeOf<AssimpVertex>(), 0), index, mesh.Indeces.Length);
                    s.End();

                }
            //Drawer2d.DrawBitmap(bitmap);
            //Drawer2d.DrawText("ПОЕХАЛИ!");

        }

        public void Dispose()
        {
            Drawer2d?.Dispose();
            bitmap?.Dispose();
            s?.Dispose();
            samler.Dispose();
        }
    }
}
