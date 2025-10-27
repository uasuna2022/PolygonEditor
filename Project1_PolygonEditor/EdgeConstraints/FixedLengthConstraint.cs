using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Project1_PolygonEditor.Models;

namespace Project1_PolygonEditor.EdgeConstraints
{
    public sealed class FixedLengthConstraint : IEdgeConstraint // fixed-length constraint (edge remains of the same size all time)
    {
        public bool Preserve(Edge edge, Polygon polygon, bool isMovingVertex = false)
        {
            if (edge.FixedLength <= 0)
                return false;

            Vertex v1 = polygon.GetVertexById(edge.V1ID);
            Vertex v2 = polygon.GetVertexById(edge.V2ID);

            // vector v1 -> v2
            Vector dir = new Vector(v2.Position.X - v1.Position.X,
                                    v2.Position.Y - v1.Position.Y);

            double len = dir.Length;
            if (len < 1e-9) 
                return true;

            dir /= len; // normalize

            // Keep midpoint fixed (creating more "interesting" change)
            Point mid = new Point((v1.Position.X + v2.Position.X) / 2.0,
                                  (v1.Position.Y + v2.Position.Y) / 2.0);

            double halfL = edge.FixedLength / 2.0;

            v1.SetPosition(new Point(mid.X - dir.X * halfL, mid.Y - dir.Y * halfL));
            v2.SetPosition(new Point(mid.X + dir.X * halfL, mid.Y + dir.Y * halfL));

            return true;
        }
    }
}
