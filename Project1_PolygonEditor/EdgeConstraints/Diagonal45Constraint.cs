using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Models;
using System.Windows;

namespace Project1_PolygonEditor.EdgeConstraints
{
    public sealed class Diagonal45Constraint : IEdgeConstraint
    {
        public bool Preserve(Edge edge, Polygon polygon, bool isMovingVertex = false)
        {
            var v1 = polygon.GetVertexById(edge.V1ID);
            var v2 = polygon.GetVertexById(edge.V2ID);

            Vector d = new Vector(v2.Position.X - v1.Position.X, v2.Position.Y - v1.Position.Y);
            if (d.Length < 1e-9) return true;

            // 45° unit directions
            Vector u1 = new Vector(1, 1); u1.Normalize(); // NE/SW
            Vector u2 = new Vector(1, -1); u2.Normalize(); // SE/NW

            // Project onto the closer diagonal
            double s1 = Vector.Multiply(d, u1);     // signed length along u1
            double s2 = Vector.Multiply(d, u2);     // signed length along u2

            Vector u = (System.Math.Abs(s1) >= System.Math.Abs(s2)) ? u1 : u2;
            double s = (System.Math.Abs(s1) >= System.Math.Abs(s2)) ? s1 : s2;

            // Rebuild the edge using the midpoint and the projected vector
            Point mid = new Point((v1.Position.X + v2.Position.X) / 2.0,
                                  (v1.Position.Y + v2.Position.Y) / 2.0);

            Vector dproj = new Vector(u.X * s, u.Y * s); // projected segment vector
            Point p1 = new Point(mid.X - 0.5 * dproj.X, mid.Y - 0.5 * dproj.Y);
            Point p2 = new Point(mid.X + 0.5 * dproj.X, mid.Y + 0.5 * dproj.Y);

            v1.SetPosition(p1);
            v2.SetPosition(p2);
            return true;
        }
    }
}
