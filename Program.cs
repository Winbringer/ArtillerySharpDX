using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DXGI;
namespace SharpDX11GameByWinbringer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (!SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(SharpDX.Direct3D.FeatureLevel.Level_11_0)) 
            {
                System.Windows.Forms.MessageBox.Show("Для запуска этой игры нужен DirectX11 ОБЯЗАТЕЛЬНО!");
                return;
            }
            using (Game game = new Game())
            {
                game.Run();
            }
        }
    }
}
