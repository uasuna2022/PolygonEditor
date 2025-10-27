using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Project1_PolygonEditor.StrategyPattern
{
    // Library algorithm. Very simple: just creates a new WPF line and adds it ti working canvas.
    public sealed class LibraryLineStrategy : IDrawStrategy 
    {
        private readonly Canvas _canvas;
        public LibraryLineStrategy(Canvas canvas)
        {
            _canvas = canvas;
        }
        public void DrawLine(System.Windows.Point p1, System.Windows.Point p2)
        {
            Line newLine = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0
            };

            _canvas.Children.Add(newLine);
        }
    }
}
