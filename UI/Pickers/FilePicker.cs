using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OTK.UI.Components;
using OTK.UI.Containers;
using OTK.UI.Interfaces;
using OTK.UI.Layouts;
using OTK.UI.Managers;
using OTK.UI.Utility;

namespace OTK.UI.Pickers
{
    /// <summary>
    /// A UI panel that lets the user browse directories, filter names,
    /// select single or multiple files, and confirm or cancel the selection.
    /// Integrates with the layout system and supports loading from XML.
    /// </summary>
    public class FilePicker : DynamicPanel
    {
        /// <summary>
        /// Text field used to filter visible files and folders.
        /// </summary>
        public TextField searchbar;

        /// <summary>
        /// Panel containing quick-access directory shortcuts
        /// (Desktop, Documents, Downloads, etc.).
        /// </summary>
        public Panel quickAccess;

        /// <summary>
        /// Panel containing the active directory's file and folder entries.
        /// </summary>
        public Panel currentFolder;

        /// <summary>
        /// Button that cancels the picker operation.
        /// </summary>
        public Button cancel;

        /// <summary>
        /// Button that confirms the picker and returns selected files.
        /// </summary>
        public Button confirm;

        /// <summary>
        /// Breadcrumb navigation controlling the current directory path.
        /// </summary>
        public BreadCrumb currentPath;

        /// <summary>
        /// Enables multi-selection when true (Ctrl/Command held).
        /// </summary>
        public bool multiSelect = false;

        private List<string> constraints = [
            //searchbar
            "element[0].left = panelleft + contentmargin",
            "element[0].top = paneltop - contentmargin - titlemargin",
            "element[0].right = panelleft + contentmargin + 150",
            "element[0].bottom = paneltop - contentmargin - titlemargin - 30",
            //quickAccess
            "element[1].left = panelleft + contentmargin",
            "element[1].top = paneltop - contentmargin - titlemargin - 40",
            "element[1].right = panelleft + contentmargin + 150",
            "element[1].bottom = panelbottom + contentmargin + 40",
            //cancel
            "element[2].left = scrollbarleft - contentmargin - 170",
            "element[2].top = panelbottom + contentmargin + 30",
            "element[2].right = scrollbarleft - contentmargin - 90",
            "element[2].bottom = panelbottom + contentmargin",
            //confirm
            "element[3].left = scrollbarleft - contentmargin - 80",
            "element[3].top = panelbottom + contentmargin + 30",
            "element[3].right = scrollbarleft - contentmargin",
            "element[3].bottom = panelbottom + contentmargin",
            //currentFolder
            "element[4].left = panelleft + contentmargin + 160",
            "element[4].top = paneltop - contentmargin - titlemargin - 40",
            "element[4].right = scrollbarleft - contentmargin",
            "element[4].bottom = panelbottom + contentmargin + 40",
            //currentPath
            "element[5].left = panelleft + contentmargin + 160",
            "element[5].top = paneltop - contentmargin - titlemargin",
            "element[5].right = scrollbarleft - contentmargin",
            "element[5].bottom = paneltop - contentmargin - titlemargin - 30",
        ];

        private bool refreshPending = false;

        private readonly Vector3 directoryColour = new Vector3(1.0f, 0.9f, 0.5f);

        private readonly Vector3 fileColour = new Vector3(0.55f, 0.85f, 1.0f);

