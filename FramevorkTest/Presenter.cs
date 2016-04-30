using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using VictoremLibrary;
using SharpDX.Direct2D1;
using SharpDX.DXGI;

namespace FramevorkTest
{

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
            // Matrix.RotationY(MathUtil.PiOverTwo) 
            var MVP =Matrix.Identity* Matrix.LookAtRH(new Vector3(-100, 10, -10), Vector3.Zero, Vector3.Up) * Matrix.PerspectiveFovRH(MathUtil.PiOverFour, g.Form.Width / (float)g.Form.Height, 1f, 1000f);
            MVP.Transpose();

            foreach (var mesh in m.Meshes)
                using (var v = Buffer.Create(g.DeviceContext.Device, BindFlags.VertexBuffer, mesh.Veteces))
                using (var index = Buffer.Create(g.DeviceContext.Device, BindFlags.IndexBuffer, mesh.Indeces))
                using (var cb = Buffer.Create<Matrix>(g.DeviceContext.Device, ref MVP, new BufferDescription(Utilities.SizeOf<Matrix>(), BindFlags.ConstantBuffer, ResourceUsage.Default)))
                {
                    g.DeviceContext.UpdateSubresource(ref MVP, cb);
                    using (Shader s = new Shader(g.DeviceContext, Environment.CurrentDirectory + "\\Assimp.hlsl", new[] { new SharpDX.Direct3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0) }))
                    {
                        s.Begin(null, null, new[] { cb });                        
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
            //foreach(var m in bb.Meshes)
            //{
            //    m.Dispose();
            //}
        }
    }
}
