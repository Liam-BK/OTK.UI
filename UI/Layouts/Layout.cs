using System.Xml.Linq;
using OpenTK.Mathematics;
using OTK.UI.Components;
using OTK.UI.Containers;
using OTK.UI.Interfaces;
using OTK.UI.Utility;

namespace OTK.UI.Layouts
{
    /// <summary>
    /// Base class for all layout systems. A layout controls how child elements 
    /// inside an <see cref="IUIContainer"/> are arranged, measured, and updated.
    /// </summary>
    public abstract class Layout
    {
        /// <summary>
        /// The container this layout operates on. The layout will position and size
        /// the elements inside this parent when <see cref="Apply"/> is called.
        /// </summary>
        public IUIContainer? Parent
        {
            get;
            set;
        }

        /// <summary>
        /// Optional spacing used by some layouts (e.g. vertical or horizontal).
        /// Represents the padding or gap between arranged elements.
        /// </summary>
        public float Spacing
        {
            get;
            set;
        } = 4.0f;

        /// <summary>
        /// A general-purpose scaling factor used by certain layout types.
        /// The meaning of this value is layout-specific (e.g. proportional sizing).
        /// </summary>
        public float ElementHeight
        {
            get;
            set;
        } = 1.0f;

        /// <summary>
        /// A general-purpose scaling factor used by certain layout types.
        /// The meaning of this value is layout-specific (e.g. proportional sizing).
        /// </summary>
        public float ElementWidth
        {
            get;
            set;
        } = 1.0f;

        /// <summary>
        /// Loads a concrete <see cref="Layout"/> instance based on the XML element name.
        /// Supports <see cref="ConstraintLayout"/> and <see cref="VerticalLayout"/>.
        /// Returns <c>null</c> if no layout is provided.
        /// </summary>
        /// <param name="element">The XML element describing the layout.</param>
        /// <returns>
        /// A constructed <see cref="Layout"/> instance, or <c>null</c> if <paramref name="element"/> is null.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown if the layout type is unknown.
        /// </exception>
        public static Layout? Load(XElement? element)
        {
            // TODO: Add support for user-defined/custom layouts (reflection-based loader?)
            if (element == null) return null;
            switch (element.Name.LocalName)
            {
                case "ConstraintLayout":
                    return ConstraintLayout.Load(element);
                case "VerticalLayout":
                    return VerticalLayout.Load(element);
                case "HorizontalLayout":
                    return HorizontalLayout.Load(element);
                case "FlowLayout":
                    return FlowLayout.Load(element);
                default:
                    throw new FormatException($"Unknown layout type: {element.Name}");
            }
        }

        protected float GetSmallestButtonTextSize()
        {
            var value = float.MaxValue;
            bool containsButtons = false;
            if (Parent is Panel panel)
            {
                if (panel.Elements.Count == 0) return 0;
                foreach (var element in panel.Elements)
                {
                    if (element is Button button && button.label.Size < value)
                    {
                        value = button.label.Size;
                        containsButtons = true;
                    }
                }
                if (containsButtons) return value;
            }
            else if (Parent is TabbedPanel tabbedPanel)
            {
                if (tabbedPanel.TabElements[tabbedPanel.CurrentTab].Count == 0) return 0;
                foreach (var element in tabbedPanel.TabElements[tabbedPanel.CurrentTab])
                {
                    if (element is Button button && button.label.Size < value)
                    {
                        value = button.label.Size;
                        containsButtons = true;
                    }
                }
                if (containsButtons) return value;
            }
            return 0;
        }

        public void UpdateButtonTextSize()
        {
            float textSize = GetSmallestButtonTextSize();
            if (Parent is Panel panel) SetButtonTextSize(panel, textSize);
            else if (Parent is TabbedPanel tabbedPanel) SetButtonTextSize(tabbedPanel, textSize);
        }

        private void SetButtonTextSize(Panel panel, float value)
        {
            foreach (var element in panel.Elements)
            {
                if (element is Button button) button.label.Size = value;
            }
        }

        private void SetButtonTextSize(TabbedPanel panel, float value)
        {
            foreach (var element in panel.TabElements[panel.CurrentTab])
            {
                if (element is Button button) button.label.Size = value;
            }
        }

        /// <summary>
        /// Applies the layout rules to the <see cref="Parent"/> container.
        /// Concrete layout types must implement the arrangement logic here.
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// Measures the layout and returns the required size, if applicable.
        /// Defaults to <see cref="Vector2.Zero"/> but may be overridden by
        /// layouts that compute their preferred dimensions.
        /// </summary>
        public virtual Vector2 MeasureLayout => Vector2.Zero;

        /// <summary>
        /// Allows the layout to perform per-frame updates (animation, transitions, etc.).
        /// Default implementation does nothing.
        /// </summary>
        /// <param name="deltaTime">Time since last frame in seconds.</param>
        public virtual void Update(float deltaTime) { }
    }
}