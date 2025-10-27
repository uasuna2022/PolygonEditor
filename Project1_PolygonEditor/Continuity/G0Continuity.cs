using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Models;

namespace Project1_PolygonEditor.Continuity
{
    public class G0Continuity : IVertexContinuity
    {
        // G0 continuity is preserved all time, so just always returns true doing nothing
        public bool Preserve(int vertexId, Polygon polygon, bool isMovingControlPoint = false) => true;
    }
}
