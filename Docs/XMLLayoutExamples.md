# Element Names and Meanings

- DisplayUnits: Determines the units used for scaling the elements by. May be one of the following.

1. Pixels
2. DPI

- Name: All Elements must have a unique name for the purposes of registering the element in the LayoutLoader. Individual elements can be retrieved via `Get<ElementType>("Name of element")`.

- Bounds: The rectangle that the element takes up. The value for Right must always be larger than the value for Left, and the value for Top must always be larger than the value for Bottom.

- Margin: This value is used for inheritors of the NinePatch class. It represents the physical offset of the inner slice edges from the outer edges.

- UVMargin: This value is used for inheritors of the NinePatch class. It represents the sample offset at the Margin position for sampling textures. It is unique for to the range being between 0 and 0.5.

- Text: Some elements display text. Edit the text to change what is displayed.

- Texture: The name of the background texture of the element. Most elements have a Texture, with Label being the exception.

- ColorRGB: A series of 3 values between 0 and 1, representing the Red, Green and Blue components of the color. These values are separated with commas.

- TextColorRGB: A series of 3 values between 0 and 1, representing the Red, Green and Blue components of the color used by the displayed Text. These values are separated with commas.

- IsVisible: Can be either True or False. Determines whether or not the element will be drawn or able to be interacted with.

- Anchor: Determines the origin that the coordinates given in Bounds are relative to. May be any one of the following or blank.

1. None
2. TopLeft
3. TopCenter
4. TopRight
5. CenterLeft
6. Center
7. CenterRight
8. BottomLeft
9. BottomCenter
10. BottomRight

- RollOverTime: The time taken in seconds for the RollOverColor to lerp to its full value.

- RollOverColorRGB: The Color in RGB format that hovering over the element will apply. Values are from 0-1 and separated by a comma.

- CheckedTexture: The texture displayed when the element is in its “checked” or active state. Used primarily by checkbox-style elements.

- CheckedColorRGB: The RGB color applied when the element is in its checked state. Values range from 0–1 and are separated by commas.

- Origin: Defines a point used as the element’s position reference instead of a bounding rectangle. Used for Labels and RadialMenus instead of Bounds.

1. X: The horizontal coordinate of the origin. Increases from the left of the screen to the right.
2. Y: The vertical coordinate of the origin. Increases from the bottom of the screen to the top.

- Size: Sets the size of the text or glyph used by the element, typically representing font size.

- Radius: Defines the radius used by circular components such as RadialMenu. Determines how large the circular layout extends from its origin.

- Alignment: Specifies how text or content is aligned horizontally within the element. Valid options are the following:

1. Left
2. Center
3. Right.

- ButtonTexture: The texture applied to the increment/decrement buttons of controls that contain embedded buttons, such as the NumericSpinner.

- ButtonTextColorRGB: The RGB color used for the text displayed on button components. Values are between 0 and 1, separated by commas.

- ButtonWidth: Sets the width of individual buttons in controls that use side buttons, such as the NumericSpinner.

- FillPercentage: The proportion of the element that should appear filled, ranging from 0 (empty) to 1 (fully filled). Applies to elements such as ProgressBar.

- FillTexture: The texture applied to the filled portion of a progress-based element.

- FillColorRGB: The RGB tint applied to the filled portion of a progress-based element. All values range from 0–1.

- IconInset: Defines the inward offset used when placing icons within a circular or segmented layout (e.g., RadialMenu slices).

- Title: Specifies the title text for elements that support labeled headers, such as panels or icons.

- TitleSize: Determines the font size used to render the title text.

- DescriptionSize: Determines the font size used to render descriptive or secondary text.

- TintExclusionRadius: Specifies a radius inside which certain tinting or color-overlay effects are not applied. Used primarily in radial components.

- ActivationKey: Defines the key that triggers or activates the component, such as opening a RadialMenu.

- ControlMode: Determines how the user interacts with the control. Mode options vary by component type (e.g., HoldAndDrag).

- Description: Secondary text describing the purpose or meaning of an icon or menu entry.

- ThumbTexture: The texture applied to the draggable “thumb” of scrollbars or sliders.

- ThumbColorRGB: The RGB tint applied to the scrollbar or slider thumb. Values range from 0–1.

- ThumbPosition: The normalized position of the thumb component relative to the track. Must be between 0 and 1.

- ThumbProportion: Specifies the size of the scrollbar thumb relative to the total scrollable content. Higher values make the thumb longer. A value of 1 means the Thumb is the same size as the track.

- TitleMargin: The offset between the panel’s outer boundary to below its title header. Used by panels and similar components.

- ScrollBarWidth: Sets the fixed width of a scrollbar used by scrollable components.

