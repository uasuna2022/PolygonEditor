using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Models;

namespace Project1_PolygonEditor.Continuity
{
    // Interface for handling vertex continuity.
    // Contains one method that has to preserve the concrete continuity at chosen vertex.
    public interface IVertexContinuity  
    {
        bool Preserve(int vertexId, Polygon polygon, bool isMovingControlPoint = false);
    }
}
