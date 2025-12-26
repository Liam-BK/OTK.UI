using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Interfaces;
using OTK.UI.Managers;
using OTK.UI.Utility;

namespace OTK.UI.Components
{
    /// <summary>
    /// A single-line editable text input field built on top of <see cref="NinePatch"/>.
    /// Supports caret movement, selection, copy/paste, and text change/enter events.
    /// </summary>
    /// <remarks>
    /// The text field handles keyboard input and mouse interactions, including:
    /// <list type="bullet">
    /// <item><description>Click to focus and place the caret.</description></item>
    /// <item><description>Click-drag to select text.</description></item>
    /// <item><description>Keyboard navigation: arrows, home/end, shift selection, backspace/delete.</description></item>
    /// <item><description>Clipboard operations: copy (Ctrl+C), cut (Ctrl+X), paste (Ctrl+V), select all (Ctrl+A).</description></item>
    /// <item><description>Caret blinks while focused.</description></item>
    /// </list>
    /// </remarks>
    public class TextField : NinePatch
    {
        /// <summary>
        /// The <see cref="Label"/> object displaying the current text.
        /// </summary>
        public Label text;

        /// <summary>
        /// Gets or sets whether the text field currently has focus.
        /// </summary>
        public bool IsFocused
        {
            get;
            set;
        } = false;

        private bool ClickInBounds
        {
            get;
            set;
        }

        /// <summary>
        /// The zero-based index of the caret within the text.
        /// </summary>
        public int CaretIndex = 0;

        /// <summary>
        /// The index of the left end of the current text selection.
        /// </summary>
        public int LeftSelectIndex = 0;

        /// <summary>
        /// The index of the right end of the current text selection.
        /// </summary>
        public int RightSelectIndex = 0;

        private static Stopwatch stopwatch = new();

        public bool displayCaret = false;

        private long timeOffset = 0;

        /// <summary>
        /// The highlight color used for selected text.
        /// </summary>
        /// <remarks>This is a Vector3 in the format RGB, within the range of 0 to 1.</remarks>
        public Vector3 HighLightColour
        {
            get;
            set;
        } = new Vector3(0.6f, 0.8f, 1.0f);

        public float InitialClickPosition = 0;

        public float CaretReductionMultiplier = 0.65f;

        public float textOffset = 0;

        /// <summary>
        /// The text currently displayed in the text field.
        /// Setting this property updates the <see cref="Label"/> as well.
        /// </summary>
        public string Text
        {
            get
            {
                return text.Text;
            }
            set
            {
                text.Text = value;
            }
        }

        /// <summary>
        /// Event fired whenever the text changes (including typing, pasting, or cutting).
        /// </summary>
        public event Action<string>? OnTextChanged;

