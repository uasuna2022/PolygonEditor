using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Project1_PolygonEditor.Models;
using Project1_PolygonEditor.Enum_classes;

namespace Project1_PolygonEditor.Continuity
{
    public class G1Continuity : IVertexContinuity
    {
        public bool Preserve(int vertexId, Polygon polygon, bool isMovingControlPoint = false)
        {
            var (prev, next) = polygon.GetIncidentEdges(vertexId); // prev.V2==vertexId, next.V1==vertexId  :contentReference[oaicite:5]{index=5}
            var v = polygon.GetVertexById(vertexId).Position;
            var vPrev = polygon.GetVertexById(prev.V1ID).Position; // the neighbor before
            var vNext = polygon.GetVertexById(next.V2ID).Position; // the neighbor after

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
                var vPos = v; // corner

                bool bezierStartsHere = (bezierEdge.V1ID == vertexId);
                Point cp = bezierStartsHere
                    ? bezierEdge.BezierCP1!.Value
                    : bezierEdge.BezierCP2!.Value;

                // Desired direction for the straight edge is the OPPOSITE of the handle direction
                Vector dir = new Vector(vPos.X - cp.X, vPos.Y - cp.Y);
                double lenDir = dir.Length;
                if (lenDir < 1e-9) return true;
                dir /= lenDir;

                // keep straight-edge length (unless FixedLength is present, we keep that too)
                double L = (straightEdge.ConstrainType == ConstrainType.FixedLength)
                    ? straightEdge.FixedLength
                    : Polygon.Distance(vPos, otherPos);

                if (isMovingControlPoint && straightEdge.ConstrainType != ConstrainType.Horizontal
                                          && straightEdge.ConstrainType != ConstrainType.Diagonal45)
                {
                    // rotate the straight edge to follow the handle
                    Point newOther = new Point(vPos.X + dir.X * L, vPos.Y + dir.Y * L);
                    polygon.GetVertexById(otherId).SetPosition(newOther);
                    return true;
                }

                // Fallback: do not move constrained straight edges; just re-aim the handle (previous logic)
                var vToOpp = Geometry.Mirror(vPos, otherPos);
                double d = bezierStartsHere
                    ? (bezierEdge.BezierCP1.HasValue ? Geometry.Dist(vPos, bezierEdge.BezierCP1.Value) : Math.Max(L / 3.0, 1.0))
                    : (bezierEdge.BezierCP2.HasValue ? Geometry.Dist(vPos, bezierEdge.BezierCP2.Value) : Math.Max(L / 3.0, 1.0));

                if (bezierStartsHere)
                    bezierEdge.SetBezierControlPoints(Geometry.WithDistance(vPos, vToOpp, d), bezierEdge.BezierCP2 ?? vPos);
                else
                    bezierEdge.SetBezierControlPoints(bezierEdge.BezierCP1 ?? vPos, Geometry.WithDistance(vPos, vToOpp, d));

                return true;
            }



            // Stationary G1 enforcement (no neighbor-moves unless needed)
            if (nextBezier && !prevBezier)
            {
                // aim next.BezierCP1 to be colinear with (vPrev, v)
                Point target = Geometry.Mirror(v, vPrev);
                double d = next.BezierCP1.HasValue ? Geometry.Dist(v, next.BezierCP1.Value) : Math.Max(Geometry.Dist(v, vNext) / 3.0, 1.0);
                next.SetBezierControlPoints(Geometry.WithDistance(v, target, d), next.BezierCP2 ?? v);
                return true;
            }
            if (prevBezier && !nextBezier)
            {
                Point target = Geometry.Mirror(v, vPrev);
                double d = prev.BezierCP2.HasValue ? Geometry.Dist(v, prev.BezierCP2.Value) : Math.Max(Geometry.Dist(v, vPrev) / 3.0, 1.0);
                prev.SetBezierControlPoints(prev.BezierCP1 ?? v, Geometry.WithDistance(v, target, d));
                return true;
            }
            if (prevBezier && nextBezier)
            {
                // Mirror direction: prev.CP2 is opposite of next.CP1 around vertex (keep prev length)
                Point cp1 = next.BezierCP1!.Value;
                double keep = Geometry.Dist(v, prev.BezierCP2!.Value);
                Point dir = Geometry.Mirror(v, cp1);
                prev.SetBezierControlPoints(prev.BezierCP1!.Value, Geometry.WithDistance(v, dir, keep));
                return true;
            }

            // Both are straight → nothing to do for G1
            return false;
        }
    }
}
