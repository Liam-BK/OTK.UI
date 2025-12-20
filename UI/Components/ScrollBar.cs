using System.Globalization;
using System.Xml.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Managers;
using OTK.UI.Utility;

namespace OTK.UI.Components
{
    /// <summary>
    /// A vertical scrollbar UI component built on top of <see cref="NinePatch"/>. 
    /// Provides a draggable thumb to scroll content, with configurable size and position.
    /// </summary>
    /// <remarks>
    /// The <see cref="ThumbProportion"/> determines the fraction of the scrollbar track the thumb occupies.
    /// The <see cref="ThumbPosition"/> represents the normalized position of the thumb along the track (0 = top, 1 = bottom).
    /// </remarks>
    public class ScrollBar : NinePatch
    {
        private NinePatch thumb;
        private Vector3 _thumbColour = Vector3.One;

        private float _thumbProportion = 1.0f;

        /// <summary>
        /// Gets or sets the fraction of the scrollbar track that the thumb occupies.
        /// Values are clamped between 0.03 and 1. Changing this value resizes the thumb and recalculates its position.
        /// </summary>
        public float ThumbProportion
        {
            get
            {
                return _thumbProportion;
            }
            set
            {
                _thumbProportion = Math.Clamp(value, 0.03f, 1);
                float newHeight = Height * _thumbProportion;
                float top = thumb.Bounds.W;
                float bottom = top - newHeight;
                if (bottom < Bounds.Y)
                {
                    bottom = Bounds.Y;
                    top = bottom + newHeight;
                }
                if (bottom >= top)
                {
                    bottom = top;
                }
                thumb.Bounds = new Vector4(Bounds.X, bottom, Bounds.Z, top);
                CalculateThumbPosition();
            }
        }

        private float _thumbPosition = 0.0f;

        /// <summary>
        /// Gets or sets the normalized position of the thumb along the scrollbar track.
        /// Values are clamped between 0 (top) and 1 (bottom). Ignored if <see cref="ThumbProportion"/> is 1.0 (thumb fills track).
        /// </summary>
        public float ThumbPosition
        {
            get
            {
                return _thumbPosition;
            }
            set
            {
                _thumbPosition = Math.Clamp(value, 0, 1);
                if (ThumbProportion >= 1.0f) _thumbPosition = 0.0f;
                float thumbTop = Bounds.W - (Height - thumb.Height) * ThumbPosition;
                float thumbBottom = thumbTop - thumb.Height;
                thumb.Bounds = new Vector4(Bounds.X, thumbBottom, Bounds.Z, thumbTop);
            }
        }

        /// <summary>
        /// Sets the texture used by the scrollbar thumb.
        /// </summary>
        public string ThumbTexture
        {
            set
            {
                thumb.Texture = value;
            }
        }

        /// <summary>
        /// Gets or sets the color of the scrollbar thumb.
        /// </summary>
        public Vector3 ThumbColour
        {
            private get
            {
                return _thumbColour;
            }

            set
            {
                _thumbColour = value;
            }
        }

        private bool IsActive = false;

        private float clickOffset = 0.0f;

        /// <summary>
        /// Gets or sets the rectangular bounds of the scrollbar track.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="Bounds"/> automatically updates the position and size of the thumb
        /// according to the current <see cref="ThumbProportion"/> and <see cref="ThumbPosition"/>.
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