        /// <summary>
        /// Event fired when the Enter key is pressed while the text field is focused.
        /// </summary>
        public event Action<string>? OnTextEnter;

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a new <see cref="TextField"/> instance with specified bounds, inset, UV inset, and optional color.
        /// </summary>
        /// <param name="bounds">The rectangular bounds of the text field as (Left, Bottom, Right, Top).</param>
        /// <param name="inset">The thickness of the inner border region for nine-patch rendering.</param>
        /// <param name="uvInset">The UV offset for the nine-patch texture coordinates. Default is 0.5.</param>
        /// <param name="colour">Optional tint color. If <c>null</c>, default color is used.</param>
        public TextField(Vector4 bounds, float inset, float uvInset = 0.5F, Vector3? colour = null) : base(bounds, inset, uvInset, colour)
        {
            float textHeight = Height * 0.5f;
            text = new Label(new Vector2(bounds.X + inset * 0.5f, Center.Y - textHeight * 0.5f), textHeight, "", Vector3.Zero);
            if (!stopwatch.IsRunning) stopwatch.Start();

            AltersMouse = true;

            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.TextField.vert", "OTK.UI.Shaders.Fragment.TextField.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Loads a <see cref="TextField"/> from an <see cref="XElement"/> containing layout and style information.
        /// </summary>
        /// <param name="element">
        /// The XML element containing configuration for the text field. Expected fields include:
        /// <list type="bullet">
        /// <item><description>Name</description></item>
        /// <item><description>Bounds</description></item>
        /// <item><description>Margin</description></item>
        /// <item><description>UVMargin</description></item>
        /// <item><description>IsVisible</description></item>
        /// <item><description>Text</description></item>
        /// <item><description>Texture</description></item>
        /// <item><description>ColorRGB</description></item>
        /// <item><description>TextColorRGB</description></item>
        /// <item><description>Anchor</description></item>
        /// </list>
        /// </param>
        /// <returns>A new <see cref="TextField"/> instance configured according to the XML.</returns>
        /// <exception cref="FormatException">
        /// Thrown if required fields (e.g., Name or Bounds) are missing or invalid.
        /// </exception>
        public static new TextField Load(Dictionary<string, IUIElement> registry, XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds") ?? throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
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

            TextField textField = new(new Vector4(left, bottom, right, top) + relativeAnchorVector, margin, uvMargin)
            {
                IsVisible = isVisible,
                Colour = colorVec
            };

            textField.text.Colour = textColorVec;
            textField.Text = text;
            textField.CaretIndex = text.Length;
            textField.LeftSelectIndex = textField.CaretIndex;
            textField.RightSelectIndex = textField.CaretIndex;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                textField.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else textField.Texture = texture;

            if (registry.ContainsKey(name)) throw new ArgumentException($"An element with name: {name} has already been registered.");
            registry.Add(name, textField);
            return textField;
        }

        /// <summary>
        /// Updates the internal label's size and origin based on the current bounds and text offset.
        /// Should be called whenever the <see cref="Bounds"/> property changes.
        /// </summary>
        public override void UpdateBounds()
        {
            base.UpdateBounds();
            if (text is not null)
            {
                text.Size = Math.Abs(_bounds.W - _bounds.Y) * 0.5f;
                text.Origin = new Vector2(Bounds.X + Inset * 0.25f + textOffset, Center.Y - text.Size * 0.5f);
            }
        }

        /// <summary>
        /// Handles a mouse button press. Places the caret and sets focus if the click is inside bounds.
        /// Also supports click-drag text selection.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickDown(MouseState mouse)
        {
            if (!IsVisible) return;
            base.OnClickDown(mouse);
            ClickInBounds = WithinBounds(ConvertMouseScreenCoords(mouse.Position));
            IsFocused = ClickInBounds;
            CaretIndex = text.FindIndexFromPos(ConvertMouseScreenCoords(mouse.Position));
            LeftSelectIndex = CaretIndex;
            RightSelectIndex = LeftSelectIndex;
            if (ClickInBounds) InitialClickPosition = ConvertMouseScreenCoords(mouse.Position).X;
            if (IsFocused)
            {
                ResetCaretBlink();
            }
            else
            {
                displayCaret = false;
            }
        }

        /// <summary>
        /// Handles mouse button release. Resets the internal click state for the text field.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickUp(MouseState mouse)
        {
            base.OnClickUp(mouse);
            ClickInBounds = false;
        }

        /// <summary>
        /// Handles mouse movement. Updates text selection when dragging and changes cursor to I-beam when hovering.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnMouseMove(MouseState mouse)
        {
            base.OnMouseMove(mouse);
            if (Window is not null)
            {
                if (WithinBounds(ConvertMouseScreenCoords(mouse.Position)))
                {
                    Window.Cursor = MouseCursor.IBeam;
                }
            }
            if (ClickInBounds)
            {
                if (ConvertMouseScreenCoords(mouse.Position).X > InitialClickPosition)
                {
                    LeftSelectIndex = text.FindIndexFromPos(new Vector2(InitialClickPosition, Center.Y));
                    RightSelectIndex = text.FindIndexFromPos(ConvertMouseScreenCoords(mouse.Position));
                    CaretIndex = RightSelectIndex;
                }
                else if (ConvertMouseScreenCoords(mouse.Position).X < InitialClickPosition)
                {
                    RightSelectIndex = text.FindIndexFromPos(new Vector2(InitialClickPosition, Center.Y));
                    LeftSelectIndex = text.FindIndexFromPos(ConvertMouseScreenCoords(mouse.Position));
                    CaretIndex = LeftSelectIndex;
                }
            }
        }

        /// <summary>
        /// Handles key presses for text editing and navigation, including arrow keys, delete/backspace,
        /// home/end, shift-selection, and clipboard operations.
        /// </summary>
        /// <param name="e">Keyboard key event arguments.</param>
        public override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (!IsVisible || !IsFocused) return;
            var sb = new StringBuilder();
            base.OnKeyDown(e);
            if (e.Key == Keys.Backspace || e.Key == Keys.Delete)
            {
                sb.Append(text.Text);
                if (LeftSelectIndex != RightSelectIndex)
                {
                    sb.Remove(LeftSelectIndex, RightSelectIndex - LeftSelectIndex);
                    CaretIndex = LeftSelectIndex;
                    RightSelectIndex = CaretIndex;
                }
                else
                {
                    if (CaretIndex == 0) return;
                    sb.Remove(CaretIndex - 1, 1);
                    CaretIndex--;
                    LeftSelectIndex = CaretIndex;
                    RightSelectIndex = LeftSelectIndex;
                }
                text.Text = sb.ToString();
                ResetCaretBlink();
                ShiftTextRight();
                OnTextChanged?.Invoke(Text);
            }
            else if (e.Shift)
            {
                if (e.Key == Keys.Left)
                {
                    if (CaretIndex > 0)
                    {
                        if (CaretIndex == LeftSelectIndex)
                        {
                            CaretIndex--;
                            LeftSelectIndex = CaretIndex;
                        }
                        else if (CaretIndex == RightSelectIndex)
                        {
                            CaretIndex--;
                            RightSelectIndex = CaretIndex;
                        }
                    }
                    ResetCaretBlink();
                    ShiftTextRight();
                }
                else if (e.Key == Keys.Right)
                {
                    if (CaretIndex < text.Text.Length)
                    {
                        if (CaretIndex == RightSelectIndex)
                        {
                            CaretIndex++;
                            RightSelectIndex = CaretIndex;
                        }
                        else if (CaretIndex == LeftSelectIndex)
                        {
                            CaretIndex++;
                            LeftSelectIndex = CaretIndex;
                        }
                    }
                    ResetCaretBlink();
                    ShiftTextLeft();
                }
                else if (e.Key == Keys.Up)
                {
                    CaretIndex = 0;
                    LeftSelectIndex = CaretIndex;
                    ResetCaretBlink();
                    text.Origin = new Vector2(Bounds.X + Inset * 0.25f, Center.Y - text.Size * 0.5f);
                    textOffset = 0;
                }
                else if (e.Key == Keys.Down)
                {
                    CaretIndex = text.Text.Length;
                    RightSelectIndex = CaretIndex;
                    ResetCaretBlink();
                    ShiftTextLeft();
                }
            }
            else if (e.Key == Keys.Left)
            {
                if (CaretIndex > 0)
                {
                    if (LeftSelectIndex != RightSelectIndex) CaretIndex = LeftSelectIndex;
                    else CaretIndex--;
                    LeftSelectIndex = CaretIndex;
                    RightSelectIndex = LeftSelectIndex;
                }
                ResetCaretBlink();
                ShiftTextRight();
            }
            else if (e.Key == Keys.Right)
            {
                if (CaretIndex < text.Text.Length)
                {
                    if (LeftSelectIndex != RightSelectIndex) CaretIndex = RightSelectIndex;
                    else CaretIndex++;
                    LeftSelectIndex = CaretIndex;
                    RightSelectIndex = LeftSelectIndex;
                }
                ResetCaretBlink();
                ShiftTextLeft();
            }
            else if (e.Key == Keys.Up)
            {
                CaretIndex = 0;
                LeftSelectIndex = CaretIndex;
                RightSelectIndex = LeftSelectIndex;
                ResetCaretBlink();
                text.Origin = new Vector2(Bounds.X + Inset * 0.25f, Center.Y - text.Size * 0.5f);
                textOffset = 0;
            }
            else if (e.Key == Keys.Down)
            {
                CaretIndex = text.Text.Length;
                LeftSelectIndex = CaretIndex;
                RightSelectIndex = LeftSelectIndex;
                ResetCaretBlink();
                ShiftTextLeft();
            }
            else if (e.Control || e.Command)
            {
                if (e.Key == Keys.C)
                {
                    CopySelection(sb);
                }
                else if (e.Key == Keys.X)
                {
                    CutSelection(sb);
                }
                else if (e.Key == Keys.V)
                {
                    PasteSelection(sb);
                }
                else if (e.Key == Keys.A)
                {
                    SelectAll();
                }
            }
            else if (e.Key == Keys.Enter || e.Key == Keys.KeyPadEnter)
            {
                OnTextEnter?.Invoke(Text);
            }
        }

