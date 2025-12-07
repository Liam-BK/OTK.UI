using OpenTK.Mathematics;
using OTK.UI.Managers;
using OTK.UI.Utility;
using System.Globalization;
using System.Xml.Linq;

namespace OTK.UI.Components
{
    /// <summary>
    /// Represents a simple image UI element that can be rendered within the GUI.
    /// Inherits from <see cref="UIBase"/> and supports setting bounds, color, visibility, texture, and anchoring.
    /// </summary>
    public class Image : UIBase
    {
        private static int defaultProgram = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class with specified bounds and optional color.
        /// </summary>
        /// <param name="bounds">The rectangular bounds of the image (X = left, Y = bottom, Z = right, W = top).</param>
        /// <param name="colour">Optional RGB color for the image. Defaults to white if not specified.</param>
        public Image(Vector4 bounds, Vector3? colour = null) : base(bounds, colour)
        {
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateUIShader(ShaderManager.UI_Fragment);
            if (defaultProgram == 0) defaultProgram = program;
        }

        /// <summary>
        /// Creates an <see cref="Image"/> from a layout XML element. Supports loading
        /// bounds, color, visibility, anchor and texture.
        /// Throws <see cref="FormatException"/> if required fields are missing.
        /// </summary>
        /// <param name="element">The XML element containing label configuration.</param>
        /// <returns>A fully initialized <see cref="Image"/> instance.</returns>
        public static Image Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
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

            Image image = new Image(new Vector4(left, bottom, right, top) + relativeAnchorVector);
            image.IsVisible = isVisible;
            image.Colour = colorVec;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                image.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else image.Texture = texture;

            return image;
        }
    }
}