using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VictoremLibrary
{
  public static  class StaticMetods
    {
        /// <summary>
        /// Создает форму в которую будет происходить рендеринг. На ней нужно обязательно вызвать метод Dispose. Форма закрываеть при нажатии Esc. Форма создаеться по размеру экрана и в его центре.
        /// </summary>
        /// <param name="Text">Текст в заголовке формы</param>
        /// <param name="IconFile">Файл в формает .ico для инконки в заголовке формы</param>
        /// <returns></returns>
        public static SharpDX.Windows.RenderForm GetRenderForm( string Text, string IconFile)
        {
            if (!SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(SharpDX.Direct3D.FeatureLevel.Level_11_0))
            {
                System.Windows.Forms.MessageBox.Show("Для запуска нужен DirectX 11 ОБЯЗАТЕЛЬНО!");
                return null;
            }
#if DEBUG
            SharpDX.Configuration.EnableObjectTracking = true;
#endif
            var _renderForm = new SharpDX.Windows.RenderForm(Text)
            {
                AllowUserResizing = false,
                IsFullscreen = false,
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                ClientSize = new System.Drawing.Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height),
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None,
                Icon = new System.Drawing.Icon(IconFile)
            };

            _renderForm.Shown += (sender, e) => { _renderForm.Activate(); };
            _renderForm.KeyDown += (sender, e) => { if (e.KeyCode == System.Windows.Forms.Keys.Escape) _renderForm.Close(); };
            return _renderForm;
        }

        private static readonly ImagingFactory Imgfactory = new ImagingFactory();

        public static Bitmap1 LoadBitmap( SharpDX.Direct3D11.DeviceContext device, string filename )
        {
            var props = new BitmapProperties1
            {
                PixelFormat =
                    new SharpDX.Direct2D1.PixelFormat(Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)
            };

            return Bitmap1.FromWicBitmap(device.QueryInterface< SharpDX.Direct2D1.DeviceContext>(), LoadBitmapSource(device,filename), props);
        }

        public static BitmapSource LoadBitmapSource( SharpDX.Direct3D11.DeviceContext device, string filename)
        {
            var d = new BitmapDecoder(
                Imgfactory,
                filename,
                DecodeOptions.CacheOnDemand
                );

            var frame = d.GetFrame(0);

            var fconv = new FormatConverter(Imgfactory);

            fconv.Initialize(
                frame,
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                BitmapDitherType.None, null,
                0.0, BitmapPaletteType.Custom);
            return fconv;
        }

        public static SharpDX.Direct3D11.Texture2D CreateTex2DFromFile( SharpDX.Direct3D11.DeviceContext device, string filename)
        {
            var bSource = LoadBitmapSource(device,filename);
            return CreateTex2DFromBitmap(device, bSource);
        }

        public static SharpDX.Direct3D11.Texture2D CreateTex2DFromBitmap( SharpDX.Direct3D11.DeviceContext device, BitmapSource bsource)
        {

            SharpDX.Direct3D11.Texture2DDescription desc;
            desc.Width = bsource.Size.Width;
            desc.Height = bsource.Size.Height;
            desc.ArraySize = 1;
            desc.BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource;
            desc.Usage = SharpDX.Direct3D11.ResourceUsage.Default;
            desc.CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None;
            desc.Format = Format.R8G8B8A8_UNorm;
            desc.MipLevels = 1;
            desc.OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;

            var s = new DataStream(bsource.Size.Height * bsource.Size.Width * 4, true, true);
            bsource.CopyPixels(bsource.Size.Width * 4, s);

            var rect = new DataRectangle(s.DataPointer, bsource.Size.Width * 4);

            var t2D = new SharpDX.Direct3D11.Texture2D(device.Device, desc, rect);

            return t2D;
        }

        /// <summary>
        /// Создает текстуру для шейдера
        /// </summary>
        /// <param name="device">Контекст Директ Икс 11</param>
        /// <param name="filename">Путь к файлу картинки</param>
        /// <returns> Текстуру готовую для использования в шейдере</returns>
        public static SharpDX.Direct3D11.ShaderResourceView LoadTextureFromFile( SharpDX.Direct3D11.DeviceContext device, string filename)
        {
            return new SharpDX.Direct3D11.ShaderResourceView(device.Device, CreateTex2DFromFile(device,filename));
        }

       public static SharpDX.Direct2D1.Bitmap GetBitmapFromSRV(SharpDX.Direct3D11.ShaderResourceView srv, RenderTarget renderTarger)
        {
            using (var texture = srv.ResourceAs<Texture2D>())
            using (var surface = texture.QueryInterface<Surface>())
            {
                var bitmap = new SharpDX.Direct2D1.Bitmap(renderTarger, surface, new SharpDX.Direct2D1.BitmapProperties(new SharpDX.Direct2D1.PixelFormat(
                                                          Format.R8G8B8A8_UNorm,
                                                          SharpDX.Direct2D1.AlphaMode.Premultiplied)));
                return bitmap;
            }
        }
        public static void CopyUAVToSRV(SharpDX.Direct3D11.Device device,ref SharpDX.Direct3D11.ShaderResourceView srv, SharpDX.Direct3D11.UnorderedAccessView uav)
        {
            

            using (var t = srv.ResourceAs<Texture2D>())
            {
                using (var t2 = uav.ResourceAs<SharpDX.Direct3D11.Texture2D>())
                {
                    // Copy the texture for the resource to the typeless texture
                    device.ImmediateContext.CopyResource(t2, t);
                }
            }
        }
    }
}
