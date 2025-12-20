using System.Globalization;
using System.Xml.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Interfaces;
using OTK.UI.Managers;
using OTK.UI.Utility;

namespace OTK.UI.Components
{
    /// <summary>
    /// A horizontal slider UI component built on top of <see cref="NinePatch"/>. 
    /// Allows the user to select a floating-point value between 0.0 and 1.0 by dragging the thumb along the slider track.
    /// </summary>
    /// <remarks>
    /// The <see cref="Value"/> property represents the current position of the thumb as a fraction of the slider width.
    /// Changes to <see cref="Value"/> automatically update the thumb's position. 
    /// The <see cref="OnValueChanged"/> event fires whenever the slider value is changed interactively.
    /// </remarks>
    public class Slider : NinePatch
    {
        NinePatch thumb;

        private float _value = 0;

        /// <summary>
        /// Gets or sets the current value of the slider in the range 0.0 to 1.0.
        /// Setting this property moves the thumb to the corresponding position along the slider track.
        /// </summary>
        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                thumb.Center = new Vector2(Bounds.X + Width * value, Center.Y);
            }
        }

        /// <summary>
        /// Fired whenever the <see cref="Value"/> changes due to user interaction.
        /// The float argument represents the new value of the slider.
        /// </summary>
        public event Action<float>? OnValueChanged;

        /// <summary>
        /// Gets or sets the texture used by the slider thumb.
        /// </summary>
        public string ThumbTexture
        {
            get
            {
                return thumb.Texture;
            }
            set
            {
                thumb.Texture = value;
            }
        }

        private Vector3 _thumbColour = Vector3.One;

        /// <summary>
        /// Gets or sets the color of the slider thumb.
        /// </summary>
        public Vector3 ThumbColour
        {
            get
            {
                return thumb.Colour;
            }
            set
            {
                thumb.Colour = value;
                _thumbColour = value;
            }
        }

        public override IUIContainer? Parent
        {
            get
            {
                return base.Parent;
            }
            set
            {
                base.Parent = value;
                thumb.Parent = value;
            }
        }

        private float _clickOffset = 0.0f;

        private bool _isActive = false;

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a new <see cref="Slider"/> with the specified bounds, inset, UV inset, and optional color.
        /// </summary>
        /// <param name="bounds">The rectangular area of the slider as a <see cref="Vector4"/> (left, bottom, right, top).</param>
        /// <param name="inset">The thickness of the inner border for nine-patch rendering.</param>
        /// <param name="uvInset">The UV inset for the nine-patch texture, from 0.0 to 0.5.</param>
        /// <param name="colour">Optional base color of the slider track.</param>
        public Slider(Vector4 bounds, float inset, float uvInset = 0.5f, Vector3? colour = null) : base(bounds, inset, uvInset, colour)
        {
            thumb = new NinePatch(new Vector4(bounds.X - Height * 0.5f, Center.Y - Height * 0.5f, bounds.X + Height * 0.5f, Center.Y + Height * 0.5f), inset, uvInset);
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="Slider"/> from an XML element.
        /// </summary>
        /// <param name="element">The <see cref="XElement"/> containing slider configuration.</param>
        /// <param name="register">Whether to register the slider in the UI manager (default true).</param>
        /// <returns>A new <see cref="Slider"/> instance configured from XML.</returns>
        public static new Slider Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds") ?? throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var thumbTexture = element.Element("ThumbTexture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var thumbColor = element.Element("ThumbColorRGB")?.Value ?? "1, 1, 1";
            var thumbPosition = float.Parse(element.Element("ThumbPosition")?.Value ?? "0");
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var thumbColorVec = LayoutLoader.ParseVector3(thumbColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            Slider slider = new(new Vector4(left, bottom, right, top) + relativeAnchorVector, margin, uvMargin)
            {
                IsVisible = isVisible,
                Colour = colorVec,
                Value = thumbPosition,
                ThumbTexture = thumbTexture,
                ThumbColour = thumbColorVec
            };
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                slider.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else slider.Texture = texture;

            return slider;
        }

        /// <summary>
        /// Updates the slider bounds and repositions the thumb according to the current value.
        /// Should be called whenever the slider's size or position changes.
        /// </summary>
        public override void UpdateBounds()
        {
            base.UpdateBounds();
            if (thumb is not null) thumb.Bounds = new Vector4(Bounds.X - Height * 0.5f + (Width * Value), Bounds.Y, Bounds.X + Height * 0.5f + (Width * Value), Bounds.W);
        }

        /// <summary>
        /// Handles mouse-down input to activate the slider if the thumb is clicked.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        public override void OnClickDown(MouseState mouse)
        {
            base.OnClickDown(mouse);
            _isActive = thumb.WithinBounds(ConvertMouseScreenCoords(mouse.Position));
            _clickOffset = ConvertMouseScreenCoords(mouse.Position).X - thumb.Center.X;
        }

        /// <summary>
        /// Updates the thumb position if it is being dragged, and fires <see cref="OnValueChanged"/>.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        public override void OnMouseMove(MouseState mouse)
        {
            base.OnMouseMove(mouse);
            if (_isActive)
            {
                thumb.Center = new Vector2(Math.Clamp(ConvertMouseScreenCoords(mouse.Position).X - _clickOffset, Bounds.X, Bounds.Z), Center.Y);
                Value = (thumb.Center.X - Bounds.X) / Width;
                OnValueChanged?.Invoke(Value);
            }
        }

        /// <summary>
        /// Deactivates the slider when the mouse button is released.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        public override void OnClickUp(MouseState mouse)
        {
            base.OnClickUp(mouse);
            _isActive = false;
        }

        /// <summary>
        /// Draws the slider track and thumb to the screen.
        /// The thumb color is slightly dimmed while being dragged.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            base.Draw();
            thumb.Colour = _thumbColour;
            if (_isActive)
                thumb.Colour = _thumbColour * 0.5f;
            thumb.Draw();
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}