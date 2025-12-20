using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Utility;
using OTK.UI.Managers;
using System.Xml.Linq;
using System.Globalization;
using OpenTK.Graphics.OpenGL4;

namespace OTK.UI.Components
{
    /// <summary>
    /// A radial selection menu composed of equally spaced icon segments
    /// arranged around a circular layout. The menu highlights the segment
    /// nearest to the mouse direction and displays its name and description.
    /// 
    /// A <see cref="RadialMenu"/> can be activated by key presses or mouse input
    /// depending on the configured control mode. It supports automatic mouse
    /// warping to the menu center, dynamic segment highlighting, and
    /// XML-based layout loading.
    /// </summary>
    public class RadialMenu : Image
    {
        private Label Name;

        private Label Description;

        private int _segments;

        private float _innerIconOffset;

        /// <summary>
        /// The total number of radial segments in the menu.
        /// This value is determined by the number of icons provided at construction.
        /// </summary>
        public int Segments
        {
            get
            {
                return _segments;
            }
            private set
            {
                _segments = value;
            }
        }

        private bool _warpMouseToCenter = false;

        private UIBase[] icons;

        private IconData[] iconData;

        /// <summary>
        /// Gets or sets whether the radial menu is visible.  
        /// When the menu becomes visible, the mouse is optionally warped to the center
        /// on the next update frame depending on the configured control mode.
        /// </summary>
        public override bool IsVisible
        {
            get
            {
                return base.IsVisible;
            }
            set
            {
                if (value != IsVisible && value)
                {
                    _warpMouseToCenter = true;
                }
                base.IsVisible = value;
            }
        }

        private float Radius;

        public int CurrentIndex = 0;

        /// <summary>
        /// The RGB colour applied to the highlighted segment.
        /// Passed to the shader as <c>hoverColour</c>.
        /// </summary>
        public Vector3 HoverColour
        {
            get;
            set;
        }

        /// <summary>
        /// A visual-only radius used by the shader when rendering ring-style textures.
        /// Any fragment within this inner radius is excluded from hover tinting,
        /// allowing the center of the radial menu to remain uncolored or transparent.
        /// </summary>
        public float TintExclusionRadius = 0;

        /// <summary>
        /// The key used to activate or toggle the radial menu,
        /// depending on the selected <see cref="RadialControlMode"/>.
        /// </summary>
        public Keys ActivationKey = Keys.Unknown;

        /// <summary>
        /// The control scheme that determines how the menu appears and is interacted with.
        /// </summary>
        public RadialControlMode controlMode = RadialControlMode.HoldKeyAndDrag;

        public enum RadialControlMode
        {
            HoldKeyAndDrag,
            PressKeyAndLeftClick,
            PressKeyAndRightClick,
            None
        }

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a radial menu with evenly spaced icon segments, a title label,
        /// and a description label. Positions the menu at the specified center point
        /// and sets the visual radius and icon placement offset.
        /// </summary>
        /// <param name="position">The center position of the menu in screen space.</param>
        /// <param name="radius">The radius of the circular background.</param>
        /// <param name="iconInset">
        /// The inward offset from the menu edge where icons are placed.
        /// </param>
        /// <param name="titleSize">Font size for the segment title label.</param>
        /// <param name="descriptionSize">Font size for the segment description label.</param>
        /// <param name="iconData">
        /// Array defining the textures, names, and sizing of each radial segment.
        /// </param>
        /// <param name="colour">
        /// Optional tint applied to the radial background texture.
        /// </param>
        public RadialMenu(Vector2 position, float radius, float iconInset, float titleSize, float descriptionSize, IconData[] iconData, Vector3? colour = null) : base(new Vector4(position.X - radius, position.Y - radius, position.X + radius, position.Y + radius), colour)
        {
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.RadialMenu.vert", "OTK.UI.Shaders.Fragment.RadialMenu.frag");
            if (defaultProgram <= 0) defaultProgram = program;
            Segments = iconData.Length;
            _innerIconOffset = iconInset;
            Radius = radius;
            this.iconData = iconData;
            Name = new Label(position, titleSize, this.iconData[0].IconName, Vector3.Zero);
            Name.Alignment = Label.TextAlign.Center;
            Description = new Label(position - Vector2.UnitY * titleSize, descriptionSize, this.iconData[0].IconDescription, Vector3.Zero);
            Description.Alignment = Label.TextAlign.Center;
            icons = new UIBase[iconData.Length];
            InitializeIcons();
        }

