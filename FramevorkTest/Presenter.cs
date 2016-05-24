using SharpDX;
using System;
using VictoremLibrary;
using SharpDX.DirectInput;

namespace FramevorkTest
{


    class Presenter : IDisposable
    {
        //TextWirter Drawer2d;
        //Bitmap bitmap;

        private Assimp3DModel model;
        Game game;
        public Presenter(Game game)
        {
            this.game = game;
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

            model = new Assimp3DModel(game, "Female.md5mesh", "3DModelsFiles\\Wm\\");
            model._world = Matrix.RotationX(MathUtil.PiOverTwo);
        }

        private void KeyKontroller(float time, KeyboardState kState)
        {

        }

        private void Upadate(float time)
        {
            model.Update(time, true);
        }


        private void Draw(float time)
        {
            model.Draw(game.DeviceContext);

            //Drawer2d.DrawBitmap(bitmap);
            //Drawer2d.DrawText("ПОЕХАЛИ!");

        }

        public void Dispose()
        {
            //Drawer2d?.Dispose();
            //bitmap?.Dispose();
            model?.Dispose();
        }
    }
}
