using System;
using System.Diagnostics.CodeAnalysis;

namespace Standard
{
    internal struct RECT
    {
        private int _left;
        private int _top;
        private int _right;
        private int _bottom;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Offset(int dx, int dy)
        {
            this._left += dx;
            this._top += dy;
            this._right += dx;
            this._bottom += dy;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Left
        {
            get => this._left;
            set => this._left = value;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Right
        {
            get => this._right;
            set => this._right = value;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Top
        {
            get => this._top;
            set => this._top = value;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Bottom
        {
            get => this._bottom;
            set => this._bottom = value;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Width => this._right - this._left;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Height => this._bottom - this._top;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public POINT Position => new POINT()
        {
            x = this._left,
            y = this._top
        };

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public SIZE Size => new SIZE()
        {
            cx = this.Width,
            cy = this.Height
        };

        public static RECT Union(RECT rect1, RECT rect2) => new RECT()
        {
            Left = Math.Min(rect1.Left, rect2.Left),
            Top = Math.Min(rect1.Top, rect2.Top),
            Right = Math.Max(rect1.Right, rect2.Right),
            Bottom = Math.Max(rect1.Bottom, rect2.Bottom)
        };

        public override bool Equals(object obj)
        {
            try
            {
                RECT rect = (RECT)obj;
                return rect._bottom == this._bottom && rect._left == this._left && rect._right == this._right && rect._top == this._top;
            }
            catch (InvalidCastException ex)
            {
                return false;
            }
        }

        public override int GetHashCode() => (this._left << 16 | Utility.LOWORD(this._right)) ^ (this._top << 16 | Utility.LOWORD(this._bottom));
    }
}
