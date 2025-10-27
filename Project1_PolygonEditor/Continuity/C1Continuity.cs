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
    public class C1Continuity : IVertexContinuity // tangent-vector continuity
    {
        public bool Preserve(int vertexId, Polygon polygon, bool isMovingControlPoint = false)
        {
            var (prev, next) = polygon.GetIncidentEdges(vertexId);
            Point v = polygon.GetVertexById(vertexId).Position;
            Point vPrev = polygon.GetVertexById(prev.V1ID).Position;
            Point vNext = polygon.GetVertexById(next.V2ID).Position; // Getting positions of 2 incident neighbor vertices

            bool prevBezier = prev.EdgeType == EdgeType.BezierCubic;
            bool nextBezier = next.EdgeType == EdgeType.BezierCubic;

            // One straight + one Bezier handle
            bool oneStraightOneBezier =
                (prevBezier && !nextBezier) || (!prevBezier && nextBezier);

            if (oneStraightOneBezier)
            {
                Edge straightEdge = prevBezier ? next : prev;
                Edge bezierEdge = prevBezier ? prev : next;

                // The endpoint that is not vertexID
                int otherId = (straightEdge.V1ID == vertexId) ? straightEdge.V2ID : straightEdge.V1ID;
                Point otherPos = polygon.GetVertexById(otherId).Position;
                Point vPos = v;

                bool bezierStartsHere = (bezierEdge.V1ID == vertexId);
                Point cp = bezierStartsHere ? bezierEdge.BezierCP1!.Value : bezierEdge.BezierCP2!.Value;

                // Desired direction: opposite of handle
                Vector dir = new Vector(vPos.X - cp.X, vPos.Y - cp.Y);
                double lenDir = dir.Length;
                if (lenDir < 1e-6) 
                    return true;
                dir /= lenDir; // normalizing

                double L = 3.0 * Geometry.Dist(vPos, cp);

                if (isMovingControlPoint && straightEdge.ConstrainType != ConstrainType.Horizontal 
                    && straightEdge.ConstrainType != ConstrainType.Diagonal45 && straightEdge.ConstrainType != ConstrainType.FixedLength)
                {
                    // Let the straight edge react: set its far vertex to satisfy C1 (both direction and length)
                    Point newOther = new Point(vPos.X + dir.X * L, vPos.Y + dir.Y * L);
                    polygon.GetVertexById(otherId).SetPosition(newOther);
                    return true;
                }

                // If the straight edge is constrained, we can't move it, instead we need to re-aim the handle as before
                Point vToOpp = Geometry.Mirror(vPos, otherPos);
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
                double d = Geometry.Dist(v, vPrev) / 3.0;
                Point dir = Geometry.Mirror(v, vPrev);  // just gives us a ray
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

            // If both are straight, do nothing
            return false;
        }
    }
}