- ScrollBarTexture: The background or track texture for scrollbars inside special components like Panels.

- ScrollBarThumbTexture: The texture used by the scrollbar thumb element that moves along the track for scrollbars inside special components like Panels.

- TabHeight: Specifies the height of each tab header in a TabbedPanel.

- TabTexture: Defines the texture applied to tab headers.

- Tab: Represents a single tab inside a TabbedPanel. Contains its own layout rules and child UI elements.

# Component Examples

## BreadCrumb Example

```xml
<UI DisplayUnits="DPI">
    <Breadcrumb>
        <Name>Breadcrumb</Name>
        <Bounds>
            <Left>0</Left>
            <Top>10</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <Text>Home/Documents/Files</Text>
        <Texture>CheckboxEmpty</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <TextColorRGB>0, 0, 1</TextColorRGB>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </Breadcrumb>
</UI>
```

## Button Example

```xml
<UI DisplayUnits="DPI">
    <Button>
        <Name>Button</Name>
        <Bounds>
            <Left>0</Left>
            <Top>50</Top>
            <Right>100</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Text>Click Me</Text>
        <Texture>TestButton</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <TextColorRGB>1, 1, 1</TextColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <RollOverTime>0.3</RollOverTime>
        <RollOverColorRGB>0, 0.6, 1</RollOverColorRGB>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </Button>
</UI>
```

## CheckBox Example

```xml
<UI DisplayUnits="DPI">
    <Checkbox>
        <Name>Checkbox</Name>
        <Bounds>
            <Left>20</Left>
            <Top>50</Top>
            <Right>50</Right>
            <Bottom>20</Bottom>
        </Bounds>
        <Texture>CheckboxEmpty</Texture>
        <CheckedTexture>CheckboxFilled</CheckedTexture>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <ColorRGB>1, 1, 1</ColorRGB>
        <CheckedColorRGB>1, 1, 1</CheckedColorRGB>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </Checkbox>
</UI>
```

## Image Example

```xml
<UI DisplayUnits="DPI">
    <Image>
        <Name>Image</Name>
        <Bounds>
            <Left>0</Left>
            <Top>100</Top>
            <Right>100</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Texture>TestImage</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </Image>
</UI>
```

## Label Example

```xml
<UI DisplayUnits="DPI">
    <Label>
        <Name>Label</Name>
        <Origin>
            <X>10</X>
            <Y>1140</Y>
        </Origin>
        <Size>30</Size>
        <Text>Hello World</Text>
        <ColorRGB>1, 1, 1</ColorRGB>
        <Alignment>Left</Alignment>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </Label>
</UI>
```

## NinePatch Image Example

```xml
<UI DisplayUnits="DPI">
    <NinePatch>
        <Name>NinePatch</Name>
        <Bounds>
            <Left>0</Left>
            <Top>200</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Texture>NinePatchTexture</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </NinePatch>
</UI>
```

## Numeric Spinner Example

```xml
<UI DisplayUnits="DPI">
    <NumericSpinner>
        <Name>NumericSpinner</Name>
        <Bounds>
            <Left>0</Left>
            <Top>250</Top>
            <Right>100</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Text>0</Text>
        <Texture>TestButton</Texture>
        <ButtonTexture>TestButton</ButtonTexture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <TextColorRGB>0, 0, 0</TextColorRGB>
        <ButtonTextColorRGB>0, 0, 0</ButtonTextColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <ButtonWidth>20</ButtonWidth>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </NumericSpinner>
</UI>
```

## Progress Bar Example

```xml
<UI DisplayUnits="DPI">
    <ProgressBar>
        <Name>ProgressBar</Name>
        <Bounds>
            <Left>0</Left>
            <Top>300</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <FillPercentage>0.5</FillPercentage>
        <Texture>TestButton</Texture>
        <FillTexture>TestButton</FillTexture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <FillColorRGB>1, 1, 1</FillColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </ProgressBar>
</UI>
```

## Radial Menu Example

```xml
<UI DisplayUnits="DPI">
    <RadialMenu>
        <Name>RadialMenu</Name>
        <Origin>
            <X>100</X>
            <Y>100</Y>
        </Origin>
        <Radius>75</Radius>
        <Texture>TestButton</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <HoverColorRGB>0, 1, 0</HoverColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IconInset>20</IconInset>
        <TitleSize>25</TitleSize>
        <DescriptionSize>20</DescriptionSize>
        <TintExclusionRadius>35</TintExclusionRadius>
        <ActivationKey>Tab</ActivationKey>
        <ControlMode>HoldAndDrag</ControlMode>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
        <IconData>
            <Icon>
                <Title>Icon1</Title>
                <Description>First Icon</Description>
                <Texture>IconTexture1</Texture>
                <Size>30</Size>
            </Icon>
            <Icon>
                <Title>Icon2</Title>
                <Description>Second Icon</Description>
                <Texture>IconTexture2</Texture>
                <Size>30</Size>
            </Icon>
        </IconData>
    </RadialMenu>
</UI>
```

