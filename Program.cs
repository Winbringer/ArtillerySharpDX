using System.Diagnostics;

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
                System.Windows.Forms.MessageBox.Show("Для запуска этой игры нужен DirectX11 ОБЯЗАТЕЛЬНО!");
                return;
            }
            using (var _renderForm = new SharpDX.Windows.RenderForm("SharpDX game by Winbringer")
            {
                AllowUserResizing = false,
                IsFullscreen = false,
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                ClientSize = new System.Drawing.Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height),
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None,
                Icon = new System.Drawing.Icon("LogoVW.ico")
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
