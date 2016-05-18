using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace VictoremLibrary
{
    public abstract class LogicBase : IDisposable
    {
        protected Game game;
        public LogicBase(Game game)
        {
            this.game = game;
            game.OnDraw += Draw;
            game.OnUpdate += Upadate;
            game.OnKeyPressed += KeyKontroller;
        }

        protected abstract void KeyKontroller(float time, KeyboardState kState);
        protected abstract void Upadate(float time);
        protected abstract void Draw(float time);
        public abstract void Dispose();
    }
}
