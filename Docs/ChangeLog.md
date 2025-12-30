# Patch 1.1.0

- Added two new Layout types: HorizontalLayout and Vertical Layout.

- Changed the XML Schema to be more descriptive. VerticalLayout elements now use the <ElementWidth> tag in place of <Size>. This will likely break some projects, so make sure to replace the <Size> tag with <ElementWidth>.

# Patch 1.1.1

- Fixed Issue where where new layout types would not work with DynamicPanel.