        /// <summary>
        /// Creates a new <see cref="FilePicker"/> with dynamic panel behavior,
        /// a search bar, quick-access shortcuts, a current-folder listing, and
        /// breadcrumb navigation.
        /// </summary>
        /// <param name="bounds">The 4-value bounding box of the picker.</param>
        /// <param name="scrollBarWidth">Width of the scrollbar (hidden by default).</param>
        /// <param name="inset">Inset/margin for child elements.</param>
        /// <param name="uvInset">Texture UV inset for visual padding.</param>
        /// <param name="colour">Optional background colour.</param>
        public FilePicker(Vector4 bounds, float scrollBarWidth, float inset, float uvInset, Vector3? colour = null) : base(bounds, scrollBarWidth, inset, uvInset, colour)
        {
            ScrollbarVisibilityType = ScrollbarVisibility.Never;
            scrollbar.IsVisible = false;
            InitializeLayout();
            searchbar = new TextField(new Vector4(), inset * 0.5f, uvInset, Colour);
            searchbar.OnTextChanged += newText =>
            {
                RefreshCurrentFolder();
            };
            Add(searchbar);
            quickAccess = new Panel(new Vector4(), scrollBarWidth, inset, uvInset, Colour);
            var quickAccessLayout = new VerticalLayout();
            quickAccessLayout.ElementHeight = 25f;
            quickAccessLayout.Spacing = 0f;
            quickAccess.ApplicableLayout = quickAccessLayout;
            var desktop = FileReference.SetUpFileRef(FileReference.Directories.Desktop);
            quickAccess.Add(desktop);
            var documents = FileReference.SetUpFileRef(FileReference.Directories.Documents);
            quickAccess.Add(documents);
            var downloads = FileReference.SetUpFileRef(FileReference.Directories.Downloads);
            quickAccess.Add(downloads);
            var music = FileReference.SetUpFileRef(FileReference.Directories.Music);
            quickAccess.Add(music);
            var pictures = FileReference.SetUpFileRef(FileReference.Directories.Pictures);
            quickAccess.Add(pictures);
            var videos = FileReference.SetUpFileRef(FileReference.Directories.Videos);
            quickAccess.Add(videos);
            Add(quickAccess);
            cancel = new Button(new Vector4(), inset, uvInset, "Cancel");
            Add(cancel);
            confirm = new Button(new Vector4(), inset, uvInset, "Confirm");
            Add(confirm);
            currentFolder = new Panel(new Vector4(), scrollBarWidth, inset, uvInset, Colour);
            var currentFolderLayout = new VerticalLayout();
            currentFolderLayout.ElementHeight = 25f;
            currentFolderLayout.Spacing = 0f;
            currentFolder.ApplicableLayout = currentFolderLayout;
            Add(currentFolder);
            currentPath = new BreadCrumb(new Vector4(), inset, uvInset, Colour);
            currentPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Add(currentPath);
            desktop.Pressed += MouseButton =>
            {
                currentPath.Text = desktop.ReferencePath;
                desktop.selected = true;
                refreshPending = true;
            };
            documents.Pressed += MouseButton =>
            {
                currentPath.Text = documents.ReferencePath;
                documents.selected = true;
                refreshPending = true;
            };
            downloads.Pressed += MouseButton =>
            {
                currentPath.Text = downloads.ReferencePath;
                downloads.selected = true;
                refreshPending = true;
            };
            music.Pressed += MouseButton =>
            {
                currentPath.Text = music.ReferencePath;
                music.selected = true;
                refreshPending = true;
            };
            pictures.Pressed += MouseButton =>
            {
                currentPath.Text = pictures.ReferencePath;
                pictures.selected = true;
                refreshPending = true;
            };
            videos.Pressed += MouseButton =>
            {
                currentPath.Text = videos.ReferencePath;
                videos.selected = true;
                refreshPending = true;
            };
            currentPath.OnNavigate += MouseButton =>
            {
                RefreshCurrentFolder();
            };
            RefreshCurrentFolder();
        }

        /// <summary>
        /// Loads a <see cref="FilePicker"/> from an XML element as part of the layout system.
        /// </summary>
        /// <param name="element">The XML element describing the FilePicker.</param>
        /// <returns>A fully configured FilePicker.</returns>
        /// <exception cref="FormatException">
        /// Thrown if required XML fields are missing or malformed.
        /// </exception>
        public static new FilePicker Load(Dictionary<string, IUIElement> registry, XElement element)
        {
            var name = element.Element("Name")?.Value.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) throw new FormatException("All elements must have a unique name");
            var bounds = element.Element("Bounds");
            if (bounds is null) throw new FormatException($"FilePicker: {name} is missing required field Bounds.");
            var minSize = element.Element("MinSize");

