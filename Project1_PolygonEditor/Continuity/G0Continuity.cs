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
        public bool Preserve(int vertexId, Polygon polygon, bool isMovingControlPoint = false) => true;
    }
}
