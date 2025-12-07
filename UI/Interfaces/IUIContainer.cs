using OpenTK.Mathematics;

namespace OTK.UI.Interfaces
{
    /// <summary>
    /// Represents a container capable of holding and managing <see cref="IUIElement"/> instances.
    /// Provides functionality for adding, removing, and laying out elements within a bounded area.
    /// </summary>
    public interface IUIContainer
    {
        /// <summary>
        /// Gets or sets the position of the container in 2D space.
        /// </summary>
        public Vector2 Position
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the bounds of the container as a <see cref="Vector4"/>.
        /// Bounds are represented as (X = left, Y = bottom, Z = right, W = top).
        /// </summary>
        public Vector4 Bounds
        {
            get;
            set;
        }

        /// <summary>
        /// Adds a UI element to the container.
        /// </summary>
        /// <param name="element">The <see cref="IUIElement"/> to add.</param>
        public void Add(IUIElement element);

        /// <summary>
        /// Removes a UI element from the container.
        /// </summary>
        /// <param name="element">The <see cref="IUIElement"/> to remove.</param>
        public void Remove(IUIElement element);

        /// <summary>
        /// Removes all elements from the container.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Applies layout rules to all elements in the container, updating positions and bounds as needed.
        /// </summary>
        public void ApplyLayout();

        /// <summary>
        /// Determines whether the specified UI element is contained within this container.
        /// </summary>
        /// <param name="element">The <see cref="IUIElement"/> to check.</param>
        /// <returns><c>true</c> if the element is contained; otherwise, <c>false</c>.</returns>
        public bool Contains(IUIElement element);

        /// <summary>
        /// Finds the minimal clipping bounds that encompass all child elements.
        /// </summary>
        /// <returns>A <see cref="Vector4"/> representing the minimal clip bounds.</returns>
        public Vector4 FindMinClipBounds();
    }
}