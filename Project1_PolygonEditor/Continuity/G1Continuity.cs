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
                // 1) identify straight edge and its "other" vertex
                var straightEdge = prevBezier ? next : prev;
                int otherId = (straightEdge.V1ID == vertexId) ? straightEdge.V2ID : straightEdge.V1ID;
                var otherPos = polygon.GetVertexById(otherId).Position;

                // 2) direction = opposite extension of the STRAIGHT edge through v
                var vToOpp = Geometry.Mirror(v, otherPos);

                // 3) choose a length (G1: keep current if present, else default ~ 1/3 of straight edge)
                var bezierEdge = prevBezier ? prev : next;
                bool bezierStartsHere = (bezierEdge.V1ID == vertexId);

                double currentLen = bezierStartsHere
                    ? (bezierEdge.BezierCP1.HasValue ? Geometry.Dist(v, bezierEdge.BezierCP1.Value) : 0.0)
                    : (bezierEdge.BezierCP2.HasValue ? Geometry.Dist(v, bezierEdge.BezierCP2.Value) : 0.0);

                double d = (currentLen > 1e-9) ? currentLen : Math.Max(Geometry.Dist(v, otherPos) / 3.0, 1.0);

                // 4) set the handle on the Bézier edge at THIS vertex
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