        /// <summary>
        /// Handles text input events, inserting typed characters at the caret position.
        /// </summary>
        /// <param name="e">Text input event arguments.</param>
        public override void OnTextInput(TextInputEventArgs e)
        {
            if (!IsVisible || !IsFocused) return;
            if (IsFocused)
            {
                base.OnTextInput(e);
                var sb = new StringBuilder();
                sb.Append(text.Text);
                if (LeftSelectIndex != RightSelectIndex)
                {
                    sb.Remove(LeftSelectIndex, RightSelectIndex - LeftSelectIndex);
                    CaretIndex = LeftSelectIndex;
                    RightSelectIndex = CaretIndex;
                }
                sb.Insert(CaretIndex, e.AsString);
                text.Text = sb.ToString();
                CaretIndex++;
                LeftSelectIndex = CaretIndex;
                RightSelectIndex = LeftSelectIndex;
                ResetCaretBlink();
                ShiftTextLeft();
                OnTextChanged?.Invoke(Text);
            }
        }

        /// <summary>
        /// Copies the currently selected text to the clipboard.
        /// </summary>
        public void CopySelection()
        {
            var sb = new StringBuilder();
            CopySelection(sb);
        }

        /// <summary>
        /// Copies the currently selected text to the clipboard.
        /// </summary>
        public void CopySelection(StringBuilder sb)
        {
            sb.Clear();
            sb.Append(text.Text.Substring(LeftSelectIndex, RightSelectIndex - LeftSelectIndex));
            if (Window is not null) Window.ClipboardString = sb.ToString();
        }

