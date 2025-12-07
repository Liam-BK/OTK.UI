using OpenTK.Mathematics;
using OTK.UI.Containers;
using OTK.UI.Layouts;
using OTK.UI.Utility;
using OTK.UI.Components;
using OTK.UI.Managers;
using System.Xml.Linq;
using System.Globalization;

namespace OTK.UI.Pickers
{
    /// <summary>
    /// A dynamic color picker panel that allows the user to select and adjust a color using
    /// a color wheel, numeric RGB inputs, and a brightness slider.  
    /// Supports real-time updates of the selected color and provides a visual swatch.  
    /// Can be loaded from XML using <see cref="Load(XElement)"/>.
    /// </summary>
    public class ColorPicker : DynamicPanel
    {
        private ColorWheel wheel;

        private NinePatch colourSwatch;

        private NumericSpinner r, g, b;

        private Slider brightness;

        /// <summary>
        /// The currently selected color as an RGB vector with components in [0, 1].
        /// </summary>
        public Vector3 CurrentColour = Vector3.One;

        private Label Red, Green, Blue;

        private Label Brightness;

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a new <see cref="ColorPicker"/> instance with the given bounds, scroll bar width, inset, UV inset, and optional background color.
        /// </summary>
        /// <param name="bounds">The rectangular bounds of the panel.</param>
        /// <param name="scrollbarWidth">The width of the vertical scrollbar.</param>
        /// <param name="inset">Inset margin for child elements.</param>
        /// <param name="uvInset">UV inset for textures.</param>
        /// <param name="colour">Optional background color.</param>
        public ColorPicker(Vector4 bounds, float scrollbarWidth, float inset = 10, float uvInset = 0.5F, Vector3? colour = null) : base(bounds, scrollbarWidth, inset, uvInset, colour)
        {
            TitleMargin = 25;
            Title = "Color Picker";
            Brightness = new Label(new Vector2(), 15, "Brightness", new Vector3());
            Brightness.Alignment = Label.TextAlign.Center;
            Red = new Label(new Vector2(), 20, "Red", new Vector3());
            Green = new Label(new Vector2(), 20, "Green", new Vector3());
            Blue = new Label(new Vector2(), 20, "Blue", new Vector3());

            scrollbar.ThumbColour = new Vector3(0.8f, 0.9f, 1.0f);
            wheel = new ColorWheel(new Vector4(bounds.X + inset, bounds.W - inset - Width, bounds.Z - inset - scrollbarWidth, bounds.W - inset));
            wheel.Pressed += (btn, mousePos) =>
            {
                CurrentColour = wheel.GetColour(mousePos);
                UpdateElements();
            };

            colourSwatch = new NinePatch(new Vector4(), 10, 0.5f);
            // colourSwatch.Texture = "CheckboxEmpty";

            brightness = new Slider(new Vector4(), 10, 0.5f);
            brightness.ThumbColour = new Vector3(0.8f, 0.9f, 1.0f);
            brightness.Value = 1;
            brightness.OnValueChanged += val =>
            {
                colourSwatch.Colour = CurrentColour * val;
            };

            r = new NumericSpinner(new Vector4(), 30, 10);
            r.Texture = "CheckboxEmpty";
            r.Text = "255";
            r.increment.Parent = this;
            r.decrement.Parent = this;
            r.increment.Colour = new Vector3(0.8f, 0.8f, 0.8f);
            r.decrement.Colour = new Vector3(0.8f, 0.8f, 0.8f);
            r.increment.RolloverColour = new Vector3(0.5f, 0.5f, 0.5f);
            r.decrement.RolloverColour = new Vector3(0.5f, 0.5f, 0.5f);
            r.increment.TimeToRollover = 0;
            r.decrement.TimeToRollover = 0;
            r.increment.Texture = "CheckboxEmpty";
            r.decrement.Texture = "CheckboxEmpty";
            r.increment.Pressed += btn =>
            {
                var IsSpun = false;
                var val = float.Parse(r.Text) / 255f;
                while (val > 1)
                {
                    val -= 1;
                    IsSpun = true;
                }
                r.Text = $"{(int)(val * 255f) - (IsSpun ? 1 : 0)}";
                CurrentColour = new Vector3(val, CurrentColour.Y, CurrentColour.Z);
                colourSwatch.Colour = CurrentColour * brightness.Value;
            };
            r.decrement.Pressed += btn =>
            {
                var IsSpun = false;
                var val = float.Parse(r.Text) / 255f;
                while (val < 0)
                {
                    val += 1;
                    IsSpun = true;
                }
                r.Text = $"{(int)(val * 255f) + (IsSpun ? 1 : 0)}";
                CurrentColour = new Vector3(val, CurrentColour.Y, CurrentColour.Z);
                colourSwatch.Colour = CurrentColour * brightness.Value;
            };
            r.OnTextChanged += text =>
            {
                UpdateCurrentColour();
            };
            r.OnTextEnter += text =>
            {
                if (string.IsNullOrWhiteSpace(r.Text)) r.Text = "0";
            };
            g = new NumericSpinner(new Vector4(), 30, 10);
            g.Texture = "CheckboxEmpty";
            g.Text = "255";
            g.increment.Parent = this;
            g.decrement.Parent = this;
            g.increment.Colour = new Vector3(0.8f, 0.8f, 0.8f);
            g.decrement.Colour = new Vector3(0.8f, 0.8f, 0.8f);
            g.increment.RolloverColour = new Vector3(0.5f, 0.5f, 0.5f);
            g.decrement.RolloverColour = new Vector3(0.5f, 0.5f, 0.5f);
            g.increment.TimeToRollover = 0;
            g.decrement.TimeToRollover = 0;
            g.increment.Texture = "CheckboxEmpty";
            g.decrement.Texture = "CheckboxEmpty";
            g.increment.Pressed += btn =>
            {
                var IsSpun = false;
                var val = float.Parse(g.Text) / 255f;
                while (val > 1)
                {
                    val -= 1;
                    IsSpun = true;
                }
                g.Text = $"{(int)(val * 255f) - (IsSpun ? 1 : 0)}";
                CurrentColour = new Vector3(CurrentColour.X, val, CurrentColour.Z);
                colourSwatch.Colour = CurrentColour * brightness.Value;
            };
            g.decrement.Pressed += btn =>
            {
                var IsSpun = false;
                var val = float.Parse(g.Text) / 255f;
                while (val < 0)
                {
                    val += 1;
                    IsSpun = true;
                }
                g.Text = $"{(int)(val * 255f) + (IsSpun ? 1 : 0)}";
                CurrentColour = new Vector3(CurrentColour.X, val, CurrentColour.Z);
                colourSwatch.Colour = CurrentColour * brightness.Value;
            };
            g.OnTextChanged += text =>
            {
                UpdateCurrentColour();
            };
            g.OnTextEnter += text =>
            {
                if (string.IsNullOrWhiteSpace(g.Text)) g.Text = "0";
            };
            b = new NumericSpinner(new Vector4(), 30, 10);
            b.Texture = "CheckboxEmpty";
            b.Text = "255";
            b.increment.Parent = this;
            b.decrement.Parent = this;
            b.increment.Colour = new Vector3(0.8f, 0.8f, 0.8f);
            b.decrement.Colour = new Vector3(0.8f, 0.8f, 0.8f);
            b.increment.RolloverColour = new Vector3(0.5f, 0.5f, 0.5f);
            b.decrement.RolloverColour = new Vector3(0.5f, 0.5f, 0.5f);
            b.increment.TimeToRollover = 0;
            b.decrement.TimeToRollover = 0;
            b.increment.Texture = "CheckboxEmpty";
            b.decrement.Texture = "CheckboxEmpty";
            b.increment.Pressed += btn =>
            {
                var IsSpun = false;
                var val = float.Parse(b.Text) / 255f;
                while (val > 1)
                {
                    val -= 1;
                    IsSpun = true;
                }
                b.Text = $"{(int)(val * 255f) - (IsSpun ? 1 : 0)}";
                CurrentColour = new Vector3(CurrentColour.X, CurrentColour.Y, val);
                colourSwatch.Colour = CurrentColour * brightness.Value;
            };
            b.decrement.Pressed += btn =>
            {
                var IsSpun = false;
                var val = float.Parse(b.Text) / 255f;
                while (val < 0)
                {
                    val += 1;
                    IsSpun = true;
                }
                b.Text = $"{(int)(val * 255f) + (IsSpun ? 1 : 0)}";
                CurrentColour = new Vector3(CurrentColour.X, CurrentColour.Y, val);
                colourSwatch.Colour = CurrentColour * brightness.Value;
            };
            b.OnTextChanged += text =>
            {
                UpdateCurrentColour();
            };
            b.OnTextEnter += text =>
            {
                if (string.IsNullOrWhiteSpace(b.Text)) b.Text = "0";
            };

            InitLayout();
            Add(wheel);
            Add(colourSwatch);
            Add(Brightness);
            Add(brightness);
            Add(Red);
            Add(r);
            Add(Green);
            Add(g);
            Add(Blue);
            Add(b);

            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="ColorPicker"/> from an XML element, initializing bounds, color, visibility, texture, and layout parameters.
        /// Throws <see cref="FormatException"/> if required fields are missing.
        /// </summary>
        /// <param name="element">The XML element describing the color picker configuration.</param>
        /// <returns>A fully initialized <see cref="ColorPicker"/> instance.</returns>
        public static new ColorPicker Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"FilePicker: {name} is missing required field Bounds.");
            var minSize = element.Element("MinSize");

            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var scrollbarWidth = float.Parse(element.Element("ScrollBarWidth")?.Value ?? "10", CultureInfo.InvariantCulture);
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var minWidth = float.Parse(minSize?.Element("MinWidth")?.Value ?? "100", CultureInfo.InvariantCulture);
            var minHeight = float.Parse(minSize?.Element("MinHeight")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            ColorPicker colorPicker = new ColorPicker(new Vector4(left, bottom, right, top) + relativeAnchorVector, scrollbarWidth, margin, uvMargin);
            colorPicker.IsVisible = isVisible;
            colorPicker.Colour = colorVec;
            colorPicker.MinimumWidth = minWidth;
            colorPicker.MinimumHeight = minHeight;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                colorPicker.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else colorPicker.Texture = texture;

            return colorPicker;
        }

