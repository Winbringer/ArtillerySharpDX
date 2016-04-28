using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX11GameByWinbringer.Models;
using System;
using System.Diagnostics;
using SharpDX.Direct2D1;
using Camera = VictoremLibrary.Camera;
namespace SharpDX11GameByWinbringer
{
    /// <summary>
    /// Наш презентер. Отвечает за работу с моделями и расчеты.
    /// </summary>
    public sealed class Presenter : IDisposable
    {
        Game _game;
        Camera _camera;

        Matrix _World;
        Matrix _View;
        Matrix _Progection;
        Matrix _View1;

        _3DLineMaganer _lineManager;

        TextWirter _text2DWriter;
        _3DWaveManager _waveManager;
        //Triangle _triangle;
        //ShadedCube _sCube;
        EarthFromOBJ _earth;
        MD5Model _boy;
        string _s;
        Stopwatch _sw;
        // Tesselation _ts;

        public Presenter(Game game)
        {
            _game = game;
            _camera = new Camera();
            _camera.Position = new Vector3(0, 0, -355f);
            _text2DWriter =
                new TextWirter(
                game.SwapChain.GetBackBuffer<Texture2D>(0),
                game.Width,
                game.Height);

            game.OnDraw += Draw;
            game.OnUpdate += Update;
            game.OnKeyPressed += ReadKeyboardState;

            _World = Matrix.Identity;
            _View = Matrix.LookAtLH(new Vector3(0, 0, -355f), new Vector3(0, 0, 0), Vector3.Up);
            _Progection = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.ViewRatio, 1f, 2000f);
            _View1= Matrix.LookAtLH(new Vector3(0, 700f, -1f), new Vector3(0, 0, 0), Vector3.Up);
            //Создаем объеты нашей сцены
            _lineManager = new _3DLineMaganer(game.DeviceContext);
            _waveManager = new _3DWaveManager(game.DeviceContext);                  
            //_triangle = new Triangle(game.DeviceContext);
            //_sCube = new ShadedCube(game.DeviceContext);
            //_sCube.World = Matrix.Translation(0, -70, 0);
            //_triangle.World = Matrix.RotationY(MathUtil.PiOverFour) * Matrix.Translation(0, 70, 0);           
            _sw = new Stopwatch();
            _sw.Start();

            _earth = new EarthFromOBJ(game.DeviceContext);
            _boy = new MD5Model(game.DeviceContext, "3DModelsFiles\\Wm\\", "Female", "Shaders\\Boy.hlsl", false, 3, true);
         // var m = new VictoremLibrary.AssimpModel("3DModelsFiles\\Wm\\Female.md5mesh");          
            _boy.World = Matrix.Scaling(10);
            // _ts = new Tesselation(game.DeviceContext.Device,6);

        }

        void Update(double time)
        {
            LPS();
            //_sCube.UpdateConsBufData(_World, _View, _Progection);
            _lineManager.Update(time);
            _boy.Update((float)time);
             _waveManager.World =Matrix.Translation(-50,0,-50)* Matrix.Scaling(10);
            _waveManager.Update(time);         
            //_triangle.UpdateConsBufData(_World, _View, _Progection);
            _earth.Update((float)time);
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
           _game.DeviceContext.Rasterizer.SetViewport(0, 0, _game.Width, _game.Height);           
            _waveManager.Draw(_World, _View, _Progection);
            _lineManager.Draw(_World, _View, _Progection);
            //_triangle.DrawTriangle(SharpDX.Direct3D.PrimitiveTopology.TriangleList,
            //                        true,
            //                        new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            //_sCube.Draw(SharpDX.Direct3D.PrimitiveTopology.TriangleList, true,
            //          new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            _earth.Draw(_World, _View, _Progection, 1f, 32);
            _boy.Draw(_World, _View, _Progection, SharpDX.Direct3D.PrimitiveTopology.TriangleList);
            //  _ts.Draw(_World, _View, _Progection);


            _game.DeviceContext.Rasterizer.SetViewport(0, 0, _game.Width/4, _game.Height/4);

            _waveManager.Draw(_World, _View1, _Progection);
            _lineManager.Draw(_World, _View1, _Progection);
            //_triangle.DrawTriangle(SharpDX.Direct3D.PrimitiveTopology.TriangleList,
            //                        true,
            //                        new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            //_sCube.Draw(SharpDX.Direct3D.PrimitiveTopology.TriangleList, true,
            //          new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            _earth.Draw(_World, _View1, _Progection, 1f, 32);
            _boy.Draw(_World, _View1, _Progection, SharpDX.Direct3D.PrimitiveTopology.TriangleList);
            //  _ts.Draw(_World, _View, _Progection);
            _text2DWriter.DrawText(_s);
        }

        #region Вспомогательные методы

        private void ReadKeyboardState(KeyboardState KeyState, float time)
        {
            float speed = 1.5f * time;
            float rSpeed = 0.001f * time;
            if (KeyState.IsPressed(Key.A))
            {
                _camera.moveLeftRight -= speed;
            }
            if (KeyState.IsPressed(Key.D))
            {
                _camera.moveLeftRight += speed;
            }
            if (KeyState.IsPressed(Key.W))
            {
                _camera.moveBackForward += speed;
            }
            if (KeyState.IsPressed(Key.S))
            {
                _camera.moveBackForward -= speed;
            }
            if (KeyState.IsPressed(Key.Up))
            {
                if (_camera.camYaw > -1f) _camera.camYaw -= rSpeed;
            }
            if (KeyState.IsPressed(Key.Right))
            {
                _camera.camPitch += rSpeed;

            }
            if (KeyState.IsPressed(Key.Down))
            {
                if (_camera.camYaw < 0) _camera.camYaw += rSpeed;
            }
            if (KeyState.IsPressed(Key.Left))
            {
                _camera.camPitch -= rSpeed;

            }
            if (KeyState.IsPressed(Key.Z))
            {
                _camera.moveUpDown += speed;
            }
            if (KeyState.IsPressed(Key.X))
            {
                _camera.moveUpDown -= speed;

            }
            _View = _camera.GetLHView();
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
                    Utilities.Dispose(ref _earth);
                    Utilities.Dispose(ref _lineManager);
                    Utilities.Dispose(ref _waveManager);
                    Utilities.Dispose(ref _text2DWriter);
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
