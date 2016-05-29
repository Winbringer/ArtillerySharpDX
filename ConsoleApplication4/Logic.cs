using SharpDX.DirectInput;
using VictoremLibrary;
using SharpDX;

namespace ConsoleApplication4
{
    class Logic : LogicBase
    {
        ModelSDX mesh;
        public Logic(Game game) : base(game)
        {
            mesh = new ModelSDX(game.DeviceContext.Device, "Wm\\", "Scene.fbx");
        }

        public override void Dispose()
        {
            mesh?.Dispose();
        }

        protected override void Draw(float time)
        {

        }

        protected override void KeyKontroller(float time, KeyboardState kState)
        {

        }

        protected override void Upadate(float time)
        {

        }
    }
}
