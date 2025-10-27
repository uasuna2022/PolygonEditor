using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Models;
using System.Windows;
using Project1_PolygonEditor.Enum_classes;

namespace Project1_PolygonEditor.EdgeConstraints
{
    public sealed class ConstraintResolver // factory to avoid switch in main window and preserve the appropriate strategy
    {
        private static IEdgeConstraint StrategyFor(ConstrainType t) => t switch
        {
            ConstrainType.Horizontal => new HorizontalConstraint(),
            ConstrainType.Diagonal45 => new Diagonal45Constraint(),
            ConstrainType.FixedLength => new FixedLengthConstraint(),
            _ => new NoConstraint(),
        };

        public static void EnforceAtEdge(Edge edge, Polygon polygon, bool isMovingVertex = false)
        {
            StrategyFor(edge.ConstrainType).Preserve(edge, polygon, isMovingVertex);
        }
    }
}
