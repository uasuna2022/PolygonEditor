using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Models;
using System.Windows;

namespace Project1_PolygonEditor.EdgeConstraints
{
    public sealed class HorizontalConstraint : IEdgeConstraint //  horizontal constraint (Y coord remains the same in both vertices)
    {
        public bool Preserve(Edge edge, Polygon polygon, bool isMovingVertex = false)
        {
            Vertex v1 = polygon.GetVertexById(edge.V1ID);
            Vertex v2 = polygon.GetVertexById(edge.V2ID);

            double avgY = (v1.Position.Y + v2.Position.Y) / 2.0;

            // Keep both vertices at same Y
            v1.SetPosition(new Point(v1.Position.X, avgY));
            v2.SetPosition(new Point(v2.Position.X, avgY));
            return true;
        }
    }
}
