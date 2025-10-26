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
                var straightEdge = prevBezier ? next : prev;
                var bezierEdge = prevBezier ? prev : next;

                int otherId = (straightEdge.V1ID == vertexId) ? straightEdge.V2ID : straightEdge.V1ID;
                var otherPos = polygon.GetVertexById(otherId).Position;
                var vPos = v;

                bool bezierStartsHere = (bezierEdge.V1ID == vertexId);
                Point cp = bezierStartsHere
                    ? bezierEdge.BezierCP1!.Value
                    : bezierEdge.BezierCP2!.Value;

                // Desired direction: opposite of handle; C1 length rule ⇒ |v-other| = 3 * |v-cp|
                Vector dir = new Vector(vPos.X - cp.X, vPos.Y - cp.Y);
                double lenDir = dir.Length;
                if (lenDir < 1e-9) return true;
                dir /= lenDir;

                double L = 3.0 * Geometry.Dist(vPos, cp);

                if (isMovingControlPoint && straightEdge.ConstrainType != ConstrainType.Horizontal
                                          && straightEdge.ConstrainType != ConstrainType.Diagonal45
                                          && straightEdge.ConstrainType != ConstrainType.FixedLength)
                {
                    // Let the straight edge react: set its far vertex to satisfy C1 (both direction and length)
                    Point newOther = new Point(vPos.X + dir.X * L, vPos.Y + dir.Y * L);
                    polygon.GetVertexById(otherId).SetPosition(newOther);
                    return true;
                }

                // If the straight edge is constrained, we can't move it; instead re-aim the handle as before
                var vToOpp = Geometry.Mirror(vPos, otherPos);
                double d = Geometry.Dist(vPos, otherPos) / 3.0;

                if (bezierStartsHere)
                    bezierEdge.SetBezierControlPoints(Geometry.WithDistance(vPos, vToOpp, d), bezierEdge.BezierCP2 ?? vPos);
                else
                    bezierEdge.SetBezierControlPoints(bezierEdge.BezierCP1 ?? vPos, Geometry.WithDistance(vPos, vToOpp, d));

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
