using SharpDX.Direct3D11;
using System;
using SharpDX;
using System.Runtime.InteropServices;
namespace VictoremLibrary
{


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct AnimConst
    {
        public Matrix WVP;
        public uint HasAnimaton;
        public uint HasDiffuseTexture;
        Vector2 padding0;
        public Matrix World;

        public AnimConst(Matrix w, Matrix v, Matrix p, uint HasAnim, uint HasTex)
        {

            HasAnimaton = HasAnim;
            HasDiffuseTexture = HasTex;
            WVP = w * v * p;
            World = w;
            padding0 = new Vector2();
        }

        public void Transpose()
        {
            WVP.Transpose();
            World.Transpose();
        }
    }


    internal class BonesConst
    {
        public Matrix[] Bones;

        public BonesConst()
        {
            Bones = new Matrix[1024];

        }
        public void init(Matrix[] bones)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var m = bones[i];
                m.Transpose();
                Bones[i] = m;
            }
        }
        public static int Size()
        {
            return Utilities.SizeOf<Matrix>() * 1024;
        }

    }

  

    public class Assimp3DModel : IDisposable
    {
        int frame = 0;
        Shader _shader;
        AssimpModel _model;
        Game _game;
        SamplerState _samler;
        BonesConst _bones;
        public Matrix _world = Matrix.Identity;
        public Matrix _view = Matrix.LookAtLH(new Vector3(0, 0, -150), Vector3.Zero, Vector3.Up);
        public Matrix _proj;
        AnimConst _constData = new AnimConst();
        private SharpDX.Direct3D11.Buffer _constBuffer1;
        private SharpDX.Direct3D11.Buffer _constBuffer0;

        public Assimp3DModel(Game game, string modelFile, string Folder)
        {
            _proj = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.Form.Width / (float)game.Form.Height, 1f, 1000f);
            _bones = new BonesConst();
            _game = game;
            _model = new AssimpModel(game.DeviceContext, Folder, modelFile);
            _shader = new Shader(game.DeviceContext, "Shaders\\Assimp.hlsl", AssimpModel.SkinnedPosNormalTexTanBi);
           
            var sD = SamplerStateDescription.Default();
            sD.AddressU = TextureAddressMode.Wrap;
            sD.AddressV = TextureAddressMode.Wrap;
            sD.AddressW = TextureAddressMode.Wrap;
            sD.MaximumAnisotropy = 16;
            sD.MaximumLod = float.MaxValue;
            sD.MinimumLod = 0;
            sD.Filter = Filter.MinMagMipLinear;
            _samler = new SamplerState(game.DeviceContext.Device, sD);
            _constData.HasAnimaton = _model.HasAnimations ? 1u : 0;
            _constData.HasDiffuseTexture =_model.Meshes3D[0].Texture != null ? 1u : 0;
            _constData.World = _world;
            _constData.WVP = _world * _view * _proj;
            _constData.Transpose();
            _constBuffer0 = new SharpDX.Direct3D11.Buffer(_game.DeviceContext.Device, Utilities.SizeOf<AnimConst>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _constBuffer1 = new SharpDX.Direct3D11.Buffer(_game.DeviceContext.Device, BonesConst.Size(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        }

        public void Update(float time, bool animate = false, int numAnimation = 0)
        {
            _constData.HasAnimaton = 0;
            if (animate && _model.HasAnimations)
            {
                _constData.HasAnimaton = 1;
                ++frame;
                if (frame >= _model.Animatons[numAnimation].numFrames) frame = 0;
                _bones.init(_model.GetAnimationFrame(numAnimation, frame));
            }

        }

        public void Draw()
        {
            _constData.World = _world;
            _constData.WVP = _world * _view * _proj;
            _constData.Transpose();
            _game.DeviceContext.UpdateSubresource(ref _constData, _constBuffer0);
            _game.DeviceContext.UpdateSubresource(_bones.Bones, _constBuffer1);
            foreach (var item in _model.Meshes3D)
            {
                _shader.Begin(new[] { _samler }, new[] { item.Texture }, new[] { _constBuffer0, _constBuffer1 });
                _game.Drawer.DrawIndexed(item.VertexBinding, item.IndexBuffer, item.IndexCount);
                _shader.End();
            }
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _samler);
            Utilities.Dispose(ref _constBuffer0);
            Utilities.Dispose(ref _constBuffer1);
            _shader?.Dispose();
            _model?.Dispose();
        }
    }
}
