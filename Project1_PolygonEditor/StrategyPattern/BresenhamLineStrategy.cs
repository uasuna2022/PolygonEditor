using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Project1_PolygonEditor.StrategyPattern
{
    public sealed class BresenhamLineStrategy : IDrawStrategy
    {
        private readonly WriteableBitmap _bitmap;
        public BresenhamLineStrategy(WriteableBitmap writeableBitmap)
        {
            _bitmap = writeableBitmap;
        }

        public void DrawLine(System.Windows.Point p1, System.Windows.Point p2)
        {
            int x1 = (int)Math.Round(p1.X);
            int y1 = (int)Math.Round(p1.Y);
            int x2 = (int)Math.Round(p2.X);
            int y2 = (int)Math.Round(p2.Y);

            _bitmap.Lock();
            try
            {
                int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
                int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
                int d = dx + dy;

                int x = x1, y = y1;
                while (true)
                {
                    SetPixel(x, y, Colors.Black);
                    if (x == x2 && y == y2) break;
                    int e2 = 2 * d;
                    if (e2 >= dy) 
                    { 
                        d += dy; 
                        x += sx; 
                    }
                    if (e2 <= dx) 
                    { 
                        d += dx;
                        y += sy;
                    }
                }

                var rx = Math.Min(x1, x2);
                var ry = Math.Min(y1, y2);
                var rw = Math.Abs(x2 - x1) + 1;
                var rh = Math.Abs(y2 - y1) + 1;
                _bitmap.AddDirtyRect(new Int32Rect(rx, ry, rw, rh));
            }
            finally { _bitmap.Unlock(); }
        }

        private unsafe void SetPixel(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= _bitmap.PixelWidth || y >= _bitmap.PixelHeight) return;
            int stride = _bitmap.BackBufferStride;
            byte* row = (byte*)_bitmap.BackBuffer + y * stride;
            int i = x * 4;
            row[i + 0] = c.B;
            row[i + 1] = c.G;
            row[i + 2] = c.R;
            row[i + 3] = c.A;
        }
    }
}
