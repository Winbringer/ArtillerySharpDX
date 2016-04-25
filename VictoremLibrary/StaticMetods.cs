using SharpDX;
using SharpDX.D3DCompiler;
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
    public static class StaticMetods
    {
        /// <summary>
        /// Создает форму в которую будет происходить рендеринг. На ней нужно обязательно вызвать метод Dispose. Форма закрываеть при нажатии Esc. Форма создаеться по размеру экрана и в его центре.
        /// </summary>
        /// <param name="Text">Текст в заголовке формы</param>
        /// <param name="IconFile">Файл в формает .ico для инконки в заголовке формы</param>
        /// <returns></returns>
        public static SharpDX.Windows.RenderForm GetRenderForm(string Text, string IconFile)
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

        public static Bitmap1 LoadBitmap(SharpDX.Direct3D11.DeviceContext device, string filename)
        {
            var props = new BitmapProperties1
            {
                PixelFormat =
                    new SharpDX.Direct2D1.PixelFormat(Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)
            };

            return Bitmap1.FromWicBitmap(device.QueryInterface<SharpDX.Direct2D1.DeviceContext>(), LoadBitmapSource(device, filename), props);
        }

        public static BitmapSource LoadBitmapSource(SharpDX.Direct3D11.DeviceContext device, string filename)
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

        public static SharpDX.Direct3D11.Texture2D CreateTex2DFromFile(SharpDX.Direct3D11.DeviceContext device, string filename)
        {
            var bSource = LoadBitmapSource(device, filename);
            return CreateTex2DFromBitmap(device, bSource);
        }

        public static SharpDX.Direct3D11.Texture2D CreateTex2DFromBitmap(SharpDX.Direct3D11.DeviceContext device, BitmapSource bsource)
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
        public static SharpDX.Direct3D11.ShaderResourceView LoadTextureFromFile(SharpDX.Direct3D11.DeviceContext device, string filename)
        {
            return new SharpDX.Direct3D11.ShaderResourceView(device.Device, CreateTex2DFromFile(device, filename));
        }

        /// <summary>
        /// Получает Bimap из ShaderResourceView (Ресурса с текстурой для шейдера).
        /// </summary>
        /// <param name="srv">Ресурс текстуры шейдера</param>
        /// <param name="renderTarger"> 2d рендертаргет который будет рисовать этот битмап</param>
        /// <returns>Битмапу с данными</returns>
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

        /// <summary>
        /// Копирует данные из неуправляемого ресурса ( Используемого в Компюте шейдере) в управляемый ресурс обычного шейдера.
        /// </summary>
        /// <param name="device">Устройстов используемое для отрисовки 3д</param>
        /// <param name="srv">ShaderResourceView с данными тестуры в который будем копировать данные UnorderedAccessView.</param>
        /// <param name="uav">UnorderedAccessView из которого будем брать данные</param>
        public static void CopyUAVToSRV(SharpDX.Direct3D11.Device device, ref SharpDX.Direct3D11.ShaderResourceView srv, SharpDX.Direct3D11.UnorderedAccessView uav)
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

        /// <summary>
        /// Создает карту битов из массива битов
        /// </summary>
        /// <param name="data">Массив битов который будет записан в карту</param>
        /// <param name="rt">Рендер таргет связанный с текущей видеокартой можно получить из класса TextWriter</param>
        /// <param name="Width">Ширина Будущей картинки в писелях</param>
        /// <param name="Height">Высота карты битов в пикселях</param>
        /// <returns></returns>
        public static SharpDX.Direct2D1.Bitmap BimapFromByteArray(byte[] data, RenderTarget rt, int Width, int Height)
        {
            var _backBufferBmp = new SharpDX.Direct2D1.Bitmap(rt, new Size2(Width, Height), new BitmapProperties(rt.PixelFormat));
            _backBufferBmp.CopyFromMemory(data, Width * 4);
            return _backBufferBmp;
        }

        /// <summary>
        /// Получает массив байтов и Текстуры.
        /// </summary>
        /// <param name="texture">Текстура с данными</param>
        /// <returns>Массив байтов с данными</returns>
        public static byte[] GetByteArrayFromTexture2D(Texture2D texture)
        {
            byte[] data = null;

            using (Surface surface = texture.QueryInterface<Surface>())
            {
                DataStream dataStream;
                var map = surface.Map(SharpDX.DXGI.MapFlags.Write, out dataStream);
                int lines = (int)(dataStream.Length / map.Pitch);
                data = new byte[surface.Description.Width * surface.Description.Height * 4];

                int dataCounter = 0;
                int actualWidth = surface.Description.Width * 4;
                for (int y = 0; y < lines; y++)
                {
                    for (int x = 0; x < map.Pitch; x++)
                    {
                        if (x < actualWidth)
                        {
                            data[dataCounter++] = dataStream.Read<byte>();
                        }
                        else
                        {
                            dataStream.Read<byte>();
                        }
                    }
                }
                dataStream.Dispose();
                surface.Unmap();
            }

            return data;
        }

        /// <summary>
        /// Получает текстуру из массива байтов
        /// </summary>
        /// <param name="data">Массив байтов</param>
        /// <param name="game">Игра для которой будет использоваться эта текстура</param>
        /// <param name="Width">Ширина текстуры</param>
        /// <param name="Height">Высота текстуры</param>
        /// <returns></returns>
        public static Texture2D GetTexture2DFromByteArray(byte[] data, Game game, int Width, int Height)
        {
            var stream = new DataStream(data.Length, true, true);
            stream.Write(data, 0, data.Length);
            Texture2DDescription readDesc = new Texture2DDescription()
            {
                ArraySize = 1,
                MipLevels = 1,
                SampleDescription = game.SwapChain.Description.SampleDescription,
                Format = game.SwapChain.Description.ModeDescription.Format,
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Usage = ResourceUsage.Staging,
                Height = Height,
                Width = Width
            };
            var readTex = new Texture2D(game.DeviceContext.Device, readDesc, new[] { new DataBox(stream.DataPointer, readDesc.Width * (int)FormatHelper.SizeOfInBytes(readDesc.Format), 0) });
            //  Texture2D readTex = new Texture2D(game.DeviceContext.Device, readDesc);

            //  game.DeviceContext.UpdateSubresource(data, readTex);
            return readTex;
        }
       
    }
}
