using System.Globalization;
using System.Xml.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Components;
using OTK.UI.Interfaces;
using OTK.UI.Layouts;
using OTK.UI.Managers;
using OTK.UI.Utility;
using Image = OTK.UI.Components.Image;

namespace OTK.UI.Containers
{
    /// <summary>
    /// A <see cref="Panel"/> that can be dynamically resized and dragged using grab handles.
    /// </summary>
    public class DynamicPanel : Panel
    {
        private GrabHandle left;

        private GrabHandle bottom;

        private GrabHandle right;

        private GrabHandle top;

        internal GrabHandle titleHandle;

        /// <summary>
        /// Gets or sets the margin for the panel's title.  
        /// Updates the title grab handle's position and size when set.
        /// </summary>
        public override float TitleMargin
        {
            get
            {
                return base.TitleMargin;
            }
            set
            {
                base.TitleMargin = value;
                if (titleHandle is not null)
                {
                    titleHandle.Center = new Vector2(Center.X, Bounds.W - TitleMargin * 0.5f);
                    titleHandle.Width = Width;
                    titleHandle.Height = value;
                }
            }
        }

        private static int defaultProgram = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicPanel"/> class with grab handles for resizing and dragging.
        /// </summary>
        /// <param name="bounds">The initial bounds of the panel (Left, Bottom, Right, Top).</param>
        /// <param name="scrollBarWidth">The width of the vertical scrollbar.</param>
        /// <param name="inset">The inset for the nine-patch border.</param>
        /// <param name="uvInset">UV margin inset for texture mapping.</param>
        /// <param name="colour">Optional panel color.</param>
        /// <remarks>
        /// This constructor creates four grab handles (left, right, bottom, top) for resizing,
        /// and a title grab handle for moving the panel. Also initializes the shader program
        /// if it has not already been created.
        /// </remarks>
        public DynamicPanel(Vector4 bounds, float scrollBarWidth, float inset, float uvInset, Vector3? colour = null) : base(bounds, scrollBarWidth, inset, uvInset, colour)
        {
            left = new GrabHandle(new Vector4(bounds.X - 1, bounds.Y - 1, bounds.X + 1, bounds.W + 1));
            bottom = new GrabHandle(new Vector4(bounds.X - 1, bounds.Y - 1, bounds.Z + 1, bounds.Y + 1));
            right = new GrabHandle(new Vector4(bounds.Z - 1, bounds.Y - 1, bounds.Z + 1, bounds.W + 1));
            top = new GrabHandle(new Vector4(bounds.X - 1, bounds.W - 1, bounds.Z + 1, bounds.W + 1));
            titleHandle = new GrabHandle(new Vector4(bounds.X - 1, bounds.W - TitleMargin - 1, bounds.Z + 1, bounds.W + 1));
            AltersMouse = true;

            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="DynamicPanel"/> from an <see cref="XElement"/> containing XML layout data.
        /// </summary>
        /// <param name="element">The XML element defining the panel's properties and child elements.</param>
        /// <returns>A configured <see cref="DynamicPanel"/> instance.</returns>
        /// <remarks>
        /// Throws an exception if the XML contains a nested <c>DynamicPanel</c> or an invalid configuration.
        /// </remarks>
        public static new DynamicPanel Load(Dictionary<string, IUIElement> registry, XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var titleMargin = float.Parse(element.Element("TitleMargin")?.Value ?? "0", CultureInfo.InvariantCulture);
            Console.WriteLine($"titleMargin: {titleMargin}");
            var title = element.Element("Title")?.Value.Trim() ?? string.Empty;
            Console.WriteLine($"title: {title}");
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var scrollbarTexture = element.Element("ScrollBarTexture")?.Value.Trim() ?? string.Empty;
            var scrollbarThumbTexture = element.Element("ScrollBarThumbTexture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var scrollbarColor = element.Element("ScrollBarColor")?.Value.Trim() ?? "1, 1, 1";
            var scrollbarThumbColor = element.Element("ScrollBarThumbColor")?.Value.Trim() ?? "1, 1, 1";
            var scrollbarWidth = float.Parse(element.Element("ScrollBarWidth")?.Value ?? "10", CultureInfo.InvariantCulture);
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";
            var layoutElement = element.Element("ConstraintLayout") ?? element.Element("VerticalLayout");

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var scrollbarColorVec = LayoutLoader.ParseVector3(scrollbarColor, name);
            var scrollbarThumbColorVec = LayoutLoader.ParseVector3(scrollbarThumbColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            DynamicPanel dynamicPanel = new DynamicPanel(new Vector4(left, bottom, right, top) + relativeAnchorVector, scrollbarWidth, margin, uvMargin);
            dynamicPanel.IsVisible = isVisible;
            dynamicPanel.Colour = colorVec;
            dynamicPanel.TitleMargin = titleMargin;
            dynamicPanel.Title = title;
            dynamicPanel.scrollbar.Texture = scrollbarTexture;
            dynamicPanel.scrollbar.ThumbTexture = scrollbarThumbTexture;
            dynamicPanel.scrollbar.Colour = scrollbarColorVec;
            dynamicPanel.scrollbar.ThumbColour = scrollbarThumbColorVec;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                dynamicPanel.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else dynamicPanel.Texture = texture;

            dynamicPanel.ApplicableLayout = Layout.Load(layoutElement);
            dynamicPanel.InitializeConstraintVariables();

            foreach (var child in element.Elements())
            {
                switch (child.Name.LocalName.ToLower())
                {
                    case "button":
                        var button = Button.Load(registry, child);
                        dynamicPanel.Add(button);
                        break;
                    case "breadcrumb":
                        var breadcrumb = BreadCrumb.Load(registry, child);
                        dynamicPanel.Add(breadcrumb);
                        break;
                    case "checkbox":
                        var checkbox = CheckBox.Load(registry, child);
                        dynamicPanel.Add(checkbox);
                        break;
                    case "image":
                        var image = Image.Load(registry, child);
                        dynamicPanel.Add(image);
                        break;
                    case "label":
                        var label = Label.Load(registry, child);
                        dynamicPanel.Add(label);
                        break;
                    case "ninepatch":
                        var ninePatch = NinePatch.Load(registry, child);
                        dynamicPanel.Add(ninePatch);
                        break;
                    case "numericspinner":
                        var numericspinner = NumericSpinner.Load(registry, child);
                        break;
                    case "progressbar":
                        var progressbar = ProgressBar.Load(registry, child);
                        dynamicPanel.Add(progressbar);
                        break;
                    case "radialmenu":
                        throw new ArgumentException("RadialMenu should not be contained in a DynamicPanel");
                    case "scrollbar":
                        var scrollbar = ScrollBar.Load(registry, child);
                        dynamicPanel.Add(scrollbar);
                        break;
                    case "slider":
                        var slider = Slider.Load(registry, child);
                        dynamicPanel.Add(slider);
                        break;
                    case "textfield":
                        var textfield = TextField.Load(registry, child);
                        dynamicPanel.Add(textfield);
                        break;
                    case "panel":
                        var panel = Panel.Load(registry, child);
                        dynamicPanel.Add(panel);
                        break;
                    case "dynamicpanel":
                        throw new ArgumentException("DynamicPanel should not be contained in a DynamicPanel");
                }
            }

            if (registry.ContainsKey(name)) throw new ArgumentException($"An element with name: {name} has already been registered.");
            registry.Add(name, dynamicPanel);
            return dynamicPanel;
        }

        /// <summary>
        /// Determines if a given position is within any of the panel's grab handles.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <returns>True if the position is within any grab handle; otherwise false.</returns>
        public bool WithinClasperBounds(Vector2 position)
        {
            return left.WithinBounds(position) || bottom.WithinBounds(position) || right.WithinBounds(position) || top.WithinBounds(position);
        }

        /// <summary>
        /// Checks if any of the resize grab handles are currently active (being dragged).
        /// </summary>
        /// <returns>True if any grab handle is active; otherwise false.</returns>
        private bool AnyClasperActive()
        {
            return left.Active || bottom.Active || right.Active || top.Active;
        }

        /// <summary>
        /// Handles mouse button press events, forwarding them to the panel and grab handles.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickDown(MouseState mouse)
        {
            base.OnClickDown(mouse);
            left.OnClickDown(mouse);
            bottom.OnClickDown(mouse);
            right.OnClickDown(mouse);
            top.OnClickDown(mouse);
            if (!AnyClasperActive())
            {
                titleHandle.OnClickDown(mouse);
            }
        }

        /// <summary>
        /// Updates the panel and child elements each frame, taking into account dragging and resizing.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update.</param>
        /// <param name="mouse">The current mouse state.</param>
        /// <param name="keyboard">The current keyboard state.</param>
        /// <remarks>
        /// Dynamically updates bounds and positions of grab handles and scrollbars during resizing or dragging.
        /// </remarks>
        public override void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard)
        {
            if (!AnyClasperActive())
                base.OnUpdate(deltaTime, mouse, keyboard);
        }

        /// <summary>
        /// Handles mouse movement, updating cursor type and resizing/moving panel as necessary.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        /// <remarks>
        /// Changes the cursor to indicate resize directions when hovering over grab handles.
        /// Adjusts panel bounds dynamically if any grab handle or the title handle is active.
        /// </remarks>
        public override void OnMouseMove(MouseState mouse)
        {
            bool mouseNotDefault = false;
            if (left.WithinBounds(ConvertMouseScreenCoords(mouse.Position)) || right.WithinBounds(ConvertMouseScreenCoords(mouse.Position)) || left.Active || right.Active)
            {
                if (Window is not null)
                {
                    Window.Cursor = MouseCursor.ResizeEW;
                    mouseNotDefault = true;
                }
            }
            else if (bottom.WithinBounds(ConvertMouseScreenCoords(mouse.Position)) || top.WithinBounds(ConvertMouseScreenCoords(mouse.Position)) || bottom.Active || top.Active)
            {
                if (Window is not null)
                {
                    Window.Cursor = MouseCursor.ResizeNS;
                    mouseNotDefault = true;
                }
            }

            left.OnMouseMove(mouse);
            bottom.OnMouseMove(mouse);
            right.OnMouseMove(mouse);
            top.OnMouseMove(mouse);
            titleHandle.OnMouseMove(mouse);
            if (AnyClasperActive())
            {
                Bounds = new Vector4(Math.Min(left.Center.X, Bounds.Z - MinimumSize.X), Math.Min(bottom.Center.Y, Bounds.W - MinimumSize.Y), Math.Max(right.Center.X, Bounds.X + MinimumSize.X), Math.Max(top.Center.Y, Bounds.Y + MinimumSize.Y));
                left.Center = new Vector2(Bounds.X, Center.Y);
                left.Height = Height + 2;
                bottom.Center = new Vector2(Center.X, Bounds.Y);
                bottom.Width = Width + 2;
                right.Center = new Vector2(Bounds.Z, Center.Y);
                right.Height = Height + 2;
                top.Center = new Vector2(Center.X, Bounds.W);
                top.Width = Width + 2;
                var scrollBarWidth = scrollbar.Width;
                scrollbar.Bounds = new Vector4(Bounds.Z - scrollBarWidth, Bounds.Y, Bounds.Z, Bounds.W - TitleMargin - ContentMargin);
                titleHandle.Center = new Vector2(Center.X, Bounds.W - TitleMargin * 0.5f);
                titleHandle.Width = Width;
                _title.Origin = titleHandle.Center - Vector2.UnitY * 0.25f * TitleMargin;
            }
            else if (titleHandle.Active)
            {
                float halfWidth = Width * 0.5f;
                float halfHeight = Height * 0.5f;
                Bounds = new Vector4(titleHandle.Center.X - halfWidth, titleHandle.Center.Y + TitleMargin * 0.5f - Height, titleHandle.Center.X + halfWidth, titleHandle.Center.Y + TitleMargin * 0.5f);
                left.Center = new Vector2(Bounds.X, Center.Y);
                left.Height = Height + 2;
                bottom.Center = new Vector2(Center.X, Bounds.Y);
                bottom.Width = Width + 2;
                right.Center = new Vector2(Bounds.Z, Center.Y);
                right.Height = Height + 2;
                top.Center = new Vector2(Center.X, Bounds.W);
                top.Width = Width + 2;
                var scrollBarWidth = scrollbar.Width;
                scrollbar.Bounds = new Vector4(Bounds.Z - scrollBarWidth, Bounds.Y, Bounds.Z, Bounds.W - TitleMargin - ContentMargin);
                _title.Origin = titleHandle.Center - Vector2.UnitY * 0.25f * TitleMargin;
            }
            if (!mouseNotDefault) base.OnMouseMove(mouse);
            ApplyLayout();
        }

        /// <summary>
        /// Handles mouse button release events, forwarding them to the panel and grab handles.
        /// </summary>
        /// <param name="mouse">Current mouse state.</param>
        public override void OnClickUp(MouseState mouse)
        {
            base.OnClickUp(mouse);
            left.OnClickUp();
            bottom.OnClickUp();
            right.OnClickUp();
            top.OnClickUp();
            titleHandle.OnClickUp();
        }
    }
}