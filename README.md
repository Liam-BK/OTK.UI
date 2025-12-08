# OTK.UI

A cross platform UI library for OpenTK with embedded resource support.

## Features

- Buttons, Sliders, Labels, Panels and more.
- UI layouts defined in XML.
- Asset agnostic. Works with both embedded and disk files.
- Compatible with OpenTK 4+.

## Getting Started

1. Clone or download the repository.
2. Add OTK.UI as a project reference in your OpenTK project.
3. Make sure your project has the necessary textures, fonts and XML layouts you wish to use in your user interface. OTK.UI does not come pre-packaged with those resources.

## Dependencies

OTK.UI depends on a number of third party libraries.

- [OpenTK version 4.9.4](https://www.nuget.org/packages/OpenTK/)
- [StbImageSharp version 2.30.15](https://www.nuget.org/packages/StbImageSharp)
- [StbTrueTypeSharp version 1.26.12](https://www.nuget.org/packages/StbTrueTypeSharp)
- [Syntellect.Typography.OpenFont.Net6 version 1.0.0](https://www.nuget.org/packages/Syntellect.Typography.OpenFont.Net6)

Make sure these dependencies are installed via NuGet in your project before using OTK.UI

## Contributing

Contributions to OTK.UI are welcome. You can help by fixing bugs, adding features, improving documentation or providing example layouts

### How to Contribute

1. **Fork the repository** and clone it locally.

```bash
# Replace your_username with your GitHub username.
git clone https://github.com/your_username/otk.ui.git
cd OTK.UI
```

2. Create a new branch for your changes

```bash
git checkout -b feature/my_change
```

3. Make your changes

- Follow the existing code style
- Test embedded resources, layouts and UI components
- Update documentation as necessary

4. Commit and Push your branch

```bash
git add .
git commit -m "Add a brief description of your changes here."
git push origin feature/my_change
```

5. Open a Pull request against the main repository. Include a description of your changes and any relevant examples.

## Reporting issues

- Report bugs or issues under the Issues tab with clear steps to reproduce.
- Feature requests and suggestions are welcome as well.

### Example

Inside your custom GamePanel OnLoad method:

```csharp
UIBase.Window = this;
TextureManager.LoadTexture("ButtonTexture.png");
FontManager.LoadFont("DefaultFont.ttf");
LayoutLoader loader = new LayoutLoader("LoadedLayout.xml");
var button = loader.Get<Button>("ExitButton");
if (button is not null)
{
    button.Released += (MouseButton) =>
    {
        UIBase.Window?.Close();
    };
}
```