        /// <summary>
        /// Loads and constructs a <see cref="RadialMenu"/> from an XML layout element.
        /// Supports settings for radius, icon placement, text sizes, dead zone radius,
        /// visibility, texture, colors, activation key, control mode, and icon data.
        /// </summary>
        /// <param name="element">The XML element defining the radial menu.</param>
        /// <returns>A fully initialized <see cref="RadialMenu"/> instance.</returns>
        /// <exception cref="FormatException">
        /// Thrown when required values such as <c>Name</c> or <c>Origin</c> are missing.
        /// </exception>
        public static new RadialMenu Load(XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var origin = element.Element("Origin");
            if (origin is null) throw new FormatException($"NinePatch: {name} is missing required field Bounds.");
            var radius = float.Parse(element.Element("Radius")?.Value ?? "50");
            var iconInset = float.Parse(element.Element("IconInset")?.Value ?? "10");
            var titleSize = float.Parse(element.Element("TitleSize")?.Value ?? "30");
            var descriptionSize = float.Parse(element.Element("DescriptionSize")?.Value ?? "20");
            var tintExclusionRadius = float.Parse(element.Element("TintExclusionRadius")?.Value ?? "20");
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var hoverColor = element.Element("HoverColorRGB")?.Value ?? "1, 1, 1";
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var activationKeyStr = element.Element("ActivationKey")?.Value ?? "Unknown";
            Keys activationKey = Enum.TryParse(activationKeyStr, true, out Keys keyResult) ? keyResult : Keys.Unknown;

            var controlModeStr = element.Element("ControlMode")?.Value ?? "HoldKeyAndDrag";
            RadialMenu.RadialControlMode controlMode =
                Enum.TryParse(controlModeStr, true, out RadialMenu.RadialControlMode modeResult)
                ? modeResult : RadialMenu.RadialControlMode.HoldKeyAndDrag;

            var x = float.Parse(origin?.Element("X")?.Value ?? "0", CultureInfo.InvariantCulture);
            var y = float.Parse(origin?.Element("Y")?.Value ?? "0", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);
            var hoverColorVec = LayoutLoader.ParseVector3(hoverColor, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult).Xy;

            var iconData = element.Element("IconData");
            List<IconData> iconDataList = new List<IconData>();
            if (iconData is not null)
            {
                foreach (var icon in iconData.Elements())
                {
                    var temp = new IconData();
                    temp.IconName = icon.Element("Title")?.Value ?? string.Empty;
                    temp.IconDescription = icon.Element("Description")?.Value ?? string.Empty;
                    temp.IconIndex = int.Parse(icon.Element("Index")?.Value ?? $"{iconDataList.Count}");
                    temp.IconTexture = icon.Element("Texture")?.Value ?? string.Empty;
                    temp.IconSize = float.Parse(icon.Element("Size")?.Value ?? "30");
                    iconDataList.Add(temp);
                }
            }

            RadialMenu radialMenu = new RadialMenu(new Vector2(x, y) + relativeAnchorVector, radius, iconInset, titleSize, descriptionSize, iconDataList.ToArray(), colorVec);
            radialMenu.IsVisible = isVisible;
            radialMenu.Colour = colorVec;
            radialMenu.HoverColour = hoverColorVec;
            radialMenu.TintExclusionRadius = tintExclusionRadius;
            radialMenu.ActivationKey = activationKey;
            radialMenu.controlMode = controlMode;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                radialMenu.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else radialMenu.Texture = texture;

            return radialMenu;
        }

        /// <summary>
        /// Computes the position and bounds of each icon based on the menu radius,
        /// icon sizes, and angular spacing. Icons are placed evenly around the circle
        /// and stored in the internal <c>icons</c> array.
        /// </summary>
        private void InitializeIcons()
        {
            for (int i = 0; i < icons.Length; i++)
            {
                float angleDelta = 360.0f / Segments;
                Vector2 direction = new Vector2(MathF.Sin(MathHelper.DegreesToRadians(angleDelta * i)), MathF.Cos(MathHelper.DegreesToRadians(angleDelta * i)));
                direction *= Radius - iconData[i].IconSize - _innerIconOffset;
                Vector2 position = new Vector2(Center.X + direction.X, Center.Y + direction.Y);
                Vector4 bounds = new Vector4(position.X - iconData[i].IconSize, position.Y - iconData[i].IconSize, position.X + iconData[i].IconSize, position.Y + iconData[i].IconSize);
                UIBase icon = new UIBase(bounds);
                icon.Texture = iconData[i].IconTexture;
                icons[i] = icon;
            }
        }

        /// <summary>
        /// Updates the radial menu using the current mouse and keyboard state.
        /// Delegates to specialized overloads for individual input sources.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last frame.</param>
        /// <param name="mouse">The current mouse state.</param>
        /// <param name="keyboard">The current keyboard state.</param>
        public override void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard)
        {
            OnUpdate(deltaTime);
            OnUpdate(deltaTime, mouse);
            OnUpdate(deltaTime, keyboard);
        }

