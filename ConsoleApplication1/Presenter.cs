using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VictoremLibrary;
using System.Diagnostics;
using SharpDX.DirectInput;

namespace ConsoleApplication1
{
    class Presenter : IDisposable
    {
        private ParticleRenderer particleSystem;
        private int totalParticles;
        private Stopwatch simTime;

        public Presenter(Game game)
        {
            game.OnDraw += Draw;
            game.OnUpdate += Upadate;
            game.OnKeyPressed += KeyKontroller;

            particleSystem = new ParticleRenderer(game);
            // Initialize renderer 
            totalParticles = 100000;
            particleSystem.Constants.DomainBoundsMax = new Vector3(20, 20, 20);
            particleSystem.Constants.DomainBoundsMin = new Vector3(-20, 0, -20);
            particleSystem.Constants.ForceDirection = -Vector3.UnitY;
            // Gravity is normally ~9.8f, we want slower snowfall
            particleSystem.Constants.ForceStrength = 1.8f;
            // Initialize particle resources
            particleSystem.InitializeParticles(totalParticles, 13f);
            // Initialize simulation timer 
            simTime = new Stopwatch();
            simTime.Start();
        }

        private void KeyKontroller(float time, KeyboardState kState)
        {
        }

        private void Upadate(float time)
        {
        }

        private void Draw(float time)
        {
            // 1. Update the particle simulation
            if (simTime.IsRunning)
            {
                particleSystem.Frame.FrameTime = (float)simTime.Elapsed.TotalSeconds - particleSystem.Frame.Time;
                particleSystem.Frame.Time = (float)simTime.Elapsed.TotalSeconds;
                // Run the compute shaders (compiles if necessary)  
                particleSystem.Update("Generator", "Snowfall");
            }
            // 2. Render the particles 
            particleSystem.Render();
        }

        public void Dispose()
        {
            particleSystem?.Dispose();
        }
    }
}