        private void InitLayout()
        {
            ConstraintLayout layout = new ConstraintLayout();
            layout.LineDSLInstance.Evaluate("var wheelbottom = 0");
            layout.LineDSLInstance.Evaluate("var width = 0");
            List<string> constraints = [
                //colorwheel
                "element[0].left = panelleft + contentmargin",
            "element[0].right = scrollbarleft - contentmargin",
            "element[0].top = paneltop - contentmargin - titlemargin",
            "wheelbottom = paneltop - contentmargin - titlemargin - ((scrollbarleft + contentmargin) - (panelleft + contentmargin))",
            "element[0].bottom = wheelbottom",
            "width = ((scrollbarleft - contentmargin) - (panelleft + contentmargin))",
            //color swatch
            "element[1].left = panelcenterx - width * 0.25",
            "element[1].right = panelcenterx + width * 0.25",
            "element[1].top = wheelbottom - contentmargin",
            "element[1].bottom = wheelbottom - contentmargin - 24",
            //brightness label
            "element[2].left = panelcenterx - 7.5",
            "element[2].right = panelcenterx + 7.5",
            "element[2].top = wheelbottom - contentmargin - 45",
            "element[2].bottom = wheelbottom - contentmargin - 60",
            //brightness slider
            "element[3].left = panelleft + contentmargin + 10",
            "element[3].right = panelleft + contentmargin + width - 10",
            "element[3].top = wheelbottom - contentmargin - contentmargin - 45",
            "element[3].bottom = wheelbottom - contentmargin - contentmargin - 65",
            //Red label
            "element[4].left = panelleft + contentmargin",
            "element[4].right = panelleft + contentmargin + 15",
            "element[4].top = wheelbottom - contentmargin - 100 - contentmargin + 7.5",
            "element[4].bottom = wheelbottom - contentmargin - 100 - contentmargin - 7.5",
            //Red numericSpinner
            "element[5].left = panelcenterx + contentmargin",
            "element[5].right = scrollbarleft - contentmargin",
            "element[5].top = wheelbottom - contentmargin - 75 - contentmargin",
            "element[5].bottom = wheelbottom - contentmargin - 100 - contentmargin",
            //Green label
            "element[6].left = panelleft + contentmargin",
            "element[6].right = panelleft + contentmargin + 15",
            "element[6].top = wheelbottom - contentmargin - 130 - contentmargin + 7.5",
            "element[6].bottom = wheelbottom - contentmargin - 130 - contentmargin - 7.5",
            // //Green numericSpinner
            "element[7].left = panelcenterx + contentmargin",
            "element[7].right = scrollbarleft - contentmargin",
            "element[7].top = wheelbottom - contentmargin - 105 - contentmargin",
            "element[7].bottom = wheelbottom - contentmargin - 130 - contentmargin",
            // //Blue label
            "element[8].left = panelleft + contentmargin",
            "element[8].right = panelleft + contentmargin + 15",
            "element[8].top = wheelbottom - contentmargin - 160 - contentmargin + 7.5",
            "element[8].bottom = wheelbottom - contentmargin - 160 - contentmargin - 7.5",
            // //Blue numericSpinner
            "element[9].left = panelcenterx + contentmargin",
            "element[9].right = scrollbarleft - contentmargin",
            "element[9].top = wheelbottom - contentmargin - 135 - contentmargin",
            "element[9].bottom = wheelbottom - contentmargin - 160 - contentmargin",
        ];
            layout.Constraints = constraints;
            ApplicableLayout = layout;
            InitializeConstraintVariables();
        }

        private void UpdateElements()
        {
            colourSwatch.Colour = CurrentColour * brightness.Value;
            r.Text = $"{(int)(CurrentColour.X * 255)}";
            g.Text = $"{(int)(CurrentColour.Y * 255)}";
            b.Text = $"{(int)(CurrentColour.Z * 255)}";
        }

        private void UpdateCurrentColour()
        {
            var rText = r.Text;
            var gText = g.Text;
            var bText = b.Text;
            var rVal = string.IsNullOrWhiteSpace(rText) ? 0 : float.Parse(rText) / 255f;
            var gVal = string.IsNullOrWhiteSpace(gText) ? 0 : float.Parse(gText) / 255f;
            var bVal = string.IsNullOrWhiteSpace(bText) ? 0 : float.Parse(bText) / 255f;
            CurrentColour = new Vector3(rVal, gVal, bVal);
            colourSwatch.Colour = CurrentColour * brightness.Value;
        }
    }
}