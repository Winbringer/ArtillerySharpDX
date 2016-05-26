﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;
using VictoremLibrary;
using SharpDX.Direct3D11;
using SharpDX;

namespace ConsoleApplication3
{
    class LogicMy : LogicBase
    {
        private List<Assimp3DModel> _meshes;
        DeviceContext[] contextList;
        int threadCount = 4;

        public LogicMy(Game game) : base(game)
        {
            //   var m = new ModelSDX("Wm\\", "Character.fbx");

            _meshes = new List<Assimp3DModel>();
            _meshes.Add(new Assimp3DModel(game, "Character.fbx", "Wm\\"){ _world = Matrix.Scaling(10)});
            _meshes.Add(new Assimp3DModel(game, "Character.fbx", "Wm\\"){ _world = Matrix.Scaling(10) });
            _meshes.Add(new Assimp3DModel(game, "Character.fbx", "Wm\\"){ _world = Matrix.Scaling(10) });
            _meshes.Add(new Assimp3DModel(game, "Character.fbx", "Wm\\"){ _world = Matrix.Scaling(10) });

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
            _meshes.ForEach(x => x.Dispose());
            for (int i = 0; i < contextList.Length; ++i)
            {
                Utilities.Dispose(ref contextList[i]);
            }
        }

        protected override void Draw(float time)
        {
            Task[] renderTasks = new Task[contextList.Length];
            CommandList[] commands = new CommandList[contextList.Length];
            var Time = time;
            for (var i = 0; i < contextList.Length; i++)
            {
                var contextIndex = i;


                renderTasks[i] = Task.Run(() =>
                {
                    var renderContext = contextList[contextIndex];
                    // TODO: regular render logic goes here                   
                    _meshes[contextIndex].Draw(renderContext);

                    if (contextList[contextIndex].TypeInfo == DeviceContextType.Deferred)
                    {
                        //Создаем  команды
                        commands[contextIndex] = contextList[contextIndex].FinishCommandList(true);
                    }
                });
            }
            // Wait for all the tasks to complete
            Task.WaitAll(renderTasks);

            // Replay the command lists on the immediate context
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
            Task[] renderTasks = new Task[_meshes.Count];
            for (var i = 0; i < contextList.Length; i++)
            {
                var contextIndex = i;
                renderTasks[i] = Task.Run(() =>
                {
                    _meshes[contextIndex].Update(time,true,1);

                });
            }
            Task.WaitAll(renderTasks);
        }
    }
}