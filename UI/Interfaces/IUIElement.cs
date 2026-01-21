using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OTK.UI.Interfaces
{
    /// <summary>
    /// Represents a single UI element that can be added to a container.
    /// Provides properties for positioning and sizing, as well as methods
    /// for input handling, updating, and rendering.
    /// </summary>
    public interface IUIElement
    {
        /// <summary>
        /// Gets or sets the parent container of this element.
        /// </summary>
        public IUIContainer? Parent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the bounding rectangle of the element.
        /// Bounds are represented as (X = left, Y = bottom, Z = right, W = top).
        /// </summary>
        public Vector4 Bounds
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the center position of the element.
        /// </summary>
        public Vector2 Center
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the height of the element (derived from bounds).
        /// </summary>
        public float Height
        {
            get;
        }

        /// <summary>
        /// Gets the width of the element (derived from bounds).
        /// </summary>
        public float Width
        {
            get;
        }

        /// <summary>
        /// Sets the underlying <c>_bounds.X</c> value without updating the model matrix.
        /// </summary>
        /// <param name="value">The new left position.</param>
        public void PreEditLeft(float value);

        /// <summary>
        /// Sets the underlying <c>_bounds.Y</c> value without updating the model matrix.
        /// </summary>
        /// <param name="value">The new bottom position.</param>
        public void PreEditBottom(float value);

        /// <summary>
        /// Sets the underlying <c>_bounds.Z</c> value without updating the model matrix.
        /// </summary>
        /// <param name="value">The new right position.</param>
        public void PreEditRight(float value);

        /// <summary>
        /// Sets the underlying <c>_bounds.W</c> value without updating the model matrix.
        /// </summary>
        /// <param name="value">The new top position.</param>
        public void PreEditTop(float value);

        /// <summary>
        /// Updates the underlying model matrix so that the drawn element fits within <c>_bounds</c>
        /// </summary>
        public void UpdateBounds();

        /// <summary>
        /// Called when a mouse button is pressed while over the element.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public void OnClickDown(MouseState mouse);

        /// <summary>
        /// Called when a mouse button is released over the element.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public void OnClickUp(MouseState mouse);

        /// <summary>
        /// Called when a key is pressed.
        /// </summary>
        /// <param name="e">The key event arguments.</param>
        public void OnKeyDown(KeyboardKeyEventArgs e);

        /// <summary>
        /// Called when a key is released.
        /// </summary>
        /// <param name="e">The key event arguments.</param>
        public void OnKeyUp(KeyboardKeyEventArgs e);

        /// <summary>
        /// Called when text input occurs while the element is focused.
        /// </summary>
        /// <param name="e">The text input event arguments.</param>
        public void OnTextInput(TextInputEventArgs e);

        /// <summary>
        /// Called when the mouse wheel is scrolled.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public void OnMouseWheel(MouseState mouse);

        /// <summary>
        /// Called when the mouse moves.
        /// </summary>
        /// <param name="mouse">The current mouse state.</param>
        public void OnMouseMove(MouseState mouse);

        /// <summary>
        /// Updates the element with a given delta time.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since last update in seconds.</param>
        public void OnUpdate(float deltaTime);

        /// <summary>
        /// Updates the element with delta time and mouse state.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since last update in seconds.</param>
        /// <param name="mouse">The current mouse state.</param>
        public void OnUpdate(float deltaTime, MouseState mouse);

        /// <summary>
        /// Updates the element with delta time and keyboard state.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since last update in seconds.</param>
        /// <param name="keyboard">The current keyboard state.</param>
        public void OnUpdate(float deltaTime, KeyboardState keyboard);

        /// <summary>
        /// Updates the element with delta time, mouse state, and keyboard state.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since last update in seconds.</param>
        /// <param name="mouse">The current mouse state.</param>
        /// <param name="keyboard">The current keyboard state.</param>
        public void OnUpdate(float deltaTime, MouseState mouse, KeyboardState keyboard);

        /// <summary>
        /// Determines whether a given position is within the bounds of the element.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <returns><c>true</c> if the position is inside the element; otherwise, <c>false</c>.</returns>
        public bool WithinBounds(Vector2 position);

        /// <summary>
        /// Deletes GPU resources associated with this element (e.g., textures, buffers).
        /// </summary>
        public void DeleteFromVRam();

        /// <summary>
        /// Renders the element on screen.
        /// </summary>
        public void Draw();
    }
}