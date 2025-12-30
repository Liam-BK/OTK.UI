using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common.Input;
using OTK.UI.Components;
using OTK.UI.Layouts;
using OTK.UI.Utility;
using OTK.UI.Interfaces;
using OTK.UI.Managers;
using System.Xml.Linq;
using System.Globalization;
using Image = OTK.UI.Components.Image;

namespace OTK.UI.Containers
{
    /// <summary>
    /// Represents a panel containing multiple tabs, each holding its own UI elements with individual layouts.
    /// Supports scrolling, constraint-based layouts, and dynamic tab switching.
    /// </summary>
    /// <remarks>
    /// Each tab can contain arbitrary UI elements implementing <see cref="IUIElement"/>.
    /// Scrolling is handled via an internal <see cref="ScrollBar"/>.  
    /// Layouts are applied per-tab using <see cref="Layout"/> or <see cref="ConstraintLayout"/>.  
    /// Tab switching updates the currently active layout and visible elements.
    /// </remarks>
    public class TabbedPanel : NinePatch, IUIContainer
    {
        /// <summary>
        /// List of all tabs in the panel.
        /// </summary>
        private readonly List<Tab> Tabs = [];

        /// <summary>
        /// A list of UI elements for each tab. Index corresponds to tab index.
        /// </summary>
        public readonly List<List<IUIElement>> TabElements = [];

        /// <summary>
        /// The internal scroll bar for the panel content.
        /// </summary>
        public ScrollBar scrollbar;

        /// <summary>
        /// Index of the currently selected tab.
        /// </summary>
        public int CurrentTab
        {
            get;
            set;
        }

        /// <summary>
        /// Layouts associated with each tab. Null if a tab has no layout assigned.
        /// </summary>
        private List<Layout?> _layouts = [];

