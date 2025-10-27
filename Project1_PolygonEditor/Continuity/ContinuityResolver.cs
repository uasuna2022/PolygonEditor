using Project1_PolygonEditor.Enum_classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Models;

namespace Project1_PolygonEditor.Continuity
{
    public static class ContinuityResolver // factory to avoid switch in main window and preserve the appropriate strategy
    {
        private static IVertexContinuity StrategyFor(ContinuityType t) => t switch
        {
            ContinuityType.G1 => new G1Continuity(),
            ContinuityType.C1 => new C1Continuity(),
            ContinuityType.G0 => new G0Continuity(),
            _ => new G0Continuity()
        };

        public static void EnforceAt(int vertexId, Polygon polygon, ContinuityType t, bool isMovingControlPoint = false)
        {
            StrategyFor(t).Preserve(vertexId, polygon, isMovingControlPoint);
        }
    }
}
