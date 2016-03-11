namespace SharpDX11GameByWinbringer
{
    class Program
    {        
        static void Main(string[] args)
        {
            if (!SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(SharpDX.Direct3D.FeatureLevel.Level_11_0)) 
            {
                System.Windows.Forms.MessageBox.Show("Для запуска этой игры нужен DirectX11 ОБЯЗАТЕЛЬНО!");
                return;
            }
            using (var _renderForm = new SharpDX.Windows.RenderForm("SharpDXGameByWinbringer")
            {
                AllowUserResizing = false,
                IsFullscreen = false,
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                ClientSize = new System.Drawing.Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height),
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
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
