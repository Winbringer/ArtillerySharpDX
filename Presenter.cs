using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX11GameByWinbringer.Models;
using SharpDX11GameByWinbringer.ViewModels;
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
        TextWirter _text2DWriter;
        _3DCubeMeneger _cubeManager;
        _3DLineMaganer _lineManager;
        _3DWaveManager _waveManager;
        string _s;
        Stopwatch _sw;

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
            _View = Matrix.LookAtLH(new Vector3(0, 0, -355f), new Vector3(0, 0, 0), Vector3.Up);
            _Progection = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.ViewRatio, 1f, 2000f);

            //Создаем объеты нашей сцены
            _waveManager = new _3DWaveManager(game.DeviceContext);
            _lineManager = new _3DLineMaganer(game.DeviceContext);
            _cubeManager = new _3DCubeMeneger(game.DeviceContext);

            _sw = new Stopwatch();
            _sw.Start();
        }

        void Update(double time)
        {
            _sw.Stop();
            _s = string.Format("LPS : {0:#####}", 1000.0f / _sw.Elapsed.TotalMilliseconds);
            _sw.Reset();
            _sw.Start();
            _lineManager.Update(time, _World, _View, _Progection);
            _waveManager.Update(time, _World, _View, _Progection);
            _cubeManager.Update(time, _World, _View, _Progection);
        }

        void Draw(double time)
        {
            _waveManager.Draw();
            _lineManager.Draw();
            _cubeManager.Draw();

            _text2DWriter.DrawText(_s);
        }

        #region Вспомогательные методы
        private void InputKeysControl(object sender, EventArgs e)
        {
            Keys Key = ((dynamic)e).KeyCode;
            if (Key == Keys.Escape) ((SharpDX.Windows.RenderForm)sender).Close();
            if (Key == Keys.A) _View *= Matrix.RotationY(MathUtil.DegreesToRadians(1));
            if (Key == Keys.D) _View *= Matrix.RotationY(MathUtil.DegreesToRadians(-1));
            if (Key == Keys.W) _View = Matrix.RotationX(MathUtil.DegreesToRadians(1)) * _View;
            if (Key == Keys.S) _View = Matrix.RotationX(MathUtil.DegreesToRadians(-1)) * _View;
            if (Key == Keys.Q) _View *= Matrix.RotationX(MathUtil.DegreesToRadians(1));
            if (Key == Keys.E) _View *= Matrix.RotationX(MathUtil.DegreesToRadians(-1));
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).                     
                    Utilities.Dispose(ref _lineManager);
                    Utilities.Dispose(ref _cubeManager);
                    Utilities.Dispose(ref _waveManager);
                    Utilities.Dispose(ref _text2DWriter);
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
