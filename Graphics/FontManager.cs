using System.Reflection;
using System.Text;
using OpenTK.Mathematics;
using Typography.OpenFont;
using static StbTrueTypeSharp.StbTrueType;


namespace OTK.UI.Managers
{
    internal class FontData
    {
        public ImageData Bitmap { get; set; } = new();

        public Dictionary<char, Vector4> GlyphUVs = new();

        public Dictionary<char, Vector4> GlyphBounds = new();

        public Dictionary<char, Vector2> Offsets = new();

        public Dictionary<(char, char), float> Kerning = new();

        public float ScaleFactor = 1;
    }

    public static class FontManager
    {
        internal static Dictionary<string, FontData> Fonts = new();

        /// <summary>
        /// This is set whenever LoadFont is called for the first time. Used by Label as a way to avoid having to manually set its font every time. 
        /// </summary>
        public static string DefaultFontKey { get; private set; } = "";

        /// <summary>
        /// Generates relevant FontData based on the provided parameters and stores it for access by Label.
        /// </summary>
        /// <param name="filePath">The path to the .ttf font file you want to load.</param>
        /// <param name="fontSize">The desired pixel height of each glyph when baked.</param>
        /// <param name="atlasWidth">The total width in pixels of the generated font atlas texture.</param>
        /// <param name="atlasHeight">The total height in pixels of the generated font atlas texture.</param>
        /// <param name="firstChar">The first Unicode character to include in the generated font atlas. 
        /// Only characters from firstChar to lastChar (inclusive) will be baked.</param>
        /// <param name="lastChar">The last Unicode character to include in the generated font atlas. 
        /// Only characters from firstChar to lastChar (inclusive) will be baked.</param>
        /// <exception cref="Exception">Thrown if baking the bitmap fails</exception>
        public static void LoadFont(string filePath, int fontSize = 32, int atlasWidth = 512, int atlasHeight = 512, char firstChar = ' ', char lastChar = '~')
        {
            var bytes = ResourceLoader.GetBytes(filePath);
            if (bytes is null)
            {
                return;
            }
            byte[] ttf = bytes;
            int numChars = lastChar - firstChar + 1;
            stbtt_bakedchar[] bakedChars = new stbtt_bakedchar[lastChar - firstChar + 1];
            byte[] bitmap = new byte[atlasWidth * atlasHeight];

            var result = stbtt_BakeFontBitmap(ttf, 0, fontSize, bitmap, atlasWidth, atlasHeight, firstChar, numChars, bakedChars);
            if (!result)
                throw new Exception("Font Baking failed");

            Dictionary<char, Vector4> glyphUVData = new();
            Dictionary<char, Vector4> glyphBoundsData = new();
            Dictionary<char, Vector2> offsets = new();
            for (int i = 0; i < numChars; i++)
            {
                var bc1 = bakedChars[i];
                float UVLeft = bc1.x0 / (float)atlasWidth;
                float UVTop = bc1.y0 / (float)atlasHeight;
                float UVRight = bc1.x1 / (float)atlasWidth;
                float UVBottom = bc1.y1 / (float)atlasHeight;
                Vector4 glyphUVs = new(UVLeft, UVTop, UVRight, UVBottom);
                Vector4 glyphBounds = new(bc1.x0, bc1.y0, bc1.x1, bc1.y1);
                glyphUVData.Add((char)(firstChar + i), glyphUVs);
                glyphBoundsData.Add((char)(firstChar + i), glyphBounds);
                offsets.Add((char)(firstChar + i), new Vector2(bc1.xoff, bc1.yoff));
            }

            var image = new ImageData()
            {
                Width = atlasWidth,
                Height = atlasHeight,
                Pixels = bitmap,
                Channels = 1
            };

            image.FlipImageVertically();

            image = ConvertFromAlphaToRGBA(image);
            string fontKey = Path.GetFileNameWithoutExtension(filePath);

            if (DefaultFontKey is "") DefaultFontKey = fontKey;

            FontData fontData = new FontData()
            {
                Bitmap = image,
                GlyphUVs = glyphUVData,
                GlyphBounds = glyphBoundsData,
                Offsets = offsets,
                Kerning = GenerateKerningTable(filePath, firstChar, lastChar),
                ScaleFactor = bakedChars['H' - firstChar].y1 - bakedChars['H' - firstChar].y0
            };

            Fonts.Add(fontKey, fontData);
            TextureManager.LoadTexture(image, fontKey);
        }

