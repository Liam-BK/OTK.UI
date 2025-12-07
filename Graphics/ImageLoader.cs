using StbImageSharp;

namespace OTK.UI.Managers
{
    public class ImageData
    {
        /// <summary>
        /// The width of a bitmap in pixels.
        /// </summary>
        public int Width;

        /// <summary>
        /// The height of a bitmap in pixels.
        /// </summary>
        public int Height;

        /// <summary>
        /// An array of bytes representing the pixel data of a bitmap.
        /// </summary>
        public byte[] Pixels = Array.Empty<byte>();

        /// <summary>
        /// The number of color channels for the bitmap.
        /// </summary>
        public int Channels;

        /// <summary>
        /// A method for flipping the bits of an image vertically. 
        /// Useful for converting between coordinate systems.
        /// </summary>
        public void FlipImageVertically()
        {
            int width = Width;
            int height = Height;
            int channels = Channels;
            byte[] pixels = Pixels;

            int stride = width * channels;
            byte[] rowBuffer = new byte[stride];

            for (int y = 0; y < height / 2; y++)
            {
                int top = y * stride;
                int bottom = (height - 1 - y) * stride;

                // Swap rows
                Array.Copy(pixels, top, rowBuffer, 0, stride);
                Array.Copy(pixels, bottom, pixels, top, stride);
                Array.Copy(rowBuffer, 0, pixels, bottom, stride);
            }
        }

        /// <summary>
        /// Converts a given bitmap to a greyscale representation.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the image has less than 3 color channels.</exception>
        public void GreyScale()
        {
            if (Channels < 3)
                throw new InvalidOperationException("Image must have at least 3 channels for greyscale conversion.");

            for (int i = 0; i < Pixels.Length; i += Channels)
            {
                byte r = Pixels[i];
                byte g = Pixels[i + 1];
                byte b = Pixels[i + 2];

                // Calculate luminance using perceptual weighting
                byte grey = (byte)(0.3 * r + 0.59 * g + 0.11 * b);

                Pixels[i] = grey;
                Pixels[i + 1] = grey;
                Pixels[i + 2] = grey;
                // Leave alpha unchanged (i + 3)
            }
        }
    }

    public static class ImageLoader
    {
        [Flags]
        public enum Flip
        {
            None = 0,
            Vertical = 1 << 1,
            Horizontal = 1 << 2
        }


        private static bool IsFlipVertical(Flip flipFlag)
        {
            return (flipFlag & Flip.Vertical) != 0;
        }

        /// <summary>
        /// Is currently unused
        /// </summary>
        /// <param name="flipFlag"></param>
        /// <returns></returns>
        public static bool IsFlipHorizontal(Flip flipFlag)
        {
            return (flipFlag & Flip.Horizontal) != 0;
        }

        /// <summary>
        /// Loads a png file and returns an ImageData representing the loaded file.
        /// </summary>
        /// <param name="filePath">The path to the image.</param>
        /// <param name="flipFlag">Determines whether or not to flip the image vertically.</param>
        /// <param name="greyscale">Sets the image to greyscale if true.</param>
        /// <returns>An instance of ImageData.</returns>
        public static ImageData LoadImage(string filePath, Flip flipFlag, bool greyscale = false)
        {
            using Stream? stream = ResourceLoader.GetStream(filePath);
            if (stream == null)
                throw new FileNotFoundException($"Image file not found: {filePath}");

            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            var temp = new ImageData
            {
                Width = image.Width,
                Height = image.Height,
                Channels = 4,
                Pixels = image.Data
            };
            if (IsFlipVertical(flipFlag)) temp.FlipImageVertically();
            if (greyscale) temp.GreyScale();
            return temp;
        }
    }
}