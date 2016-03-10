using SharpDX;
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
        string s;
        Stopwatch sw;
               
        public Presenter(Game game, TextWirter Text2D)
        {
            _text2DWriter = Text2D;
            game.OnDraw += Draw;
            game.OnUpdate += Update;
            game.Form.KeyDown += InputKeysControl;
            _waves = new Wave(game.DeviceContext);
            _World = Matrix.Identity;
            _View = Matrix.LookAtLH(new Vector3(0, 300f, 600f), new Vector3(0, 0, 0), Vector3.Up);
            _Progection = Matrix.PerspectiveFovLH(MathUtil.Pi / 3, game.ViewRatio, 1f, 2000f);
            sw = new Stopwatch();
            sw.Start();
        }

        void Update(double time)
        {
            sw.Stop();
            s = string.Format("LPS : {0:#####}", 1000.0f / sw.Elapsed.TotalMilliseconds);
            sw.Reset();
            sw.Start();
            _waves.Update(_World, _View, _Progection);
        }

        void Draw(double time)
        {
            _waves.Draw();
            _text2DWriter.DrawText(s);
        }
       
        private void InputKeysControl(object sender, EventArgs e)
        {
            Keys Key = ((dynamic)e).KeyCode;
            if (Key == Keys.Escape) ((SharpDX.Windows.RenderForm)sender).Close();
            if (Key == Keys.A) _View *= Matrix.RotationY(MathUtil.DegreesToRadians(1));
            if (Key == Keys.D) _View *= Matrix.RotationY(MathUtil.DegreesToRadians(-1)) ;           
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
                    Utilities.Dispose(ref _waves);
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