        /// <summary>
        /// Cuts the currently selected text to the clipboard.
        /// </summary>
        public void CutSelection()
        {
            var sb = new StringBuilder();
            CutSelection(sb);
        }

        /// <summary>
        /// Cuts the currently selected text to the clipboard.
        /// </summary>
        public void CutSelection(StringBuilder sb)
        {
            sb.Clear();
            sb.Append(text.Text.Substring(LeftSelectIndex, RightSelectIndex - LeftSelectIndex));
            if (Window is not null) Window.ClipboardString = sb.ToString();
            sb.Clear();
            sb.Append(text.Text);
            if (LeftSelectIndex != RightSelectIndex)
            {
                sb.Remove(LeftSelectIndex, RightSelectIndex - LeftSelectIndex);
                CaretIndex = LeftSelectIndex;
                RightSelectIndex = CaretIndex;
            }
            text.Text = sb.ToString();
            ShiftTextRight();
        }

        /// <summary>
        /// Pastes the current clipboard contents into the text field at the caret position.
        /// </summary>
        public void PasteSelection()
        {
            var sb = new StringBuilder();
            PasteSelection(sb);
        }

        /// <summary>
        /// Pastes the current clipboard contents into the text field at the caret position.
        /// </summary>
        public void PasteSelection(StringBuilder sb)
        {
            sb.Clear();
            sb.Append(text.Text);
            if (LeftSelectIndex != RightSelectIndex)
            {
                sb.Remove(LeftSelectIndex, RightSelectIndex - LeftSelectIndex);
                CaretIndex = LeftSelectIndex;
                RightSelectIndex = CaretIndex;
            }
            if (Window is not null)
            {
                sb.Insert(CaretIndex, Window.ClipboardString);
                CaretIndex += Window.ClipboardString.Length;
                LeftSelectIndex = CaretIndex;
                RightSelectIndex = CaretIndex;
                text.Text = sb.ToString();
            }
            ShiftTextLeft();
        }

