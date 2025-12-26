using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Components;
using OTK.UI.Containers;
using OTK.UI.Interfaces;
using OTK.UI.Pickers;
using System.Globalization;
using System.Xml.Linq;
using Image = OTK.UI.Components.Image;

namespace OTK.UI.Utility
{
    /// <summary>
    /// Responsible for loading and managing UI elements from an XML layout file.  
    /// Provides methods to access elements by name, update them, and forward input events.  
    /// Supports positioning elements relative to window dimensions using <see cref="RelativeOrigin"/>.  
    /// </summary>
    public class LayoutLoader
    {
        /// <summary>
        /// Defines reference points for positioning elements relative to the window.
        /// </summary>
        public enum RelativeOrigin
        {
            None,
            Center,
            BottomLeft,
            CenterLeft,
            TopLeft,
            TopCenter,
            TopRight,
            CenterRight,
            BottomRight,
            BottomCenter
        }

        /// <summary>
        /// Internal list of all loaded UI elements.
        /// </summary>
        protected List<IUIElement> list = new List<IUIElement>();

        /// <summary>
        /// Registry of UI elements keyed by their unique names.
        /// </summary>
        protected Dictionary<string, IUIElement> registry = new Dictionary<string, IUIElement>();

        /// <summary>
        /// Mapping of string keys to <see cref="RelativeOrigin"/> values for XML parsing.
        /// </summary>
        public static readonly Dictionary<string, RelativeOrigin> RelativeOrigins = new()
        {
            { "center", RelativeOrigin.Center },
            { "bottomleft", RelativeOrigin.BottomLeft },
            { "centerleft", RelativeOrigin.CenterLeft },
            { "topleft", RelativeOrigin.TopLeft },
            { "topcenter", RelativeOrigin.TopCenter },
            { "topright", RelativeOrigin.TopRight },
            { "centerright", RelativeOrigin.CenterRight },
            { "bottomright", RelativeOrigin.BottomRight },
            { "bottomcenter", RelativeOrigin.BottomCenter },
            { "none", RelativeOrigin.None}
        };

        /// <summary>
        /// Gets the number of loaded UI elements.
        /// </summary>
        /// <remarks>Does not count nested UI elements in the final value.</remarks>
        public int Count => list.Count;

        /// <summary>
        /// Loads UI elements from an XML file immediately upon construction.
        /// </summary>
        /// <param name="fileName">Path to the XML layout file.</param>
        public LayoutLoader(string fileName)
        {
            Load(fileName);
        }

        /// <summary>
        /// Loads UI elements from the specified XML file and registers them internally.
        /// </summary>
        /// <param name="fileName">Path to the XML layout file.</param>
        public void Load(string fileName)
        {
            list.Clear();
            registry.Clear();
            using Stream? stream = ResourceLoader.GetStream(fileName);
            if (stream is null)
            {
                Console.WriteLine($"No resource found with name: {fileName}");
                return;
            }
            XDocument xDocument = XDocument.Load(stream);
            var root = xDocument.Root;
            if (root is null) return;

            var displayUnits = root.Attribute("DisplayUnits")?.Value ?? "DPI";

            if (displayUnits.Equals("Pixels", StringComparison.InvariantCultureIgnoreCase))
            {
                UIBase.DisplayUnits = UIBase.DisplayUnitType.Pixels;
            }

            foreach (var element in root.Elements())
            {
                var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
                switch (element.Name.LocalName.ToLower())
                {
                    case "button":
                        Store(name, Button.Load(registry, element));
                        break;
                    case "breadcrumb":
                        Store(name, BreadCrumb.Load(registry, element));
                        break;
                    case "checkbox":
                        Store(name, CheckBox.Load(registry, element));
                        break;
                    case "image":
                        Store(name, Image.Load(registry, element));
                        break;
                    case "label":
                        Store(name, Label.Load(registry, element));
                        break;
                    case "ninepatch":
                        Store(name, NinePatch.Load(registry, element));
                        break;
                    case "numericspinner":
                        Store(name, NumericSpinner.Load(registry, element));
                        break;
                    case "progressbar":
                        Store(name, ProgressBar.Load(registry, element));
                        break;
                    case "radialmenu":
                        Store(name, RadialMenu.Load(registry, element));
                        break;
                    case "scrollbar":
                        Store(name, ScrollBar.Load(registry, element));
                        break;
                    case "slider":
                        Store(name, Slider.Load(registry, element));
                        break;
                    case "textfield":
                        Store(name, TextField.Load(registry, element));
                        break;
                    case "panel":
                        Store(name, Panel.Load(registry, element));
                        break;
                    case "dynamicpanel":
                        Store(name, DynamicPanel.Load(registry, element));
                        break;
                    case "tabbedpanel":
                        Store(name, TabbedPanel.Load(registry, element));
                        break;
                    case "colorpicker":
                    case "colourpicker":
                        Store(name, ColorPicker.Load(registry, element));
                        break;
                    case "filepicker":
                        Store(name, FilePicker.Load(registry, element));
                        break;
                }
            }
        }

        /// <summary>
        /// Retrieves a loaded element by name and casts it to the requested type.
        /// </summary>
        /// <typeparam name="T">Type of the element to retrieve. Must implement <see cref="IUIElement"/>.</typeparam>
        /// <param name="name">The unique name of the element.</param>
        /// <returns>The element cast to <typeparamref name="T"/>, or null if not found or type mismatch.</returns>
        public T? Get<T>(string name) where T : class, IUIElement
        {
            if (registry.TryGetValue(name, out var result) && result is T tResult)
                return tResult;
            return null;
        }

