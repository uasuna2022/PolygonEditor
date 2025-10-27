using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Models;
using System.Windows;

namespace Project1_PolygonEditor.EdgeConstraints
{
    public sealed class NoConstraint : IEdgeConstraint
    {
        public bool Preserve(Edge edge, Polygon polygon, bool isMovingVertex = false) => true;
    }
}
