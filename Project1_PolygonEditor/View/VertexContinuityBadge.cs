using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Project1_PolygonEditor.Models;
using Project1_PolygonEditor.Enum_classes;
using System.Windows.Controls;
using System.Windows.Media;

namespace Project1_PolygonEditor.View
{
    // VertexContinuity View badge. Its appearance defined. Possesses a method which can draw it next to appropriate vertex.
    public static class VertexContinuityBadge
    {
        public static FrameworkElement? CreateBadge(Vertex v)
        {
            string? text = v.ContinuityType switch
            {
                ContinuityType.G1 => "G1",
                ContinuityType.C1 => "C1",
                _ => null
            };

            if (text == null)
                return null;

            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#4B2E83")!, 
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };

            Border border = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFrom("#EFE6FF")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#C5B3E6")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(6, 2, 6, 2),
                Child = tb,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    ShadowDepth = 0,
                    BlurRadius = 2,
                    Opacity = 0.25
                },
                IsHitTestVisible = false
            };

            return border;
        }

        public static FrameworkElement? DrawNearVertex(Canvas canvas, Vertex v)
        {
            FrameworkElement? badge = CreateBadge(v);
            if (badge == null) 
                return null;

            canvas.Children.Add(badge);
            badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var sz = badge.DesiredSize;

            Canvas.SetLeft(badge, v.Position.X + 14 - sz.Width / 2);
            Canvas.SetTop(badge, v.Position.Y - 18 - sz.Height / 2);
            Panel.SetZIndex(badge, 1000);
            return badge;
        }
    }
}
