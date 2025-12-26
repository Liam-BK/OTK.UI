using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Interfaces;
using OTK.UI.Managers;
using OTK.UI.Utility;
using System.Globalization;
using System.Xml.Linq;

namespace OTK.UI.Components
{
    /// <summary>
    /// Represents a clickable checkbox UI element built on top of <see cref="NinePatch"/>.
    /// The checkbox can display a filled or empty state, change color when checked, and fire events on press and release.
    /// </summary>
    /// <remarks>
    /// Press and release events only fire when a click begins and ends within the checkbox's bounds.
    /// The checkbox supports custom textures and colors for both checked and unchecked states.
    /// </remarks>
    public class CheckBox : NinePatch
    {
        private string _checkedTexture = "";

        /// <summary>
        /// The texture displayed when the checkbox is checked.
        /// If <c>null</c> or empty, only <see cref="Colour"/> is used to indicate state.
        /// </summary>
        public string CheckedTexture
        {
            get
            {
                return _checkedTexture;
            }
            set
            {
                _checkedTexture = value;
            }
        }

        private Vector3 _checkedColour = Vector3.One;

        /// <summary>
        /// The color applied to the checkbox when it is checked.
        /// </summary>
        public Vector3 CheckedColour
        {
            get
            {
                return _checkedColour;
            }
            set
            {
                _checkedColour = value;
            }
        }

        /// <summary>
        /// Indicates whether the checkbox is currently checked.
        /// </summary>
        public bool IsChecked
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the checkbox is currently being clicked.
        /// </summary>
        public bool IsClicked
        {
            get;
            set;
        }

        private bool UseCheckedTexture
        {
            get
            {
                return CheckedTexture is not "";
            }
        }

        private int UseCheckedTextureInt
        {
            get
            {
                return UseCheckedTexture ? 1 : 0;
            }
        }

        public override Vector4 Bounds
        {
            get
            {
                return base.Bounds;
            }
            set
            {
                var left = value.Z - (value.W - value.Y);
                var bottom = value.Y;
                var right = value.Z;
                var top = value.W;
                base.Bounds = new Vector4(left, bottom, right, top);
            }
        }

        /// <summary>
        /// Event fired when the checkbox state changes due to a mouse click.
        /// </summary>
        public event Action<MouseButton>? CheckUpdated;

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a new <see cref="CheckBox"/> instance with specified bounds, inset, and optional color.
        /// </summary>
        /// <param name="bounds">
        /// The rectangular bounds of the checkbox as a <see cref="Vector4"/>: (Left, Bottom, Right, Top).
        /// </param>
        /// <param name="inset">
        /// The thickness of the inner nine-patch border.
        /// </param>
        /// <param name="uvInset">
        /// The UV offset corresponding to <paramref name="inset"/> for the texture coordinates.
        /// Must be between 0 and 0.5.
        /// </param>
        /// <param name="colour">
        /// Optional base color for the checkbox. If <c>null</c>, the default color is used.
        /// </param>
        public CheckBox(Vector4 bounds, float inset, float uvInset = 0.5f, Vector3? colour = null) : base(bounds, inset, uvInset, colour)
        {
            Texture = "CheckboxEmpty";
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="CheckBox"/> from an XML element.
        /// </summary>
        /// <param name="element">The <see cref="XElement"/> containing checkbox properties.</param>
        /// <returns>A new <see cref="CheckBox"/> instance configured from the XML.</returns>
        public static new CheckBox Load(Dictionary<string, IUIElement> registry, XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var checkedTexture = element.Element("CheckedTexture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var checkedColor = element.Element("CheckedColorRGB")?.Value ?? "1, 1, 1";
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var checkedColorVec = LayoutLoader.ParseVector3(checkedColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            CheckBox checkbox = new CheckBox(new Vector4(left, bottom, right, top) + relativeAnchorVector, margin, uvMargin);
            checkbox.IsVisible = isVisible;
            checkbox.Colour = colorVec;
            checkbox.CheckedColour = checkedColorVec;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                checkbox.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else checkbox.Texture = texture;

            if (LayoutLoader.IsFilePath(checkedTexture))
            {
                TextureManager.LoadTexture(checkedTexture, Path.GetFileNameWithoutExtension(checkedTexture));
                checkbox.CheckedTexture = Path.GetFileNameWithoutExtension(checkedTexture);
            }
            else checkbox.CheckedTexture = checkedTexture;

            if (registry.ContainsKey(name)) throw new ArgumentException($"An element with name: {name} has already been registered.");
            registry.Add(name, checkbox);
            return checkbox;
        }

        /// <summary>
        /// Handles the mouse-down event. Sets <see cref="IsClicked"/> when the click begins inside bounds.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickDown(MouseState mouse)
        {
            if (WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
            {
                IsClicked = mouse.IsButtonPressed(MouseButton.Left);
            }
        }

        /// <summary>
        /// Handles the mouse-move event. Updates <see cref="IsClicked"/> state while dragging.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnMouseMove(MouseState mouse)
        {
            if (IsClicked)
            {
                IsClicked = WithinBounds(ConvertMouseScreenCoords(mouse.Position));
            }
        }

        /// <summary>
        /// Handles the mouse-up event. Toggles <see cref="IsChecked"/> if click ends inside bounds and fires <see cref="CheckUpdated"/>.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickUp(MouseState mouse)
        {
            if (IsClicked && WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
            {
                IsChecked = !IsChecked;

                MouseButton releasedButton;
                if (mouse.IsButtonReleased(MouseButton.Left)) releasedButton = MouseButton.Left;
                else if (mouse.IsButtonReleased(MouseButton.Right)) releasedButton = MouseButton.Right;
                else return;
                CheckUpdated?.Invoke(releasedButton);
            }
            IsClicked = false;
        }

        /// <summary>
        /// Sends uniform values to the shader, including checked/unchecked color and texture logic.
        /// </summary>
        protected override void PassUniform()
        {

            bool transpose = false;

            if (IsChecked)
            {
                PassUniform(UseCheckedTextureInt, "useTexture");
                if (UseCheckedTexture)
                {
                    TextureManager.Bind(CheckedTexture, 0);
                    PassUniform(0, "sampler");
                }
            }
            else
            {
                PassUniform(UseTextureInt, "useTexture");
                if (UseTexture)
                {
                    TextureManager.Bind(Texture, 0);
                    PassUniform(0, "sampler");
                }
            }

            PassUniform(Math.Clamp(Inset, 0, Math.Min(Width, Height) * 0.5f), "margin");
            PassUniform(Math.Clamp(uvInset, 0, 0.5f), "uvMargin");
            PassUniform((IsChecked ? CheckedColour : Colour) * (IsClicked ? 0.5f : 1.0f), "colour");
            PassUniform(projection, transpose, "projection");
            PassUniform(model, transpose, "model");
        }

        /// <summary>
        /// Draws the checkbox, using the appropriate texture and color for the current state.
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