        /// <summary>
        /// Selects all text within the text field.
        /// </summary>
        public void SelectAll()
        {
            LeftSelectIndex = 0;
            RightSelectIndex = text.Text.Length;
            CaretIndex = RightSelectIndex;
            ShiftTextLeft();
        }

        private void ShiftTextLeft()
        {
            float caretPos = text.FindXPosFromIndex(CaretIndex);
            float diff = caretPos - (Bounds.Z - Inset);
            if (diff > 0) text.Origin -= Vector2.UnitX * diff;
            textOffset -= Math.Max(diff, 0);
        }

        private void ShiftTextRight()
        {
            float caretPos = text.FindXPosFromIndex(CaretIndex);
            float diff = Bounds.X + Inset * 0.25f - caretPos;
            if (diff > 0) text.Origin += Vector2.UnitX * diff;
            textOffset += Math.Max(diff, 0);
        }

        /// <summary>
        /// Updates the text field state per frame. Manages caret blinking when focused.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last update, in seconds.</param>
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            if (!IsFocused)
            {
                displayCaret = false;
                return;
            }
            else if (stopwatch.ElapsedMilliseconds + timeOffset >= 500)
            {
                timeOffset = Math.Max(stopwatch.ElapsedMilliseconds + timeOffset - 500, 0);
                stopwatch.Restart();
                displayCaret = !displayCaret;
            }
        }

        /// <summary>
        /// Updates the text field for the current frame, including general logic, mouse input, and keyboard input.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last update, in seconds.</param>
        /// <param name="mouse">Current mouse state.</param>
        /// <param name="keyboard">Current keyboard state.</param>
        public override void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard)
        {
            OnUpdate(deltaTime);
            OnUpdate(deltaTime, mouse);
            OnUpdate(deltaTime, keyboard);
        }

        private void ResetCaretBlink()
        {
            displayCaret = true;
            stopwatch.Restart();
        }

        /// <summary>
        /// Passes relevant uniform variables to the shader program, including caret position, selection bounds, visibility,
        /// margins, bounds, and highlight color. Called automatically during rendering.
        /// </summary>
        protected override void PassUniform()
        {
            base.PassUniform();
            PassUniform(text.FindXPosFromIndex(CaretIndex) * DPIScaleX, "caretPos");
            PassUniform(text.FindXPosFromIndex(LeftSelectIndex) * DPIScaleX, "leftSelect");
            PassUniform(text.FindXPosFromIndex(RightSelectIndex) * DPIScaleX, "rightSelect");
            PassUniform(displayCaret ? 1 : 0, "caretVisible");
            PassUniform(Inset * DPIScaleY * CaretReductionMultiplier, "caretMargin");
            PassUniform(Bounds * DPIScaleVec4, "bounds");
            PassUniform(HighLightColour, "highlightColour");
        }

        /// <summary>
        /// Draws the text field, including the background nine-patch and the text label.
        /// Applies clipping based on the text field's bounds.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            base.Draw();
            var clipBounds = FindMinClipBounds();
            var clipWidth = clipBounds.Z - clipBounds.X;
            var clipHeight = clipBounds.W - clipBounds.Y;
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor((int)Math.Round(clipBounds.X), (int)Math.Round(clipBounds.Y), (int)Math.Round(clipWidth), (int)Math.Round(clipHeight));
            text.Draw();
            GL.Disable(EnableCap.ScissorTest);
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}