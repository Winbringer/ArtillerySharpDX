using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX11GameByWinbringer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDX11GameByWinbringer.Models
{
    sealed class _3DWaveManager : IDisposable
    {
        Drawer _CubeDrawer;
        ViewModel _cubeVM = new ViewModel();
        Wave _cube;
        public _3DWaveManager(DeviceContext DeviceContext)
        {
            InputElement[] inputElements = new InputElement[]
              {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("TEXCOORD",0,SharpDX.DXGI.Format.R32G32_Float,12,0)
              };


            _CubeDrawer = new Drawer(
                "Shaders\\Shader.hlsl",
                inputElements,
                DeviceContext);
            _cube = new Wave(DeviceContext.Device);
        }
        public void Dispose()
        {
            Utilities.Dispose(ref _CubeDrawer);
            Utilities.Dispose(ref _cube);
        }
        public void Update(double time, Matrix _World, Matrix _View, Matrix _Progection)
        {
            _cube.Update(_World, _View, _Progection);
            _cube.FillViewModel(_cubeVM);
        }

        public void Draw()
        {
            _CubeDrawer.Draw(_cubeVM, PrimitiveTopology.TriangleList, false);
        }
    }
}
