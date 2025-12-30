using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common.Input;
using OTK.UI.Layouts;
using OTK.UI.Interfaces;
using OTK.UI.Utility;
using OTK.UI.Components;
using OTK.UI.Managers;
using System.Xml.Linq;
using System.Globalization;
using Image = OTK.UI.Components.Image;

namespace OTK.UI.Containers
{
    /// <summary>
    /// A Panel is a UI container that can hold multiple IUIElement children, 
    /// provides optional scrolling via a ScrollBar, and supports constraint-based layouts.
    /// It extends NinePatch to provide resizable 9-slice rendering for the background.
    /// </summary>
    public class Panel : NinePatch, IUIContainer
    {
        /// <summary>
        /// Gets or sets the center position of the panel.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return Center;
            }
            set
            {
                Center = value;
            }
        }

        /// <summary>
        /// The collection of child elements contained within this panel.
        /// </summary>
        public List<IUIElement> Elements = new();

        private Layout? _layout = null;

        /// <summary>
        /// The vertical scrollbar for the panel.
        /// </summary>
        public ScrollBar scrollbar;

        /// <summary>
        /// Gets or sets the layout applied to the panel. Setting a layout initializes constraints.
        /// </summary>
        /// <remarks>Constraint initialization only succeeds if the ApplicableLayout is a ConstraintLayout.</remarks>
        public Layout? ApplicableLayout
        {
            set
            {
                _layout = value;
                if (_layout is not null)
                {
                    _layout.Parent = this;
                }
                InitializeConstraintVariables();
                UpdateLayoutLambdas();

            }
            get
            {
                return _layout;
            }
        }

        /// <summary>
        /// Height of the content area, excluding tabs and margins.
        /// </summary>
        protected float ContentHeight
        {
            get
            {
                return Height - 2 * _contentMargin - TitleMargin;
            }
        }

        /// <summary>
        /// Visibility modes for the scrollbar.
        /// </summary>
        public enum ScrollbarVisibility
        {
            Always,
            Adaptive,
            MouseOver,
            Never
        }

        private ScrollbarVisibility _scrollbarVisibilityType;

        /// <summary>
        /// Current scrollbar visibility behavior.
        /// </summary>
        public ScrollbarVisibility ScrollbarVisibilityType
        {
            get
            {
                return _scrollbarVisibilityType;
            }
            set
            {
                if (value is ScrollbarVisibility.Always && scrollbar is not null) scrollbar.IsVisible = true;
                if (value is ScrollbarVisibility.Never && scrollbar is not null) scrollbar.IsVisible = false;
                _scrollbarVisibilityType = value;
            }
        }

        private float _titleMargin = 0;

        /// <summary>
        /// The vertical space reserved for the title at the top of the panel.
        /// </summary>
        public virtual float TitleMargin
        {
            get
            {
                return _titleMargin;
            }
            set
            {
                _titleMargin = value;
                if (_title is not null)
                {
                    _title.Size = value * 0.5f;
                    _title.Alignment = Label.TextAlign.Center;
                    _title.Origin = new Vector2(Center.X, Bounds.W - 0.75f * _titleMargin);
                }
            }
        }

        /// <summary>
        /// The label displayed as the panel's title.
        /// </summary>
        protected Label _title = new Label(Vector2.Zero, 0, "", Vector3.Zero);

        private Vector2 _manualMinSize = Vector2.Zero;

        /// <summary>
        /// Minimum size constraints for the panel.
        /// </summary>
        protected Vector2 MinimumSize
        {
            get
            {
                return _manualMinSize;
            }
            set
            {
                _manualMinSize = value;
            }
        }

        /// <summary>
        /// The minimum allowed width of the panel.
        /// </summary>
        public float MinimumWidth
        {
            get
            {
                return _manualMinSize.X;
            }
            set
            {
                _manualMinSize.X = value;
            }
        }

        /// <summary>
        /// The minimum allowed height of the panel.
        /// </summary>
        public float MinimumHeight
        {
            get
            {
                return _manualMinSize.Y;
            }
            set
            {
                _manualMinSize.Y = value;
            }
        }

        /// <summary>
        /// The displayed title of the panel.
        /// </summary>
        public string Title
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

        public int NumberOfElements
        {
            get
            {
                return Elements.Count;
            }
        }

        private float _contentMargin;

        /// <summary>
        /// Margin around content within the panel.
        /// </summary>
        public float ContentMargin
        {
            get
            {
                return _contentMargin;
            }
            set
            {
                _contentMargin = value;
            }
        }

        /// Gets or sets the element’s axis-aligned rectangle in UI coordinates.
        /// 
        /// Bounds are stored as a <see cref="Vector4"/> formatted as:
        /// X = left,  
        /// Y = bottom,  
        /// Z = right,  
        /// W = top,
        ///
        /// Changing the bounds automatically updates the element’s model matrix and scrollbar.
        public override Vector4 Bounds
        {
            get
            {
                return base.Bounds;
            }
            set
            {
                base.Bounds = value;
                if (scrollbar is not null)
                    scrollbar.Bounds = new Vector4(_bounds.Z - scrollbar.Width, _bounds.Y, _bounds.Z, _bounds.W - 0.5f * Inset);
            }
        }

        private static int defaultProgram = 0;

        /// <summary>
        /// Constructs a new <see cref="Panel"/> with a specified bounds rectangle, scrollbar width, inset, UV inset, and optional background color.
        /// </summary>
        /// <param name="bounds">The panel's bounding rectangle as a <see cref="Vector4"/> in the format (left, bottom, right, top).</param>
        /// <param name="scrollBarWidth">Width of the vertical scrollbar in pixels.</param>
        /// <param name="inset">Inset size for the panel's NinePatch borders.</param>
        /// <param name="uvInset">Inset applied to the UV mapping for the NinePatch texture.</param>
        /// <param name="colour">Optional base color for the panel's background. Defaults to white if null.</param>
        /// <remarks>
        /// Initializes the panel's vertical scrollbar and sets default layout and visibility settings.  
        /// The shader program used to render the NinePatch is either retrieved from a cached default or created anew if none exists.  
        /// Content margin is set equal to the inset, and title margin defaults to zero.
        /// </remarks>
        public Panel(Vector4 bounds, float scrollBarWidth, float inset, float uvInset, Vector3? colour = null) : base(bounds, inset, uvInset, colour)
        {
            scrollbar = new ScrollBar(new Vector4(bounds.Z - scrollBarWidth, bounds.Y, bounds.Z, bounds.W - 0.5f * inset), inset);
            InitializeConstraintVariables();
            ScrollbarVisibilityType = ScrollbarVisibility.Always;
            ContentMargin = inset;
            TitleMargin = 0;

            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="Panel"/> from an <see cref="XElement"/> representing XML configuration.
        /// </summary>
        /// <param name="element">The XML element containing panel configuration and child elements.</param>
        /// <returns>A fully initialized <see cref="Panel"/> instance with its children, layout, and scrollbar settings applied.</returns>
        /// <exception cref="FormatException">Thrown if required fields like Name or Bounds are missing.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if unsupported elements like RadialMenu or DynamicPanel are contained within this panel.
        /// </exception>
        /// <remarks>
        /// This method parses XML fields for the following:
        /// - Bounds: left, bottom, right, top
        /// - Margin and UVMargin for NinePatch
        /// - Title and TitleMargin
        /// - Visibility flag
        /// - Texture paths and color settings
        /// - Scrollbar textures and colors
        /// - Layout information
        /// 
        /// Child elements are recursively loaded based on their type.
        /// Relative anchoring is applied if specified.
        /// </remarks>
        public static new Panel Load(Dictionary<string, IUIElement> registry, XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var titleMargin = float.Parse(element.Element("TitleMargin")?.Value ?? "0", CultureInfo.InvariantCulture);
            var title = element.Element("Title")?.Value.Trim() ?? string.Empty;
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var scrollbarTexture = element.Element("ScrollBarTexture")?.Value.Trim() ?? string.Empty;
            var scrollbarThumbTexture = element.Element("ScrollBarThumbTexture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var scrollbarColor = element.Element("ScrollBarColor")?.Value.Trim() ?? "1, 1, 1";
            var scrollbarThumbColor = element.Element("ScrollBarThumbColor")?.Value.Trim() ?? "1, 1, 1";
            var scrollbarWidth = float.Parse(element.Element("ScrollBarWidth")?.Value ?? "10", CultureInfo.InvariantCulture);
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";
            var layoutElement = element.Element("ConstraintLayout") ?? element.Element("VerticalLayout") ?? element.Element("HorizontalLayout") ?? element.Element("FlowLayout");

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var scrollbarColorVec = LayoutLoader.ParseVector3(scrollbarColor, name);
            var scrollbarThumbColorVec = LayoutLoader.ParseVector3(scrollbarThumbColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            Panel panel = new Panel(new Vector4(left, bottom, right, top) + relativeAnchorVector, scrollbarWidth, margin, uvMargin);
            panel.IsVisible = isVisible;
            panel.Colour = colorVec;
            panel.TitleMargin = titleMargin;
            panel.Title = title;
            panel.scrollbar.Texture = scrollbarTexture;
            panel.scrollbar.ThumbTexture = scrollbarThumbTexture;
            panel.scrollbar.Colour = scrollbarColorVec;
            panel.scrollbar.ThumbColour = scrollbarThumbColorVec;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                panel.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else panel.Texture = texture;

            panel.ApplicableLayout = Layout.Load(layoutElement);
            panel.InitializeConstraintVariables();

            foreach (var child in element.Elements())
            {
                switch (child.Name.LocalName.ToLower())
                {
                    case "button":
                        var button = Button.Load(registry, child);
                        panel.Add(button);
                        break;
                    case "breadcrumb":
                        var breadcrumb = BreadCrumb.Load(registry, child);
                        panel.Add(breadcrumb);
                        break;
                    case "checkbox":
                        var checkbox = CheckBox.Load(registry, child);
                        panel.Add(checkbox);
                        break;
                    case "image":
                        var image = Image.Load(registry, child);
                        panel.Add(image);
                        break;
                    case "label":
                        var label = Label.Load(registry, child);
                        panel.Add(label);
                        break;
                    case "ninepatch":
                        var ninePatch = NinePatch.Load(registry, child);
                        panel.Add(ninePatch);
                        break;
                    case "numericspinner":
                        var numericspinner = NumericSpinner.Load(registry, child);
                        panel.Add(numericspinner);
                        break;
                    case "progressbar":
                        var progressbar = ProgressBar.Load(registry, child);
                        panel.Add(progressbar);
                        break;
                    case "radialmenu":
                        throw new ArgumentException("Radial menu should not be contained in a Panel");
                    case "scrollbar":
                        var scrollbar = ScrollBar.Load(registry, child);
                        panel.Add(scrollbar);
                        break;
                    case "slider":
                        var slider = Slider.Load(registry, child);
                        panel.Add(slider);
                        break;
                    case "textfield":
                        var textfield = TextField.Load(registry, child);
                        panel.Add(textfield);
                        break;
                    case "panel":
                        var childPanel = Load(registry, child);
                        panel.Add(childPanel);
                        break;
                    case "dynamicpanel":
                        throw new ArgumentException("DynamicPanel should not be contained in a Panel");
                }
            }

            if (registry.ContainsKey(name)) throw new ArgumentException($"An element with name: {name} has already been registered.");
            registry.Add(name, panel);
            return panel;
        }

        /// <summary>
        /// Initializes lambda references for constraint-based layout variables.
        /// </summary>
        public void InitializeConstraintVariables()
        {
            if (ApplicableLayout is not ConstraintLayout layout) return;
            layout.LineDSLInstance?.ClearLambdaRefs();
            layout.LineDSLInstance?.AddLambdaRef("panelleft", (
                    () => _bounds.X,
                    val => _bounds = new Vector4((float)val, _bounds.Y, _bounds.Z, _bounds.W)
                )
            );
            layout.LineDSLInstance?.AddLambdaRef("panelbottom", (
                    () => _bounds.Y,
                    val => _bounds = new Vector4(_bounds.X, (float)val, _bounds.Z, _bounds.W)
                )
            );
            layout.LineDSLInstance?.AddLambdaRef("panelright", (
                    () => _bounds.Z,
                    val => _bounds = new Vector4(_bounds.X, _bounds.Y, (float)val, _bounds.W)
                )
            );
            layout.LineDSLInstance?.AddLambdaRef("paneltop", (
                () => _bounds.W,
                val => _bounds = new Vector4(_bounds.X, _bounds.Y, _bounds.Z, (float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef("panelcenterx", (
                () => Center.X,
                val => Center = new Vector2((float)val, Center.Y)
            ));
            layout.LineDSLInstance?.AddLambdaRef("panelcentery", (
                () => Center.Y,
                val => Center = new Vector2(Center.X, (float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef("scrollbarthumbposition", (
                () => scrollbar.ThumbPosition,
                val => scrollbar.ThumbPosition = (float)val
            ));
            layout.LineDSLInstance?.AddLambdaRef("scrollbarthumbproportion", (
                () => scrollbar.ThumbProportion,
                val => scrollbar.ThumbProportion = (float)val
            ));
            layout.LineDSLInstance?.AddLambdaRef("scrollbarleft", (
                () => scrollbar.Bounds.X,
                val => scrollbar.PreEditLeft((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"contentmargin", (
                () => ContentMargin * DPIScaleX,
                val => ContentMargin = (float)val * InvDPIScaleX
            ));
            layout.LineDSLInstance?.AddLambdaRef($"titlemargin", (
                () => TitleMargin,
                val => TitleMargin = (float)val
            ));
        }

        /// <summary>
        /// Adds an <see cref="IUIElement"/> to this panel and sets up layout lambda references for constraint-based positioning.
        /// </summary>
        /// <param name="element">The element to add. Must not already have a parent.</param>
        /// <exception cref="ArgumentException">Thrown if the element already has a parent.</exception>
        /// <remarks>
        /// Updates the scrollbar's <see cref="ScrollBar.ThumbProportion"/> based on content height versus measured content.  
        /// If a constraint layout is applied, lambda references are added for all sides, center coordinates, and NinePatch margins.
        /// </remarks>
        public virtual void Add(IUIElement element)
        {
            if (element.Parent is not null) throw new ArgumentException("Provided Element already has a parent. Make sure the parent is null before adding to an IUIContainer.");
            Elements.Add(element);
            element.Parent = this;
            if (ApplicableLayout is not ConstraintLayout layout) return;
            var target = Elements[^1];
            layout.LineDSLInstance?.AddLambdaRef($"element[{Elements.Count - 1}].left", (
                () => target.Bounds.X,
                val => target.PreEditLeft((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{Elements.Count - 1}].bottom", (
                () => target.Bounds.Y,
                val => target.PreEditBottom((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{Elements.Count - 1}].right", (
                () => target.Bounds.Z,
                val => target.PreEditRight((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{Elements.Count - 1}].top", (
                () => target.Bounds.W,
                val => target.PreEditTop((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{Elements.Count - 1}].centerx", (
                () => target.Center.X,
                val => target.Center = new Vector2((float)val, Center.Y)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{Elements.Count - 1}].centery", (
                () => target.Center.Y,
                val => target.Center = new Vector2(Center.X, (float)val)
            ));
            if (target is NinePatch ninePatch)
            {
                layout.LineDSLInstance?.AddLambdaRef($"element[{Elements.Count - 1}].margin", (
                    () => ninePatch.Inset,
                    val => ninePatch.Inset = (float)val
                ));
            }
            scrollbar.ThumbProportion = ContentHeight / MeasureContent();
        }

        /// <summary>
        /// Updates the lambda references in the associated <see cref="ConstraintLayout"/> for all child elements.
        /// </summary>
        /// <param name="clear">
        /// If true, clears all existing lambda references before adding new ones.  
        /// Defaults to false.
        /// </param>
        /// <remarks>
        /// This method ensures that the layout system can dynamically access and modify the bounds and centers
        /// of each child element through lambda references.  
        /// Special handling is applied for <see cref="NinePatch"/> elements to also reference their margin (Inset).
        /// </remarks>
        private void UpdateLayoutLambdas(bool clear = false)
        {
            if (ApplicableLayout is not ConstraintLayout layout) return;
            if (clear) layout.LineDSLInstance.ClearLambdaRefs();
            for (int i = 0; i < Elements.Count; i++)
            {
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].left", (
                    () => Elements[i].Bounds.X,
                    val => Elements[i].PreEditLeft((float)val)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].bottom", (
                    () => Elements[i].Bounds.Y,
                    val => Elements[i].PreEditBottom((float)val)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].right", (
                    () => Elements[i].Bounds.Z,
                    val => Elements[i].PreEditRight((float)val)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].top", (
                    () => Elements[i].Bounds.W,
                    val => Elements[i].PreEditTop((float)val)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].centerx", (
                    () => Elements[i].Center.X,
                    val => Elements[i].Center = new Vector2((float)val, Center.Y)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].centery", (
                    () => Elements[i].Center.Y,
                    val => Elements[i].Center = new Vector2(Center.X, (float)val)
                ));
                if (Elements[i] is NinePatch ninePatch)
                {
                    layout.LineDSLInstance?.AddLambdaRef($"element[{i}].margin", (
                        () => ninePatch.Inset,
                        val => ninePatch.Inset = (float)val
                    ));
                }
            }
        }

        /// <summary>
        /// Measures the vertical content height spanned by all child elements.
        /// </summary>
        /// <returns>The height from the bottom of the lowest element to the top of the highest element.</returns>
        public float MeasureContent()
        {
            float top = 0;
            float bottom = 0;
            for (int i = 0; i < Elements.Count; i++)
            {
                if (i == 0)
                {
                    top = Elements[i].Bounds.W;
                    bottom = Elements[i].Bounds.Y;
                }
                else
                {
                    top = Math.Max(top, Elements[i].Bounds.W);
                    bottom = Math.Min(bottom, Elements[i].Bounds.Y);
                }
            }
            return top - bottom;
        }

        /// <summary>
        /// Gets the proportion of content height exceeding the visible content area.
        /// </summary>
        /// <returns>The difference between measured content height and the panel's content height.</returns>
        public float GetContentProportion()
        {
            return MeasureContent() - ContentHeight;
        }

        /// <summary>
        /// Applies the panel's layout and updates child elements and scrollbar visibility.
        /// </summary>
        /// <remarks>
        /// Recursively applies the layout to nested <see cref="IUIContainer"/> elements.  
        /// Scrollbar visibility is adjusted according to <see cref="ScrollbarVisibilityType"/> and content proportion.
        /// </remarks>
        public void ApplyLayout()
        {
            if (!IsVisible) return;
            if (_layout is not null)
            {
                _layout.Apply();
            }
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].UpdateBounds();
                if (Elements[i] is IUIContainer container)
                {
                    container.ApplyLayout();
                }
            }
            scrollbar.ThumbProportion = ContentHeight / MeasureContent();
            if (ScrollbarVisibilityType is ScrollbarVisibility.Adaptive && scrollbar.ThumbProportion < 1.0f)
                scrollbar.IsVisible = true;
            else if (ScrollbarVisibilityType is ScrollbarVisibility.Always)
                scrollbar.IsVisible = true;
            else
                scrollbar.IsVisible = false;
        }

        /// <summary>
        /// Handles mouse click down events.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        /// <remarks>
        /// Clicks are propagated to the scrollbar and all child elements in reverse order (topmost first).
        /// </remarks>
        public override void OnClickDown(MouseState mouse)
        {
            if (!IsVisible) return;
            if (!WithinBounds(ConvertMouseScreenCoords(mouse.Position))) return;
            scrollbar.OnClickDown(mouse);
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].OnClickDown(mouse);
            }
        }

        /// <summary>
        /// Handles mouse movement events.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        /// <remarks>
        /// Updates scrollbar visibility based on <see cref="ScrollbarVisibilityType"/> and whether the cursor is over the panel.  
        /// Propagates mouse move events to child elements in reverse order. Stops propagation if a child element modifies the mouse cursor.
        /// </remarks>
        public override void OnMouseMove(MouseState mouse)
        {
            if (!IsVisible) return;
            if (Window is not null && Parent is null) Window.Cursor = MouseCursor.Default;
            scrollbar.OnMouseMove(mouse);
            if (!WithinBounds(ConvertMouseScreenCoords(mouse.Position))) return;
            if (ScrollbarVisibilityType is ScrollbarVisibility.MouseOver && scrollbar.WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
            {
                scrollbar.IsVisible = true;
            }
            else if (ScrollbarVisibilityType is ScrollbarVisibility.Always)
            {
                scrollbar.IsVisible = true;
            }
            else if (ScrollbarVisibilityType is ScrollbarVisibility.Adaptive && scrollbar.ThumbProportion < 1.0f)
            {
                scrollbar.IsVisible = true;
            }
            else
            {
                scrollbar.IsVisible = false;
            }
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].OnMouseMove(mouse);
                if (Elements[i] is UIBase iBase && iBase.AltersMouse && Elements[i].WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Handles mouse click release events.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        public override void OnClickUp(MouseState mouse)
        {
            scrollbar.OnClickUp(mouse);
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].OnClickUp(mouse);
            }
        }

        /// <summary>
        /// Updates the panel and all child elements.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last frame in seconds.</param>
        /// <param name="mouse">The current <see cref="MouseState"/>.</param>
        /// <param name="keyboard">The current <see cref="KeyboardState"/>.</param>
        /// <remarks>
        /// Applies layout, updates bounds, propagates updates to all child elements, and handles scroll offsets based on scrollbar position.
        /// </remarks>
        public override void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard)
        {
            ApplyLayout();
            base.OnUpdate(deltaTime);
            var moveAmount = scrollbar.ThumbPosition * Math.Max(MeasureContent() - ContentHeight, 0);
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].PreEditBottom(Elements[i].Bounds.Y + moveAmount);
                Elements[i].PreEditTop(Elements[i].Bounds.W + moveAmount);
                Elements[i].UpdateBounds();
                Elements[i].OnUpdate(deltaTime);
                Elements[i].OnUpdate(deltaTime, mouse);
                Elements[i].OnUpdate(deltaTime, keyboard);
                Elements[i].OnUpdate(deltaTime, mouse, keyboard);
            }
            if (ScrollbarVisibilityType is ScrollbarVisibility.MouseOver && scrollbar.WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
                scrollbar.IsVisible = true;
            ApplicableLayout?.UpdateButtonTextSize();
        }

        /// <summary>
        /// Handles vertical scrolling via the mouse wheel.
        /// </summary>
        /// <param name="mouse">The current <see cref="MouseState"/> representing mouse position and scroll delta.</param>
        /// <remarks>
        /// Propagates the scroll to nested <see cref="Panel"/> or <see cref="TabbedPanel"/> containers if the mouse is over them.  
        /// Scroll consumption is tracked to allow proper chaining of scroll events.
        /// </remarks>
        public override void OnMouseWheel(MouseState mouse)
        {
            if (!IsVisible) return;
            if (!WithinBounds(ConvertMouseScreenCoords(mouse.Position))) return;
            bool IsUnconsumed = true;
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                var element = Elements[i];
                if (element.WithinBounds(ConvertMouseScreenCoords(mouse.Position)) && element is IUIContainer subContainer)
                {
                    if (ConvertMouseScreenCoords(mouse.Position).Y < Bounds.W && ConvertMouseScreenCoords(mouse.Position).Y > Bounds.W - TitleMargin) break;
                    if (subContainer is Panel panel)
                    {
                        panel.OnMouseWheel(mouse);
                        IsUnconsumed = panel.scrollbar.ThumbPosition == 0 && mouse.ScrollDelta.Y > 0 || panel.scrollbar.ThumbPosition == 1 && mouse.ScrollDelta.Y < 0;
                    }
                    else if (subContainer is TabbedPanel tabbedPanel)
                    {
                        tabbedPanel.OnMouseWheel(mouse);
                        IsUnconsumed = tabbedPanel.scrollbar.ThumbPosition == 0 && mouse.ScrollDelta.Y > 0 || tabbedPanel.scrollbar.ThumbPosition == 1 && mouse.ScrollDelta.Y < 0;
                    }
                }
            }
            if (IsUnconsumed)
            {
                scrollbar.OnMouseWheel(mouse);
            }
        }

        /// <summary>
        /// Handles key down events and propagates them to child elements.
        /// </summary>
        /// <param name="e">The keyboard event data.</param>
        public override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (!IsVisible) return;
            base.OnKeyDown(e);
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].OnKeyDown(e);
            }
        }

        /// <summary>
        /// Handles key up events and propagates them to child elements.
        /// </summary>
        /// <param name="e">The keyboard event data.</param>
        public override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (!IsVisible) return;
            base.OnKeyDown(e);
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].OnKeyUp(e);
            }
        }

        /// <summary>
        /// Handles text input events and propagates them to child elements.
        /// </summary>
        /// <param name="e">The text input event data.</param>
        public override void OnTextInput(TextInputEventArgs e)
        {
            if (!IsVisible) return;
            base.OnTextInput(e);
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].OnTextInput(e);
            }
        }

        /// <summary>
        /// Updates the panel's bounds and adjusts the scrollbar's position accordingly.
        /// </summary>
        public override void UpdateBounds()
        {
            if (!IsVisible) return;
            base.UpdateBounds();
            if (scrollbar is not null)
                scrollbar.Bounds = new Vector4(_bounds.Z - scrollbar.Width, _bounds.Y, _bounds.Z, _bounds.W - 0.5f * Inset);
        }

        /// <summary>
        /// Clears all child elements from the panel and frees VRAM resources.
        /// </summary>
        /// <remarks>
        /// If the panel uses a constraint layout, all lambda references are cleared.
        /// Nested <see cref="IUIContainer"/> children are recursively cleared.
        /// </remarks>
        public void Clear()
        {
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                var element = Elements[i];
                element.DeleteFromVRam();
                if (element is IUIContainer container)
                {
                    container.Clear();
                }
            }
            Elements.Clear();
            if (ApplicableLayout is ConstraintLayout layout)
            {
                layout.LineDSLInstance?.ClearLambdaRefs();
            }
        }

        /// <summary>
        /// Checks if a given element is contained within the panel.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <returns>True if the element is part of this panel; otherwise, false.</returns>
        public bool Contains(IUIElement element)
        {
            return Elements.Contains(element);
        }

        /// <summary>
        /// Removes an <see cref="IUIElement"/> from the panel and updates layout references.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        public void Remove(IUIElement element)
        {
            Elements.Remove(element);
            InitializeConstraintVariables();
            UpdateLayoutLambdas();
        }

        /// <summary>
        /// Retrieves a child element at the specified index.
        /// </summary>
        /// <param name="i">Zero-based index of the element to retrieve.</param>
        /// <returns>The <see cref="IUIElement"/> at the given index.</returns>
        public IUIElement Get(int i)
        {
            return Elements[i];
        }

        /// <summary>
        /// Draws the panel, including background, title, scrollbar, and all child elements.
        /// </summary>
        /// <remarks>
        /// Uses OpenGL scissor testing to clip child elements within the panel's content bounds, excluding margins and title area.  
        /// Scroll offset is applied to child element rendering according to the scrollbar's <see cref="ScrollBar.ThumbPosition"/>.
        /// </remarks>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            base.Draw();
            scrollbar.Draw();
            _title.Draw();
            GL.Enable(EnableCap.ScissorTest);
            var clipBounds = FindMinClipBounds();
            var clipWidth = clipBounds.Z - clipBounds.X;
            var clipHeight = clipBounds.W - clipBounds.Y;
            var clipContentHeight = clipHeight - 2 * _contentMargin - TitleMargin;
            GL.Scissor((int)Math.Round(clipBounds.X + _contentMargin), (int)Math.Round(clipBounds.Y + _contentMargin), (int)Math.Round(clipWidth - scrollbar.Width - _contentMargin), (int)Math.Round(clipContentHeight));
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                Elements[i].Draw();
            }
            GL.Disable(EnableCap.ScissorTest);
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}