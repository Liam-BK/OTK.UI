using System.Globalization;
using System.Xml.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OTK.UI.Interfaces;
using OTK.UI.Managers;
using OTK.UI.Utility;

namespace OTK.UI.Components
{
    /// <summary>
    /// A UI element representing a progress bar, derived from <see cref="NinePatch"/>. 
    /// Can display a fill either as a solid color or a texture, and supports dynamic fill percentage updates.
    /// </summary>
    public class ProgressBar : NinePatch
    {
        /// <summary>
        /// The color tint of the filled portion of the progress bar.
        /// </summary>
        public Vector3 FillColour
        {
            get;
            set;
        }

        /// <summary>
        /// Optional texture key to display as the fill of the progress bar.
        /// </summary>
        public string FillTexture { get; set; } = "";

        private bool UseFillTexture
        {
            get
            {
                return FillTexture is not "";
            }
        }

        private int UseFillTextureInt
        {
            get
            {
                return UseFillTexture ? 1 : 0;
            }
        }

        private float _value = 0.0f;

        /// <summary>
        /// Current fill percentage of the progress bar, clamped between 0 (empty) and 1 (full).
        /// </summary>
        public float FillPercentage
        {
            get
            {
                return _value;
            }
            set
            {
                _value = Math.Clamp(value, 0, 1);
            }
        }

        private static int defaultProgram = 0;

        /// <summary>
        /// Constructs a new <see cref="ProgressBar"/> with specified bounds and optional color/texture settings.
        /// </summary>
        /// <param name="bounds">UI bounds of the progress bar.</param>
        /// <param name="inset">Inset for the NinePatch edges.</param>
        /// <param name="uvInset">Optional UV inset for texture mapping.</param>
        /// <param name="colour">Optional background color.</param>
        public ProgressBar(Vector4 bounds, float inset, float uvInset = 0.5f, Vector3? colour = null) : base(bounds, inset, uvInset, colour)
        {
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.ProgressBar.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="ProgressBar"/> from an XML element.
        /// </summary>
        /// <param name="element">The XML element containing progress bar data.</param>
        /// <returns>A fully configured <see cref="ProgressBar"/> instance.</returns>
        /// <exception cref="FormatException">Thrown if required fields are missing or invalid.</exception>
        public static new ProgressBar Load(Dictionary<string, IUIElement> registry, XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var fillTexture = element.Element("FillTexture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var fillColor = element.Element("FillColorRGB")?.Value ?? "1, 1, 1";
            var fillAmount = float.Parse(element.Element("FillPercentage")?.Value ?? "0");
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var fillColorVec = LayoutLoader.ParseVector3(fillColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            ProgressBar progressBar = new ProgressBar(new Vector4(left, bottom, right, top) + relativeAnchorVector, margin, uvMargin);
            progressBar.IsVisible = isVisible;
            progressBar.Colour = colorVec;
            progressBar.FillColour = fillColorVec;
            progressBar.FillPercentage = fillAmount;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                progressBar.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else progressBar.Texture = texture;

            if (LayoutLoader.IsFilePath(fillTexture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                progressBar.FillTexture = Path.GetFileNameWithoutExtension(fillTexture);
            }
            else progressBar.FillTexture = fillTexture;

            if (registry.ContainsKey(name)) throw new ArgumentException($"An element with name: {name} has already been registered.");
            registry.Add(name, progressBar);
            return progressBar;
        }

        /// <summary>
        /// Sends ProgressBar-specific uniform values (FillTexture, FillPercentage, Bounds and FillColour)
        /// to the shader in addition to the standard UIBase uniforms.
        /// </summary>
        protected override void PassUniform()
        {
            base.PassUniform();
            if (UseFillTexture)
            {
                TextureManager.Bind(FillTexture, 1);
                PassUniform(1, "sampler1");
            }
            else
            {
                TextureManager.Unbind(1);
            }
            PassUniform(FillPercentage, "fillAmount");
            PassUniform(Bounds * DPIScaleVec4, "bounds");
            PassUniform(UseFillTextureInt, "useFillTexture");
            PassUniform(FillColour, "fillColour");
        }

        /// <summary>
        /// Draws the progress bar to the screen if it is visible.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.UseProgram(program);
            PassUniform();
            GL.BindVertexArray(vao);
            GL.DrawElements(BeginMode.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}