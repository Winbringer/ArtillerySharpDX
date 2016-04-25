using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using VictoremLibrary;
using SharpDX.Direct2D1;
using SharpDX.DXGI;

namespace FramevorkTest
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct ComputeConstants
    {
        public float Intensity;
        public Vector3 _padding0;
    }

    class Presenter : IDisposable
    {
        TextWirter Drawer2d;
        Bitmap bitmap;
        public Presenter(Game game)
        {
            game.OnDraw += Draw;
            game.OnUpdate += Upadate;
            game.OnKeyPressed += KeyKontroller;

            var srcTextureSRV = StaticMetods.LoadTextureFromFile(game.DeviceContext, "Village.png");
            var srcTexture = srcTextureSRV.ResourceAs<Texture2D>();
            var desc = srcTexture.Description;
            desc.BindFlags = BindFlags.UnorderedAccess;
            var target = new Texture2D(game.DeviceContext.Device, desc);
            target.DebugName = "CSTarget";
            var targetUAV = new UnorderedAccessView(game.DeviceContext.Device, target);

            var computeBuffer = new Buffer(game.DeviceContext.Device,
                Utilities.SizeOf<ComputeConstants>(),
                ResourceUsage.Default, BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None, 0);

            var constants = new ComputeConstants { Intensity = 0.5f };

            game.DeviceContext.UpdateSubresource(ref constants, computeBuffer);

            // Define the thread group size 
            SharpDX.Direct3D.ShaderMacro[] defines = new[] {
                new SharpDX.Direct3D.ShaderMacro("THREADSX", 32),
                new SharpDX.Direct3D.ShaderMacro("THREADSY", 32), };
            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif
            using (var bytecode = ShaderBytecode.CompileFromFile("DesaturateCS.hlsl",
                     "DesaturateCS", "cs_5_0", shaderFlags, EffectFlags.None, defines, null))
            {
                using (var cs = new ComputeShader(game.DeviceContext.Device, bytecode))
                {
                    game.DeviceContext.ComputeShader.Set(cs);
                    // Set the source resource 
                    game.DeviceContext.ComputeShader.SetShaderResource(0, srcTextureSRV);
                    // Set the destination resource 
                    game.DeviceContext.ComputeShader.SetUnorderedAccessView(0, targetUAV);
                    game.DeviceContext.ComputeShader.SetConstantBuffer(0, computeBuffer);
                    // e.g. 640x480 -> Dispatch(20, 15, 1); 
                    game.DeviceContext.Dispatch((int)Math.Ceiling(desc.Width / 32.0),
                        (int)Math.Ceiling(desc.Height / 32.0), 1);
                }
            }

            Utilities.Dispose(ref computeBuffer);
            Drawer2d = new TextWirter(game.SwapChain.GetBackBuffer<Texture2D>(0), 800, 600);
            StaticMetods.CopyUAVToSRV(game.DeviceContext.Device, ref srcTextureSRV, targetUAV);
            bitmap = StaticMetods.GetBitmapFromSRV(srcTextureSRV, Drawer2d.RenderTarget);
            Drawer2d.SetTextColor(Color.Red);
            Drawer2d.SetTextSize(36);
        }

        private void KeyKontroller(object sender, EventArgs e)
        {

        }

        private void Upadate(object sender, EventArgs e)
        {

        }

        private void Draw(object sender, EventArgs e)
        {
            Drawer2d.DrawBitmap(bitmap);
            Drawer2d.DrawText("ПОЕХАЛИ!");
        }

        public void Dispose()
        {
            Drawer2d?.Dispose();
            bitmap?.Dispose();
        }
    }
}
