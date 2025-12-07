using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Containers;
using OTK.UI.Interfaces;
using OTK.UI.Managers;

namespace OTK.UI.Utility
{
    /// <summary>
    /// Base class for all UI elements.
    /// </summary>
    /// <remarks>
    /// Handles:
    /// - Bounds and transforms
    /// - DPI scaling
    /// - GPU buffers and shaders
    /// - Parent clipping
    /// - Input events (mouse, keyboard, text)
    ///
    /// All UI components inherit from this class.
    /// </remarks>
    public class UIBase : IUIElement
    {
        private bool _isVisible = true;

        private bool WindowClosing => Window is not null ? Window.IsExiting : false;

        /// <summary>
        /// Gets or sets whether the element is visible.  
        /// Visibility is also influenced by parent clipping regions.
        /// </summary>
        public virtual bool IsVisible
        {
            get
            {
                return _isVisible && VisibleInParent() && !WindowClosing;
            }
            set
            {
                _isVisible = value;
            }
        }

        /// <summary>
        /// Determines whether this element changes the mouse cursor appearance.
        /// </summary>
        public bool AltersMouse
        {
            get;
            protected set;
        } = false;

        private static GameWindow? _window = null;

        /// <summary>
        /// The <see cref="GameWindow"/> associated with the UI system.
        /// Setting this property updates the orthographic <see cref="projection"/> matrix
        /// according to the window size and current DPI scale.
        /// </summary>
        public static GameWindow? Window
        {
            get
            {
                return _window;
            }
            set
            {
                _window = value;
                projection = Window is not null ? Matrix4.CreateOrthographic(WindowDimensions.X * InvDPIScaleX, WindowDimensions.Y * InvDPIScaleY, 0.01f, 1.0f) : Matrix4.Identity;
            }
        }

        /// <summary>
        /// The parent container of this UI element, if any.
        /// Used for hierarchical layout and visibility calculations.
        /// </summary>
        public virtual IUIContainer? Parent
        {
            get;
            set;
        } = null;

        /// <summary>
        /// The colour tint of the element. Follows RGB format ranging from 0-1. 
        /// </summary>
        public Vector3 Colour
        {
            get;
            set;
        }

        /// <summary>
        /// Represents the unit type used for UI element measurements.
        /// </summary>
        public enum DisplayUnitType
        {
            /// <summary>
            /// Measurements are in absolute pixels.
            /// </summary>
            Pixels,
            /// <summary>
            /// Measurements are scaled according to the display DPI.
            /// </summary>
            DPI
        }

        private static DisplayUnitType _displayUnits = DisplayUnitType.DPI;

        /// <summary>
        /// The current unit type used for measuring UI elements.
        /// Changing this will update the orthographic projection accordingly.
        /// </summary>
        public static DisplayUnitType DisplayUnits
        {
            get
            {
                return _displayUnits;
            }
            set
            {
                _displayUnits = value;
                projection = Window is not null ? Matrix4.CreateOrthographic(WindowDimensions.X * InvDPIScaleX, WindowDimensions.Y * InvDPIScaleY, 0.01f, 1.0f) : Matrix4.Identity;
            }
        }

        /// <summary>
        /// The current orthographic projection matrix for the UI, updated whenever
        /// the window size or display units change.
        /// </summary>
        public static Matrix4 projection = Matrix4.Identity;

        /// <summary>
        /// The model matrix that defines the transformation of the element
        /// </summary>
        public Matrix4 model = Matrix4.Identity;

        /// <summary>
        /// The width of the bounding box around the element.
        /// </summary>
        public float Width => Math.Max(_bounds.Z - _bounds.X, 0);

        /// <summary>
        /// The height of the bounding box around the element.
        /// </summary>
        public float Height => Math.Max(_bounds.W - _bounds.Y, 0);

        /// <summary>
        /// Determines whether or not a texture will be used in the current shader.
        /// </summary>
        public bool UseTexture => !string.IsNullOrEmpty(Texture);

