using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1_PolygonEditor.Enum_classes;
using Project1_PolygonEditor.Models;
using System.Windows;

namespace Project1_PolygonEditor.Continuity
{
    public class C1Continuity : IVertexContinuity
    {
        public bool Preserve(int vertexId, Polygon polygon, bool isMovingControlPoint = false)
        {
            var (prev, next) = polygon.GetIncidentEdges(vertexId);
            var v = polygon.GetVertexById(vertexId).Position;
            var vPrev = polygon.GetVertexById(prev.V1ID).Position;
            var vNext = polygon.GetVertexById(next.V2ID).Position;

            bool prevBezier = prev.EdgeType == EdgeType.BezierCubic;
            bool nextBezier = next.EdgeType == EdgeType.BezierCubic;

            // --- one straight + one bezier ---
            bool oneStraightOneBezier =
                (prevBezier && !nextBezier) || (!prevBezier && nextBezier);

            if (oneStraightOneBezier)
            {
                // 1) identify the straight edge and its "other" vertex
                var straightEdge = prevBezier ? next : prev;
                int otherId = (straightEdge.V1ID == vertexId) ? straightEdge.V2ID : straightEdge.V1ID;
                var otherPos = polygon.GetVertexById(otherId).Position;

                // 2) direction = opposite extension of the STRAIGHT edge through v
                var vToOpp = Geometry.Mirror(v, otherPos);

                // 3) C1 length rule: |v - cp| = |v - other| / 3
                double d = Geometry.Dist(v, otherPos) / 3.0;

                // 4) set the handle on the Bézier edge at THIS vertex
                var bezierEdge = prevBezier ? prev : next;
                bool bezierStartsHere = (bezierEdge.V1ID == vertexId); // CP1 at start, CP2 at end

                if (bezierStartsHere)
                    bezierEdge.SetBezierControlPoints(
                        Geometry.WithDistance(v, vToOpp, d),
                        bezierEdge.BezierCP2 ?? v
                    );
                else
                    bezierEdge.SetBezierControlPoints(
                        bezierEdge.BezierCP1 ?? v,
                        Geometry.WithDistance(v, vToOpp, d)
                    );

                return true;
            }


            // Line–Bezier: set handle colinear with the straight neighbor and at 1/3 distance rule.
            if (nextBezier && !prevBezier)
            {
                // distance v–cp1 must be |v - vPrev| / 3
                double d = Geometry.Dist(v, vPrev) / 3.0;
                Point dir = Geometry.Mirror(v, vPrev);                     // just gives us a ray
                next.SetBezierControlPoints(Geometry.WithDistance(v, dir, d), next.BezierCP2 ?? v);
                return true;
            }
            if (prevBezier && !nextBezier)
            {
                double d = Geometry.Dist(v, vNext) / 3.0;
                Point dir = Geometry.Mirror(v, vPrev);
                prev.SetBezierControlPoints(prev.BezierCP1 ?? v, Geometry.WithDistance(v, dir, d));
                return true;
            }

            // Bezier–Bezier: make the two handles colinear and symmetric (equal lengths)
            if (prevBezier && nextBezier)
            {
                Point cp1 = next.BezierCP1!.Value;
                // Direction through vertex: use the line (cp1 — v) and place prev.cp2 on the opposite side with the same length as cp1
                double len = Geometry.Dist(v, cp1);
                Point opposite = Geometry.Mirror(v, cp1);
                prev.SetBezierControlPoints(prev.BezierCP1!.Value, Geometry.WithDistance(v, opposite, len));
                return true;
            }

            // Both straight → nothing to do
            return false;
        }
    }
}
