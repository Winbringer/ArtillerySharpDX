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
        Vector3 padding0;
        
        public AnimConst(Matrix w, Matrix v, Matrix p, uint HasAnim)
        {
          
            HasAnimaton = HasAnim;
            WVP = w * v * p;           
            padding0 = new Vector3();
        }       
        
        public void Transpose()
        {
            WVP.Transpose();
        }
    }
    public class BonesConst
    {
        public Matrix[] Bones;

        public BonesConst(Matrix[] bones)
        {
            Bones = new Matrix[1024];
            for (int i = 0; i < bones.Length; i++)
            {
                var m = bones[i];
                m.Transpose();
                Bones[i] = m;
            }
        }
        public int Size2()
        {
            return Utilities.SizeOf<Matrix>() * 1024;
        }

    }

    class Presenter : IDisposable
    {
        TextWirter Drawer2d;
        Bitmap bitmap;

        private AssimpModel m;

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

           m = new AssimpModel("3DModelsFiles\\Wm\\Female.md5mesh");

         var   bbb = 10;
        //  m.AplyAnimashonFrame(0,20);
        }

        private void KeyKontroller(object sender, EventArgs e)
        {
            var a = (UpdateArgs)e;
        }

        private void Upadate(object sender, EventArgs e)
        {
            var a = (UpdateArgs)e;

        }

        private void Draw(object sender, EventArgs e)
        {
            var a = (UpdateArgs)e;
            var g = (Game)sender;
            var w = Matrix.RotationX(MathUtil.PiOverTwo);
            var vi = Matrix.LookAtLH(new Vector3(-10, 10, -70), Vector3.Zero, Vector3.Up);
            var p = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, g.Form.Width / (float)g.Form.Height, 1f, 1000f);
            var MVP = new AnimConst(w, vi, p,0);// m.GetAnimationFrame(0,0));
            MVP.Transpose();
            var bo = new BonesConst(m.GetAnimationFrame(0, 0));
            foreach (var mesh in m.Meshes)
                using (var v = Buffer.Create(g.DeviceContext.Device, BindFlags.VertexBuffer, mesh.Veteces))
                using (var index = Buffer.Create(g.DeviceContext.Device, BindFlags.IndexBuffer, mesh.Indeces))
                using (var cb = Buffer.Create(g.DeviceContext.Device, ref MVP, new BufferDescription(Utilities.SizeOf<AnimConst>(), BindFlags.ConstantBuffer, ResourceUsage.Default)))
                using(var cb2 = new Buffer(g.DeviceContext.Device, bo.Size2(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0))
                {
                    g.DeviceContext.UpdateSubresource(ref MVP, cb);
                    g.DeviceContext.UpdateSubresource( bo.Bones, cb2);

                    using (Shader s = new Shader(g.DeviceContext, Environment.CurrentDirectory + "\\Assimp.hlsl", AssimpModel.SkinnedPosNormalTexTanBi))
                    {
                        s.Begin(null, null, new[] { cb,cb2 });
                        g.Drawer.DrawIndexed(new VertexBufferBinding(v, Utilities.SizeOf<AssimpVertex>(), 0), index, mesh.Indeces.Length);
                        s.End();
                    }
                }
            //Drawer2d.DrawBitmap(bitmap);
            //Drawer2d.DrawText("ПОЕХАЛИ!");

        }

        public void Dispose()
        {
            Drawer2d?.Dispose();
            bitmap?.Dispose();
           
        }
    }
}
