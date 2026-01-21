using OpenTK.Windowing.Common.Input;

namespace OTK.UI.Utility
{
    public static class CursorManager
    {
        private static MouseCursor requested = MouseCursor.Default;

        public static void Request(MouseCursor cursor)
        {
            requested = cursor;
        }

        public static void Apply()
        {
            if (UIBase.Window is not null)
                UIBase.Window.Cursor = requested;
            requested = MouseCursor.Default; // reset for next frame
        }
    }
}
