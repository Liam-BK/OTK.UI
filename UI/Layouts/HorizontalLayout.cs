using System.Xml.Linq;
using OpenTK.Mathematics;
using OTK.UI.Containers;
using OTK.UI.Interfaces;

namespace OTK.UI.Layouts
{
    /// <summary>
    /// A simple horizontal stacking layout that arranges UI elements from left to right
    /// inside its parent container. Each element is assigned a fixed width
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
    public class HorizontalLayout : Layout
    {
        /// <summary>
        /// Loads a <see cref="VerticalLayout"/> from an XML layout definition.
        /// Expects optional <c>Size</c> and <c>Spacing</c> fields.
        /// </summary>
        /// <param name="element">The XML element describing the layout.</param>
        /// <returns>A configured <see cref="VerticalLayout"/> instance.</returns>
        public static new HorizontalLayout Load(XElement element)
        {
            var layout = new HorizontalLayout();
            var elementHeight = float.Parse(element.Element("ElementHeight")?.Value ?? "20");
            var elementWidth = float.Parse(element.Element("ElementWidth")?.Value ?? "100");
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
                var top = panel.Bounds.W - panel.TitleMargin - panel.ContentMargin;
                var bottom = top - ElementHeight;
                var start = panel.Bounds.X + panel.ContentMargin;
                bool squish = PositionElements(panel.Elements, top, bottom, start, panel.scrollbar.Bounds.X);

                if (squish) ApplySquish();
            }
            else if (Parent is TabbedPanel tabbedPanel)
            {
                var top = tabbedPanel.Bounds.W - tabbedPanel.ContentMargin - tabbedPanel.TabHeight;
                var bottom = top - ElementHeight;
                var start = tabbedPanel.Bounds.X + tabbedPanel.ContentMargin;
                bool squish = PositionElements(tabbedPanel.TabElements[tabbedPanel.CurrentTab], top, bottom, start, tabbedPanel.scrollbar.Bounds.X);

                if (squish) ApplySquish();
            }
        }

        private void ApplySquish()
        {
            if (Parent is null) return;
            if (Parent is Panel panel)
            {
                var count = panel.Elements.Count;
                var totalSpacing = (count - 1) * Spacing;
                var availableWidth = Math.Max(panel.scrollbar.Bounds.X - panel.ContentMargin - (panel.Bounds.X + panel.ContentMargin) - totalSpacing, 0);
                var width = availableWidth / count;
                float start = panel.Bounds.X + panel.ContentMargin;
                var top = panel.Bounds.W - panel.TitleMargin - panel.ContentMargin;
                PositionElements(panel.Elements, top, top - ElementHeight, start, panel.scrollbar.Bounds.X, width);
            }
            else if (Parent is TabbedPanel tabbedPanel)
            {
                var count = tabbedPanel.TabElements[tabbedPanel.CurrentTab].Count;
                var totalSpacing = (count - 1) * Spacing;
                var availableWidth = Math.Max(tabbedPanel.scrollbar.Bounds.X - tabbedPanel.ContentMargin - (tabbedPanel.Bounds.X + tabbedPanel.ContentMargin) - totalSpacing, 0);
                var width = availableWidth / count;
                float start = tabbedPanel.Bounds.X + tabbedPanel.ContentMargin;
                var top = tabbedPanel.Bounds.W - tabbedPanel.TabHeight - tabbedPanel.ContentMargin;
                PositionElements(tabbedPanel.TabElements[tabbedPanel.CurrentTab], top, top - ElementHeight, start, tabbedPanel.scrollbar.Bounds.X, width);
            }
        }

        private bool PositionElements(List<IUIElement> elements, float top, float bottom, float start, float end, float? elementWidth = null)
        {
            bool squish = false;
            float width = elementWidth ?? ElementWidth;
            foreach (var element in elements)
            {
                element.Bounds = new Vector4(start, bottom, start + width, top);
                start += width + Spacing;
                if (start > end)
                {
                    squish = true;
                    break;
                }
            }
            return squish;
        }

        /// <summary>
        /// Returns the measured size of this layout. HorizontalLayout currently
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