        /// <summary>
        /// Helper that converts from bool to int for easier use in the current shader.
        /// </summary>
        protected int UseTextureInt => UseTexture ? 1 : 0;

        /// <summary>
        /// The name of the texture to be used when drawing the element.
        /// </summary>
        public string Texture { get; set; } = "";

        /// <summary>
        /// The Vector4 representing the bounding box around the element.
        /// </summary>
        protected Vector4 _bounds;

        /// <summary>
        /// Gets or sets the element’s axis-aligned rectangle in UI coordinates.
        /// 
        /// Bounds are stored as a <see cref="Vector4"/> formatted as:
        /// X = left,  
        /// Y = bottom,  
        /// Z = right,  
        /// W = top,
        ///
        /// Changing the bounds automatically updates the element’s model matrix.
        /// </summary>
        public virtual Vector4 Bounds
        {
            get
            {
                return _bounds;
            }
            set
            {
                _bounds = value;
                UpdateBounds();
            }
        }

        public Vector2 Center
        {
            get
            {
                return new Vector2((Bounds.X + Bounds.Z) * 0.5f, (Bounds.Y + Bounds.W) * 0.5f);
            }
            set
            {
                float halfWidth = Width * 0.5f;
                float halfHeight = Height * 0.5f;
                Bounds = new Vector4(value.X - halfWidth, value.Y - halfHeight, value.X + halfWidth, value.Y + halfHeight);
            }
        }

        /// <summary>
        /// A constant value for the z positioning of the element.
        /// </summary>
        protected const float z = -1;

        /// <summary>
        /// The vertex data used for the quad. Stored as a List of floats
        /// </summary>
        protected List<float> vertices = [
            -0.5f, 0.5f, z, 0, 1,
            -0.5f, -0.5f, z, 0, 0,
            0.5f, -0.5f, z, 1, 0,
            0.5f, 0.5f, z, 1, 1
        ];

        public List<float> Vertices
        {
            get
            {
                return vertices;
            }
        }

        protected List<uint> indices = [
            0, 1, 2,
            0, 2, 3
        ];

        public List<uint> Indices
        {
            get
            {
                return indices;
            }
        }

        private static float referenceDPI = 96.0f;

        /// <summary>
        /// The DPI scale value for the X axis.
        /// </summary>
        public static float DPIScaleX
        {
            get
            {
                return Window is not null && DisplayUnits == DisplayUnitType.DPI ? (Window.CurrentMonitor.HorizontalDpi / referenceDPI) : 1;
            }
        }

        /// <summary>
        /// The DPI scale value for the X axis.
        /// </summary>
        public static float DPIScaleY
        {
            get => Window is not null && DisplayUnits == DisplayUnitType.DPI ? (Window.CurrentMonitor.VerticalDpi / referenceDPI) : 1;
        }

        /// <summary>
        /// The inverse DPI scale value for the X axis.
        /// </summary>
        public static float InvDPIScaleX
        {
            get
            {
                return 1.0f / DPIScaleX;
            }
        }

        /// <summary>
        /// The inverse DPI scale value for the Y axis.
        /// </summary>
        public static float InvDPIScaleY
        {
            get => 1.0f / DPIScaleY;
        }

        /// <summary>
        /// A Vector2 that can be multiplied with a position to multiply it by the DPI scale.
        /// </summary>
        public static Vector2 DPIScaleVec2
        {
            get => new Vector2(DPIScaleX, DPIScaleY);
        }

        /// <summary>
        /// A Vector2 that can be multiplied with a position to multiply it by the inverse DPI scale.
        /// </summary>
        public static Vector2 InvDPIScaleVec2
        {
            get => new Vector2(InvDPIScaleX, InvDPIScaleY);
        }

        /// <summary>
        /// A Vector4 that can be multiplied with Bounds to multiply it by the DPI scale.
        /// </summary>
        public static Vector4 DPIScaleVec4
        {
            get => new Vector4(DPIScaleX, DPIScaleY, DPIScaleX, DPIScaleY);
        }

