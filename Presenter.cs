using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX11GameByWinbringer.Models;
using System;
using System.Diagnostics;


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
        _3DLineMaganer _lineManager;

        //TextWirter _text2DWriter;        
        //_3DWaveManager _waveManager;
        //Triangle _triangle;
        //ShadedCube _sCube;
        //EarthFromOBJ _earth;
        MD5Model _boy;
        //string _s;
        //Stopwatch _sw;
       // Tesselation _ts;

        public Presenter(Game game)
        {
            //_text2DWriter =
            //    new TextWirter(
            //    game.SwapChain.GetBackBuffer<Texture2D>(0),
            //    game.Width,
            //    game.Height);

            game.OnDraw += Draw;
            game.OnUpdate += Update;
            game.OnKeyPressed += ReadKeyboardState;

            _World = Matrix.Identity;
            _View = Matrix.LookAtLH(new Vector3(0, 0, -355f), new Vector3(0, 0, 0), Vector3.Up);
            _Progection = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.ViewRatio, 1f, 2000f);

            //Создаем объеты нашей сцены
           _lineManager = new _3DLineMaganer(game.DeviceContext);     
            //_waveManager = new _3DWaveManager(game.DeviceContext);                  
            //_triangle = new Triangle(game.DeviceContext);
            //_sCube = new ShadedCube(game.DeviceContext);
            //_sCube.World = Matrix.Translation(0, -70, 0);
            //_triangle.World = Matrix.RotationY(MathUtil.PiOverFour) * Matrix.Translation(0, 70, 0);           
            //_sw = new Stopwatch();
            //_sw.Start();

            //_earth = new EarthFromOBJ(game.DeviceContext);
            _boy = new MD5Model(game.DeviceContext, "3DModelsFiles\\Wm\\","Female", "Shaders\\BoyT.hlsl", true, 3);
             _boy.World = Matrix.Scaling(10);
            // _ts = new Tesselation(game.DeviceContext.Device,6);

        }

        void Update(double time)
        {
          //  LPS();
           //_sCube.UpdateConsBufData(_World, _View, _Progection);
            _lineManager.Update(time);
           
           _boy.Update((float)time);   
           // _waveManager.World =Matrix.Translation(-50,0,-50)* Matrix.Scaling(10);
           // _waveManager.Update(time);         
           //_triangle.UpdateConsBufData(_World, _View, _Progection);
           //_earth.Update((float)time);
        }

        private void LPS()
        {
            //_sw.Stop();
            //_s = string.Format("LPS : {0:#####}", 1000.0f / _sw.Elapsed.TotalMilliseconds);
            //_sw.Reset();
            //_sw.Start();
        }

        void Draw(double time)
        {
          
            //_waveManager.Draw(_World, _View, _Progection);
           _lineManager.Draw(_World, _View, _Progection);
            //_triangle.DrawTriangle(SharpDX.Direct3D.PrimitiveTopology.TriangleList,
            //                        true,
            //                        new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            //_sCube.Draw(SharpDX.Direct3D.PrimitiveTopology.TriangleList, true,
            //          new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            //_earth.Draw(_World, _View, _Progection);
            _boy.Draw(_World, _View, _Progection,SharpDX.Direct3D.PrimitiveTopology.PatchListWith3ControlPoints);
          //  _ts.Draw(_World, _View, _Progection);
          //  _text2DWriter.DrawText(_s);
        }

        #region Вспомогательные методы
        private void ReadKeyboardState(KeyboardState KeyState, float time)
        {
            const float speed = 0.2f;
            if (KeyState.IsPressed(Key.A)) _View *= Matrix.Translation(speed * time, 0, 0);
            if (KeyState.IsPressed(Key.D)) _View *= Matrix.Translation(-speed * time, 0, 0);
            if (KeyState.IsPressed(Key.W)) _View *= Matrix.Translation(0, 0, -speed * time);
            if (KeyState.IsPressed(Key.S)) _View *= Matrix.Translation(0, 0, speed * time);
            if (KeyState.IsPressed(Key.Q)) _View *= Matrix.RotationY(speed / 200 * time);
            if (KeyState.IsPressed(Key.E)) _View *= Matrix.RotationY(-speed / 200 * time);
            if (KeyState.IsPressed(Key.Z)) _View *= Matrix.Translation(0, -speed * time, 0);
            if (KeyState.IsPressed(Key.X)) _View *= Matrix.Translation(0, speed * time, 0);
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
                    //Utilities.Dispose(ref _earth);
                    Utilities.Dispose(ref _lineManager);
                    //Utilities.Dispose(ref _waveManager);
                    //Utilities.Dispose(ref _text2DWriter);
                    //Utilities.Dispose(ref _triangle);
                    //Utilities.Dispose(ref _sCube);
                    Utilities.Dispose(ref _boy);
                   // Utilities.Dispose(ref _ts);
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
