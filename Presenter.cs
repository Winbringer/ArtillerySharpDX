using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
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
    public sealed class Presenter : IDisposable
    {
        Matrix _World;
        Matrix _View;
        Matrix _Progection;
        TextWirter _text2DWriter;
        _3DCubeMeneger _cubeManager;
        _3DLineMaganer _lineManager;
        _3DWaveManager _waveManager;
        Triangle _triangle;
        ShadedCube _sCube;
        string _s;
        Stopwatch _sw;

        DirectInput _directInput;
        Keyboard _keyboard;

        public Presenter(Game game)
        {
            _text2DWriter =
                new TextWirter(
                game.SwapChain.GetBackBuffer<Texture2D>(0),
                game.Width,
                game.Height);
            game.OnDraw += Draw;
            game.OnUpdate += Update;
            _World = Matrix.Identity;
            _View = Matrix.LookAtRH(new Vector3(0, 0, 355f), new Vector3(0, 0, 0), Vector3.Up);
            _Progection = Matrix.PerspectiveFovRH(MathUtil.PiOverFour, game.ViewRatio, 1f, 2000f);

            //Создаем объеты нашей сцены
            _waveManager = new _3DWaveManager(game.DeviceContext);
            _lineManager = new _3DLineMaganer(game.DeviceContext);
            _cubeManager = new _3DCubeMeneger(game.DeviceContext);
            _triangle = new Triangle(game.DeviceContext);
            _sCube = new ShadedCube(game.DeviceContext);


            // Initialize DirectInput
            _directInput = new DirectInput();
            // Instantiate the keyboard
            _keyboard = new Keyboard(_directInput);
            // Acquire the keyboard
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();

            _sw = new Stopwatch();
            _sw.Start();
        }

        void Update(double time)
        {
            UpdateKeyboardState((float)time);
            LPS();
            _sCube.UpdateConsBufData(_World, _View, _Progection);
            _lineManager.Update(time, _World, _View, _Progection);
            //_waveManager.Update(time, _World, _View, _Progection);
            //_cubeManager.Update(time, _World, _View, _Progection);
            //_triangle.World = Matrix.RotationY(MathUtil.PiOverFour) * Matrix.Translation(0, -50, 0);
            //_triangle.UpdateConsBufData(_World, _View, _Progection);
        }

        private void LPS()
        {
            _sw.Stop();
            _s = string.Format("LPS : {0:#####}", 1000.0f / _sw.Elapsed.TotalMilliseconds);
            _sw.Reset();
            _sw.Start();
        }

        void Draw(double time)
        {
            //    _waveManager.Draw();
            _lineManager.Draw();
            //    _cubeManager.Draw();
            //    _triangle.DrawTriangle(
            //        PrimitiveTopology.TriangleList,
            //        true,
            //        new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f)
            //        );   
            _sCube.Draw(
                   PrimitiveTopology.TriangleList,
                 true,
                   new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f)
                  );
            _text2DWriter.DrawText(_s);
        }

        #region Вспомогательные методы
        private void UpdateKeyboardState(float time)
        {
            // Poll events from joystick
            // keyboard.Poll();
            var m = _keyboard.GetCurrentState();            
            if (m.IsPressed(Key.A)) _View *= Matrix.Translation(1*time , 0, 0);
            if (m.IsPressed(Key.D)) _View *= Matrix.Translation(-1 * time, 0, 0);
            if (m.IsPressed(Key.W)) _View *= Matrix.Translation(0, 0, 1 * time);
            if (m.IsPressed(Key.S)) _View *= Matrix.Translation(0, 0, -1 * time);
            if (m.IsPressed(Key.Q)) _View *= Matrix.RotationY(0.001f * time);
            if (m.IsPressed(Key.E)) _View *= Matrix.RotationY(-0.001f * time);
            if (m.IsPressed(Key.Z)) _View *= Matrix.Translation(0, -1 * time, 0);
            if (m.IsPressed(Key.X)) _View *= Matrix.Translation(0, 1 * time, 0);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты). 
                    Utilities.Dispose(ref _keyboard);
                    Utilities.Dispose(ref _directInput);
                    Utilities.Dispose(ref _lineManager);
                    Utilities.Dispose(ref _cubeManager);
                    Utilities.Dispose(ref _waveManager);
                    Utilities.Dispose(ref _text2DWriter);
                    Utilities.Dispose(ref _triangle);
                    Utilities.Dispose(ref _sCube);
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
