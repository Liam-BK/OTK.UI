using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Components;
using OTK.UI.Managers;

namespace OTK.UI.Utility
{
    internal class Tab : NinePatch
    {
        private Label _title = new Label(Vector2.Zero, 0, "", Vector3.Zero);

        public string Text
        {
            get
            {
                return _title.Text;
            }
            set
            {
                _title.Text = value;
            }
        }

        public bool Pressed = false;

        private float rolloverValue = 0;

        private Vector3 _rolloverColour = new Vector3(0.4f, 0.4f, 0.8f);

        public Vector3 RolloverColour
        {
            get
            {
                return _rolloverColour;
            }
            set
            {
                _rolloverColour = value;
            }
        }

        public float TimeToRollover = 0.5f;

        private static int defaultProgram = 0;

        public Tab(Vector4 bounds, float margin, float uvMargin = 0.5F, Vector3? colour = null) : base(bounds, margin, uvMargin, colour)
        {
            _title.Alignment = Label.TextAlign.Center;
            _title.Size = Height * 0.5f;
            _title.Origin = Center - Vector2.UnitY * 0.25f * Height;

            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        public override void OnUpdate(float deltaTime, MouseState mouse)
        {
            base.OnUpdate(deltaTime, mouse);
            if (WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
            {
                rolloverValue += deltaTime / TimeToRollover;
            }
            else
            {
                rolloverValue -= deltaTime / TimeToRollover;
            }
            rolloverValue = Math.Clamp(rolloverValue, 0.0f, 1.0f);
        }

        protected override void PassUniform()
        {
            base.PassUniform();
            PassUniform(Vector3.Lerp(Colour, _rolloverColour, rolloverValue) * (Pressed ? 0.5f : 1.0f), "colour");
        }

        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            base.Draw();
            _title.Draw();
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}