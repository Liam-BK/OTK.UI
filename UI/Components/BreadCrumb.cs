using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Interfaces;
using OTK.UI.Managers;
using OTK.UI.Utility;
using System.Globalization;
using System.Xml.Linq;

namespace OTK.UI.Components
{
    /// <summary>
    /// A navigable breadcrumb UI component for path-like text.
    /// 
    /// <para>
    /// <see cref="BreadCrumb"/> displays a delimited string (e.g., file system paths)
    /// and allows users to interactively navigate through its segments. Clicking or
    /// hovering selects logical sections based on the configured <see cref="Delimiter"/>,
    /// and navigation events are surfaced through <see cref="OnNavigate"/>.
    /// </para>
    /// 
    /// <para>
    /// This control inherits from <see cref="TextField"/> but overrides input handling
    /// to disable text editing. Instead, it focuses purely on path navigation behavior:
    /// snapping to delimiters, trimming the text on click, and supporting keyboard shortcuts
    /// for moving "up" one level.
    /// </para>
    /// 
    /// <para>
    /// Breadcrumbs can be constructed programmatically or loaded from XML via
    /// <see cref="Load(System.Xml.Linq.XElement)"/>. The XML loader supports bounds,
    /// margins, colors, textures, text content, anchors, and visibility.
    /// </para>
    /// </summary>
    public class BreadCrumb : TextField
    {
        /// <summary>
        /// The delimiter used to determine the split in the file path.
        /// </summary>
        public char Delimiter = OperatingSystem.IsWindows() ? '\\' : '/';