## ScrollBar Example

```xml
<UI DisplayUnits="DPI">
    <ScrollBar>
        <Name>ScrollBar</Name>
        <Bounds>
            <Left>0</Left>
            <Top>400</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Width>12</Width>
        <Texture>CheckboxEmpty</Texture>
        <ThumbTexture>TestButton</ThumbTexture>
        <ThumbColorRGB>1, 1, 1</ThumbColorRGB>
        <ThumbPosition>0</ThumbPosition>
        <ThumbProportion>1</ThumbProportion>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </ScrollBar>
</UI>
```

## Slider Example

```xml
<UI DisplayUnits="DPI">
    <Slider>
        <Name>Slider</Name>
        <Bounds>
            <Left>0</Left>
            <Top>450</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Value>0</Value>
        <Texture>TestButton</Texture>
        <ThumbTexture>TestButton</ThumbTexture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <ThumbColorRGB>1, 1, 1</ThumbColorRGB>
        <ThumbPosition>0.5</ThumbPosition>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </Slider>
</UI>
```

## TextField Example

```xml
<UI DisplayUnits="DPI">
    <TextField>
        <Name>TextField</Name>
        <Bounds>
            <Left>0</Left>
            <Top>500</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Text>Enter text...</Text>
        <Texture>CheckboxEmpty</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <TextColorRGB>0, 0, 0</TextColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </TextField>
</UI>
```

# Panels

## DynamicPanel Example

```xml
<UI DisplayUnits="DPI">
    <DynamicPanel>
        <Name>DynamicPanel</Name>
        <Bounds>
            <Left>0</Left>
            <Top>600</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <TitleMargin>20</TitleMargin>
        <Title>Panel</Title>
        <Texture>TestButton</Texture>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <ScrollBarWidth>12</ScrollBarWidth>
        <ScrollBarTexture>CheckboxEmpty</ScrollBarTexture>
        <ScrollBarThumbTexture>TestButton</ScrollBarThumbTexture>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
        <!-- Pick one Layout. If you have multiple Layouts the first one in the xml file will be used. -->
        <ConstraintLayout>
            <Constraint>element[0].left = panelleft + contentmargin</Constraint>
            <Constraint>element[0].top = paneltop - contentmargin - titlemargin</Constraint>
            <Constraint>element[0].right = scrollbarleft - contentmargin</Constraint>
            <Constraint>element[0].bottom = paneltop - contentmargin - titlemargin - 30</Constraint>
        </ConstraintLayout>
        <VerticalLayout>
            <Size>25</Size>
            <Spacing>15</Spacing>
        </VerticalLayout>
        <TextField>
            <Name>TextField</Name>
            <Bounds>
                <Left>0</Left>
                <Bottom>0</Bottom>
                <Right>10</Right>
                <Top>10</Top>
            </Bounds>
            <Text>Type Here</Text>
            <Texture>CheckboxEmpty</Texture>
            <Margin>10</Margin>
            <UVMargin>0.25</UVMargin>
        </TextField>
    </DynamicPanel>
</UI>
```

## Panel Example

```xml
<UI DisplayUnits="DPI">
    <Panel>
        <Name>Panel</Name>
        <Title>Panel</Title>
        <Bounds>
            <Left>0</Left>
            <Top>550</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <TitleMargin>20</TitleMargin>
        <Texture>TestButton</Texture>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <ScrollBarWidth>12</ScrollBarWidth>
        <ScrollBarTexture>CheckboxEmpty</ScrollBarTexture>
        <ScrollBarThumbTexture>TestButton</ScrollBarThumbTexture>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
        <ConstraintLayout>
            <Constraint>element[0].left = panelleft + contentmargin</Constraint>
            <Constraint>element[0].top = paneltop - contentmargin - titlemargin</Constraint>
            <Constraint>element[0].right = scrollbarleft - contentmargin</Constraint>
            <Constraint>element[0].bottom = paneltop - contentmargin - titlemargin - 30</Constraint>
        </ConstraintLayout>
        <VerticalLayout>
            <Size>25</Size>
            <Spacing>15</Spacing>
        </VerticalLayout>
        <TextField>
            <Name>TextField</Name>
            <Bounds>
                <Left>0</Left>
                <Bottom>0</Bottom>
                <Right>10</Right>
                <Top>10</Top>
            </Bounds>
            <Text>Type Here</Text>
            <Texture>CheckboxEmpty</Texture>
            <Margin>10</Margin>
            <UVMargin>0.25</UVMargin>
        </TextField>
    </Panel>
</UI>
```