        /// <summary>
        /// The layout applicable to the current tab.
        /// </summary>
        /// <remarks>
        /// Setting this property updates the layout for the current tab and reinitializes constraint variables.
        /// </remarks>
        public Layout? ApplicableLayout
        {
            set
            {
                _layouts[CurrentTab] = value;
                if (_layouts is not null)
                {
                    var currentLayout = _layouts[CurrentTab];
                    if (currentLayout is not null)
                        currentLayout.Parent = this;
                }
                InitializeConstraintVariables();
                UpdateLayoutLambdas();

            }
            protected get
            {
                try
                {
                    return _layouts[CurrentTab];
                }
                catch (IndexOutOfRangeException e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Height of the content area, excluding tabs and margins.
        /// </summary>
        protected float ContentHeight
        {
            get
            {
                return Height - 2 * _contentMargin - TabHeight;
            }
        }

        /// <summary>
        /// Panel position, represented by the center coordinates.
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

        private float _contentMargin;

        /// <summary>
        /// Margin between the panel border and content.
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

        private float _tabHeight;

        /// <summary>
        /// Height of each tab in the panel.
        /// </summary>
        public float TabHeight
        {
            get
            {
                return _tabHeight;
            }
            set
            {
                _tabHeight = value;
            }
        }

        /// <summary>
        /// Texture applied to all tabs.
        /// </summary>
        public string TabTexture
        {
            set
            {
                foreach (var tab in Tabs)
                {
                    tab.Texture = value;
                }
            }
        }

        /// <summary>
        /// Text of the currently selected tab.
        /// </summary>
        public string TabText
        {
            get
            {
                return Tabs[CurrentTab].Text;
            }
            set
            {
                Tabs[CurrentTab].Text = value;
            }
        }

        private static int defaultProgram = 0;

        /// <summary>
        /// Constructs a new TabbedPanel with a given number of tabs and appearance settings.
        /// </summary>
        /// <param name="bounds">The panel bounds.</param>
        /// <param name="tabs">Number of tabs.</param>
        /// <param name="tabHeight">Height of each tab.</param>
        /// <param name="tabInnerMargin">Inner margin for tabs.</param>
        /// <param name="scrollBarWidth">Width of the scroll bar.</param>
        /// <param name="inset">Inset for NinePatch rendering.</param>
        /// <param name="uvInset">UV inset for texture mapping.</param>
        /// <param name="colour">Optional panel color.</param>
        public TabbedPanel(Vector4 bounds, int tabs, float tabHeight, float tabInnerMargin, float scrollBarWidth, float inset, float uvInset, Vector3? colour = null) : base(new Vector4(bounds.X, bounds.Y, bounds.Z, bounds.W - tabHeight), inset, uvInset, colour)
        {
            float tabWidth = Width / tabs;

            for (int i = 0; i < tabs; i++)
            {
                TabElements.Add(new List<IUIElement>());
                Tabs.Add(new Tab(new Vector4(bounds.X + tabWidth * i, bounds.W - tabHeight, bounds.X + tabWidth * (i + 1), bounds.W), tabInnerMargin));
                _layouts.Add(null);
            }
            scrollbar = new ScrollBar(new Vector4(bounds.Z - scrollBarWidth, bounds.Y, bounds.Z, bounds.W - 0.5f * inset - tabHeight), inset);
            InitializeConstraintVariables();
            ScrollbarVisibilityType = ScrollbarVisibility.Always;
            ContentMargin = inset;

            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="TabbedPanel"/> from XML layout data.
        /// </summary>
        /// <param name="element">The XML element describing the panel.</param>
        /// <returns>The initialized <see cref="TabbedPanel"/>.</returns>
        /// <exception cref="FormatException">Thrown if required elements or attributes are missing.</exception>
        public static new TabbedPanel Load(Dictionary<string, IUIElement> registry, XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                throw new FormatException("All elements must have a unique name");

            var bounds = element.Element("Bounds");
            if (bounds is null)
                throw new FormatException($"TabbedPanel: {name} is missing required field Bounds.");

            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var tabTexture = element.Element("TabTexture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var scrollbarTexture = element.Element("ScrollBarTexture")?.Value.Trim() ?? string.Empty;
            var scrollbarThumbTexture = element.Element("ScrollBarThumbTexture")?.Value.Trim() ?? string.Empty;
            var scrollbarColor = element.Element("ScrollBarColor")?.Value.Trim() ?? "1, 1, 1";
            var scrollbarThumbColor = element.Element("ScrollBarThumbColor")?.Value.Trim() ?? "1, 1, 1";
            var scrollbarWidth = float.Parse(element.Element("ScrollBarWidth")?.Value ?? "10", CultureInfo.InvariantCulture);
            var tabHeight = float.Parse(element.Element("TabHeight")?.Value ?? "30", CultureInfo.InvariantCulture);
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            // --- bounds parsing ---
            var left = float.Parse(bounds.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            // --- color parsing ---
            var colorVec = LayoutLoader.ParseVector3(color, name);
            var scrollbarColorVec = LayoutLoader.ParseVector3(scrollbarColor, name);
            var scrollbarThumbColorVec = LayoutLoader.ParseVector3(scrollbarThumbColor, name);

            // --- anchor adjustment ---
            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult))
                anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            // --- find tabs ---
            var tabElements = element.Elements("Tab").ToList();
            int tabCount = tabElements.Count;
            if (tabCount == 0)
                throw new FormatException($"TabbedPanel: {name} must contain at least one <Tab> element.");

            // --- construct panel ---
            var tabbedPanel = new TabbedPanel(
                new Vector4(left, bottom, right, top) + relativeAnchorVector,
                tabCount,
                tabHeight,
                margin,         // inner tab margin
                scrollbarWidth,
                margin,
                uvMargin,
                colorVec
            );

            tabbedPanel.IsVisible = isVisible;
            tabbedPanel.scrollbar.Texture = scrollbarTexture;
            tabbedPanel.scrollbar.ThumbTexture = scrollbarThumbTexture;
            tabbedPanel.scrollbar.Colour = scrollbarColorVec;
            tabbedPanel.scrollbar.ThumbColour = scrollbarThumbColorVec;

            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                tabbedPanel.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else tabbedPanel.Texture = texture;

            if (LayoutLoader.IsFilePath(tabTexture))
            {
                TextureManager.LoadTexture(tabTexture, Path.GetFileNameWithoutExtension(tabTexture));
                tabbedPanel.TabTexture = Path.GetFileNameWithoutExtension(tabTexture);
            }
            else tabbedPanel.TabTexture = tabTexture;

            // --- iterate through each tab ---
            for (int i = 0; i < tabElements.Count; i++)
            {
                var tabElement = tabElements[i];
                var layoutElement = tabElement.Element("ConstraintLayout") ?? tabElement.Element("VerticalLayout") ?? tabElement.Element("HorizontalLayout") ?? tabElement.Element("FlowLayout");
                Console.WriteLine($"layout element: {layoutElement?.Name.LocalName}");

                tabbedPanel.CurrentTab = i;
                tabbedPanel.ApplicableLayout = Layout.Load(layoutElement);
                tabbedPanel.InitializeConstraintVariables();
                tabbedPanel.TabText = tabElement.Element("Text")?.Value ?? string.Empty;

                // --- load children of this tab ---
                foreach (var child in tabElement.Elements())
                {
                    switch (child.Name.LocalName.ToLower())
                    {
                        case "button":
                            var button = Button.Load(registry, child);
                            tabbedPanel.Add(button);
                            break;
                        case "breadcrumb":
                            var breadcrumb = BreadCrumb.Load(registry, child);
                            tabbedPanel.Add(breadcrumb);
                            break;
                        case "checkbox":
                            var checkbox = CheckBox.Load(registry, child);
                            tabbedPanel.Add(checkbox);
                            break;
                        case "image":
                            var image = Image.Load(registry, child);
                            tabbedPanel.Add(image);
                            break;
                        case "label":
                            var label = Label.Load(registry, child);
                            tabbedPanel.Add(label);
                            break;
                        case "ninepatch":
                            var ninePatch = NinePatch.Load(registry, child);
                            tabbedPanel.Add(ninePatch);
                            break;
                        case "numericspinner":
                            var numericspinner = NumericSpinner.Load(registry, child);
                            tabbedPanel.Add(numericspinner);
                            break;
                        case "progressbar":
                            var progressbar = ProgressBar.Load(registry, child);
                            tabbedPanel.Add(progressbar);
                            break;
                        case "radialmenu":
                            throw new ArgumentException("Radial menu should not be contained in a TabbedPanel");
                        case "scrollbar":
                            var scrollbar = ScrollBar.Load(registry, child);
                            tabbedPanel.Add(scrollbar);
                            break;
                        case "slider":
                            var slider = Slider.Load(registry, child);
                            tabbedPanel.Add(slider);
                            break;
                        case "textfield":
                            var textfield = TextField.Load(registry, child);
                            tabbedPanel.Add(textfield);
                            break;
                        case "panel":
                            var childPanel = Panel.Load(registry, child);
                            tabbedPanel.Add(i, childPanel);
                            break;
                        case "dynamicpanel":
                            throw new ArgumentException("DynamicPanel cannot exist as a child of TabbedPanel");
                        case "tab": // nested tabs disallowed
                            throw new ArgumentException("Nested <Tab> elements are not supported inside other <Tab>.");
                    }
                }
            }

            tabbedPanel.CurrentTab = 0;

            if (registry.ContainsKey(name)) throw new ArgumentException($"An element with name: {name} has already been registered.");
            registry.Add(name, tabbedPanel);
            return tabbedPanel;
        }

        /// <summary>
        /// Initializes constraint variables for the current tab's constraintlayout so that it can interface with the code variables.
        /// </summary>
        /// <remarks>Note that this only works if the ApplicableLayout is a ConstraintLayout.</remarks>
        public void InitializeConstraintVariables()
        {
            if (_layouts.Count > 0)
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
                    () => ContentMargin,
                    val => ContentMargin = (float)val
                ));
                layout.LineDSLInstance?.AddLambdaRef($"titlemargin", (
                    () => TabHeight,
                    val => TabHeight = (float)val
                ));
            }
        }

        /// <summary>
        /// Adds a UI element to the current tab.
        /// </summary>
        /// <param name="element">The element to add.</param>
        /// <exception cref="ArgumentException">Thrown if the element already has a parent.</exception>
        public void Add(IUIElement element)
        {
            if (element.Parent is not null) throw new ArgumentException("Provided Element already has a parent. Make sure the parent is null before adding to an IUIContainer.");
            TabElements[CurrentTab].Add(element);
            element.Parent = this;
            if (ApplicableLayout is not ConstraintLayout layout) return;
            var target = TabElements[CurrentTab][^1];
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[CurrentTab].Count - 1}].left", (
                () => target.Bounds.X,
                val => target.PreEditLeft((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[CurrentTab].Count - 1}].bottom", (
                () => target.Bounds.Y,
                val => target.PreEditBottom((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[CurrentTab].Count - 1}].right", (
                () => target.Bounds.Z,
                val => target.PreEditRight((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[CurrentTab].Count - 1}].top", (
                () => target.Bounds.W,
                val => target.PreEditTop((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[CurrentTab].Count - 1}].centerx", (
                () => target.Center.X,
                val => target.Center = new Vector2((float)val, Center.Y)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[CurrentTab].Count - 1}].centery", (
                () => target.Center.Y,
                val => target.Center = new Vector2(Center.X, (float)val)
            ));
            if (target is NinePatch ninePatch)
            {
                layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[CurrentTab].Count - 1}].margin", (
                    () => ninePatch.Inset,
                    val => ninePatch.Inset = (float)val
                ));
            }
            scrollbar.ThumbProportion = ContentHeight / MeasureContent();
        }

        /// <summary>
        /// Adds a UI element to a specific tab.
        /// </summary>
        /// <param name="tab">The tab index to add the element to.</param>
        /// <param name="element">The element to add.</param>
        public void Add(int tab, IUIElement element)
        {
            if (element.Parent is not null) throw new ArgumentException("Provided Element already has a parent. Make sure the parent is null before adding to an IUIContainer.");
            TabElements[tab].Add(element);
            element.Parent = this;
            if (ApplicableLayout is not ConstraintLayout layout) return;
            var target = TabElements[tab][^1];
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[tab].Count - 1}].left", (
                () => target.Bounds.X,
                val => target.PreEditLeft((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[tab].Count - 1}].bottom", (
                () => target.Bounds.Y,
                val => target.PreEditBottom((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[tab].Count - 1}].right", (
                () => target.Bounds.Z,
                val => target.PreEditRight((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[tab].Count - 1}].top", (
                () => target.Bounds.W,
                val => target.PreEditTop((float)val)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[tab].Count - 1}].centerx", (
                () => target.Center.X,
                val => target.Center = new Vector2((float)val, Center.Y)
            ));
            layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[tab].Count - 1}].centery", (
                () => target.Center.Y,
                val => target.Center = new Vector2(Center.X, (float)val)
            ));
            if (target is NinePatch ninePatch)
            {
                layout.LineDSLInstance?.AddLambdaRef($"element[{TabElements[tab].Count - 1}].margin", (
                    () => ninePatch.Inset,
                    val => ninePatch.Inset = (float)val
                ));
            }
            scrollbar.ThumbProportion = ContentHeight / MeasureContent();
        }

        /// <summary>
        /// Updates lambda references for the current tab's layout.
        /// </summary>
        /// <param name="clear">If true, clears all existing lambda references before updating.</param>
        private void UpdateLayoutLambdas(bool clear = false)
        {
            if (ApplicableLayout is not ConstraintLayout layout) return;
            if (clear) layout.LineDSLInstance.ClearLambdaRefs();
            for (int i = 0; i < TabElements[CurrentTab].Count; i++)
            {
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].left", (
                    () => TabElements[CurrentTab][i].Bounds.X,
                    val => TabElements[CurrentTab][i].PreEditLeft((float)val)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].bottom", (
                    () => TabElements[CurrentTab][i].Bounds.Y,
                    val => TabElements[CurrentTab][i].PreEditBottom((float)val)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].right", (
                    () => TabElements[CurrentTab][i].Bounds.Z,
                    val => TabElements[CurrentTab][i].PreEditRight((float)val)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].top", (
                    () => TabElements[CurrentTab][i].Bounds.W,
                    val => TabElements[CurrentTab][i].PreEditTop((float)val)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].centerx", (
                    () => TabElements[CurrentTab][i].Center.X,
                    val => TabElements[CurrentTab][i].Center = new Vector2((float)val, Center.Y)
                ));
                layout.LineDSLInstance?.AddLambdaRef($"element[{i}].centery", (
                    () => TabElements[CurrentTab][i].Center.Y,
                    val => TabElements[CurrentTab][i].Center = new Vector2(Center.X, (float)val)
                ));
                if (TabElements[CurrentTab][i] is NinePatch ninePatch)
                {
                    layout.LineDSLInstance?.AddLambdaRef($"element[{i}].margin", (
                        () => ninePatch.Inset,
                        val => ninePatch.Inset = (float)val
                    ));
                }
            }
        }

        /// <summary>
        /// Measures the total vertical content height of the current tab.
        /// </summary>
        /// <returns>The height of the content.</returns>
        private float MeasureContent()
        {
            float top = 0;
            float bottom = 0;
            for (int i = 0; i < TabElements[CurrentTab].Count; i++)
            {
                if (i == 0)
                {
                    top = TabElements[CurrentTab][i].Bounds.W;
                    bottom = TabElements[CurrentTab][i].Bounds.Y;
                }
                else
                {
                    top = Math.Max(top, TabElements[CurrentTab][i].Bounds.W);
                    bottom = Math.Min(bottom, TabElements[CurrentTab][i].Bounds.Y);
                }
            }
            return top - bottom;
        }

        /// <summary>
        /// Removes an element from the current tab.
        /// </summary>
        /// <param name="element">Element to remove.</param>
        public void Remove(IUIElement element)
        {
            TabElements[CurrentTab].Remove(element);
        }

        /// <summary>
        /// Clears all elements from the current tab.
        /// </summary>
        public void Clear()
        {
            TabElements[CurrentTab].Clear();
        }

        /// <summary>
        /// Applies the current layout and updates bounds for all elements.
        /// Also updates scrollbar visibility and thumb proportion.
        /// </summary>
        public void ApplyLayout()
        {
            var currentLayout = ApplicableLayout;
            currentLayout?.Apply();
            foreach (var element in TabElements[CurrentTab])
            {
                element.UpdateBounds();
                if (element is IUIContainer container)
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
        /// Updates the panel and its tabs for the given delta time and mouse state.
        /// </summary>
        public override void OnUpdate(float deltaTime, MouseState mouse)
        {
            foreach (var tab in Tabs)
            {
                tab.OnUpdate(deltaTime, mouse);
            }
        }

        /// <summary>
        /// Updates the panel and its contained elements for mouse and keyboard state.
        /// </summary>
        public override void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard)
        {
            if (!IsVisible) return;
            ApplyLayout();
            base.OnUpdate(deltaTime);
            var moveAmount = scrollbar.ThumbPosition * Math.Max(MeasureContent() - ContentHeight, 0);
            foreach (var element in TabElements[CurrentTab])
            {
                element.PreEditBottom(element.Bounds.Y + moveAmount);
                element.PreEditTop(element.Bounds.W + moveAmount);
                element.UpdateBounds();
                element.OnUpdate(deltaTime);
                element.OnUpdate(deltaTime, mouse);
                element.OnUpdate(deltaTime, keyboard);
                element.OnUpdate(deltaTime, mouse, keyboard);
            }
            if (ScrollbarVisibilityType is ScrollbarVisibility.MouseOver && scrollbar.WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
                scrollbar.IsVisible = true;
            OnUpdate(deltaTime, mouse);
        }

        /// <summary>
        /// Propagates key down events to all elements in the current tab.
        /// </summary>
        public override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (!IsVisible) return;
            base.OnKeyDown(e);
            foreach (var element in TabElements[CurrentTab])
            {
                element.OnKeyDown(e);
            }
        }

        /// <summary>
        /// Propagates key up events to all elements in the current tab.
        /// </summary>
        public override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (!IsVisible) return;
            base.OnKeyDown(e);
            foreach (var element in TabElements[CurrentTab])
            {
                element.OnKeyUp(e);
            }
        }

        /// <summary>
        /// Propagates text input events to all elements in the current tab.
        /// </summary>
        public override void OnTextInput(TextInputEventArgs e)
        {
            if (!IsVisible) return;
            base.OnTextInput(e);
            foreach (var element in TabElements[CurrentTab])
            {
                element.OnTextInput(e);
            }
        }

        /// <summary>
        /// Handles mouse down events and updates tab selection.
        /// </summary>
        public override void OnClickDown(MouseState mouse)
        {
            if (!IsVisible) return;
            base.OnClickDown(mouse);
            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
                {
                    CurrentTab = i;
                    Tabs[i].Pressed = true;
                    break;
                }
            }
            if (!WithinBounds(ConvertMouseScreenCoords(mouse.Position))) return;
            foreach (var element in TabElements[CurrentTab])
            {
                element.OnClickDown(mouse);
            }
        }

        /// <summary>
        /// Handles mouse movement events, updates scrollbar visibility, and propagates to elements.
        /// </summary>
        public override void OnMouseMove(MouseState mouse)
        {
            if (!IsVisible) return;
            scrollbar.OnMouseMove(mouse);
            if (Window is not null) Window.Cursor = MouseCursor.Default;
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
            foreach (var element in TabElements[CurrentTab])
            {
                element.OnMouseMove(mouse);
            }
        }

        /// <summary>
        /// Handles mouse up events and resets tab pressed states.
        /// </summary>
        public override void OnClickUp(MouseState mouse)
        {
            if (!IsVisible) return;
            base.OnClickUp(mouse);
            foreach (var tab in Tabs)
            {
                tab.Pressed = false;
            }
            foreach (var element in TabElements[CurrentTab])
            {
                element.OnClickUp(mouse);
            }
        }

        /// <summary>
        /// Handles mouse wheel events for scrolling content and propagating to child containers.
        /// </summary>
        public override void OnMouseWheel(MouseState mouse)
        {
            if (!IsVisible) return;
            if (!WithinBounds(ConvertMouseScreenCoords(mouse.Position))) return;
            bool IsUnconsumed = true;
            foreach (var element in TabElements[CurrentTab])
            {
                // if (i >= TabElements[CurrentTab].Count || i < 0) break;
                if (element.WithinBounds(ConvertMouseScreenCoords(mouse.Position)) && element is IUIContainer subContainer)
                {
                    if (ConvertMouseScreenCoords(mouse.Position).Y < Bounds.W && ConvertMouseScreenCoords(mouse.Position).Y > Bounds.W - TabHeight) break;
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
        /// Determines whether a specific element exists in the current tab.
        /// </summary>
        public bool Contains(IUIElement element)
        {
            return TabElements[CurrentTab].Contains(element);
        }

        /// <summary>
        /// Retrieves a specific element by index in the current tab.
        /// </summary>
        public IUIElement Get(int i)
        {
            return TabElements[CurrentTab][i];
        }

        /// <summary>
        /// Draws the panel, tabs, scrollbar, and all elements in the current tab.
        /// Applies scissor clipping to prevent drawing outside the content area.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            base.Draw();
            foreach (var tab in Tabs)
            {
                tab.Draw();
            }
            scrollbar.Draw();
            GL.Enable(EnableCap.ScissorTest);
            var clipBounds = FindMinClipBounds();
            var clipWidth = clipBounds.Z - clipBounds.X;
            var clipHeight = clipBounds.W - clipBounds.Y;
            var clipContentHeight = clipHeight - 2 * _contentMargin - TabHeight;
            GL.Scissor((int)Math.Round(clipBounds.X + _contentMargin), (int)Math.Round(clipBounds.Y + _contentMargin), (int)Math.Round(clipWidth - scrollbar.Width - _contentMargin), (int)Math.Round(clipContentHeight));
            for (int i = TabElements[CurrentTab].Count - 1; i >= 0; i--)
            {
                TabElements[CurrentTab][i].Draw();
            }
            GL.Disable(EnableCap.ScissorTest);
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}