using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Managers;
using OTK.UI.Utility;
using System.Globalization;
using System.Xml.Linq;

namespace OTK.UI.Components
{
    /// <summary>
    /// A clickable UI button built on top of <see cref="NinePatch"/>, providing text display,
    /// rollover highlighting, press/release events, and optional async event handlers.
    /// </summary>
    /// <remarks>
    /// The button handles mouse-over transitions using a smooth interpolation toward the
    /// configured <see cref="RolloverColour"/>. Press and release events fire only when the
    /// click begins and ends within the button’s bounds.
    /// </remarks>
    public class Button : NinePatch
    {
        /// <summary>
        /// The internal <see cref="Label"/> used to render the button’s text.
        /// </summary>
        public Label label;
        private string _text = "";

        /// <summary>
        /// The text displayed on the button.
        /// </summary>
        /// <remarks>
        /// Updating this property automatically updates the internal label’s text.
        /// </remarks>
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                label.Text = _text;
            }
        }

        protected float rolloverValue = 0;

        private Vector3 _rolloverColour = new Vector3(0.75f, 0.75f, 0.75f);

        /// <summary>
        /// The colour the button tints toward when the mouse is hovering over it.
        /// </summary>
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

        /// <summary>
        /// The duration, in seconds, required for the hover colour transition to fully complete.
        /// </summary>
        /// <remarks>
        /// A value of <c>0</c> disables smoothing and makes rollover behaviour instantaneous.
        /// </remarks>
        public float TimeToRollover = 0.5f;

        /// <summary>
        /// Indicates whether the button is currently held down (i.e., pressed but not yet released).
        /// </summary>
        protected bool pressed = false;

        /// <summary>
        /// Gets or sets the bounding rectangle of the button.
        /// </summary>
        /// <remarks>
        /// When set, the label is re-centered and resized based on the updated height.
        /// </remarks>
        public override Vector4 Bounds
        {
            get
            {
                return base.Bounds;
            }
            set
            {
                base.Bounds = value;
                if (label is not null)
                {
                    label.Size = Height * 0.5f;
                    label.Origin = Center - Vector2.UnitY * Height * 0.25f;
                }
            }
        }

        /// <summary>
        /// Raised when a mouse button is pressed while within the button’s bounds.
        /// </summary>
        public event Action<MouseButton>? Pressed;

        /// <summary>
        /// Raised when a previously pressed mouse button is released while still over the button.
        /// </summary>
        public event Action<MouseButton>? Released;

        /// <summary>
        /// Raised asynchronously when a valid mouse press occurs within the button’s bounds.
        /// </summary>
        public event Func<MouseButton, Task>? PressedAsync;

        /// <summary>
        /// Raised asynchronously when a mouse release occurs while the button is still pressed.
        /// </summary>
        public event Func<MouseButton, Task>? ReleasedAsync;

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a new button with the specified bounds, nine-patch inset, UV inset, text, and colour.
        /// </summary>
        /// <param name="bounds">
        /// The rectangular button bounds as <see cref="Vector4"/>: (Left, Bottom, Right, Top).
        /// </param>
        /// <param name="inset">
        /// Pixel inset applied to the nine-patch rendering.
        /// </param>
        /// <param name="uvInset">
        /// UV inset used by the nine-patch shader. Must be between 0.0 and 0.5.
        /// </param>
        /// <param name="text">
        /// The initial text displayed on the button.
        /// </param>
        /// <param name="colour">
        /// Optional colour tint for the button background.
        /// </param>
        /// <remarks>
        /// This constructor also creates and positions an internal <see cref="Label"/> to
        /// display the button text.
        /// </remarks>
        public Button(Vector4 bounds, float inset, float uvInset = 0.5f, string text = "", Vector3? colour = null) : base(bounds, inset, uvInset, colour)
        {
            label = new Label(Center - Vector2.UnitY * Height * 0.25f, Height * 0.5f, text, new Vector3(0, 0, 0));
            label.Alignment = Label.TextAlign.Center;
            _text = text;
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Creates a <see cref="Button"/> instance from an XML element.
        /// </summary>
        /// <param name="element">The XML definition of the button.</param>
        /// <returns>A configured <see cref="Button"/> instance.</returns>
        /// <exception cref="FormatException">
        /// Thrown when required attributes (e.g., <c>Name</c> or <c>Bounds</c>) are missing.
        /// </exception>
        /// <remarks>
        /// This method handles texture loading when file paths are provided and applies
        /// text colours, rollover colours, anchor offsets, and visibility settings.
        /// </remarks>
        public static new Button Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"Button: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var text = element.Element("Text")?.Value.Trim() ?? string.Empty;
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var textColor = element.Element("TextColorRGB")?.Value ?? "0, 0, 0";
            var rolloverColor = element.Element("RollOverColorRGB")?.Value ?? "0.75, 0.75, 0.75";
            var rollover = float.Parse(element.Element("RollOverTime")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var textColorVec = LayoutLoader.ParseVector3(textColor, name);
            var rollOverColorVec = LayoutLoader.ParseVector3(rolloverColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            Button button = new Button(new Vector4(left, bottom, right, top) + relativeAnchorVector, margin, uvMargin, text);
            button.IsVisible = isVisible;
            button.Colour = colorVec;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                button.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else
            {
                button.Texture = texture;
            }
            button.label.Colour = textColorVec;
            button.TimeToRollover = rollover;
            button.RolloverColour = rollOverColorVec;

            return button;
        }

        /// <summary>
        /// Recomputes the button's layout and updates the position and size of the internal label.
        /// </summary>
        /// <remarks>
        /// The label is always centered horizontally and positioned slightly above vertical center
        /// for visual balance.
        /// </remarks>
        public override void UpdateBounds()
        {
            base.UpdateBounds();
            if (label is not null)
            {
                label.Size = Math.Abs(_bounds.W - _bounds.Y) * 0.5f;
                label.Origin = new Vector2((_bounds.X + _bounds.Z) * 0.5f, (_bounds.Y + _bounds.W) * 0.5f - label.Size * 0.5f);
            }
        }

        /// <summary>
        /// Updates rollover transition logic and button state each frame.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last frame.</param>
        /// <param name="mouse">The current mouse state.</param>
        /// <remarks>
        /// The rollover animation interpolates toward the hover colour based on
        /// <see cref="TimeToRollover"/>. If <c>TimeToRollover == 0</c>, the transition is instantaneous.
        /// </remarks>
        public override void OnUpdate(float deltaTime, MouseState mouse)
        {
            if (!IsVisible) return;
            base.OnUpdate(deltaTime);
            if (TimeToRollover == 0)
            {
                rolloverValue = WithinBounds(ConvertMouseScreenCoords(mouse.Position)) ? 1f : 0f;
            }
            else
            {
                if (WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
                {
                    rolloverValue += deltaTime / TimeToRollover;
                }
                else
                {
                    rolloverValue -= deltaTime / TimeToRollover;
                }
            }
            rolloverValue = Math.Clamp(rolloverValue, 0.0f, 1.0f);
        }

        /// <summary>
        /// Handles asynchronous click-down behaviour when the mouse is pressed.
        /// </summary>
        /// <param name="mouse">The mouse state.</param>
        /// <remarks>
        /// Only fires if the click begins inside the button's bounds.  
        /// Async handlers are executed sequentially in the order they were subscribed.
        /// </remarks>
        public override async Task OnClickDownAsync(MouseState mouse)
        {
            if (!IsVisible) return;
            pressed = WithinBounds(ConvertMouseScreenCoords(mouse.Position));
            if (!pressed) return;

            MouseButton pressedButton;
            if (mouse.IsButtonDown(MouseButton.Left)) pressedButton = MouseButton.Left;
            else if (mouse.IsButtonDown(MouseButton.Right)) pressedButton = MouseButton.Right;
            else return;

            if (mouse.IsAnyButtonDown)
            {
                Pressed?.Invoke(pressedButton);
                if (PressedAsync != null)
                {
                    foreach (Func<MouseButton, Task> h in PressedAsync.GetInvocationList())
                        await h(pressedButton);
                }
            }
        }

        /// <summary>
        /// Handles click-down behaviour when the mouse is pressed.
        /// </summary>
        /// <param name="mouse">The mouse state.</param>
        public override void OnClickDown(MouseState mouse)
        {
            base.OnClickDown(mouse);
            if (!IsVisible) return;
            pressed = WithinBounds(ConvertMouseScreenCoords(mouse.Position));
            if (!pressed) return;

            MouseButton pressedButton;

            if (mouse.IsButtonDown(MouseButton.Left)) pressedButton = MouseButton.Left;
            else if (mouse.IsButtonDown(MouseButton.Right)) pressedButton = MouseButton.Right;
            else return;

            Pressed?.Invoke(pressedButton);
        }

        /// <summary>
        /// Updates the pressed state when the mouse moves while a button is held down.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        /// <remarks>
        /// If the user drags the cursor outside the button while holding the mouse,
        /// the button is no longer considered pressed.
        /// </remarks>
        public override void OnMouseMove(MouseState mouse)
        {
            if (!IsVisible) return;
            base.OnMouseMove(mouse);
            if (pressed)
            {
                pressed = WithinBounds(ConvertMouseScreenCoords(mouse.Position));
            }
        }

        /// <summary>
        /// Handles mouse button release events.
        /// </summary>
        /// <param name="mouse">The mouse state.</param>
        /// <remarks>
        /// A release only counts if the cursor is still within bounds at the moment of release.
        /// </remarks>
        public override void OnClickUp(MouseState mouse)
        {
            base.OnClickUp(mouse);
            if (!IsVisible) return;
            if (!pressed) return;

            MouseButton releasedButton;

            if (mouse.IsButtonReleased(MouseButton.Left)) releasedButton = MouseButton.Left;
            else if (mouse.IsButtonReleased(MouseButton.Right)) releasedButton = MouseButton.Right;
            else return;

            Released?.Invoke(releasedButton);

            pressed = false;
        }

        /// <summary>
        /// Asynchronously handles mouse button release events.
        /// </summary>
        /// <param name="mouse">The mouse state.</param>
        /// <remarks>
        /// Like the synchronous version, releases only trigger if the pointer is still inside the bounds.
        /// Async handlers are executed sequentially.
        /// </remarks>
        public override async Task OnClickUpAsync(MouseState mouse)
        {
            if (!IsVisible) return;
            if (!pressed) return;
            await base.OnClickUpAsync(mouse);

            MouseButton pressedButton;

            if (mouse.IsButtonReleased(MouseButton.Left)) pressedButton = MouseButton.Left;
            else if (mouse.IsButtonReleased(MouseButton.Right)) pressedButton = MouseButton.Right;
            else return;

            if (mouse.IsAnyButtonDown)
            {
                Released?.Invoke(pressedButton);
                if (ReleasedAsync != null)
                {
                    foreach (Func<MouseButton, Task> h in ReleasedAsync.GetInvocationList().Cast<Func<MouseButton, Task>>())
                        await h(pressedButton);
                }
            }
            pressed = false;
        }

        /// <summary>
        /// Sends button-specific uniform data to the shader, including rollover and press tinting.
        /// </summary>
        /// <remarks>
        /// The final colour is a blend between the base colour and <see cref="RolloverColour"/>,
        /// darkened by 50% when pressed.
        /// </remarks>
        protected override void PassUniform()
        {
            base.PassUniform();
            PassUniform(Vector3.Lerp(Colour, _rolloverColour, rolloverValue) * (pressed ? 0.5f : 1.0f), "colour");
        }

        /// <summary>
        /// Renders the button and its internal label.
        /// </summary>
        /// <remarks>
        /// A scissor region is applied using <see cref="FindMinClipBounds"/> to prevent drawing
        /// outside parent clipping areas.
        /// </remarks>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.UseProgram(program);
            PassUniform();
            var clipBounds = FindMinClipBounds();
            var clipWidth = clipBounds.Z - clipBounds.X;
            var clipHeight = clipBounds.W - clipBounds.Y;
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor((int)Math.Round(clipBounds.X), (int)Math.Round(clipBounds.Y), (int)Math.Round(clipWidth), (int)Math.Round(clipHeight));
            GL.BindVertexArray(vao);
            GL.DrawElements(BeginMode.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
            label.Draw();
            GL.Disable(EnableCap.ScissorTest);
            GL.BindVertexArray(0);
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}