using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Project1_PolygonEditor.Models;
using Project1_PolygonEditor.Enum_classes;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media;

namespace Project1_PolygonEditor.View
{
    public static class EdgeConstraintBadge
    {
        public static FrameworkElement? CreateBadge(Edge e)
        {
            if (e.ConstrainType == ConstrainType.None)
                return null;

            string badgeText = e.ConstrainType switch
            {
                ConstrainType.Horizontal => "H",
                ConstrainType.Diagonal45 => "D",
                ConstrainType.FixedLength => $"{e.FixedLength:0.#} 🔒",
                _ => ""
            };

            TextBlock tb = new TextBlock
            {
                Text = badgeText,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            Border border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = (Brush)new BrushConverter().ConvertFrom("#F0F3F7")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#9AA7B0")!,
                BorderThickness = new Thickness(1),
                Child = tb,
                Padding = new Thickness(2, 0, 2, 0),
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

        public static FrameworkElement? DrawAt(Canvas canvas, Edge e, Point center)
        {
            var badge = CreateBadge(e);
            if (badge == null) 
                return null;

            canvas.Children.Add(badge);
            badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var sz = badge.DesiredSize;

            Canvas.SetLeft(badge, center.X - sz.Width / 2);
            Canvas.SetTop(badge, center.Y - sz.Height / 2);
            Panel.SetZIndex(badge, 1000);
            return badge;
        }
    }
}