        /// <summary>
        /// A Vector4 that can be multiplied with Bounds to multiply it by the inverse DPI scale.
        /// </summary>
        public static Vector4 InvDPIScaleVec4
        {
            get => new Vector4(InvDPIScaleX, InvDPIScaleY, InvDPIScaleX, InvDPIScaleY);
        }

        public static Vector2 WindowDimensions
        {
            get
            {
                return new Vector2(Window is not null ? Window.Size.X : 720, Window is not null ? Window.Size.Y : 1280) * InvDPIScaleVec2;
            }
        }

        private static int defaultProgram = 0;

        /// <summary>
        /// The handle of the current shader program. Can be changed by overriding the class and calling ShaderManager.CreateShader.
        /// </summary>
        protected int program;
        protected int vao;
        protected int vbo;
        protected int ebo;

        /// <summary>
        /// Creates a new UI element with the given bounds and colour.
        /// 
        /// If the bounds describe an empty rectangle (left == right or bottom == top),
        /// a default size of 10×10 units is used.
        /// </summary>
        /// <param name="bounds">UI rectangle formatted as (left, bottom, right, top).</param>
        /// <param name="colour">Base colour used when rendering the element.</param>
        public UIBase(Vector4 bounds, Vector3? colour = null)
        {
            bounds = new Vector4(bounds.X, bounds.Y, bounds.Z, bounds.W);
            Bounds = (bounds.X == bounds.Z && bounds.Y == bounds.W) ? new Vector4(0, 0, 10, 10) : bounds;
            Colour = colour ?? Vector3.One;

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            UploadVertexData(vertices.ToArray(), indices.ToArray());
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateUIShader(ShaderManager.UI_Fragment);
            if (defaultProgram == 0) defaultProgram = program;
        }

        /// <summary>
        /// Called to directly edit the Bounds vector without changing the underlying model matrix. 
        /// </summary>
        /// <param name="value">The value that Bounds.X will be set to</param>
        public virtual void PreEditLeft(float value)
        {
            _bounds.X = value;
        }

        /// <summary>
        /// Called to directly edit the Bounds vector without changing the underlying model matrix. 
        /// </summary>
        /// <param name="value">The value that Bounds.Y will be set to</param>
        public virtual void PreEditBottom(float value)
        {
            _bounds.Y = value;
        }

        /// <summary>
        /// Called to directly edit the Bounds vector without changing the underlying model matrix. 
        /// </summary>
        /// <param name="value">The value that Bounds.Z will be set to</param>
        public virtual void PreEditRight(float value)
        {
            _bounds.Z = value;
        }

        /// <summary>
        /// Called to directly edit the Bounds vector without changing the underlying model matrix. 
        /// </summary>
        /// <param name="value">The value that Bounds.W will be set to</param>
        public virtual void PreEditTop(float value)
        {
            _bounds.W = value;
        }

        /// <summary>
        /// Called to force the underlying model matrix to match up with the current bounds.
        /// </summary>
        public virtual void UpdateBounds()
        {
            Vector2 center = new Vector2((_bounds.X + _bounds.Z) * 0.5f, (_bounds.Y + _bounds.W) * 0.5f);
            model = Matrix4.CreateScale(Width, Height, 1) * (Window is not null ? Matrix4.CreateTranslation(center.X - Window.Size.X * InvDPIScaleX * 0.5f, center.Y - Window.Size.Y * InvDPIScaleY * 0.5f, 0) : Matrix4.Identity);
        }

        public static void OnResize()
        {
            projection = Window is not null ? Matrix4.CreateOrthographic(Window.Size.X * InvDPIScaleX, Window.Size.Y * InvDPIScaleY, 0.01f, 1.0f) : Matrix4.Identity;
        }

        /// <summary>
        /// Called when the mouse button is pressed.
        /// Override to implement click-down behaviour.
        /// </summary>
        public virtual void OnClickDown(MouseState mouse) { }

        /// <summary>
        /// Called when the mouse button is pressed.
        /// Override to implement click-up behaviour.
        /// </summary>
        public virtual void OnClickUp(MouseState mouse) { }

