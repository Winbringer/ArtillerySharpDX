using SharpDX.DirectInput;
using VictoremLibrary;
using SharpDX;
using SharpDX.Direct3D11;
using System.Threading.Tasks;
using System;

namespace ConsoleApplication4
{
    class Logic : LogicBase
    {
        Assimp3DModel mesh;
        DeviceContext[] contextList;
        int threadCount = 4;
        public Logic(Game game) : base(game)
        {
            mesh = new Assimp3DModel(game, "Scene.fbx", "Wm\\");
        mesh._world = mesh._world* Matrix.Scaling(5f) *Matrix.RotationX(MathUtil.PiOverTwo);
            contextList = new DeviceContext[threadCount];

            for (var i = 0; i < threadCount; i++)
            {
                contextList[i] = new DeviceContext(game.DeviceContext.Device);
                InitializeContext(contextList[i]);
            }

        }
        protected void InitializeContext(DeviceContext context)
        {
            context.OutputMerger.DepthStencilState = game.Drawer.DepthState;
            // Set viewport 
            context.Rasterizer.SetViewports(game.DeviceContext.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>());
            // Set render targets   
            context.OutputMerger.SetTargets(game.DepthView, game.RenderView);
        }

        public override void Dispose()
        {
            mesh?.Dispose();
            for (int i = 0; i < contextList.Length; ++i)
            {
                Utilities.Dispose(ref contextList[i]);
            }
        }

        protected override void Draw(float time)
        {
          //  DrawAsynk(mesh.Draw, time);
            mesh.Draw(game.DeviceContext);
        }
        void DrawAsynk(Action<DeviceContext> dc, float time)
        {

            Task[] renderTasks = new Task[contextList.Length];
            CommandList[] commands = new CommandList[contextList.Length];
            var Time = time;
            for (var i = 0; i < contextList.Length; i++)
            {
                var contextIndex = i;
                renderTasks[i] = Task.Run(() =>
                {
                    // TODO: regular render logic goes here                   
                    dc?.Invoke(contextList[contextIndex]);

                    if (contextList[contextIndex].TypeInfo == DeviceContextType.Deferred)
                    {
                        commands[contextIndex] = contextList[contextIndex].FinishCommandList(true);
                    }
                });
            }

            Task.WaitAll(renderTasks);

            for (var i = 0; i < contextList.Length; i++)
            {
                if (contextList[i].TypeInfo == DeviceContextType.Deferred && commands[i] != null)
                {
                    game.DeviceContext.ExecuteCommandList(commands[i], false);
                    commands[i].Dispose();
                    commands[i] = null;
                }
            }
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