            var margin = float.Parse(element.Element("Margin")?.Value ?? "10", CultureInfo.InvariantCulture);
            var uvMargin = float.Parse(element.Element("UVMargin")?.Value ?? "0.5", CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(element.Element("IsVisible")?.Value ?? "True");
            var texture = element.Element("Texture")?.Value.Trim() ?? string.Empty;
            var color = element.Element("ColorRGB")?.Value ?? "1, 1, 1";
            var scrollbarWidth = float.Parse(element.Element("ScrollBarWidth")?.Value ?? "10", CultureInfo.InvariantCulture);
            var anchor = element.Element("Anchor")?.Value.ToLower() ?? "none";

            var left = float.Parse(bounds?.Element("Left")?.Value ?? "0", CultureInfo.InvariantCulture);
            var bottom = float.Parse(bounds?.Element("Bottom")?.Value ?? "0", CultureInfo.InvariantCulture);
            var right = float.Parse(bounds?.Element("Right")?.Value ?? "100", CultureInfo.InvariantCulture);
            var top = float.Parse(bounds?.Element("Top")?.Value ?? "100", CultureInfo.InvariantCulture);

            var minWidth = float.Parse(minSize?.Element("MinWidth")?.Value ?? "100", CultureInfo.InvariantCulture);
            var minHeight = float.Parse(minSize?.Element("MinHeight")?.Value ?? "100", CultureInfo.InvariantCulture);

            var colorVec = LayoutLoader.ParseVector3(color, name);

            if (!LayoutLoader.RelativeOrigins.TryGetValue(anchor, out var anchorResult)) anchorResult = LayoutLoader.RelativeOrigin.None;
            var relativeAnchorVector = LayoutLoader.GetRelativeOrigin(anchorResult);

            FilePicker filePicker = new FilePicker(new Vector4(left, bottom, right, top) + relativeAnchorVector, scrollbarWidth, margin, uvMargin);
            filePicker.IsVisible = isVisible;
            filePicker.Colour = colorVec;
            filePicker.MinimumWidth = minWidth;
            filePicker.MinimumHeight = minHeight;
            if (LayoutLoader.IsFilePath(texture))
            {
                TextureManager.LoadTexture(texture, Path.GetFileNameWithoutExtension(texture));
                filePicker.Texture = Path.GetFileNameWithoutExtension(texture);
            }
            else filePicker.Texture = texture;

            if (registry.ContainsKey(name)) throw new ArgumentException($"An element with name: {name} has already been registered.");
            registry.Add(name, filePicker);
            return filePicker;
        }

        /// <summary>
        /// Handles mouse click-down events, manages refresh timing,
        /// and determines search bar focus state.
        /// </summary>
        public override void OnClickDown(MouseState mouse)
        {
            base.OnClickDown(mouse);
            if (refreshPending)
            {
                RefreshCurrentFolder();
                refreshPending = false;
            }
            if (!WithinBounds(ConvertMouseScreenCoords(mouse.Position))) searchbar.IsFocused = false;
        }

        /// <summary>
        /// Updates multi-select state based on keyboard modifiers
        /// (Ctrl/Command) when a key is pressed.
        /// </summary>
        public override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            multiSelect = !searchbar.IsFocused && (e.Command || e.Control);
        }