        private static ImageData ConvertFromAlphaToRGBA(ImageData data)
        {
            if (data.Channels != 1)
                throw new Exception("Provided bitmap has incorrect number of channels");

            byte[] pixels = new byte[data.Width * data.Height * 4];
            for (int i = 0; i < data.Pixels.Length; i++)
            {
                byte value = data.Pixels[i];
                int rgbaIndex = i * 4;

                pixels[rgbaIndex] = 255;
                pixels[rgbaIndex + 1] = 255;
                pixels[rgbaIndex + 2] = 255;
                pixels[rgbaIndex + 3] = value;
            }
            return new ImageData()
            {
                Width = data.Width,
                Height = data.Height,
                Pixels = pixels,
                Channels = 4
            };
        }

        private static Dictionary<(char, char), float> GenerateKerningTable(string filePath, char firstChar, char lastChar)
        {
            Dictionary<(char, char), float> kerning = new();

            using Stream? fontStream = ResourceLoader.GetStream(filePath);
            if (fontStream == null)
                throw new FileNotFoundException($"Font file not found: {filePath}");



            var reader = new OpenFontReader();
            using var fs = ResourceLoader.GetStream(filePath);
            // using var fs = File.OpenRead(filePath);
            Typeface typeface = reader.Read(fs);
            var gpos = typeface.GPOSTable;
            if (gpos is not null)
            {
                string resourceName = filePath.EndsWith("Italic.ttf") ? "OTK.UI.Kerning.DefaultKerningItalic.txt" : "OTK.UI.Kerning.DefaultKerning.txt";
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream(resourceName) ?? throw new Exception($"Resource not found: {resourceName}");
                using var resourceReader = new StreamReader(stream);

                foreach (var line in resourceReader.ReadToEnd().Split('\n'))
                {
                    var parts = line.Split(' ');
                    if (parts.Length != 3) continue;
                    if (parts[0].Length != 1 || parts[1].Length != 1) continue;
                    char leftChar = parts[0][0];
                    char rightChar = parts[1][0];
                    if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float kernValue)) continue;
                    kerning.Add((leftChar, rightChar), kernValue);
                }
                return kerning;
            }
            for (char a = firstChar; a <= lastChar; a++)
            {
                for (char b = firstChar; b <= lastChar; b++)
                {
                    try
                    {
                        float kernValue = typeface.GetKernDistance(typeface.GetGlyphIndex(a), typeface.GetGlyphIndex(b));
                        var hIndex = typeface.GetGlyphIndex('H');
                        float divisor = typeface.GetAdvanceWidthFromGlyphIndex(hIndex);
                        kerning.Add((a, b), kernValue / divisor);
                    }
                    catch (NullReferenceException e)
                    {
                        kerning.Add((a, b), 0);
                        Console.WriteLine($"Error accessing fallback legacy kerning table: {e.StackTrace}");
                        Console.WriteLine("Setting Glyph pair kerning to zero");
                    }
                }
            }
            return kerning;
        }

        /// <summary>
        /// Creates and saves a Kerning file. Useful for creating a backup to handle fonts without kerning data.
        /// </summary>
        /// <param name="fontPath">The path to the .ttf font file you want to extract the kerning from.</param>
        /// <param name="writePath">The path to the file you want to create.</param>
        /// <param name="firstChar">The first Unicode character to get the kerning data from</param>
        /// <param name="lastChar">The last Unicode character to get the kerning data from</param>
        public static void PopulateNewKerningFile(string fontPath, string writePath, char firstChar = ' ', char lastChar = '~')
        {
            var reader = new OpenFontReader();
            using var fs = File.OpenRead(fontPath);
            Typeface typeface = reader.Read(fs);
            var sb = new StringBuilder();
            for (char a = firstChar; a <= lastChar; a++)
            {
                for (char b = firstChar; b <= lastChar; b++)
                {
                    float kernValue = typeface.GetKernDistance(typeface.GetGlyphIndex(a), typeface.GetGlyphIndex(b));
                    var hIndex = typeface.GetGlyphIndex('H');
                    float divisor = typeface.GetAdvanceWidthFromGlyphIndex(hIndex);
                    sb.AppendLine($"{a} {b} {kernValue / divisor}");
                }
            }
            File.WriteAllText(writePath, sb.ToString());
        }
    }
}