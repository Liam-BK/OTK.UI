using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Managers;
using OTK.UI.Utility;
using System.Globalization;
using System.Xml.Linq;

namespace OTK.UI.Components
{
    /// <summary>
    /// A <see cref="TextField"/>-based component that allows numeric input with increment/decrement buttons.
    /// Supports mathematical expressions using <see cref="LineDSL"/>.
    /// </summary>
    /// <remarks>
    /// The numeric spinner consists of a central text field and two buttons: increment and decrement.
    /// Users can click the buttons or type numeric expressions directly into the text field.
    /// Pressing Enter evaluates the expression using <see cref="LineDSL"/>.  
    /// The component handles bounds updates, button positioning, and text evaluation automatically.
    /// </remarks>
    public class NumericSpinner : TextField
    {
        private readonly LineDSL _solver = new();

        /// <summary>
        /// The button used to increment the numeric value.
        /// </summary>
        public Button increment;

        /// <summary>
        /// The button used to decrement the numeric value.
        /// </summary>
        public Button decrement;

        private float buttonWidth;

        /// <summary>
        /// Gets or sets the texture for both increment and decrement buttons.
        /// </summary>
        public string ButtonTexture
        {
            get
            {
                return increment.Texture;
            }
            set
            {
                increment.Texture = value;
                decrement.Texture = value;
            }
        }

        private static int defaultProgram = 0;

        /// <summary>
        /// Initializes a new instance of <see cref="NumericSpinner"/> with specified bounds, button width, and styling.
        /// </summary>
        /// <param name="bounds">The total bounds of the component (including buttons).</param>
        /// <param name="buttonWidth">The width of the increment/decrement buttons.</param>
        /// <param name="inset">Inset margin for the text field and buttons.</param>
        /// <param name="uvInset">UV inset used for texture rendering.</param>
        /// <param name="colour">The background color of the text field portion.</param>
        public NumericSpinner(Vector4 bounds, float buttonWidth, float inset, float uvInset = 0.5f, Vector3? colour = null) : base(bounds - Vector4.UnitZ * buttonWidth, inset, uvInset, colour)
        {
            increment = new Button(new Vector4(bounds.Z - buttonWidth, Center.Y, bounds.Z, bounds.W), inset, uvInset, "+");
            decrement = new Button(new Vector4(bounds.Z - buttonWidth, bounds.Y, bounds.Z, Center.Y), inset, uvInset, "-");

            increment.Pressed += btn => { if (btn == MouseButton.Left) Increment(); };
            decrement.Pressed += btn => { if (btn == MouseButton.Left) Decrement(); };

            this.buttonWidth = buttonWidth;

            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.TextField.vert", "OTK.UI.Shaders.Fragment.TextField.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="NumericSpinner"/> from an XML element.
        /// </summary>
        /// <param name="element">The XML element defining properties for the spinner.</param>
        /// <returns>A fully configured <see cref="NumericSpinner"/> instance.</returns>
        /// <exception cref="FormatException">
        /// Thrown if the element is missing required fields or the Text field is not numeric.
        /// </exception>
        public static new NumericSpinner Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var buttonWidth = float.Parse(element.Element("ButtonWidth")?.Value ?? "20", CultureInfo.InvariantCulture);
            var text = element.Element("Text")?.Value ?? string.Empty;
            if (!float.TryParse(text, out _)) throw new FormatException($"The Text field for {name} must be Numeric in nature for NumericSpinner.");
            var textColor = element.Element("TextColorRGB")?.Value ?? "0, 0, 0";
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var buttonTexture = element.Element("ButtonTexture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var buttonColor = element.Element("ButtonColorRGB")?.Value ?? "1, 1, 1";
            var buttonTextColor = element.Element("ButtonTextColorRGB")?.Value ?? "0, 0, 0";
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var buttonColorVec = LayoutLoader.ParseVector3(buttonColor, name);
            var textColorVec = LayoutLoader.ParseVector3(textColor, name);
            var buttonTextColorVec = LayoutLoader.ParseVector3(buttonTextColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            NumericSpinner numericSpinner = new NumericSpinner(new Vector4(left, bottom, right, top) + relativeAnchorVector, buttonWidth, margin, uvMargin);
            numericSpinner.IsVisible = isVisible;
            numericSpinner.Colour = colorVec;
            numericSpinner.text.Colour = textColorVec;
            numericSpinner.Text = text;
            numericSpinner.CaretIndex = text.Length;
            numericSpinner.LeftSelectIndex = numericSpinner.CaretIndex;
            numericSpinner.RightSelectIndex = numericSpinner.CaretIndex;
            numericSpinner.ButtonTexture = buttonTexture;
            numericSpinner.increment.Colour = buttonColorVec;
            numericSpinner.decrement.Colour = buttonColorVec;
            numericSpinner.increment.label.Colour = buttonTextColorVec;
            numericSpinner.decrement.label.Colour = buttonTextColorVec;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                numericSpinner.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else numericSpinner.Texture = texture;

            return numericSpinner;
        }

        /// <summary>
        /// Updates the bounds of the text field and positions the increment/decrement buttons.
        /// </summary>
        public override void UpdateBounds()
        {
            base.UpdateBounds();
            if (increment is not null) increment.Bounds = new Vector4(Bounds.Z, Center.Y, Bounds.Z + buttonWidth, Bounds.W);
            if (decrement is not null) decrement.Bounds = new Vector4(Bounds.Z, Bounds.Y, Bounds.Z + buttonWidth, Center.Y);
        }

        /// <summary>
        /// Adjusts the component's bounds when editing from the right side, including the button positions.
        /// </summary>
        /// <param name="value">The new right-side X coordinate.</param>
        public override void PreEditRight(float value)
        {
            _bounds.Z = value - buttonWidth;
            if (increment is not null)
            {
                increment.PreEditLeft(value - buttonWidth);
                increment.PreEditRight(value);
            }
            if (decrement is not null)
            {
                decrement.PreEditLeft(value - buttonWidth);
                decrement.PreEditRight(value);
            }
        }

        /// <summary>
        /// Handles text input, filtering out non-numeric characters except operators.
        /// </summary>
        /// <param name="e">The text input event data.</param>
        public override void OnTextInput(TextInputEventArgs e)
        {
            char c = e.AsString[0];
            if ((c >= '0' && c <= '9') || c == '.' || c == '+' || c == '-' || c == '*' || c == '/' || c == '^' || c == '(' || c == ')')
                base.OnTextInput(e);
        }

        /// <summary>
        /// Handles key presses, evaluating numeric expressions on Enter key press.
        /// </summary>
        /// <param name="e">The keyboard key event data.</param>
        public override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Keys.Enter)
            {
                try
                {
                    if (Text.Length == 0) return;
                    var value = _solver.SimpleEvaluate(Text);
                    Text = $"{value}";
                    IsFocused = false;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Invalid expression: {exception.Message}");
                }
            }
        }

