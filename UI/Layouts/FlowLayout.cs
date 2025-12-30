using System.Xml.Linq;
using OpenTK.Mathematics;
using OTK.UI.Containers;
using OTK.UI.Interfaces;

namespace OTK.UI.Layouts
{
    /// <summary>
    /// A simple vertical stacking layout that arranges UI elements from top to bottom
    /// inside its parent container. Each element is assigned a fixed height
    /// (<see cref="Size"/>) and separated by a configurable <see cref="Spacing"/>.
    /// 
    /// When used with a <see cref="Panel"/>, elements are positioned within the
    /// panel's content region, respecting its content margins and title bar height.
    /// 
    /// When used with a <see cref="TabbedPanel"/>, elements are positioned inside
    /// the currently active tab, below the tab strip, and constrained by its scrollbar.
    /// 
    /// This layout does not perform dynamic size measurement and does not update
    /// automatically per frame.
    /// </summary>
    public class FlowLayout : Layout
    {
        /// <summary>
        /// Loads a <see cref="VerticalLayout"/> from an XML layout definition.
        /// Expects optional <c>Size</c> and <c>Spacing</c> fields.
        /// </summary>
        /// <param name="element">The XML element describing the layout.</param>
        /// <returns>A configured <see cref="VerticalLayout"/> instance.</returns>
        public static new FlowLayout Load(XElement element)
        {
            var layout = new FlowLayout();
            var elementHeight = float.Parse(element.Element("ElementHeight")?.Value ?? "20");
            var elementWidth = float.Parse(element.Element("ElementWidth")?.Value ?? "20");
            var spacing = float.Parse(element.Element("Spacing")?.Value ?? "0");
            layout.ElementHeight = elementHeight;
            layout.ElementWidth = elementWidth;
            layout.Spacing = spacing;
            return layout;
        }

        /// <summary>
        /// Applies the vertical stacking algorithm to the elements contained
        /// in the parent. Supports <see cref="Panel"/> and <see cref="TabbedPanel"/>.
        /// Elements are assigned uniform height (<see cref="Size"/>) and separated
        /// by <see cref="Spacing"/>.
        ///
        /// The layout positions elements starting from the top of the parent’s
        /// content region and moves downward.
        /// </summary>
        public override void Apply()
        {
            if (Parent is null) return;
            if (Parent is Panel panel)
            {
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

        /// <summary>
        /// Returns the measured size of this layout. VerticalLayout currently
        /// does not calculate dynamic measurement and always returns <c>Vector2.Zero</c>.
        /// </summary>
        public override Vector2 MeasureLayout => Vector2.Zero;

        /// <summary>
        /// Updates the layout each frame. VerticalLayout has no per-frame behavior.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update.</param>
        public override void Update(float deltaTime) { }
    }
}