        /// <summary>
        /// Updates multi-select state based on keyboard modifiers
        /// when a key is released.
        /// </summary>
        public override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            multiSelect = !searchbar.IsFocused && (e.Command || e.Control);
        }

        private void SetQuickAccessSelection()
        {
            foreach (var element in quickAccess.Elements)
            {
                if (element is FileReference fr)
                {
                    fr.selected = fr.ReferencePath.Trim(currentPath.Delimiter) == currentPath.Text.Trim(currentPath.Delimiter);
                }
            }
        }

        private void InitializeLayout()
        {
            var layout = new ConstraintLayout();
            layout.Constraints = constraints;
            ApplicableLayout = layout;
            InitializeConstraintVariables();
        }

        private void ClearSelected()
        {
            foreach (var element in currentFolder.Elements)
            {
                if (element is FileReference fr)
                {
                    fr.selected = false;
                }
            }
        }

        private void RefreshCurrentFolder()
        {
            if (!searchbar.IsFocused) searchbar.Text = "";
            var filter = searchbar.Text.Trim();
            currentFolder.Clear();
            try
            {
                var files = Directory.GetFiles(currentPath.Text);
                var directories = Directory.GetDirectories(currentPath.Text);
                if (directories is not null)
                {
                    Array.Sort(directories);
                    foreach (var directory in directories)
                    {
                        if (!string.IsNullOrEmpty(filter) && !Path.GetFileName(directory).Contains(filter, StringComparison.OrdinalIgnoreCase))
                            continue;
                        var directoryRef = FileReference.SetUpFileRef(directory);
                        directoryRef.Colour = directoryColour;
                        currentFolder.Add(directoryRef);
                        directoryRef.DoubleClick += MouseButton =>
                        {
                            currentPath.Text = directoryRef.ReferencePath;
                            refreshPending = true;
                        };
                        directoryRef.Pressed += MouseButton =>
                        {
                            if (!multiSelect) ClearSelected();
                            directoryRef.selected = true;
                        };
                    }
                }
                if (files is not null)
                {
                    Array.Sort(files);
                    foreach (var file in files)
                    {
                        if (!string.IsNullOrEmpty(filter) && !Path.GetFileName(file).Contains(filter, StringComparison.OrdinalIgnoreCase))
                            continue;
                        var fileRef = FileReference.SetUpFileRef(file);
                        fileRef.Colour = fileColour;
                        currentFolder.Add(fileRef);

                        fileRef.Pressed += MouseButton =>
                        {
                            if (!multiSelect) ClearSelected();
                            fileRef.selected = true;
                        };
                    }
                }
                SetQuickAccessSelection();
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"User does not have access to this file location: {e.Message}");
            }
        }

        private string[] GetSelectedPaths()
        {
            var list = new List<string>();
            foreach (var element in currentFolder.Elements)
            {
                if (element is FileReference fr && fr.selected)
                {
                    list.Add(fr.ReferencePath);
                }
            }
            return list.ToArray();
        }

        private Task<string[]?> GetPathFromPicker()
        {
            var tcs = new TaskCompletionSource<string[]?>();

            IsVisible = true;

            void CancelHandler(MouseButton btn)
            {
                tcs.TrySetResult(null);
                Cleanup();
                IsVisible = false;
            }

            void ConfirmHandler(MouseButton btn)
            {
                tcs.TrySetResult(GetSelectedPaths());
                Cleanup();
                IsVisible = false;
            }

            void Cleanup()
            {
                cancel.Released -= CancelHandler;
                confirm.Released -= ConfirmHandler;
            }

            cancel.Released += CancelHandler;
            confirm.Released += ConfirmHandler;

            return tcs.Task;
        }

        /// <summary>
        /// Asynchronously displays the picker and waits until the user
        /// confirms or cancels the selection.
        /// </summary>
        /// <returns>
        /// A string array of selected file paths, or null if the user canceled.
        /// </returns>
        public async Task<string[]?> SelectFile()
        {
            return await GetPathFromPicker();
        }
    }

    /// <summary>
    /// Represents a file or directory entry inside a FilePicker.
    /// Acts like a selectable button with optional double-click detection.
    /// </summary>
    internal class FileReference : Button
    {
        public enum Directories
        {
            Recent,
            Desktop,
            Documents,
            Downloads,
            Music,
            Pictures,
            Videos,
        }

        /// <summary>
        /// Filesystem path that this reference represents.
        /// May be a file or directory.
        /// </summary>
        public string ReferencePath
        {
            get;
            set;
        } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        private static readonly double DoubleClickThreshold = 300; // milliseconds
        private readonly Stopwatch clickTimer = new Stopwatch();
        private bool clickPending = false;

        /// <summary>
        /// Fired when the user double-clicks this file reference.
        /// </summary>
        public event Action<MouseButton>? DoubleClick;

        /// <summary>
        /// True when the entry is currently selected.
        /// </summary>
        public bool selected = false;

        private Vector3 SelectionColor = new Vector3(0.2f, 0.6f, 1.0f);

        private static int defaultProgram = 0;

        /// <summary>
        /// Creates a <see cref="FileReference"/> with the given bounds and label.
        /// Used for both files and directories inside FilePicker.
        /// </summary>
        public FileReference(Vector4 bounds, float margin = 10, float uvMargin = 0.5f, string text = "", Vector3? colour = null) : base(bounds, margin, uvMargin, text, colour)
        {
            label.Alignment = Label.TextAlign.Left;
            label.Origin = new Vector2(Bounds.X + Inset, label.Origin.Y);
            program = defaultProgram > 0 ? defaultProgram : ShaderManager.CreateShader("OTK.UI.Shaders.Vertex.NinePatch.vert", "OTK.UI.Shaders.Fragment.NinePatch.frag");
            if (defaultProgram <= 0) defaultProgram = program;
        }

        public override void UpdateBounds()
        {
            base.UpdateBounds();
            if (label is not null) label.Origin = new Vector2(Bounds.X + Inset, label.Origin.Y);
        }

        /// <summary>
        /// Handles click detection and double-click timing logic.
        /// </summary>
        public override void OnClickDown(MouseState mouse)
        {
            if (!IsVisible) return;
            if (!WithinBounds(ConvertMouseScreenCoords(mouse.Position))) return;
            if (clickPending && clickTimer.ElapsedMilliseconds <= DoubleClickThreshold)
            {
                clickPending = false;
                clickTimer.Reset();
                DoubleClick?.Invoke(MouseButton.Left);
                pressed = WithinBounds(ConvertMouseScreenCoords(mouse.Position));
                if (!pressed) return;
            }
            else
            {
                clickPending = true;
                clickTimer.Restart();
                base.OnClickDown(mouse);
            }
        }

        /// <summary>
        /// Handles internal double-click timing expiration.
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            if (clickPending == true && clickTimer.ElapsedMilliseconds > DoubleClickThreshold)
            {
                clickTimer.Reset();
                clickPending = false;
            }
        }

        /// <summary>
        /// Returns true if the path points to an existing directory.
        /// </summary>
        public static bool IsDirectory(string path) => Directory.Exists(path);

        /// <summary>
        /// Returns true if this instance's ReferencePath is a directory.
        /// </summary>
        public bool IsDirectory() => Directory.Exists(ReferencePath);

        /// <summary>
        /// Returns true if the path points to an existing file.
        /// </summary>
        public static bool IsFile(string path) => File.Exists(path);

        /// <summary>
        /// Returns true if this instance's ReferencePath is a file.
        /// </summary>
        public bool IsFile() => File.Exists(ReferencePath);

        /// <summary>
        /// Creates a FileReference representing one of the well-known
        /// system directories (Desktop, Documents, etc.).
        /// </summary>
        public static FileReference SetUpFileRef(Directories directory)
        {
            FileReference temp = new FileReference(new Vector4());
            temp.SetPath(directory);
            temp.Text = GetNameOfPath(temp.ReferencePath);
            temp.TimeToRollover = 0;
            return temp;
        }

        /// <summary>
        /// Creates a FileReference representing a specific filesystem path.
        /// </summary>
        public static FileReference SetUpFileRef(string path)
        {
            FileReference temp = new FileReference(new Vector4());
            temp.ReferencePath = path;
            temp.Text = GetNameOfPath(temp.ReferencePath);
            temp.TimeToRollover = 0.0f;
            return temp;
        }

        private static string GetNameOfPath(string path)
        {
            if (Directory.Exists(path))
            {
                var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return Path.GetFileName(trimmed);
            }
            else if (File.Exists(path))
            {
                return Path.GetFileName(path);
            }
            else
            {
                Console.WriteLine($"not a directory or file: {path}");
                return "";
            }
        }

        /// <summary>
        /// Sets ReferencePath based on a preset directory enum.
        /// </summary>
        public void SetPath(Directories directory)
        {
            switch (directory)
            {
                case Directories.Recent:
                    ReferencePath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                    break;
                case Directories.Desktop:
                    ReferencePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    break;
                case Directories.Documents:
                    ReferencePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    break;
                case Directories.Downloads:
                    ReferencePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    break;
                case Directories.Music:
                    ReferencePath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                    break;
                case Directories.Pictures:
                    ReferencePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    break;
                case Directories.Videos:
                    ReferencePath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                    break;
            }
        }

        /// <summary>
        /// Passes colour and selection state uniforms into the shader.
        /// </summary>
        protected override void PassUniform()
        {
            base.PassUniform();
            PassUniform(Vector3.Lerp(Colour, RolloverColour, rolloverValue) * (pressed ? 0.5f : 1.0f) * (selected ? SelectionColor : Vector3.One), "colour");
        }
    }
}