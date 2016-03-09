using SharpDX;
using SharpDX11GameByWinbringer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDX11GameByWinbringer
{
    public class Presenter : IDisposable
    {
        Matrix _World;
        Matrix _View;
        Matrix _Progection;
        Wave _waves = null;
        public Presenter(Game game)
        {
            game.OnDraw += Draw;
            game.OnUpdate += Update;
            game.Form.KeyUp += InputKeysControl;
            _waves = new Wave(game.DeviceContext);
            _World = Matrix.Identity;
            _View = Matrix.LookAtLH(new Vector3(0, 50f, -400f), new Vector3(0, 0, 0), Vector3.Up);
            _Progection = Matrix.PerspectiveFovLH(MathUtil.Pi / 3, game.ViewRatio, 1f, 2000f);
        }

        void Update(double time)
        {
            _waves.Update(_World, _View, _Progection);
        }

        void Draw(double time)
        {
            _waves.Draw();
        }

        private void InputKeysControl(object sender, EventArgs e)
        {
            System.Windows.Forms.Keys Key = ((dynamic)e).KeyCode;
            if ( Key== System.Windows.Forms.Keys.Escape) ((SharpDX.Windows.RenderForm)sender).Close();
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
                    _waves.Dispose();
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
