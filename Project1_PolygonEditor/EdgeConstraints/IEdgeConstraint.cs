using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Models;

namespace Project1_PolygonEditor.EdgeConstraints
{
    // Interface for handling edge constraint.
    // Contains one method that has to preserve the concrete constraint continuity at chosen edge. 
    public interface IEdgeConstraint 
    {
        bool Preserve(Edge edge, Polygon polygon, bool isMovingVertex = false);
    }
}
