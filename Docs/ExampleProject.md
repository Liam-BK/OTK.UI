# Example Project

This example demonstrates loading a UI from XML, handling input events, opening a file picker by clicking a button, and animating a progress bar using LineDSL.

## Program

```csharp
using OpenTK.Windowing.Desktop;

public static class Program
{
    public static void Main()
    {
        Directory.SetCurrentDirectory(Path.Combine(AppContext.BaseDirectory, @"../../../"));
        var gameSettings = GameWindowSettings.Default;
        var nativeSettings = new NativeWindowSettings()
        {
            Title = "UI Editor",
            StencilBits = 8
        };

        using var window = new MainPanel(gameSettings, nativeSettings);
        window.Run();
    }
}
```

## MainPanel

```csharp
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OTK.UI.Components;
using OTK.UI.Pickers;
using OTK.UI.Utility;
using OTK.UI.Managers;

public class MainPanel : GameWindow
{
    private LayoutLoader? loader = null;
    private LineDSL dsl = new LineDSL();
    private Label? label = null;
    public MainPanel(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        UIBase.Window = this;
        WindowState = WindowState.Fullscreen;
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.2f, 0.2f, 0.2f, 1);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.DepthTest);
        //set a component's Texture property to the TextureName to use it.
        TextureManager.LoadTexture("PathToImage.png", "TextureName", true);
        FontManager.LoadFont("2DLevelEditor.Fonts.DejaVuSans.ttf");
        loader = new LayoutLoader("2DLevelEditor.LayoutXML.Example.xml");
        var progressBar = loader.Get<ProgressBar>("ProgressBar");
        if (progressBar is not null)
        {
            dsl.AddLambdaRef("progress",
                (
                    () => progressBar.FillPercentage,
                    val => progressBar.FillPercentage = (float)val
                )
            );
        }
        var button = loader.Get<Button>("FileDialog");
        if (button is not null)
        {
            button.Released += MouseButton =>
            {
                var picker = loader?.Get<FilePicker>("FilePicker");
                if (picker is not null && picker.IsVisible) return;
                picker?.SelectFile().ContinueWith(task =>
                {
                    string[]? result = task.Result;
                    if (result != null)
                    {
                        foreach (var value in result)
                        {
                            Console.WriteLine($"confirmed: {value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("canceled");
                    }
                });
            };
        }
        label = loader.Get<Label>("Label");
        // initializing LineDSL variables
        dsl.Evaluate("var t = 0");
        dsl.Evaluate("var temp = 0");
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        UIBase.OnResize();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        loader?.OnKeyDown(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        loader?.OnTextInput(e);
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);
        loader?.OnKeyUp(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        loader?.OnClickDown(MouseState);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        UIBase.ResetCursorDisplay();
        loader?.OnMouseMove(MouseState);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        loader?.OnClickUp(MouseState);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        loader?.OnMouseWheel(MouseState);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        float delta = (float)args.Time;
        if (label is not null)
        {
            label.Text = $"t = {dsl.Evaluate("t = leqz(t - 359) ? t + 1 : t - 359")}";
        }
        else
        {
            dsl.Evaluate("t = leqz(t - 359) ? t + 1 : t - 359");
        }
        dsl.Evaluate("temp = degtorad(t)");
        //Animate progressBar with LineDSL driven sine wave.
        //The variable "progress" is directly linked to the ProgressBar's FillPercentage property.
        dsl.Evaluate("progress = (sin(temp) + 1) * 0.5");
        loader?.OnUpdate(delta, MouseState, KeyboardState);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        DrawUI();
        SwapBuffers();
    }

    public override void Close()
    {
        TextureManager.DeleteAll();
        base.Close();
    }

    private void DrawUI()
    {
        loader?.Draw();
    }
}
```

## Layout.xml

```xml
<UI DisplayUnits="DPI">
    <ProgressBar>
        <Name>ProgressBar</Name>
        <Bounds>
            <Left>-200</Left>
            <Top>20</Top>
            <Right>200</Right>
            <Bottom>-20</Bottom>
        </Bounds>
        <!-- FillPercentage is driven by LineDSL in the code example. -->
        <FillPercentage>0.0</FillPercentage>
        <ColorRGB>1, 1, 1</ColorRGB>
        <FillColorRGB>0, 1, 0</FillColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </ProgressBar>

    <Button>
        <Name>FileDialog</Name>
        <Bounds>
            <Left>20</Left>
            <Top>-20</Top>
            <Right>120</Right>
            <Bottom>-60</Bottom>
        </Bounds>
        <Text>Files</Text>
        <Texture>TestButton</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <TextColorRGB>1, 1, 1</TextColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <RollOverTime>0.3</RollOverTime>
        <RollOverColorRGB>0, 0.6, 1</RollOverColorRGB>
        <IsVisible>True</IsVisible>
        <Anchor>TopLeft</Anchor>
    </Button>

    <FilePicker>
        <Name>FilePicker</Name>
        <Bounds>
            <Left>0</Left>
            <Top>400</Top>
            <Right>400</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <ColorRGB>1, 1, 1</ColorRGB>
        <ScrollBarWidth>12</ScrollBarWidth>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IsVisible>False</IsVisible>
        <Anchor>Center</Anchor>
    </FilePicker>

    <Label>
        <Name>Label</Name>
        <Origin>
            <X>0</X>
            <Y>-30</Y>
        </Origin>
        <Size>30</Size>
        <Text>text</Text>
        <ColorRGB>1, 1, 1</ColorRGB>
        <Alignment>Center</Alignment>
        <IsVisible>True</IsVisible>
        <Anchor>TopCenter</Anchor>
    </Label>
</UI>
```

## Additional notes

- Textures and fonts are provided by the consuming application.
- UI layout is defined entirely in XML and loaded at runtime.
- LineDSL variables can be bound directly to component properties via lambda references.
