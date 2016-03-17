using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    class WavesMesh<B>:System.IDisposable where B : struct 
    {       
        protected Buffer _constantBuffer;
        
        public Matrix World;
        public B ConstantBufferData;
        Wave _wave;
        Drawer _wavesDrawer;

        public WavesMesh(Wave wave, Drawer drawer, DeviceContext DeviceContext)
        {
            World = Matrix.Translation(-250, 0, 250) * Matrix.RotationY(MathUtil.PiOverFour);
            _wave = wave;
            _wavesDrawer = drawer;
            //Создаем буфферы для видеокарты           
            _constantBuffer = new Buffer(DeviceContext.Device, Utilities.SizeOf<B>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, Utilities.SizeOf<B>());           
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _constantBuffer);
        }
    }
}
