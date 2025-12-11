# Frequently Asked Questions

### Why isn't my component being displayed on screen?

- The `Draw()` function isn't being called in the OnRenderFrame method of OpenTK's GamePanel

### Why isn't text rendering on the screen?

- If your component is or contains a Label, you may need to download a .ttf file to your project and call `FontManager.LoadFont("Path\To\Font.ttf");` inside the OnLoad method of OpenTK's GamePanel. Labels do not work without having a font to draw.

### Why is my component not responding to clicks an other input?

- Make sure you call these methods on each component in the corresponding GamePanel override:

1. OnKeyDown()
2. OnTextInput()
3. OnKeyUp()
4. OnClickDown()
5. OnMouseMove()
6. OnClickUp()
7. OnMouseWheel()

### Why does my cursor look incorrect after hovering in certain areas?

- Make sure you call `UIBase.ResetCursorDisplay();` at the beginning of the overridden `OnMouseMove` method in the OpenTK GamePanel instance.

**Note:** `UIBase.ResetCursorDisplay()` does not work if the Window property of `UIBase` has not been set as the cursor effects rely on it.

### Why am I getting an error about duplicate names?

- All elements must have a unique Name. If two elements share the same one, the LayoutLoader will throw an error telling you exactly which name was duplicated in the provided xml file.

### Why is my textured component throwing an error?

- If the Texture value is set but the texture can’t be found in the atlas or texture manager, the system throws an error.

- If the Texture is not set at all, the element is drawn as a flat rectangle instead — this is the fallback behavior.

### Why does my UI glitch when parent and child sizes depend on each other?

- If two elements depend on each other’s size indirectly (like circular layout), the system does its best to resolve it, but the result may look glitchy or unstable.

- Try avoid writing LineDSL expressions that both read and write the same variable.

### Why isn't my scroll bar affecting anything?

- Scrollbars don't automatically control other elements. They output a value instead. It's up to your code to decide what to do with the provided value (e.g., to a scrollable panel’s offset).

### Why does my instantiated LayoutLoader say a referenced element can't be found?

- The name you referenced doesn't exist yet at the time it’s parsed (order matters),
  or the name doesn't match exactly (case-sensitive),
  or the element was removed or renamed.

### Why aren't my lambda-based properties updating?

- Make sure you call LineDSL's Evaluate method assigning a value to the name of the desired lambda variable. Otherwise the referenced value will not be modified.

### Why does my text kerning look weird?

- You’re probably using a font that depends on GPOS kerning, which the library doesn’t support yet.

- When this happens, the system falls back to an approximate kerning file, so spacing may look a bit off.

- If this limitation bugs you, contributions are very welcome!