        /// <summary>
        /// Handles activation logic depending on the configured control mode.
        /// For modes requiring key-hold activation, the menu becomes visible on key press
        /// and optionally warps the mouse to the menu center.
        /// </summary>
        /// <param name="e">The triggering keyboard event.</param>
        public override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (controlMode is RadialControlMode.HoldKeyAndDrag)
            {
                if (e.Key == ActivationKey && !e.IsRepeat)
                {
                    _warpMouseToCenter = true;
                    IsVisible = true;
                }

            }
            else if (controlMode is RadialControlMode.PressKeyAndLeftClick)
            {
                if (e.Key == ActivationKey && !e.IsRepeat)
                {
                    IsVisible = true;
                }
            }
            else if (controlMode is RadialControlMode.PressKeyAndRightClick)
            {
                if (e.Key == ActivationKey && !e.IsRepeat)
                {
                    IsVisible = true;
                }
            }
        }

        /// <summary>
        /// Handles deactivation for hold-to-open control modes.
        /// </summary>
        /// <param name="e">The triggering keyboard event.</param>
        public override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (controlMode is RadialControlMode.HoldKeyAndDrag)
            {
                IsVisible = false;
            }
        }

        /// <summary>
        /// Handles activation/deactivation logic for click-triggered control modes.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnClickDown(MouseState mouse)
        {
            base.OnClickDown(mouse);
            if (controlMode is RadialControlMode.PressKeyAndLeftClick)
            {
                if (mouse.IsButtonPressed(MouseButton.Left))
                {
                    IsVisible = false;
                }
            }
            else if (controlMode is RadialControlMode.PressKeyAndRightClick)
            {
                if (mouse.IsButtonPressed(MouseButton.Right))
                {
                    IsVisible = false;
                }
            }
        }

        /// <summary>
        /// Performs all mouse-based interaction updates while the menu is visible.
        /// Handles initial mouse warping, dead-zone behavior, angle calculation,
        /// and determining the currently selected segment.  
        /// Updates the displayed name and description labels accordingly.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last frame.</param>
        /// <param name="mouse">The current mouse state.</param>
        public override void OnUpdate(float deltaTime, MouseState mouse)
        {
            if (!IsVisible) return;
            if (_warpMouseToCenter && Window is not null)
            {
                Window.MousePosition = new Vector2(Center.X * DPIScaleX, Window.Size.Y - Center.Y * DPIScaleY);
            }
            if (_warpMouseToCenter)
            {
                _warpMouseToCenter = false;
                return;
            }

            var convertedMouse = ConvertMouseScreenCoords(mouse.Position);
            if (Vector2.Distance(convertedMouse, Center) > 1)
            {
                float angle = MathHelper.RadiansToDegrees(MathF.Acos(Vector2.Dot(Vector2.UnitY, (new Vector2(convertedMouse.X, convertedMouse.Y) - Center).Normalized())));
                if (convertedMouse.X < Center.X)
                {
                    angle = 360 - angle;
                }
                float segmentArc = 360.0f / Segments;
                angle += segmentArc * 0.5f;
                angle %= 360;
                CurrentIndex = (int)(angle / segmentArc);

                Name.Text = iconData[CurrentIndex].IconName;
                Description.Text = iconData[CurrentIndex].IconDescription;
            }
            base.OnUpdate(deltaTime, mouse);
        }

        /// <summary>
        /// Sends radial-menu-specific uniform values to the active shader, including:
        /// <c>hoverColour</c>, <c>currentIndex</c>, <c>bounds</c>, <c>segments</c>,
        /// and <c>deadZone</c>.
        /// </summary>
        protected override void PassUniform()
        {
            base.PassUniform();
            PassUniform(HoverColour, "hoverColour");
            PassUniform(CurrentIndex, "currentIndex");
            PassUniform(Bounds * DPIScaleVec4, "bounds");
            PassUniform(Segments, "segments");
            PassUniform(TintExclusionRadius, "deadZone");
        }

        /// <summary>
        /// Draws the radial background, the currently selected name and description labels,
        /// and all icon elements. Rendering only occurs when the menu is visible.
        /// </summary>
        public override void Draw()
        {
            if (!IsVisible) return;
            bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            base.Draw();
            Name.Draw();
            Description.Draw();
            foreach (var icon in icons)
            {
                icon.Draw();
            }
            if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            if (blendEnabled) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }

    /// <summary>
    /// Defines the per-segment properties used by the radial menu:  
    /// texture, icon size, index, display name, and description.
    /// </summary>
    public struct IconData
    {
        public string IconTexture;
        public float IconSize;
        public int IconIndex;
        public string IconName;
        public string IconDescription;
    }
}