                if (thumb is not null)
                {
                    float thumbHeight = Height * ThumbProportion;
                    float trackRange = Height - thumbHeight;
                    thumb.Bounds = new Vector4(value.X, Bounds.W - trackRange * ThumbPosition - thumbHeight, value.Z, Bounds.W - trackRange * ThumbPosition);
                }
            }
        }

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a new <see cref="ScrollBar"/> with the specified bounds, inset, UV inset, and optional color.
        /// </summary>
        /// <param name="bounds">The rectangular area of the scrollbar track (left, bottom, right, top).</param>
        /// <param name="inset">The thickness of the inner border for nine-patch rendering.</param>
        /// <param name="uvInset">The UV inset for the nine-patch texture, from 0.0 to 0.5.</param>
        /// <param name="colour">Optional base color of the scrollbar track.</param>
        public ScrollBar(Vector4 bounds, float inset, float uvInset = 0.5f, Vector3? colour = null) : base(bounds, inset, uvInset, colour)
        {
            thumb = new NinePatch(new Vector4(bounds.X, bounds.W - Height * ThumbProportion, bounds.Z, bounds.W), inset, uvInset);
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="ScrollBar"/> from an XML element.
        /// </summary>
        /// <param name="element">The <see cref="XElement"/> containing scrollbar configuration.</param>
        /// <param name="register">Whether to register the scrollbar in the UI manager (default true).</param>
        /// <returns>A new <see cref="ScrollBar"/> instance configured from XML.</returns>
        /// <exception cref="FormatException">Thrown if required XML elements are missing or invalid.</exception>
        public static new ScrollBar Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var thumbTexture = element.Element("ThumbTexture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var thumbColor = element.Element("ThumbColorRGB")?.Value ?? "1, 1, 1";
            var thumbPosition = float.Parse(element.Element("ThumbPosition")?.Value ?? "0");
            var thumbProportion = float.Parse(element.Element("ThumbProportion")?.Value ?? "1");
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var thumbColorVec = LayoutLoader.ParseVector3(thumbColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            ScrollBar scrollbar = new ScrollBar(new Vector4(left, bottom, right, top) + relativeAnchorVector, margin, uvMargin);
            scrollbar.IsVisible = isVisible;
            scrollbar.Colour = colorVec;
            scrollbar.ThumbPosition = thumbPosition;
            scrollbar.ThumbProportion = thumbProportion;
            scrollbar.ThumbTexture = thumbTexture;
            scrollbar.ThumbColour = thumbColorVec;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                scrollbar.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else scrollbar.Texture = texture;

            return scrollbar;
        }

        /// <summary>
        /// Handles mouse-down input to activate the scrollbar if the thumb or track is clicked.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        public override void OnClickDown(MouseState mouse)
        {
            if (!IsVisible) return;
            base.OnClickDown(mouse);
            if (WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
            {
                if (thumb.WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
                {
                    clickOffset = ConvertMouseScreenCoords(mouse.Position).Y - thumb.Center.Y;
                    Console.WriteLine($"click offset: {clickOffset}, mousePos: {ConvertMouseScreenCoords(mouse.Position).Y}, thumb center: {thumb.Center.Y}");
                }
                else if (ConvertMouseScreenCoords(mouse.Position).Y < thumb.Bounds.Y)
                {
                    clickOffset = -thumb.Height * 0.5f;
                    Console.WriteLine($"click offset: {clickOffset}, mousePos: {ConvertMouseScreenCoords(mouse.Position).Y}, thumb center: {thumb.Center.Y}");
                }
                else if (ConvertMouseScreenCoords(mouse.Position).Y > thumb.Bounds.W)
                {
                    clickOffset = thumb.Height * 0.5f;
                    Console.WriteLine($"click offset: {clickOffset}, mousePos: {ConvertMouseScreenCoords(mouse.Position).Y}, thumb center: {thumb.Center.Y}");
                }
                IsActive = true;
                thumb.Center = new Vector2(Center.X, Math.Clamp(ConvertMouseScreenCoords(mouse.Position).Y - clickOffset, Bounds.Y + thumb.Height * 0.5f, Bounds.W - thumb.Height * 0.5f));
                CalculateThumbPosition();
            }
        }

        private void CalculateThumbPosition()
        {
            _thumbPosition = ThumbProportion < 1.0f ? (Bounds.W - thumb.Bounds.W) / (Height - thumb.Height) : 0;
        }

        /// <summary>
        /// Updates the thumb position if it is being dragged.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        public override void OnMouseMove(MouseState mouse)
        {
            if (IsActive)
            {
                thumb.Center = new Vector2(Center.X, Math.Clamp(ConvertMouseScreenCoords(mouse.Position).Y - clickOffset, Bounds.Y + thumb.Height * 0.5f, Bounds.W - thumb.Height * 0.5f));
                CalculateThumbPosition();
            }
        }

        /// <summary>
        /// Updates the thumb position based on mouse wheel input.
        /// Scrolling up moves the thumb up, scrolling down moves it down.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        public override void OnMouseWheel(MouseState mouse)
        {
            if (!IsVisible) return;
            ThumbPosition -= mouse.ScrollDelta.Y * 0.02f * ThumbProportion;
        }

        /// <summary>
        /// Deactivates the scrollbar when the mouse button is released.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        public override void OnClickUp(MouseState mouse)
        {
            IsActive = false;
        }

        /// <summary>
        /// Draws the scrollbar track and thumb to the screen.
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
            thumb.Colour = ThumbColour * (IsActive ? 0.5f : 1.0f);
            thumb.Draw();
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}