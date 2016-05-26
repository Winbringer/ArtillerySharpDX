using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VictoremLibrary;

namespace ConsoleApplication4
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var form = StaticMetods.GetRenderForm("By Victorem", "Log"))
            using (var game = new Game(form))
            using (var logic = new Logic(game))
            {
                game.Run();
            }
        }
    }
}
