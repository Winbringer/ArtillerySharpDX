using SharpDX.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace SharpDX11GameByWinbringer
{

    class Program
    {
        [System.STAThread]
        static void Main(string[] args)
        {
            System.Console.WriteLine("Для начала игры нажмите Enter");
            System.Console.ReadLine();
            if (!SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(SharpDX.Direct3D.FeatureLevel.Level_11_0)) 
            {
                MessageBox.Show("Для запуска этой игры нужен DirectX11 ОБЯЗАТЕЛЬНО!");
                return;
            }
            using (var _renderForm = new RenderForm("SharpDX game by Winbringer")
            {
                AllowUserResizing = false,
                IsFullscreen = false,
                StartPosition = FormStartPosition.CenterScreen,
                ClientSize = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height),
                FormBorderStyle = FormBorderStyle.None,
                Icon = new Icon("LogoVW.ico")
            })
            {
                _renderForm.Shown += (sender, e) => { _renderForm.Activate(); };
                using (Game game = new Game(_renderForm))
                {
                    game.Run();                   
                }
            }           
        }
    }
}