        /// <summary>
        /// Called when the mouse button is pressed.
        /// Override to implement async click-down behaviour.
        /// </summary>
        public virtual async Task OnClickDownAsync(MouseState mouse)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when the mouse button is pressed.
        /// Override to implement async click-up behaviour.
        /// </summary>
        public virtual async Task OnClickUpAsync(MouseState mouse)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when a key is pressed.
        /// Override to implement key press down behaviour.
        /// </summary>
        public virtual void OnKeyDown(KeyboardKeyEventArgs e) { }

        /// <summary>
        /// Called when a key is released.
        /// Override to implement key released behaviour.
        /// </summary>
        public virtual void OnKeyUp(KeyboardKeyEventArgs e) { }

        /// <summary>
        /// Called when a key is pressed.
        /// Override to implement text input behaviour.
        /// </summary>
        public virtual void OnTextInput(TextInputEventArgs e) { }

        /// <summary>
        /// Called when the mouse is scrolled.
        /// Override to implement mouse scroll behaviour.
        /// </summary>
        public virtual void OnMouseWheel(MouseState mouse) { }

        /// <summary>
        /// Called when the mouse is moved.
        /// Override to implement mouse movement behaviour.
        /// </summary>
        public virtual void OnMouseMove(MouseState mouse) { }

        /// <summary>
        /// Should be called at the start of a GamePanel's OnMouseMove method to reset the mouse cursor to its default appearance.
        /// </summary>
        public static void ResetCursorDisplay()
        {
            if (Window is not null) Window.Cursor = MouseCursor.Default;
        }

        /// <summary>
        /// Called once per frame to update the element's state.
        /// Override this method to implement per-frame logic that does not require input data.
        /// </summary>
        /// <param name="deltaTime">The elapsed time in seconds since the last frame.</param>
        public virtual void OnUpdate(float deltaTime)
        {

        }

        /// <summary>
        /// Called once per frame to update the element's state with keyboard input.
        /// Override this method to implement per-frame logic that depends on the keyboard state.
        /// </summary>
        /// <param name="deltaTime">The elapsed time in seconds since the last frame.</param>
        /// <param name="keyboard">The current state of the keyboard.</param>
        public virtual void OnUpdate(float deltaTime, KeyboardState keyboard)
        {

        }

        /// <summary>
        /// Called once per frame to update the element's state with mouse input.
        /// Override this method to implement per-frame logic that depends on the mouse state.
        /// </summary>
        /// <param name="deltaTime">The elapsed time in seconds since the last frame.</param>
        /// <param name="mouse">The current state of the mouse.</param>
        public virtual void OnUpdate(float deltaTime, MouseState mouse)
        {

        }

        /// <summary>
        /// Called once per frame to update the element's state with both mouse and keyboard input.
        /// Override this method to implement per-frame logic that depends on both mouse and keyboard state.
        /// </summary>
        /// <param name="deltaTime">The elapsed time in seconds since the last frame.</param>
        /// <param name="mouse">The current state of the mouse.</param>
        /// <param name="keyboard">The current state of the keyboard.</param>
        public virtual void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard)
        {
        }

