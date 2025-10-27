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
            Vertex v1 = polygon.GetVertexById(edge.V1ID);
            Vertex v2 = polygon.GetVertexById(edge.V2ID);

            // Edge vector
            Vector d = new Vector(v2.Position.X - v1.Position.X, v2.Position.Y - v1.Position.Y);
            if (d.Length < 1e-9) 
                return true;

            // Defining 45° unit directions
            Vector u1 = new Vector(1, 1);  // NorthEast-SouthWest
            u1.Normalize(); 
            Vector u2 = new Vector(1, -1); // SouthEast-NorthWest
            u2.Normalize(); 

            // Project onto the closest diagonal (getting dot products).
            double s1 = Vector.Multiply(d, u1);     
            double s2 = Vector.Multiply(d, u2);   
            
            // Now s1 and s2 are lengths of d along u1 and u2 respectively.

            // Choosing a diagonal that has a larger absolute component of d (so we rotate to the nearest 45degree direction).
            Vector u = (Math.Abs(s1) >= Math.Abs(s2)) ? u1 : u2;
            double s = (Math.Abs(s1) >= Math.Abs(s2)) ? s1 : s2;

            // Rebuild the edge using the midpoint and the projected vector
            Point mid = new Point((v1.Position.X + v2.Position.X) / 2.0, (v1.Position.Y + v2.Position.Y) / 2.0);
            Vector dproj = new Vector(u.X * s, u.Y * s); // projected segment vector

            Point p1 = new Point(mid.X - 0.5 * dproj.X, mid.Y - 0.5 * dproj.Y);
            Point p2 = new Point(mid.X + 0.5 * dproj.X, mid.Y + 0.5 * dproj.Y);

            v1.SetPosition(p1);
            v2.SetPosition(p2);
            return true;
        }
    }
}
