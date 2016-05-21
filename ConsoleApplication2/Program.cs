using VictoremLibrary;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var form = StaticMetods.GetRenderForm("Victorem", "LogoVW.ico"))
            using (var game = new Game(form))
            using (var presenter = new Logic(game))
            {
                game.Run();
            }
        }
    }
}
