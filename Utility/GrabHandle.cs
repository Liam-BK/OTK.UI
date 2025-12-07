using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OTK.UI.Utility
{
    internal class GrabHandle
    {
        public Vector4 Bounds;

        public bool Active = false;

        private Vector2 clickOffset = new();

        public Vector2 Center
        {
            get
            {
                return new Vector2((Bounds.X + Bounds.Z) * 0.5f, (Bounds.Y + Bounds.W) * 0.5f);
            }
            set
            {
                float halfWidth = Width * 0.5f;
                float halfHeight = Height * 0.5f;
                Bounds.X = value.X - halfWidth;
                Bounds.Y = value.Y - halfHeight;
                Bounds.Z = value.X + halfWidth;
                Bounds.W = value.Y + halfHeight;
            }
        }

        public float Width
        {
            get
            {
                return Bounds.Z - Bounds.X;
            }
            set
            {
                Bounds.X = Center.X - value * 0.5f;
                Bounds.Z = Center.X + value * 0.5f;
            }
        }

        public float Height
        {
            get
            {
                return Bounds.W - Bounds.Y;
            }
            set
            {
                Bounds.Y = Center.Y - value * 0.5f;
                Bounds.W = Center.Y + value * 0.5f;
            }
        }

        public GrabHandle(Vector4 bounds)
        {
            Bounds = bounds;
        }

        public void OnClickDown(MouseState mouse)
        {
            Active = WithinBounds(UIBase.ConvertMouseScreenCoords(mouse.Position));
            var temp = UIBase.ConvertMouseScreenCoords(mouse.Position);
            clickOffset.X = Center.X - temp.X;
            clickOffset.Y = Center.Y - temp.Y;
        }

        public void OnMouseMove(MouseState mouse)
        {
            if (Active)
            {
                Center = UIBase.ConvertMouseScreenCoords(mouse.Position) + clickOffset;
            }
        }

        public void OnClickUp()
        {
            Active = false;
        }

        public bool WithinBounds(Vector2 mouse)
        {
            return mouse.X >= Bounds.X && mouse.X <= Bounds.Z && mouse.Y >= Bounds.Y && mouse.Y <= Bounds.W;
        }
    }
}