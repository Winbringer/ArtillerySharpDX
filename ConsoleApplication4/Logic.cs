using SharpDX.DirectInput;
using VictoremLibrary;
using SharpDX;

namespace ConsoleApplication4
{
    class Logic : LogicBase
    {
        Assimp3DModel mesh;
        public Logic(Game game) : base(game)
        {
            mesh = new Assimp3DModel(game, "Character.fbx", "Wm\\");
           mesh._world = Matrix.Scaling(10f) * mesh._world;
        }

        public override void Dispose()
        {
            mesh?.Dispose();
        }

        protected override void Draw(float time)
        {
          
            mesh.Draw(game.DeviceContext);
        }

        protected override void KeyKontroller(float time, KeyboardState kState)
        {
            
        }

        protected override void Upadate(float time)
        {
            mesh.Update(time,true,0);
        }
    }
}
