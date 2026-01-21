using System.Xml.Linq;
using OpenTK.Mathematics;
using OTK.UI.Containers;
using OTK.UI.Interfaces;
using OTK.UI.Layouts;

public class GridLayout : Layout
{
    private int _columns = 1;
    public int Columns
    {
        get
        {
            return _columns;
        }
        set
        {
            _columns = Math.Max(value, 1);
        }
    }

    /// <summary>
    /// Loads a <see cref="GridLayout"/> from an XML layout definition.
    /// Expects optional <c>ElementHeight</c>, <c>Columns</c> and <c>Spacing</c> fields.
    /// </summary>
    /// <param name="element">The XML element describing the layout.</param>
    /// <returns>A configured <see cref="GridLayout"/> instance.</returns>
    public static new GridLayout Load(XElement element)
    {
        var layout = new GridLayout();
        var elementHeight = float.Parse(element.Element("ElementHeight")?.Value ?? "20");
        var columns = (int)Math.Floor(float.Parse(element.Element("Columns")?.Value ?? "1"));
        var spacing = float.Parse(element.Element("Spacing")?.Value ?? "0");
        layout.ElementHeight = elementHeight;
        layout.Columns = columns;
        layout.Spacing = spacing;
        return layout;
    }

    public override void Apply()
    {
        if (Parent is null) return;
        if (Parent is Panel panel)
        {
            ElementWidth = (panel.Width - 2 * panel.ContentMargin - Columns * Spacing) / Columns;
            if (ElementWidth <= 0) return;
            var left = panel.Bounds.X + panel.ContentMargin;
            var top = panel.Bounds.W - panel.TitleMargin - panel.ContentMargin;
            var right = panel.scrollbar.Bounds.X - panel.ContentMargin;
            PositionElements(panel.Elements, left, top, right);
        }
        else if (Parent is TabbedPanel tabbedPanel)
        {
            var left = tabbedPanel.Bounds.X + tabbedPanel.ContentMargin;
            var top = tabbedPanel.Bounds.W - tabbedPanel.TabHeight - tabbedPanel.ContentMargin;
            var right = tabbedPanel.scrollbar.Bounds.X - tabbedPanel.ContentMargin;
            PositionElements(tabbedPanel.TabElements[tabbedPanel.CurrentTab], left, top, right);
        }
        UpdateButtonTextSize();
    }

    private void PositionElements(List<IUIElement> elements, float left, float top, float right)
    {
        var column = 0;
        var row = 0;
        foreach (var element in elements)
        {
            if (left + column * (ElementWidth + Spacing) + ElementWidth > right && column > 0)
            {
                column = 0;
                row++;
            }
            element.Bounds = new Vector4(left + column * (ElementWidth + Spacing), top - (row * (ElementHeight + Spacing) + ElementHeight), left + column * (ElementWidth + Spacing) + ElementWidth, top - row * (ElementHeight + Spacing));
            column++;
        }
    }
}