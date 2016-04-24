using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VictoremLibrary;

namespace FramevorkTest
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var window = StaticMetods.GetRenderForm("FrameworkTesting", "LogoVW.ico"))
            using (var game = new Game(window))
            using (var presenter = new Presenter(game))
            {
                game.Run();
            }
        }
    }
}
