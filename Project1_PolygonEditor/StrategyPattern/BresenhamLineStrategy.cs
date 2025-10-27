using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Project1_PolygonEditor.StrategyPattern
{
    // Bresenham algorithm. Implemented an idea from wikipedia:
    // https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
    // Contains one more method to add a pixel (rectangle 1x1) to working canvas.
    public sealed class BresenhamLineStrategy : IDrawStrategy
    {
        private readonly Canvas _canvas;
        public BresenhamLineStrategy(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void DrawLine(System.Windows.Point p1, System.Windows.Point p2)
        {
            int x1 = (int)Math.Round(p1.X);
            int y1 = (int)Math.Round(p1.Y);
            int x2 = (int)Math.Round(p2.X);
            int y2 = (int)Math.Round(p2.Y);

            int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
            int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
            int d = dx + dy;

            int x = x1, y = y1;
            while (true)
            {
                PutPixel(x, y);
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
        }

        private void PutPixel(int x, int y)
        {
            if (x < 0 || y < 0 ||
                x >= (int)Math.Ceiling(_canvas.ActualWidth) ||
                y >= (int)Math.Ceiling(_canvas.ActualHeight))
                return;

            Rectangle pixel = new Rectangle
            {
                Width = 1,
                Height = 1,
                Fill = Brushes.Black,
            };

            Canvas.SetLeft(pixel, x);
            Canvas.SetTop(pixel, y);

            _canvas.Children.Add(pixel);
        }
    }
}
