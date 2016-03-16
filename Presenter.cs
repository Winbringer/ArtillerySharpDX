﻿using SharpDX;
using SharpDX.Direct3D11;
using SharpDX11GameByWinbringer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharpDX11GameByWinbringer
{
    /// <summary>
    /// Наш презентер. Отвечает за работу с моделями и расчеты.
    /// </summary>
    public class Presenter : IDisposable
    {
        Matrix _World;
        Matrix _View;
        Matrix _Progection;
        Wave _waves = null;
        TextWirter _text2DWriter;
        Drawer _WavesDrawer;       
        string _s;
        Stopwatch _sw;
        TexturedCube _cube;
        public Presenter(Game game)
        {
            _text2DWriter =
                new TextWirter(
                game.SwapChain.GetBackBuffer<Texture2D>(0),
                game.Width,
                game.Height);
            game.OnDraw += Draw;
            game.OnUpdate += Update;
            game.Form.KeyDown += InputKeysControl;
            _World = Matrix.Identity;
            _View = Matrix.LookAtLH(new Vector3(0, 0, 400f), new Vector3(0, 0,0), Vector3.Up);
            _Progection = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.ViewRatio, 1f, 2000f);
            InitDrawers(game);
            _waves = new Wave(game.DeviceContext.Device);
            _cube = new TexturedCube(game.DeviceContext.Device);
            //Привязка событий
            _waves.OnDraw += _WavesDrawer.Draw;
            _cube.OnDraw += _WavesDrawer.Draw;
            _sw = new Stopwatch();
            _sw.Start();
        }

        private void InitDrawers(Game game)
        {
            InputElement[] inputElements = new InputElement[]
           {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("TEXCOORD",0,SharpDX.DXGI.Format.R32G32_Float,12,0)
           };
            //Установка Сампрелар для текстуры.
            SamplerStateDescription description = SamplerStateDescription.Default();
            description.Filter = Filter.MinMagMipLinear;
            description.AddressU = TextureAddressMode.Wrap;
            description.AddressV = TextureAddressMode.Wrap;
            _WavesDrawer = new Drawer(
                "Shaders\\Shader.hlsl",
                inputElements,
                game.DeviceContext,
                "Textures\\grass.jpg",
                description);          
        }

        void Update(double time)
        {
            _sw.Stop();
            _s = string.Format("LPS : {0:#####}", 1000.0f / _sw.Elapsed.TotalMilliseconds);
            _sw.Reset();
            _sw.Start();
            _waves.Update(_World, _View, _Progection);
            _cube.Update(_World, _View, _Progection);
        }

        void Draw(double time)
        {
            _waves.Draw();
            _cube.Draw();
            _text2DWriter.DrawText(_s);
        }

        private void InputKeysControl(object sender, EventArgs e)
        {
            Keys Key = ((dynamic)e).KeyCode;
            if (Key == Keys.Escape) ((SharpDX.Windows.RenderForm)sender).Close();
            if (Key == Keys.A) _View *= Matrix.RotationY(MathUtil.DegreesToRadians(1));
            if (Key == Keys.D) _View *= Matrix.RotationY(MathUtil.DegreesToRadians(-1));
            if (Key == Keys.W) _View *= Matrix.Translation(0,1,0);
            if (Key == Keys.S) _View *= Matrix.Translation(0, -1, 0);
        }

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты). 
                    Utilities.Dispose(ref _cube);                 
                    Utilities.Dispose(ref _waves);
                    Utilities.Dispose(ref _text2DWriter);
                    Utilities.Dispose(ref _WavesDrawer);
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
