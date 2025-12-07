using System.Reflection;
using OpenTK.Graphics.OpenGL4;

namespace OTK.UI.Managers
{
    public static class ShaderManager
    {
        /// <summary>
        /// The file path to the default vertex shader
        /// </summary>
        public const string UI_Vertex = "OTK.UI.Shaders.Vertex.UI.vert";

        /// <summary>
        /// The file path to the default fragment shader
        /// </summary>
        public const string UI_Fragment = "OTK.UI.Shaders.Fragment.UI.frag";

        /// <summary>
        /// Creates an OpenGL shader program using the default vertex shader and the fragment shader located at the specified path.
        /// </summary>
        /// <param name="path">The file path to the fragment shader to use.</param>
        /// <returns>The OpenGL program handle of the created shader.</returns>
        public static int CreateUIShader(string path)
        {
            return CreateShader(UI_Vertex, path);
        }

        private static string LoadShaderSource(string resourceName)
        {
            using var stream = ResourceLoader.GetStream(resourceName);
            if (stream is null)
            {
                throw new IOException($"No resource found with name {resourceName}");
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Creates an OpenGL shader program using the vertex and fragment shader located at the specified paths.
        /// </summary>
        /// <param name="vertexPath">The file path to the vertex shader to use.</param>
        /// <param name="fragmentPath">The file path to the fragment shader to use.</param>
        /// <returns>The OpenGL program handle of the created shader.</returns>
        public static int CreateShader(string vertexPath, string fragmentPath)
        {
            // string vertexCode = File.ReadAllText(vertexPath);
            // string fragmentCode = File.ReadAllText(fragmentPath);
            string vertexCode = LoadShaderSource(vertexPath);
            string fragmentCode = LoadShaderSource(fragmentPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexCode);
            GL.CompileShader(vertexShader);
            CheckCompileErrors(vertexShader, "VERTEX");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentCode);
            GL.CompileShader(fragmentShader);
            CheckCompileErrors(fragmentShader, "FRAGMENT");

            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            CheckLinkErrors(program);

            // Cleanup
            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return program;
        }

        /// <summary>
        /// Deletes the program with the specified handle from the GPU.
        /// </summary>
        /// <param name="shader">The handle of the program to be deleted.</param>
        public static void DeleteProgram(int shader)
        {
            GL.DeleteProgram(shader);
        }

        private static void CheckCompileErrors(int shader, string type)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                Console.WriteLine($"ERROR::SHADER_COMPILATION_ERROR of type: {type}\n{info}");
                throw new Exception($"Shader compilation error: {type}");
            }
        }

        private static void CheckLinkErrors(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetProgramInfoLog(program);
                Console.WriteLine($"ERROR::PROGRAM_LINKING_ERROR\n{info}");
                throw new Exception("Shader program linking error.");
            }
        }
    }
}