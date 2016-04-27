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
        public Presenter(Game game)
        {
            game.OnDraw += Draw;
            game.OnUpdate += Upadate;
            game.OnKeyPressed += KeyKontroller;

            var srcTextureSRV = StaticMetods.LoadTextureFromFile(game.DeviceContext, "Village.png");
            var b = StaticMetods.LoadBytesFormFile(game.DeviceContext, "Village.png");
            var intt = game.FilterFoTexture.Histogram(srcTextureSRV);
            game.FilterFoTexture.SobelEdgeColor(ref srcTextureSRV, 0.5f);
            game.FilterFoTexture.Sepia(ref srcTextureSRV, 0.5f);
            game.FilterFoTexture.Contrast(ref srcTextureSRV, 2f);
            Drawer2d = new TextWirter(game.SwapChain.GetBackBuffer<Texture2D>(0), 800, 600);
            bitmap = StaticMetods.GetBitmapFromSRV(srcTextureSRV, Drawer2d.RenderTarget);
            Drawer2d.SetTextColor(Color.Red);
            Drawer2d.SetTextSize(36);
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
            Drawer2d.DrawBitmap(bitmap);
            Drawer2d.DrawText("ПОЕХАЛИ!");

        }

        public void Dispose()
        {
            Drawer2d?.Dispose();
            bitmap?.Dispose();
        }
    }
}
