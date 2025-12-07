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
3. Make sure your project has the textures, fonts and XML layouts. OTK.UI does not come pre-packaged with those resources.

## Dependencies

OTK.UI depends on a number of third party libraries.

- [OpenTK version 4.9.4](https://www.nuget.org/packages/OpenTK/)
- [StbImageSharp version 2.30.15](https://www.nuget.org/packages/StbImageSharp)
- [StbTrueTypeSharp version 1.26.12](https://www.nuget.org/packages/StbTrueTypeSharp)
- [Syntellect.Typography.OpenFont.Net6 version 1.0.0](https://www.nuget.org/packages/Syntellect.Typography.OpenFont.Net6)

Make sure these dependencies are installed via NuGet in your project before using OTK.UI

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
