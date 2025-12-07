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

### Example

Inside custom GamePanel OnLoad method:

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