        /// <summary>
        /// Forwards click down events to the increment and decrement buttons.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickDown(MouseState mouse)
        {
            base.OnClickDown(mouse);
            increment.OnClickDown(mouse);
            decrement.OnClickDown(mouse);
        }

        /// <summary>
        /// Forwards click up events to the increment and decrement buttons.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickUp(MouseState mouse)
        {
            base.OnClickUp(mouse);
            increment.OnClickUp(mouse);
            decrement.OnClickUp(mouse);
        }

        private void Increment()
        {
            try
            {
                if (Text.Length == 0) return;
                var value = _solver.SimpleEvaluate(Text);
                Text = $"{value + 1}";
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Invalid expression: {exception.Message}");
            }
        }

        private void Decrement()
        {
            try
            {
                if (Text.Length == 0) return;
                var value = _solver.SimpleEvaluate(Text);
                Text = $"{value - 1}";
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Invalid expression: {exception.Message}");
            }
        }

        /// <summary>
        /// Updates the state of the <see cref="NumericSpinner"/> and its increment/decrement buttons based on the current mouse state.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last update, in seconds.</param>
        /// <param name="mouse">The current state of the mouse.</param>
        public override void OnUpdate(float deltaTime, MouseState mouse)
        {
            base.OnUpdate(deltaTime, mouse);
            increment.OnUpdate(deltaTime, mouse);
            decrement.OnUpdate(deltaTime, mouse);
        }

        /// <summary>
        /// Updates the state of the <see cref="NumericSpinner"/> and its components based on the current mouse and keyboard states.
        /// Calls other update overloads to ensure consistent behavior of the text field and buttons.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last update, in seconds.</param>
        /// <param name="mouse">The current state of the mouse.</param>
        /// <param name="keyboard">The current state of the keyboard.</param>
        public override void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard)
        {
            base.OnUpdate(deltaTime, mouse, keyboard);
            OnUpdate(deltaTime);
            OnUpdate(deltaTime, mouse);
            OnUpdate(deltaTime, keyboard);
        }

        /// <summary>
        /// Draws the text field and the increment/decrement buttons.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            base.Draw();
            increment.Draw();
            decrement.Draw();
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}