## Tabbed Panel Example

```xml
<UI DisplayUnits="DPI">
    <TabbedPanel>
        <Name>TabbedPanel</Name>
        <Bounds>
            <Left>-200</Left>
            <Top>200</Top>
            <Right>200</Right>
            <Bottom>-200</Bottom>
        </Bounds>
        <Anchor>Center</Anchor>
        <Texture>TestButton</Texture>
        <Margin>15</Margin>
        <UVMargin>0.35</UVMargin>
        <IsVisible>True</IsVisible>
        <ScrollBarWidth>12</ScrollBarWidth>
        <ScrollBarTexture>CheckboxEmpty</ScrollBarTexture>
        <ScrollBarThumbTexture>TestButton</ScrollBarThumbTexture>
        <TabHeight>25</TabHeight>
        <TabTexture>TestButton</TabTexture>

        <Tab>
            <Text>Click</Text>
            <ConstraintLayout>
                <Constraint>element[0].left = panelleft + contentmargin</Constraint>
                <Constraint>element[0].top = paneltop - contentmargin</Constraint>
                <Constraint>element[0].right = scrollbarleft - contentmargin</Constraint>
                <Constraint>element[0].bottom = paneltop - contentmargin - 30</Constraint>
            </ConstraintLayout>

            <Button>
                <Name>A Button</Name>
                <Bounds>
                    <Left>0</Left>
                    <Bottom>0</Bottom>
                    <Right>10</Right>
                    <Top>10</Top>
                </Bounds>
                <Margin>12</Margin>
                <UVMargin>0.5</UVMargin>
                <Text>Play</Text>
                <Texture>TestButton</Texture>
                <ColorRGB>0.2, 0.6, 0.9</ColorRGB>
                <TextColorRGB>1, 1, 1</TextColorRGB>
                <RollOverColorRGB>0.3, 0.7, 1</RollOverColorRGB>
                <RollOverTime>0.4</RollOverTime>
            </Button>
        </Tab>

        <Tab>
            <Text>Type</Text>
            <VerticalLayout>
                <Size>25</Size>
                <Spacing>15</Spacing>
            </VerticalLayout>
            <TextField>
                <Name>TextField1</Name>
                <Bounds>
                    <Left>0</Left>
                    <Bottom>0</Bottom>
                    <Right>10</Right>
                    <Top>10</Top>
                </Bounds>
                <Margin>12</Margin>
                <UVMargin>0.5</UVMargin>
                <Text>Play</Text>
                <Texture>TestButton</Texture>
                <ColorRGB>0.2, 0.6, 0.9</ColorRGB>
                <TextColorRGB>1, 1, 1</TextColorRGB>
            </TextField>
            <TextField>
                <Name>TextField2</Name>
                <Bounds>
                    <Left>0</Left>
                    <Bottom>0</Bottom>
                    <Right>10</Right>
                    <Top>10</Top>
                </Bounds>
                <Margin>12</Margin>
                <UVMargin>0.5</UVMargin>
                <Text>Play</Text>
                <Texture>TestButton</Texture>
                <ColorRGB>0.2, 0.6, 0.9</ColorRGB>
                <TextColorRGB>1, 1, 1</TextColorRGB>
            </TextField>
        </Tab>
    </TabbedPanel>
</UI>
```

# Picker Examples

## File Picker Example

```xml
<UI DisplayUnits="DPI">
    <FilePicker>
        <Name>FilePicker</Name>
        <Bounds>
            <Left>0</Left>
            <Top>700</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Texture>TestButton</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <ScrollBarWidth>12</ScrollBarWidth>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
    </FilePicker>
</UI>
```

## Color Picker Example

```xml
<UI DisplayUnits="DPI">
    <ColorPicker>
        <Name>ColorPicker</Name>
        <Bounds>
            <Left>0</Left>
            <Top>750</Top>
            <Right>200</Right>
            <Bottom>0</Bottom>
        </Bounds>
        <Texture>TestButton</Texture>
        <ColorRGB>1, 1, 1</ColorRGB>
        <Margin>10</Margin>
        <UVMargin>0.25</UVMargin>
        <ScrollBarWidth>12</ScrollBarWidth>
        <IsVisible>True</IsVisible>
        <Anchor>Center</Anchor>
        <ScrollBarWidth>12</ScrollBarWidth>
    </ColorPicker>
</UI>
```