        /// <summary>
        /// Uploads vertex and index data to the element’s VAO, VBO, and EBO.
        /// </summary>
        /// <param name="vertices">
        /// The packed vertex array. Each vertex must contain 3 floats of position
        /// followed by 2 floats of texture coordinates (stride = 5 floats).
        /// </param>
        /// <param name="indices">
        /// The index buffer defining the draw order for the vertices.
        /// </param>
        /// <remarks>
        /// This method binds the existing VAO/VBO/EBO for the element, uploads the
        /// provided data using <c>DynamicDraw</c>, and sets up attribute locations 0
        /// (vec3 position) and 1 (vec2 UV).
        /// </remarks>
        protected virtual void UploadVertexData(float[] vertices, uint[] indices)
        {
            // vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            // vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            // ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

            int stride = 5 * sizeof(float);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Converts a mouse position from window-screen coordinates into the UI layout's coordinate space.
        /// </summary>
        /// <param name="position">
        /// The raw mouse position as provided by the windowing system (origin at the top-left).
        /// </param>
        /// <returns>
        /// The mouse position transformed into the UI coordinate system (origin at the bottom-left),
        /// with DPI scaling applied.
        /// </returns>
        public static Vector2 ConvertMouseScreenCoords(Vector2 position)
        {
            return new Vector2(position.X, Window is not null ? (Window.Size.Y - position.Y) : position.Y) * InvDPIScaleVec2;
        }

        /// <summary>
        /// Determines whether the specified position lies inside this element's bounding rectangle.
        /// </summary>
        /// <param name="position">The position to test, in this element’s local coordinate space.</param>
        /// <returns><c>true</c> if the position is inside the bounds; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If you're checking a mouse position, be sure to convert it first using <see cref="ConvertMouseScreenCoords"/>,
        /// otherwise the test will be inaccurate.
        /// </remarks>
        public bool WithinBounds(Vector2 position)
        {
            return position.X > Bounds.X && position.X <= Bounds.Z && position.Y > Bounds.Y && position.Y <= Bounds.W;
        }

        private bool VisibleInParent()
        {
            if (Parent is not null) return !(Bounds.X > Parent.Bounds.Z || Bounds.Z < Parent.Bounds.X || Bounds.Y > Parent.Bounds.W || Bounds.W < Parent.Bounds.Y);
            return true;
        }

        /// <summary>
        /// Uploads a float uniform to the active shader program.
        /// </summary>
        /// <param name="value">The float value to upload.</param>
        /// <param name="variable">The name of the uniform variable in the shader.</param>
        protected void PassUniform(float value, string variable)
        {
            GL.Uniform1(GL.GetUniformLocation(program, variable), value);
        }

        /// <summary>
        /// Uploads an integer uniform to the active shader program.
        /// </summary>
        /// <param name="value">The integer value to upload.</param>
        /// <param name="variable">The name of the uniform variable in the shader.</param>
        protected void PassUniform(int value, string variable)
        {
            GL.Uniform1(GL.GetUniformLocation(program, variable), value);
        }

        /// <summary>
        /// Uploads a <see cref="Vector2"/> uniform to the active shader program.
        /// </summary>
        /// <param name="value">The <see cref="Vector2"/> value to upload.</param>
        /// <param name="variable">The name of the uniform variable in the shader.</param>
        protected void PassUniform(Vector2 value, string variable)
        {
            GL.Uniform2(GL.GetUniformLocation(program, variable), value);
        }

        /// <summary>
        /// Uploads a <see cref="Vector3"/> uniform to the active shader program.
        /// </summary>
        /// <param name="value">The <see cref="Vector3"/> value to upload.</param>
        /// <param name="variable">The name of the uniform variable in the shader.</param>
        protected void PassUniform(Vector3 value, string variable)
        {
            GL.Uniform3(GL.GetUniformLocation(program, variable), value);
        }

        /// <summary>
        /// Uploads a <see cref="Vector4"/> uniform to the active shader program.
        /// </summary>
        /// <param name="value">The <see cref="Vector4"/> value to upload.</param>
        /// <param name="variable">The name of the uniform variable in the shader.</param>
        protected void PassUniform(Vector4 value, string variable)
        {
            GL.Uniform4(GL.GetUniformLocation(program, variable), value);
        }

        /// <summary>
        /// Uploads a <see cref="Matrix4"/> uniform to the active shader program.
        /// </summary>
        /// <param name="value">The matrix to upload.</param>
        /// <param name="transpose">Whether the matrix should be transposed before upload.</param>
        /// <param name="variable">The name of the uniform variable in the shader.</param>
        protected void PassUniform(Matrix4 value, bool transpose, string variable)
        {
            GL.UniformMatrix4(GL.GetUniformLocation(program, variable), transpose, ref value);
        }

        /// <summary>
        /// Passes an array of Matrix4s to the current shader program.
        /// </summary>
        /// <param name="values">The array of Matrix4s to upload to the current shader.</param>
        /// <param name="transpose">If true, transpose the matrices before uploading.</param>
        /// <param name="variable">The name of the uniform variable in the shader.</param>
        protected void PassUniform(Matrix4[] values, bool transpose, string variable)
        {
            float[] floats = new float[16 * values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                floats[i * 16] = values[i].M11;
                floats[i * 16 + 1] = values[i].M21;
                floats[i * 16 + 2] = values[i].M31;
                floats[i * 16 + 3] = values[i].M41;
                floats[i * 16 + 4] = values[i].M12;
                floats[i * 16 + 5] = values[i].M22;
                floats[i * 16 + 6] = values[i].M32;
                floats[i * 16 + 7] = values[i].M42;
                floats[i * 16 + 8] = values[i].M13;
                floats[i * 16 + 9] = values[i].M23;
                floats[i * 16 + 10] = values[i].M33;
                floats[i * 16 + 11] = values[i].M43;
                floats[i * 16 + 12] = values[i].M14;
                floats[i * 16 + 13] = values[i].M24;
                floats[i * 16 + 14] = values[i].M34;
                floats[i * 16 + 15] = values[i].M44;
            }
            GL.UniformMatrix4(GL.GetUniformLocation(program, variable), values.Length, transpose, floats);
        }

        /// <summary>
        /// Passes UseTextureInt, Texture, Colour, projection and model to the current shader program.
        /// </summary>
        protected virtual void PassUniform()
        {

            PassUniform(UseTextureInt, "useTexture");
            if (UseTexture)
            {
                TextureManager.Bind(Texture, 0);
                PassUniform(0, "sampler");
            }
            PassUniform(Colour, "colour");
            bool transpose = false;
            PassUniform(projection, transpose, "projection");
            PassUniform(model, transpose, "model");
        }

        /// <summary>
        /// Calculates the smallest valid clipping rectangle by walking up the parent chain.
        /// 
        /// Each ancestor may shrink the clip region based on its own bounds, margins,
        /// title bars, or layout rules. The returned rectangle is in DPI-scaled coordinates
        /// and can be passed directly to <c>GL.Scissor</c>.
        /// </summary>
        public virtual Vector4 FindMinClipBounds()
        {
            Vector4 currentClipBounds = Bounds * DPIScaleVec4;
            IUIContainer? currentParent = Parent;

            while (currentParent is not null)
            {
                float titleMargin = 0;
                float margin = 0;
                if (currentParent is Panel panel)
                {
                    titleMargin = panel.TitleMargin * DPIScaleY;
                    margin = panel.ContentMargin * 0.5f * DPIScaleX;
                }
                else if (currentParent is TabbedPanel tabbedPanel)
                {
                    titleMargin = tabbedPanel.TabHeight;
                    margin = tabbedPanel.ContentMargin * 0.5f;
                }
                if (currentParent is IUIElement element)
                {
                    currentClipBounds.X = Math.Max(currentClipBounds.X, element.Bounds.X * DPIScaleX + margin);
                    currentClipBounds.Y = Math.Max(currentClipBounds.Y, element.Bounds.Y * DPIScaleY + margin);
                    currentClipBounds.Z = Math.Min(currentClipBounds.Z, element.Bounds.Z * DPIScaleX - margin);
                    currentClipBounds.W = Math.Min(currentClipBounds.W, element.Bounds.W * DPIScaleY - margin - titleMargin);
                    currentParent = element.Parent;
                }
            }
            return currentClipBounds;
        }

        /// <summary>
        /// Deletes all data associated with this object from the GPU.
        /// </summary>
        public virtual void DeleteFromVRam()
        {
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
        }

        /// <summary>
        /// Draws the element using the current shader, vertex data, and clip region.
        /// Respects visibility, DPI scaling, and parent clipping.
        /// </summary>
        public virtual void Draw()
        {
            if (!IsVisible || WindowClosing) return;
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