using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Project1_PolygonEditor
{
    public static class Geometry
    {
        public static double Dist(Point a, Point b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        public static Point MoveFrom(Point origin, Point toward, double distance)
        {
            double dx = toward.X - origin.X, dy = toward.Y - origin.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-9) return origin;
            double ux = dx / len, uy = dy / len;
            return new Point(origin.X + ux * distance, origin.Y + uy * distance);
        }
        public static Point Mirror(Point center, Point refPoint)
        {
            // Put point opposite to refPoint with the same distance from 'center'
            return new Point(2 * center.X - refPoint.X, 2 * center.Y - refPoint.Y);
        }
        public static Point WithDistance(Point center, Point along, double newDist)
        {
            return MoveFrom(center, along, newDist);
        }
    }
}
