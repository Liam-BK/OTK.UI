using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Managers;

namespace OTK.UI.Utility
{
    /// <summary>
    /// A circular color-picker UI element that allows the user to select a color
    /// by clicking within its radius. The selected color is derived from the
    /// wheel’s polar coordinates and mapped to RGB via <see cref="ColorUtils"/>.
    /// </summary>
    public class ColorWheel : UIBase
    {
        private float radius;

        private static int defaultProgram = 0;

        /// <summary>
        /// Fired when the user clicks inside the wheel’s radius using either
        /// the left or right mouse button. Provides the pressed button and the
        /// converted mouse position in screen coordinates.
        /// </summary>
        public event Action<MouseButton, Vector2>? Pressed;

        /// <summary>
        /// Creates a new <see cref="ColorWheel"/> using the given bounds.
        /// Automatically compiles or reuses the shared color-wheel shader program.
        /// </summary>
        /// <param name="bounds">The wheel’s bounding rectangle.</param>
        /// <param name="colour">Optional tint applied to the wheel.</param>
        public ColorWheel(Vector4 bounds, Vector3? colour = null) : base(bounds, colour)
        {
            radius = Width * 0.5f;
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.ColorWheel.vert", "OTK.UI.Shaders.Fragment.ColorWheel.frag");
            if (defaultProgram == 0) defaultProgram = program;
        }

        /// <summary>
        /// Handles mouse-down events. If the click occurs within the wheel’s radius,
        /// determines which mouse button was used and fires <see cref="Pressed"/>.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickDown(MouseState mouse)
        {
            base.OnClickDown(mouse);
            if (!((ConvertMouseScreenCoords(mouse.Position) - Center).LengthSquared <= radius * radius)) return;

            MouseButton pressedButton;

            if (mouse.IsButtonDown(MouseButton.Left)) pressedButton = MouseButton.Left;
            else if (mouse.IsButtonDown(MouseButton.Right)) pressedButton = MouseButton.Right;
            else return;

            Pressed?.Invoke(pressedButton, ConvertMouseScreenCoords(mouse.Position));
        }

        /// <summary>
        /// Computes an RGB color corresponding to a given position inside the wheel.
        /// The coordinate is converted to polar form and then mapped to HSV space.
        /// </summary>
        /// <param name="position">The point from which to derive the color.</param>
        /// <returns>The resulting RGB color.</returns>
        public Vector3 GetColour(Vector2 position)
        {
            return ColorUtils.ColorFromPolar(ColorUtils.CartesianToPolar(position - Center, radius));
        }

        /// <summary>
        /// Passes projection, model, and DPI-scaled bounds uniforms to the shader.
        /// </summary>
        protected override void PassUniform()
        {
            bool transpose = false;
            PassUniform(projection, transpose, "projection");
            PassUniform(model, transpose, "model");
            PassUniform(Bounds * DPIScaleVec4, "bounds");
        }

        /// <summary>
        /// Draws the base image and any inherited visuals. The color wheel itself
        /// is rendered entirely via the fragment shader.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            base.Draw();
        }
    }
}

namespace OTK.UI.Utility
{
    /// <summary>
    /// Helper functions for converting between color spaces and computing
    /// color values used by <see cref="ColorWheel"/>. Includes utilities for
    /// HSV-to-RGB conversion, positive modulus, and polar/cartesian transforms.
    /// </summary>
    public static class ColorUtils
    {
        private static float PosMod(float x, float m)
        {
            return (x % m + m) % m;
        }

        private static Vector3 Mod(Vector3 vec, float m)
        {
            return new Vector3(PosMod(vec.X, m), PosMod(vec.Y, m), PosMod(vec.Z, m));
        }

        private static Vector3 Abs(Vector3 vec)
        {
            return new Vector3(MathF.Abs(vec.X), MathF.Abs(vec.Y), MathF.Abs(vec.Z));
        }

        private static Vector3 Clamp(Vector3 v, float min, float max)
        {
            return new Vector3(
                MathF.Min(MathF.Max(v.X, min), max),
                MathF.Min(MathF.Max(v.Y, min), max),
                MathF.Min(MathF.Max(v.Z, min), max)
            );
        }

        /// <summary>
        /// Converts a hue–saturation–value triplet into an RGB color.
        /// Hue is expected in the range [0, 1], representing a full rotation.
        /// </summary>
        /// <param name="hsv">Hue, saturation and brightness components (0-1).</param>
        /// <returns>An RGB color vector.</returns>
        public static Vector3 HsvTorgb(Vector3 hsv)
        {
            return HsvToRgb(hsv.X, hsv.Y, hsv.Z);
        }

        /// <summary>
        /// Converts a hue–saturation–value triplet into an RGB color.
        /// Hue is expected in the range [0, 1], representing a full rotation.
        /// </summary>
        /// <param name="h">Hue component (0–1).</param>
        /// <param name="s">Saturation component (0–1).</param>
        /// <param name="v">Value/brightness component (0–1).</param>
        /// <returns>An RGB color vector.</returns>
        public static Vector3 HsvToRgb(float h, float s, float v)
        {
            h = h * 6f; // sector 0..6
            int i = (int)MathF.Floor(h) % 6;
            float f = h - MathF.Floor(h);
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            return i switch
            {
                0 => new Vector3(v, t, p),
                1 => new Vector3(q, v, p),
                2 => new Vector3(p, v, t),
                3 => new Vector3(p, q, v),
                4 => new Vector3(t, p, v),
                5 => new Vector3(v, p, q),
                _ => Vector3.Zero
            };
        }

        /// <summary>
        /// Converts a polar coordinate (radius, angle) into an RGB color
        /// using HSV color space. The radius is used as saturation, and angle
        /// determines hue.
        /// </summary>
        /// <param name="polar">Polar coordinate (r, θ).</param>
        /// <returns>The RGB color represented by the polar location.</returns>
        public static Vector3 ColorFromPolar(Vector2 polar)
        {
            float r = (float)Math.Clamp(polar.X, 0.0, 1.0);
            float theta = polar.Y;
            float h = theta / (2.0f * MathF.PI);
            float s = r;
            float v = 1.0f;
            return HsvToRgb(h, s, v);
        }

        /// <summary>
        /// Converts a 2D cartesian coordinate to normalized polar coordinates,
        /// where radius is scaled to <c>radius</c> and angle is in the range [0, 2π].
        /// </summary>
        /// <param name="p">The cartesian point.</param>
        /// <param name="radius">The scaling radius.</param>
        /// <returns>A vector containing normalized radius and angle.</returns>
        public static Vector2 CartesianToPolar(Vector2 p, float radius)
        {
            float r = p.Length;
            float theta = MathF.Atan2(p.Y, p.X);
            if (theta < 0.0)
            {
                theta += 2 * MathF.PI;
            }
            return new Vector2(r / radius, theta);
        }
    }
}