        /// <summary>
        /// Determines if the given string represents a file path.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <returns>True if the string appears to be a path; false otherwise.</returns>
        public static bool IsFilePath(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;

            return s.Contains(Path.DirectorySeparatorChar) ||
                   s.Contains(Path.AltDirectorySeparatorChar) ||
                   Path.HasExtension(s);
        }

        /// <summary>
        /// Parses a comma-separated "R,G,B" string into a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="input">The string containing RGB values.</param>
        /// <param name="name">The element name (used for error messages).</param>
        /// <returns>A <see cref="Vector3"/> with each component normalized from the string.</returns>
        /// <exception cref="FormatException">Thrown if the string does not contain exactly three comma-separated values.</exception>
        public static Vector3 ParseVector3(string input, string name)
        {
            input = new string([.. input.Where(c => !char.IsWhiteSpace(c))]);
            var parts = input.Split(',');
            if (parts.Length != 3) throw new FormatException($"{name} is malformed. Must be \"R,G,B\".");
            return new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture)
            );
        }

        /// <summary>
        /// Registers a UI element internally and adds it to the element list.
        /// </summary>
        /// <param name="name">The unique name of the element.</param>
        /// <param name="element">The element instance to register.</param>
        /// <exception cref="ArgumentException">Thrown if the name is empty or already registered.</exception>
        protected void Store(string name, IUIElement element)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException($"Every element must have an identifier");
            list.Add(element);
        }

        /// <summary>
        /// Returns a <see cref="Vector4"/> representing the offset for the given <see cref="RelativeOrigin"/>.
        /// Used to calculate element positions relative to the window.
        /// </summary>
        /// <param name="relativeOrigin">The relative origin to use.</param>
        /// <returns>A <see cref="Vector4"/> where (X,Y) and (Z,W) are both set to the offset.</returns>
        public static Vector4 GetRelativeOrigin(RelativeOrigin relativeOrigin)
        {
            Vector2 offset;
            switch (relativeOrigin)
            {
                case RelativeOrigin.TopLeft:
                    offset = new Vector2(0, UIBase.WindowDimensions.Y);
                    break;
                case RelativeOrigin.TopCenter:
                    offset = new Vector2(UIBase.WindowDimensions.X * 0.5f, UIBase.WindowDimensions.Y);
                    break;
                case RelativeOrigin.TopRight:
                    offset = UIBase.WindowDimensions;
                    break;
                case RelativeOrigin.CenterLeft:
                    offset = new Vector2(0, UIBase.WindowDimensions.Y * 0.5f);
                    break;
                case RelativeOrigin.Center:
                    offset = new Vector2(UIBase.WindowDimensions.X * 0.5f, UIBase.WindowDimensions.Y * 0.5f);
                    break;
                case RelativeOrigin.CenterRight:
                    offset = new Vector2(UIBase.WindowDimensions.X, UIBase.WindowDimensions.Y * 0.5f);
                    break;
                case RelativeOrigin.BottomLeft:
                    offset = Vector2.Zero;
                    break;
                case RelativeOrigin.BottomCenter:
                    offset = new Vector2(UIBase.WindowDimensions.X * 0.5f, 0);
                    break;
                case RelativeOrigin.BottomRight:
                    offset = new Vector2(UIBase.WindowDimensions.X, 0);
                    break;
                default:
                    offset = Vector2.Zero;
                    break;
            }
            return new Vector4(offset.X, offset.Y, offset.X, offset.Y);
        }

        /// <summary>
        /// Updates all loaded elements for a given frame, forwarding delta time and input states.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update.</param>
        /// <param name="mouse">Current mouse state.</param>
        /// <param name="keyboard">Current keyboard state.</param>
        public void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard)
        {
            foreach (var element in list)
            {
                element.OnUpdate(deltaTime);
                element.OnUpdate(deltaTime, mouse);
                element.OnUpdate(deltaTime, keyboard);
                element.OnUpdate(deltaTime, mouse, keyboard);
            }
        }

        /// <summary>
        /// Forwards text input events to all loaded elements.
        /// </summary>
        public void OnTextInput(TextInputEventArgs e)
        {
            foreach (var element in list)
            {
                element.OnTextInput(e);
            }
        }

        /// <summary>
        /// Forwards key-down events to all loaded elements.
        /// </summary>
        public void OnKeyDown(KeyboardKeyEventArgs e)
        {
            foreach (var element in list)
            {
                element.OnKeyDown(e);
            }
        }

        /// <summary>
        /// Forwards key-up events to all loaded elements.
        /// </summary>
        public void OnKeyUp(KeyboardKeyEventArgs e)
        {
            foreach (var element in list)
            {
                element.OnKeyUp(e);
            }
        }

        /// <summary>
        /// Forwards mouse button-down events to all loaded elements.
        /// </summary>
        public void OnClickDown(MouseState mouse)
        {
            foreach (var element in list)
            {
                element.OnClickDown(mouse);
            }
        }

        /// <summary>
        /// Forwards mouse button-up events to all loaded elements.
        /// </summary>
        public void OnClickUp(MouseState mouse)
        {
            foreach (var element in list)
            {
                element.OnClickUp(mouse);
            }
        }

        /// <summary>
        /// Forwards mouse wheel events to all loaded elements.
        /// </summary>
        public void OnMouseWheel(MouseState mouse)
        {
            foreach (var element in list)
            {
                element.OnMouseWheel(mouse);
            }
        }

        /// <summary>
        /// Forwards mouse movement events to all loaded elements.
        /// </summary>
        public void OnMouseMove(MouseState mouse)
        {
            foreach (var element in list)
            {
                element.OnMouseMove(mouse);
            }
        }

        /// <summary>
        /// Draws all loaded elements.
        /// </summary>
        public void Draw()
        {
            foreach (var element in list)
            {
                element.Draw();
            }
        }
    }
}
