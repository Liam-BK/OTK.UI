using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OTK.UI.Utility;
using OTK.UI.Managers;
using System.Xml.Linq;
using System.Globalization;

namespace OTK.UI.Components
{
    /// <summary>
    /// A UI text element that renders glyph-based text using a loaded font.
    /// Handles alignment, per-glyph vertex generation, kerning, bounds
    /// computation, multi-line layout, and GPU upload of vertex/index buffers.
    /// 
    /// The label's position is defined by <see cref="Origin"/> and <see cref="Size"/>,
    /// and its bounding rectangle auto-adjusts to the text content.
    /// </summary>
    public class Label : UIBase
    {
        private string _fontKey = "";

        /// <summary>
        /// The key used to look up the font inside <see cref="FontManager"/>.
        /// Changing this does not rebuild geometry automatically.
        /// </summary>
        public string FontKey
        {
            get
            {
                return _fontKey;
            }
            set
            {
                _fontKey = value;
            }
        }

        private string _text = "";

        /// <summary>
        /// The text content displayed by the label. Setting this triggers a full
        /// rebuild of glyph vertex data and updates the GPU buffers.
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                RebuildVertexData();
                UploadVertexData(vertices.ToArray(), indices.ToArray());
            }
        }

        private Vector2 _origin = Vector2.Zero;

        /// <summary>
        /// The origin point of the label. Changing this automatically recalculates
        /// layout bounds.
        /// </summary>
        public Vector2 Origin
        {
            get
            {
                return _origin;
            }
            set
            {
                _origin = value;
                _bounds = new Vector4(_origin.X, _origin.Y - Size * 0.5f, _origin.X + Size, _origin.Y + Size * 0.5f);
                UpdateBounds();
            }
        }

        private TextAlign _alignment = TextAlign.Left;

        /// <summary>
        /// Horizontal text alignment. Valid values are Left, Center, and Right.
        /// Changing this rebuilds glyph geometry.
        /// </summary>
        public TextAlign Alignment
        {
            private get
            {
                return _alignment;
            }
            set
            {
                _alignment = value;
                RebuildVertexData();
                UploadVertexData(vertices.ToArray(), indices.ToArray());
            }
        }

        public enum TextAlign
        {
            Left,
            Center,
            Right
        }

        private float _size;

        /// <summary>
        /// Horizontal text alignment. Valid values are Left, Center, and Right.
        /// Changing this rebuilds glyph geometry.
        /// </summary>
        public float Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
                _bounds = new Vector4(_origin.X, _origin.Y - _size * 0.5f, _origin.X + _size, _origin.Y + _size * 0.5f);
                UpdateBounds();
            }
        }

        /// <summary>
        /// Gets or sets the bounding box of the rendered text, adjusted after
        /// alignment and glyph layout. Setting the bounds repositions the label
        /// according to the current alignment.
        /// </summary>
        public override Vector4 Bounds
        {
            get
            {
                return CalculateBounds();
            }
            set
            {
                _bounds = value;
                if (Alignment is TextAlign.Left)
                {
                    Origin = new Vector2(_bounds.X, (_bounds.Y + _bounds.W) * 0.5f);
                }
                else if (Alignment is TextAlign.Center)
                {
                    Origin = new Vector2((_bounds.X + _bounds.Z) * 0.5f, (_bounds.Y + _bounds.W) * 0.5f);
                }
                else if (Alignment is TextAlign.Right)
                {
                    Origin = new Vector2(_bounds.Z, (_bounds.Y + _bounds.W) * 0.5f);
                }
                Size = (_bounds.W - _bounds.Y);
            }
        }

        /// <summary>
        /// The total rendered width of the current line(s) of text in pixels,
        /// calculated during geometry rebuild.
        /// </summary>
        /// <remarks>May be unstable. It is recommended to calculate the width from Bounds directly.</remarks>
        public float MeasureText
        {
            get;
            set;
        } = 0.0f;

        /// <summary>
        /// Additional spacing between characters.
        /// </summary>
        public float CharacterSpacing = 0.05f;

        /// <summary>
        /// Additional spacing between lines.
        /// </summary>
        public float LineSpacing => 0.5f;

        private static int defaultProgram = 0;

        public Label(Vector2 origin, float size, string text, Vector3? colour = null) : base(new Vector4(origin.X, origin.Y - size * 0.5f, origin.X + size, origin.Y + size * 0.5f), colour)
        {
            FontKey = FontManager.DefaultFontKey;
            Texture = FontKey;
            Size = size;
            Origin = origin;
            Text = text;
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateUIShader(ShaderManager.UI_Fragment);
            if (defaultProgram == 0) defaultProgram = program;
        }

        /// <summary>
        /// Creates a <see cref="Label"/> from a layout XML element. Supports loading
        /// origin, size, color, visibility, alignment, anchor, and text.
        /// Throws <see cref="FormatException"/> if required fields are missing.
        /// </summary>
        /// <param name="element">The XML element containing label configuration.</param>
        /// <returns>A fully initialized <see cref="Label"/> instance.</returns>
        public static Label Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var origin = element.Element("Origin");
            if (origin is null) throw new FormatException($"Label: {name} is missing required field Origin.");
            var size = MathF.Max(float.Parse(element.Element("Size")?.Value ?? "20"), 1.0f);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var alignment = element.Element("Alignment")?.Value.Trim() ?? "Left";
            var color = element.Element("ColorRGB")?.Value ?? "0, 0, 0";
            var text = element.Element("Text")?.Value ?? string.Empty;
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var x = float.Parse(origin.Element("X")?.Value ?? "0", CultureInfo.InvariantCulture);
            var y = float.Parse(origin.Element("Y")?.Value ?? "0", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            Label label = new Label(new Vector2(x, y) + relativeAnchorVector.Xy, size, text, colorVec);
            label.IsVisible = isVisible;
            switch (alignment.ToLower())
            {
                case "left":
                    label.Alignment = Label.TextAlign.Left;
                    break;
                case "center":
                    label.Alignment = Label.TextAlign.Center;
                    break;
                case "right":
                    label.Alignment = Label.TextAlign.Right;
                    break;
            }

            return label;
        }

        /// <summary>
        /// Computes the axis-aligned bounding box of the transformed glyphs.
        /// Used for hit testing and placement.
        /// </summary>
        public Vector4 CalculateBounds()
        {
            Vector4 value = new Vector4(Origin.X, Origin.Y, Origin.X, Origin.Y);
            if (Text.Length == 0) return value;

            bool hasBounds = false;
            for (int i = 0; i < Text.Length; i++)
            {
                var glyphBounds = GetTransformedGlyphBounds(i);
                if (!glyphBounds.HasValue) continue;
                Vector4 b = glyphBounds.Value;
                if (!hasBounds)
                {
                    value = b;
                    hasBounds = true;
                }
                else
                {
                    value.X = MathF.Min(value.X, b.X);
                    value.Y = MathF.Min(value.Y, b.Y);
                    value.Z = MathF.Max(value.Z, b.Z);
                    value.W = MathF.Max(value.W, b.W);
                }
            }
            return value;
        }

        /// <summary>
        /// Reconstructs glyph quads, kerning, alignment offsets, and vertex/index
        /// buffers for the current text. Also updates <see cref="MeasureText"/> and
        /// recalculates label bounds.
        /// </summary>
        private void RebuildVertexData()
        {
            vertices.Clear();
            indices.Clear();
            MeasureText = 0;

            float defaultCursorValue = -0.5f;
            float XCursor = defaultCursorValue;
            float YCursor = defaultCursorValue;

            uint indexOffset = 0;

            if (!FontManager.Fonts.TryGetValue(FontKey, out var fontData)) return;
            int iterations = 0;
            float measureText = 0.0f;
            float maxMeasureText = 0.0f;
            int stopPos = 0;
            float inverseSize = 1.0f / ((Size == 0) ? 1 : Size);
            for (int i = 0; i < Text.Length; i++)
            {
                char c = Text[i];
                if (c == '\n')
                {
                    YCursor -= 1 + LineSpacing;
                    XCursor = defaultCursorValue;
                    maxMeasureText = Math.Max(measureText, maxMeasureText);

                    //this was intended to handle alignment

                    float halfLineWidth = measureText * 0.5f;
                    float shiftValue = halfLineWidth * inverseSize;
                    if (Alignment == TextAlign.Left)
                    {
                        shiftValue *= 0;
                    }
                    else if (Alignment == TextAlign.Right)
                    {
                        shiftValue *= 2;
                        shiftValue += defaultCursorValue;
                    }

                    for (int n = i; n >= stopPos; n--)
                    {
                        var glyphVertices = GetGlyphQuad(n);
                        if (glyphVertices is not null)
                        {
                            glyphVertices[0].X -= shiftValue;
                            glyphVertices[1].X -= shiftValue;
                            glyphVertices[2].X -= shiftValue;
                            glyphVertices[3].X -= shiftValue;
                            SetGlyphQuad(n, glyphVertices);
                        }
                    }
                    stopPos = i;
                    measureText = 0;
                    continue;
                }

                if (!fontData.GlyphUVs.TryGetValue(c, out var UVs)) continue;
                if (!fontData.GlyphBounds.TryGetValue(c, out var charBounds)) continue;
                if (!fontData.Offsets.TryGetValue(c, out var offset)) continue;

                float kern = 0.0f;
                if (i < Text.Length - 1)
                {
                    var pair = (c, Text[i + 1]);
                    if (!fontData.Kerning.TryGetValue(pair, out kern))
                    {
                        kern = 0.0f;
                    }
                }

                offset /= fontData.ScaleFactor;

                iterations++;

                float glyphHeight = (float)(charBounds.W - charBounds.Y) / fontData.ScaleFactor;
                float glyphWidth = c == ' ' ? 0.5f : (UVs.Z - UVs.X) / (UVs.W - UVs.Y) * glyphHeight;

                float x0 = XCursor;
                float y0 = YCursor + 0.5f - glyphHeight - offset.Y;
                float x1 = XCursor + glyphWidth;
                float y1 = YCursor + 0.5f - offset.Y;

                vertices.AddRange([
                    x0, y1, z, UVs.X, 1.0f - UVs.Y,
                    x0, y0, z, UVs.X, 1.0f - UVs.W,
                    x1, y0, z, UVs.Z, 1.0f - UVs.W,
                    x1, y1, z, UVs.Z, 1.0f - UVs.Y
                ]);

                indices.AddRange([
                    indexOffset, indexOffset + 1, indexOffset + 2,
                    indexOffset, indexOffset + 2, indexOffset + 3
                ]);

                indexOffset += 4;
                XCursor += glyphWidth + CharacterSpacing + kern;
                measureText += (glyphWidth + CharacterSpacing + kern) * Size;
            }
            MeasureText = Math.Max(measureText, maxMeasureText);
            float halfWidth = measureText * 0.5f;
            float movement = halfWidth * inverseSize;
            float measureMultiplier = 0;
            if (Alignment == TextAlign.Left)
            {
                movement *= 0;
            }
            else if (Alignment == TextAlign.Center)
            {
                measureMultiplier = 0.5f;
            }
            else if (Alignment == TextAlign.Right)
            {
                movement *= 2;
                measureMultiplier = 1.0f;
            }

            for (int n = Text.Length - 1; n >= stopPos; n--)
            {
                var glyphVertices = GetGlyphQuad(n);
                if (glyphVertices is not null)
                {
                    glyphVertices[0].X -= movement;
                    glyphVertices[1].X -= movement;
                    glyphVertices[2].X -= movement;
                    glyphVertices[3].X -= movement;
                    SetGlyphQuad(n, glyphVertices);
                }
            }
            _bounds = new Vector4(Origin.X - MeasureText * measureMultiplier, Origin.Y + YCursor * Size, Origin.X + MeasureText - MeasureText * measureMultiplier, Origin.Y + 1 * Size);
        }

        private Vector2[]? GetGlyphQuad(int index)
        {
            int start = index * 20;
            if (start + 20 > vertices.Count || index < 0 || index > Text.Length) return null;
            return [
                new Vector2(vertices[start], vertices[start + 1]),
            new Vector2(vertices[start + 5], vertices[start + 6]),
            new Vector2(vertices[start + 10], vertices[start + 11]),
            new Vector2(vertices[start + 15], vertices[start + 16]),
        ];
        }

        /// <summary>
        /// /// Returns the screen-space bounding box of the glyph at the specified
        /// index, after all transforms are applied. Useful for hit-testing,
        /// cursor placement, and mouse interactions.
        /// </summary>
        /// <param name="index">The index of the character in the text</param>
        /// <returns>The bounds of the character at the given index, in screen coordinates.</returns>
        public Vector4? GetTransformedGlyphBounds(int index)
        {
            var glyphQuad = GetGlyphQuad(index);
            if (glyphQuad is null) return null;
            var transformed = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                var v = new Vector4(glyphQuad[i].X, glyphQuad[i].Y, z, 1.0f) * model;
                transformed[i] = new Vector2(v.X, v.Y);
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var v in transformed)
            {
                minX = MathF.Min(v.X, minX);
                minY = MathF.Min(v.Y, minY);
                maxX = MathF.Max(v.X, maxX);
                maxY = MathF.Max(v.Y, maxY);
            }

            if (Window is not null)
            {
                float shiftX = WindowDimensions.X * 0.5f;
                float shiftY = WindowDimensions.Y * 0.5f;

                minX += shiftX;
                minY += shiftY;
                maxX += shiftX;
                maxY += shiftY;
            }

            return new Vector4(minX, minY, maxX, maxY);
        }

        private void SetGlyphQuad(int index, Vector2[] newPositions)
        {
            if (newPositions is null || newPositions.Length != 4) return;
            int start = index * 20;
            if (start + 20 > vertices.Count) return;

            vertices[start] = newPositions[0].X;
            vertices[start + 1] = newPositions[0].Y;

            vertices[start + 5] = newPositions[1].X;
            vertices[start + 6] = newPositions[1].Y;

            vertices[start + 10] = newPositions[2].X;
            vertices[start + 11] = newPositions[2].Y;

            vertices[start + 15] = newPositions[3].X;
            vertices[start + 16] = newPositions[3].Y;
        }

        /// <summary>
        /// Returns the character index located at the specified position.
        /// </summary>
        /// <param name="pos">The position to evaluate.</param>
        /// <returns>The index of the character nearest to the given position.</returns>
        /// <remarks>
        /// If you're using mouse coordinates, call <see cref="UIBase.ConvertMouseScreenCoords(Vector2)"/>
        /// before passing the position.
        /// </remarks>
        public int FindIndexFromPos(Vector2 pos)
        {
            int index = 0;
            float midY = Origin.Y + Size * 0.5f;
            float minYDist = Math.Abs(pos.Y - midY);
            bool lockForLine = false;

            for (int i = 0; i < Text.Length; i++)
            {
                char c = Text[i];

                if (c == '\n')
                {
                    midY -= Size + Size * LineSpacing;
                    lockForLine = false;

                    float yDist = Math.Abs(pos.Y - midY);
                    if (yDist > minYDist)
                        break; // mouse is past this line, stop
                    else
                        minYDist = yDist;

                    index++;
                }

                var glyphBounds = GetTransformedGlyphBounds(i);
                if (glyphBounds is null) continue;

                float midX = (glyphBounds.Value.X + glyphBounds.Value.Z) * 0.5f;

                if (!lockForLine)
                {
                    index = i; // default to last glyph until we lock
                }

                if (pos.X <= midX)
                {
                    lockForLine = true; // stop updating index for this line
                }
            }

            // Post-process: check if the mouse is past the midpoint of the last glyph
            var lastGlyphBounds = GetTransformedGlyphBounds(index);
            if (lastGlyphBounds is not null)
            {
                float midX = (lastGlyphBounds.Value.X + lastGlyphBounds.Value.Z) * 0.5f;
                bool isLastChar = index == Text.Length - 1;
                bool nextIsNewLine = index + 1 < Text.Length && Text[index + 1] == '\n';

                if ((pos.X > midX) && (isLastChar || nextIsNewLine))
                {
                    index++;
                }
            }

            return index;
        }

        /// <summary>
        /// Computes the horizontal screen-space position associated with a given
        /// character insertion index.
        /// </summary>
        /// <param name="index">
        /// The character index for which to compute the x-position. This can be
        /// anywhere from 0 to <see cref="Text.Length"/>.
        /// </param>
        /// <returns>
        /// The x-coordinate that represents the caret position for the specified index.
        /// For indices inside the text, this is the midpoint between adjacent glyphs.
        /// For indices at the start or end of the text, the method returns the leading
        /// or trailing glyph boundary.
        /// </returns>
        /// <remarks>
        /// This is primarily used for text-editing behaviors such as caret placement.
        /// The returned coordinate is already transformed into screen space.
        /// </remarks>
        public float FindXPosFromIndex(int index)
        {
            Vector4 currentGlyphBounds = new();
            var temp = GetTransformedGlyphBounds(index);
            if (temp.HasValue) currentGlyphBounds = temp.Value;
            if (index == 0)
            {
                if (Text.Length > 0) return currentGlyphBounds.X;
                if (Text.Length == 0) return Origin.X;
            }
            temp = GetTransformedGlyphBounds(index - 1);
            Vector4 prevGlyphBounds = new();
            if (temp.HasValue) prevGlyphBounds = temp.Value;
            if (index >= Text.Length)
            {
                if (temp.HasValue) return prevGlyphBounds.Z;
            }
            return (prevGlyphBounds.Z + currentGlyphBounds.X) * 0.5f;
        }

        /// <summary>
        /// Draws the Label element on the screen if IsVisible is true.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.UseProgram(program);
            PassUniform();
            GL.BindVertexArray(vao);
            if (Parent is not null)
            {
                var clipBounds = FindMinClipBounds();
                var clipWidth = clipBounds.Z - clipBounds.X;
                var clipHeight = clipBounds.W - clipBounds.Y;
                GL.Enable(EnableCap.ScissorTest);
                GL.Scissor((int)Math.Round(clipBounds.X), (int)Math.Round(clipBounds.Y), (int)Math.Round(clipWidth), (int)Math.Round(clipHeight));
            }
            GL.DrawElements(BeginMode.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.Disable(EnableCap.ScissorTest);
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }
}