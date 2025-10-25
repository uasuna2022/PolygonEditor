using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Project1_PolygonEditor.Models;

namespace Project1_PolygonEditor.View
{
    public class VertexFigure
    {
        public Vertex Model { get; }
        public double Radius { get; set; } = 4.0;
        public Ellipse Shape { get; set; }

        public VertexFigure(Vertex model)
        {
            Model = model;
            Shape = new Ellipse
            {
                Width = 2 * Radius,
                Height = 2 * Radius,
                Fill = Brushes.DodgerBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0
            };
        }

        public Ellipse DrawVertex(Canvas canvas)
        {
            Canvas.SetLeft(Shape, Model.Position.X - Radius);
            Canvas.SetTop(Shape, Model.Position.Y - Radius);

            Shape.Tag = Model.ID;
            canvas.Children.Add(Shape);

            return Shape;
        }

        public void SyncToModel()
        {
            double x = Canvas.GetLeft(Shape) + Radius;
            double y = Canvas.GetTop(Shape) + Radius;
            Model.SetPosition(new Point(x, y));
        }
    }
}
