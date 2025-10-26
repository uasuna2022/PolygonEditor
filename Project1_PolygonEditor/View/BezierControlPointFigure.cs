using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace Project1_PolygonEditor.View
{
    public sealed class BezierControlPointFigure
    {
        public int EdgeId { get; }
        public bool IsFirst; // true -> CP1, false -> CP2
        public Point Position { get; private set; }

        public double Radius { get; set; } = 4.0;
        public Rectangle Shape { get; }

        public BezierControlPointFigure(int edgeId, bool isFirst, Point pos)
        {
            EdgeId = edgeId;
            IsFirst = isFirst;
            Position = pos;

            Shape = new Rectangle
            {
                Width = 2 * Radius,
                Height = 2 * Radius,
                Fill = Brushes.Orange,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0
            };
        }

        public Rectangle DrawFigure(Canvas canvas)
        {
            Canvas.SetLeft(Shape, Position.X - Radius);
            Canvas.SetTop(Shape, Position.Y - Radius);
            Shape.Tag = (EdgeId, IsFirst);
            canvas.Children.Add(Shape);
            return Shape;
        }

        public void SetPosition(Point p)
        {
            Position = p;
            Canvas.SetLeft(Shape, p.X - Radius);
            Canvas.SetTop(Shape, p.Y - Radius);
        }
    }
}