        /// <summary>
        /// An action that occurs when the breadcrumb is clicked
        /// </summary>
        public event Action<string>? OnNavigate;

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a breadcrumb UI element with the specified bounds, margin, and visual options.
        /// </summary>
        /// <param name="bounds">
        /// The rectangular bounds of the element, represented as a <see cref="Vector4"/> in the form  
        /// <c>(Left, Bottom, Right, Top)</c>.  
        /// <c>bounds.X</c> = left, <c>bounds.Y</c> = bottom,  
        /// <c>bounds.Z</c> = right, <c>bounds.W</c> = top.
        /// </param>
        /// <param name="inset">
        /// The thickness of the inner border region, measured in the same units as <paramref name="bounds"/>.
        /// This represents how far the inner rectangle of a nine-patch layout sits inside the outer rectangle.
        /// </param>
        /// <param name="uvInset">
        /// The UV offset that corresponds to <paramref name="inset"/>.  
        /// Must be within the range <c>0.0</c> to <c>0.5</c>.  
        /// This controls how far the texture coordinates move inward when sampling the inner region
        /// (i.e., how much the UVs shift at the nine-patch inset boundary).
        /// </param>
        /// <param name="colour">
        /// Optional colour tint. If <c>null</c>, the default breadcrumb colour is used.
        /// </param>
        public BreadCrumb(Vector4 bounds, float inset, float uvInset = 0.5F, Vector3? colour = null) : base(bounds, inset, uvInset, colour)
        {
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.TextField.vert", "OTK.UI.Shaders.Fragment.TextField.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Creates a <see cref="BreadCrumb"/> instance from an XML layout element.  
        /// This reads all required breadcrumb properties—bounds, margins, colors,
        /// text content, texture references, and visibility—and constructs a fully
        /// configured UI element.  
        /// Throws a <see cref="FormatException"/> if mandatory fields are missing
        /// or improperly formatted.
        /// </summary>
        /// <param name="element">The XML element containing the breadcrumb definition.</param>
        /// <returns>A fully initialized <see cref="BreadCrumb"/> created from the XML data.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the breadcrumb is missing required fields (such as a name or bounds)
        /// or when numeric values cannot be parsed.
        /// </exception>
        public static new BreadCrumb Load(Dictionary<string, IUIElement> registry, XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var text = element.Element("Text")?.Value ?? string.Empty;
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var textColor = element.Element("TextColorRGB")?.Value ?? "0, 0, 0";
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var textColorVec = LayoutLoader.ParseVector3(textColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            BreadCrumb breadcrumb = new BreadCrumb(new Vector4(left, bottom, right, top) + relativeAnchorVector, margin, uvMargin);
            breadcrumb.IsVisible = isVisible;
            breadcrumb.Colour = colorVec;
            breadcrumb.text.Colour = textColorVec;
            breadcrumb.Text = text;
            breadcrumb.CaretIndex = text.Length;
            breadcrumb.LeftSelectIndex = breadcrumb.CaretIndex;
            breadcrumb.RightSelectIndex = breadcrumb.CaretIndex;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                breadcrumb.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else breadcrumb.Texture = texture;

            if (registry.ContainsKey(name)) throw new ArgumentException($"An element with name: {name} has already been registered.");
            registry.Add(name, breadcrumb);
            return breadcrumb;
        }

        /// <summary>
        /// Handles a mouse click on the breadcrumb element.  
        /// If the click occurs within the element’s bounds, the breadcrumb text is
        /// cut off at the first delimiter to the right of the mouse, and the <see cref="OnNavigate"/>
        /// callback is invoked with the updated text.
        /// </summary>
        /// <param name="mouse">The current mouse state used to determine click position.</param>
        public override void OnClickDown(MouseState mouse)
        {
            if (!IsVisible) return;
            if (WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
            {
                Text = Text.Substring(0, RightSelectIndex);
                OnNavigate?.Invoke(Text);
            }
        }

        /// <summary>
        /// Updates the breadcrumb’s selection range as the mouse moves.  
        /// When the cursor is within the element’s bounds, the right-side selection index
        /// is updated based on the hovered text position, snapping forward to the nearest
        /// delimiter. If the cursor leaves the bounds, the selection resets.
        /// </summary>
        /// <param name="mouse">The current mouse state used to determine cursor position.</param>
        public override void OnMouseMove(MouseState mouse)
        {
            if (!IsVisible) return;
            CaretIndex = 0;
            LeftSelectIndex = 0;
            if (WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
            {
                RightSelectIndex = text.FindIndexFromPos(ConvertMouseScreenCoords(mouse.Position));
                string data = Text;
                for (int i = RightSelectIndex; i < data.Length; i++)
                {
                    if (data[i] == Delimiter)
                    {
                        RightSelectIndex = i + 1;
                        return;
                    }
                }
                RightSelectIndex = data.Length;
            }
            else RightSelectIndex = 0;
        }

        /// <summary>
        /// Breadcrumbs do not process text input, so this method intentionally performs no action.
        /// </summary>
        /// <param name="e">Unused text input event data.</param>
        public override void OnTextInput(TextInputEventArgs e) { }

        /// <summary>
        /// Handles keyboard navigation shortcuts.  
        /// When <c>Ctrl + -</c> (or the keypad subtract key) is pressed, the breadcrumb
        /// navigates up one level by trimming the text to the delimiter before the current one,
        /// then invoking <see cref="OnNavigate"/> with the updated value.
        /// </summary>
        /// <param name="e">The key event used to detect navigation shortcuts.</param>
        public override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (!IsVisible) return;
            bool firstDelimiterFound = false;
            if (e.Control)
            {
                if (e.Key == Keys.Minus || e.Key == Keys.KeyPadSubtract)
                {
                    for (int i = Text.Length - 1; i >= 0; i--)
                    {
                        if (Text[i] == Delimiter)
                        {
                            if (!firstDelimiterFound)
                            {
                                firstDelimiterFound = true;
                            }
                            else
                            {
                                Text = Text.Substring(0, i + 1);
                                OnNavigate?.Invoke(Text);
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the breadcrumb each frame.  
        /// Breadcrumbs never display a blinking text caret, so this method disables it
        /// after performing the base update logic.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the previous frame.</param>
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            displayCaret = false;
        }
    }
}