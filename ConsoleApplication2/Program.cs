﻿using VictoremLibrary;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Для продолжения нажмите Enter");
            System.Console.WriteLine("Чтобы врашать используйте клавиши W A S D ");
            System.Console.WriteLine("Для приблежения стрелка вверх на клавиатуре. ");
            System.Console.WriteLine("Для отдаления стрелка вниз на клавиатуре");
            System.Console.WriteLine("Для выхода нажмите клавишу Esc");
            System.Console.WriteLine("Таки это эмитация движения частиц в зоне переменной гравитации ");
            System.Console.ReadLine();
            System.Console.WriteLine("Загрузка... ");
            using (var form = StaticMetods.GetRenderForm("Victorem", "LogoVW.ico"))
            using (var game = new Game(form))
            using (var presenter = new Logic(game))
            {
               
                game.Run();
            }
        }
    }
}
