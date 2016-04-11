
using System;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX11GameByWinbringer.ViewModels;

namespace SharpDX11GameByWinbringer.Models
{
    class Quad : Object3D11<Vertex>
    {
        public Quad(Device dv)
        {
            _indeces = new uint[]
           {
                0, 1, 2,
                1,3,2
           };
            _veteces = new[]
            {
                new Vertex(new Vector3(0,0,0),new Vector2(0,0)),
                new Vertex(new Vector3(-100,0,0),new Vector2(1,0)),
                new Vertex(new Vector3(0,100,0),new Vector2(0,1)),
                new Vertex(new Vector3(-100,100,0),new Vector2(1,1)),
            };
            InitBuffers(dv);
        }
    }

    class Tri : Object3D11<Vertex>
    {
        public Tri(Device dv)
        {
            _indeces = new uint[]
            {
                0, 1, 2
            };
            _veteces = new[]
            {
                new Vertex(new Vector3(0,0,0),new Vector2(0,0)),
                new Vertex(new Vector3(100,0,0),new Vector2(1,0)),
                new Vertex(new Vector3(0,100,0),new Vector2(0,1)),
            };
            InitBuffers(dv);
        }

    }

    class Tesselation : Meneger3D
    {

        Buffer _cb;
        Device _dv;
        Tri _tri;
        Quad _quad;
        public Tesselation(Device dv)
        {
            this.World = Matrix.Identity;
            _dv = dv;
            _tri = new Tri(dv);
            _quad = new Quad(dv);

            _cb = new Buffer(dv, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            var inputElements = new InputElement[]
                            {
                         new InputElement("SV_Position",0,SharpDX.DXGI.Format.R32G32B32_Float,0,0),
                       new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float,12, 0)
                             };

            _drawer = new Drawer("Shaders\\Tes.hlsl", inputElements, dv.ImmediateContext);
            var m = _dv.ImmediateContext.LoadTextureFromFile("Textures\\grass.jpg");
            _viewModel.Textures = new[] { m };
            _viewModel.ConstantBuffers = new[] { _cb };

        }

        public override void Update(double time)
        {
        }

        public override void Draw(Matrix w, Matrix v, Matrix p)
        {
            Matrix mvp = _tri.ObjWorld * this.World * w * v * p;
            mvp.Transpose();

            _dv.ImmediateContext.UpdateSubresource(ref mvp, _cb);

            _tri.FillVM(ref _viewModel);
            _drawer.Draw(_viewModel);

            _quad.FillVM(ref _viewModel);
            _drawer.Draw(_viewModel);
        }

        public override void Dispose()
        {
            base.Dispose();
            Utilities.Dispose(ref _cb);
            _tri.Dispose();
            _quad.Dispose();
        }
    }
}
