using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX11GameByWinbringer.ViewModels;
using System;

namespace SharpDX11GameByWinbringer.Models
{
    public sealed class _3DCubeMeneger : IDisposable
    {
        Drawer _CubeDrawer;
        ViewModel _cubeVM = new ViewModel();
        TexturedCube _cube;
        public _3DCubeMeneger(DeviceContext DeviceContext)
        {
            InputElement[] inputElements = new InputElement[]
              {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("TEXCOORD",0,SharpDX.DXGI.Format.R32G32_Float,12,0)
              };

            InputElement[] inputElements1 = new InputElement[]
           {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("COLOR",0,SharpDX.DXGI.Format.R32G32B32A32_Float,12,0)
           };
           
            _CubeDrawer = new Drawer("Shaders\\CubeShader.hlsl",
                                      inputElements,
                                      DeviceContext);
            _cube = new TexturedCube(DeviceContext.Device);
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _CubeDrawer);
            Utilities.Dispose(ref _cube);
            Utilities.Dispose(ref _cubeVM);
        }
        public void Update(double time)
        {
            
        }

        public void Draw(Matrix _World, Matrix _View, Matrix _Progection)
        {
            _cube.Update(_World, _View, _Progection);
            _cube.FillViewModel(_cubeVM);
            _CubeDrawer.Draw(_cubeVM, PrimitiveTopology.TriangleList, false);
        }

    }
}
