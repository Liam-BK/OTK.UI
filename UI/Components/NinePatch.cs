using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OTK.UI.Utility;
using OTK.UI.Managers;
using System.Xml.Linq;
using System.Globalization;

namespace OTK.UI.Components
{
    /// <summary>
    /// A resizable UI element that renders a 3×3 “nine-patch” texture.
    /// The element preserves the corners while stretching the edges and
    /// center region to fit the assigned bounds. This is useful for scalable
    /// panels, windows, and backgrounds.
    /// 
    /// The inset controls how much of the texture is treated as fixed-size
    /// corners vs stretchable edges. UV inset defines how the nine-patch
    /// logic maps onto the texture coordinates.
    /// </summary>
    public class NinePatch : UIBase
    {
        private float _inset;

        /// <summary>
        /// The inset defining the inset region for the nine-patch, in display units.
        /// Corners remain unscaled outside this inset, while the center and
        /// edges stretch when the element is resized.
        /// </summary>
        public float Inset
        {
            get
            {
                return _inset;
            }
            set
            {
                _inset = value;
            }
        }

        /// <summary>
        /// The normalized UV inset used to define how far the nine-patch splits
        /// extend into the texture. Typically a value between 0 and 0.5.
        /// </summary>
        public float uvInset;

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a new nine-patch element with fixed bounds, margin settings,
        /// and optional tint color.
        /// </summary>
        /// <param name="bounds">The rectangle in screen space where the element is drawn.</param>
        /// <param name="inset">
        /// The pixel inset defining how much of the interior can stretch.
        /// Also used to determine corner sizes.
        /// </param>
        /// <param name="uvInset">
        /// UV-space inset for texture splitting. Default is 0.5.
        /// </param>
        /// <param name="colour">Optional color tint applied to the texture.</param>
        public NinePatch(Vector4 bounds, float inset, float uvInset = 0.5f, Vector3? colour = null) : base(bounds, colour)
        {
            Inset = inset;
            this.uvInset = uvInset;

            vertices = [
                //outer 
                -0.5f, -0.5f, z, 0, 0, 0,
                //inner
                -0.5f, -0.5f, z, 0, 0, 1,
                //bottom left
                -0.5f, -0.5f, z, 0, 0, 2,
                //left bottom
                -0.5f, -0.5f, z, 0, 0, 3,

                //outer
                0.5f, -0.5f, z, 1, 0, 0,
                //inner
                0.5f, -0.5f, z, 1, 0, 4,
                //bottom right
                0.5f, -0.5f, z, 1, 0, 5,
                //right bottom
                0.5f, -0.5f, z, 1, 0, 6,

                //outer
                -0.5f, 0.5f, z, 0, 1, 0,
                //inner
                -0.5f, 0.5f, z, 0, 1, 7,
                //top left
                -0.5f, 0.5f, z, 0, 1, 8,
                //left top
                -0.5f, 0.5f, z, 0, 1, 9,

                //outer
                0.5f, 0.5f, z, 1, 1, 0,
                //inner
                0.5f, 0.5f, z, 1, 1, 10,
                //top right
                0.5f, 0.5f, z, 1, 1, 11,
                //right top
                0.5f, 0.5f, z, 1, 1, 12,
            ];

            indices = [
                //corners
                0, 2, 1,
                1, 3, 0,

                4, 5, 6,
                4, 7, 5,

                8, 11, 9,
                9, 10, 8,

                12, 14, 13,
                13, 15, 12,

            //edges

                1, 2, 6,
                6, 5, 1,

                5, 7, 15,
                15, 13, 5,

                13, 14, 10,
                10, 9, 13,

                9, 11, 3,
                3, 1, 9,

            //center

                1, 5, 13,
                13, 9, 1

            ];

            UploadVertexData(vertices.ToArray(), indices.ToArray());
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        /// <summary>
        /// Creates and initializes a <see cref="NinePatch"/> from an XML layout element.
        /// Supports bounds, margin, UV margin, visibility, texture assignment, color,
        /// and anchor-based relative positioning.
        /// </summary>
        /// <param name="element">The XML element describing the nine-patch.</param>
        /// <param name="register">
        /// Optional flag indicating whether the element should be registered by the layout system.
        /// </param>
        /// <returns>A fully initialized <see cref="NinePatch"/> instance.</returns>
        /// <exception cref="FormatException">
        /// Thrown if required fields (such as <c>Name</c> or <c>Bounds</c>) are missing.
        /// </exception>
        public static NinePatch Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            NinePatch ninePatch = new NinePatch(new Vector4(left, bottom, right, top) + relativeAnchorVector, margin, uvMargin);
            ninePatch.IsVisible = isVisible;
            ninePatch.Colour = colorVec;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                ninePatch.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else ninePatch.Texture = texture;

            return ninePatch;
        }

        /// <summary>
        /// Uploads the nine-patch vertex and index buffers to the GPU and
        /// configures vertex attribute layouts for position, UV, and region index.
        /// </summary>
        /// <remarks>
        /// Region index is used by the shader to determine which of the nine
        /// segments each vertex belongs to.
        /// </remarks>
        protected override void UploadVertexData(float[] vertices, uint[] indices)
        {
            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

            int stride = 6 * sizeof(float);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Sends nine-patch-specific uniform values (margin and UV margin)
        /// to the shader in addition to the standard UIBase uniforms.
        /// </summary>
        protected override void PassUniform()
        {
            base.PassUniform();
            PassUniform(Math.Clamp(Inset, 0, Math.Min(Width, Height) * 0.5f), "margin");
            PassUniform(Math.Clamp(uvInset, 0, 0.5f), "uvMargin");
        }

        /// <summary>
        /// Renders the nine-patch using scissoring inherited from parent UI elements.
        /// Applies the configured shader, passes uniform data, binds buffers,
        /// and draws all nine segments as triangles.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            GL.Enable(EnableCap.ScissorTest);
            var clipBounds = FindMinClipBounds();
            var clipWidth = clipBounds.Z - clipBounds.X;
            var clipHeight = clipBounds.W - clipBounds.Y;
            GL.Scissor((int)Math.Round(clipBounds.X), (int)Math.Round(clipBounds.Y), (int)Math.Round(clipWidth), (int)Math.Round(clipHeight));
            GL.UseProgram(program);
            PassUniform();
            GL.BindVertexArray(vao);
            GL.DrawElements(BeginMode.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.Disable(EnableCap.ScissorTest);
        }
    }
}