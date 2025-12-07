using OpenTK.Graphics.OpenGL4;

namespace OTK.UI.Managers
{
    public static class TextureManager
    {
        private static readonly Dictionary<string, int> _textures = new();
        private static readonly List<string> _textureNames = new();
        private static readonly TextureUnit[] units = Enumerable.Range(0, 32).Select(i => TextureUnit.Texture0 + i).ToArray();

        /// <summary>
        /// Total number of textures currently in memory.
        /// </summary>
        public static int TextureCount
        {
            get => _textureNames.Count;
        }

        /// <summary>
        /// Loads a texture from disk, uploads it to the GPU, and stores it in the TextureManager
        /// under the specified key.
        /// </summary>
        /// <param name="texturePath">The file path of the image to load.</param>
        /// <param name="name">The identifier used to access the texture later.</param>
        /// <param name="greyscale">If true, the image is converted to greyscale before being uploaded.</param>
        public static void LoadTexture(string texturePath, string name, bool greyscale = false)
        {
            var data = ImageLoader.LoadImage(texturePath, ImageLoader.Flip.Vertical, greyscale);
            byte[] buffer = data.Pixels;
            byte maxValue = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] > maxValue)
                {
                    maxValue = buffer[i];
                }
            }

            int textureID;
            GL.GenTextures(1, out textureID);
            _textures.Add(name, textureID);
            _textureNames.Add(name);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, buffer);
        }

        /// <summary>
        /// Calls LoadTexture for all pngs within a given directory. Does not handle embedded resources and as such is intended for development purposes.
        /// </summary>
        /// <param name="directory">The directory from which to load the images.</param>
        /// <param name="greyscale">If true, loads all files as greyscale.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown when the provided directory is not found.</exception>
        public static void LoadAllTextures(string directory, bool greyscale = false)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Directory '{directory}' not found.");

            foreach (var file in Directory.GetFiles(directory))
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext is not ".png") continue;

                string name = Path.GetFileNameWithoutExtension(file);
                LoadTexture(file, name, greyscale);
            }
        }

        /// <summary>
        /// Uploads a given <see cref="ImageData"/> to the GPU and stores it in the TextureManager 
        /// under the specified key.
        /// </summary>
        /// <param name="image">The ImageData to upload to the GPU.</param>
        /// <param name="name">The identifier used to access the texture later.</param>
        public static void LoadTexture(ImageData image, string name)
        {
            byte[] buffer = image.Pixels;
            byte maxValue = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] > maxValue)
                {
                    maxValue = buffer[i];
                }
            }

            int textureID;
            GL.GenTextures(1, out textureID);
            _textures.Add(name, textureID);
            _textureNames.Add(name);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, buffer);
        }

        /// <summary>
        /// Gets the handle of the texture by name.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The handle of the texture with the given name.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no texture with the given name is found.</exception>
        public static int GetTexture(string name)
        {
            if (!_textures.TryGetValue(name, out int textureID))
                throw new KeyNotFoundException($"Texture '{name}' not found.");
            return textureID;
        }

        /// <summary>
        /// Gets the handle of the texture by index.
        /// </summary>
        /// <param name="index">The index of the texture.</param>
        /// <returns>The handle of the texture at the given index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the given index exceeds the bounds of _textureNames.</exception>
        public static int GetTexture(int index)
        {
            if (index < 0 || index >= _textureNames.Count)
            {
                throw new ArgumentOutOfRangeException($"Texture index {index} is out of range.");
            }
            return _textures[_textureNames[index]];
        }

        /// <summary>
        /// Binds the texture to a given TextureUnit by name. 
        /// </summary>
        /// <param name="name">The name of the texture to be bound.</param>
        /// <param name="textureUnit">The texture unit to bind the texture to.</param>
        /// <exception cref="KeyNotFoundException">Thrown when no texture with the given name exists.</exception>
        public static void Bind(string name, int textureUnit)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            if (!_textures.ContainsKey(name))
            {
                throw new KeyNotFoundException($"Texture '{name}' not found.");
            }

            GL.ActiveTexture(units[textureUnit]);
            GL.BindTexture(TextureTarget.Texture2D, GetTexture(name));
        }

        /// <summary>
        /// Unbinds the texture at the given TextureUnit.
        /// </summary>
        /// <param name="textureUnit">The TextureUnit to unbind.</param>
        public static void Unbind(int textureUnit)
        {
            GL.ActiveTexture(units[textureUnit]);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Deletes a texture based on the given key.
        /// </summary>
        /// <param name="name">The name of the texture to delete.</param>
        public static void Delete(string name)
        {
            GL.DeleteTexture(_textures[name]);
            _textures.Remove(name);
            _textureNames.Remove(name);
        }

        /// <summary>
        /// Deletes a single texture from the GPU at the given index.
        /// </summary>
        /// <param name="index">The index of the texture to be deleted.</param>
        public static void Delete(int index)
        {
            Delete(_textureNames[index]);
        }

        /// <summary>
        /// Deletes all loaded textures from the GPU.
        /// </summary>
        public static void DeleteAll()
        {
            for (int i = _textureNames.Count - 1; i >= 0; i--)
            {
                Delete(i);
            }
        }
    }
}