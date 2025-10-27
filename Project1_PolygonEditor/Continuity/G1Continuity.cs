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
    public class G1Continuity : IVertexContinuity // geometric (tangent-direction) continuity
    {
        public bool Preserve(int vertexId, Polygon polygon, bool isMovingControlPoint = false)
        {
            var (prev, next) = polygon.GetIncidentEdges(vertexId); // prev.V2==vertexId, next.V1==vertexId
            Point v = polygon.GetVertexById(vertexId).Position;
            Point vPrev = polygon.GetVertexById(prev.V1ID).Position; // neighbor before v
            Point vNext = polygon.GetVertexById(next.V2ID).Position; // neighbor after v 

            bool prevBezier = prev.EdgeType == EdgeType.BezierCubic; // Helper bool variables to choose a way of preserving continuity 
            bool nextBezier = next.EdgeType == EdgeType.BezierCubic;
            bool prevArc = prev.EdgeType == EdgeType.Arc;
            bool nextArc = next.EdgeType == EdgeType.Arc;

            // Arc + Bezier/Line 
            if (prevArc && !nextArc)
            {
                // Vector from V toward the NEXT edge’s free endpoint
                int nextOtherId = (next.V1ID == vertexId) ? next.V2ID : next.V1ID;
                Point nextOtherPos = polygon.GetVertexById(nextOtherId).Position;
                Vector towardNext = new Vector(nextOtherPos.X - v.X, nextOtherPos.Y - v.Y);

                // Pick the correct tangent direction at V on the ARC so that it points toward the NEXT edge
                Vector t = ArcTangentToward(v, prev.ArcCenter, towardNext);

                if (nextBezier) // next is Bezier cubic curve
                {
                    double keep = next.BezierCP1.HasValue ? Geometry.Dist(v, next.BezierCP1.Value)
                                                          : Math.Max(Geometry.Dist(v, nextOtherPos) / 3.0, 1.0);

                    // Setting CP1 so it's colinear with arc tangent
                    Point cp1 = new Point(v.X + t.X * keep, v.Y + t.Y * keep);
                    next.SetBezierControlPoints(cp1, next.BezierCP2 ?? v);

                    return true;
                }
                else // next is straight
                {
                    if (isMovingControlPoint && next.ConstrainType != ConstrainType.Horizontal 
                        && next.ConstrainType != ConstrainType.Diagonal45)
                    {
                        double L = (next.ConstrainType == ConstrainType.FixedLength) ? next.FixedLength
                                                                                     : Geometry.Dist(v, nextOtherPos);
                        Point newOther = new Point(v.X + t.X * L, v.Y + t.Y * L);
                        polygon.GetVertexById(nextOtherId).SetPosition(newOther);
                        return true;
                    }
                }
            }


            if (nextArc && !prevArc)
            {
                int prevOtherId = (prev.V1ID == vertexId) ? prev.V2ID : prev.V1ID;
                Point prevOther = polygon.GetVertexById(prevOtherId).Position;
                Vector towardPrev = new Vector(prevOther.X - v.X, prevOther.Y - v.Y);

                // tangent at V on the ARC pointing toward the PREV edge
                Vector t = ArcTangentToward(v, next.ArcCenter, towardPrev);

                if (prevBezier)
                {
                    double keep = prev.BezierCP2.HasValue ? Geometry.Dist(v, prev.BezierCP2.Value)
                                                          : Math.Max(Geometry.Dist(v, prevOther) / 3.0, 1.0);
                    Point cp2 = new Point(v.X - t.X * keep, v.Y - t.Y * keep);
                    prev.SetBezierControlPoints(prev.BezierCP1 ?? v, cp2);
                    return true;
                }
                else // prev is straight
                {
                    if (isMovingControlPoint && prev.ConstrainType != ConstrainType.Horizontal
                                             && prev.ConstrainType != ConstrainType.Diagonal45)
                    {
                        double L = (prev.ConstrainType == ConstrainType.FixedLength) ? prev.FixedLength
                                                                                     : Geometry.Dist(v, prevOther);
                        Point newOther = new Point(v.X - t.X * L, v.Y - t.Y * L);
                        polygon.GetVertexById(prevOtherId).SetPosition(newOther);
                        return true;
                    }
                }
            }


            // Bezier + Line || Line + Bezier
            bool oneStraightOneBezier =
                (prevBezier && !nextBezier) || (!prevBezier && nextBezier);

            if (oneStraightOneBezier)
            {
                Edge straightEdge = prevBezier ? next : prev;
                Edge bezierEdge = prevBezier ? prev : next;

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
                if (lenDir < 1e-9) 
                    return true;
                dir /= lenDir;

                double L = (straightEdge.ConstrainType == ConstrainType.FixedLength)
                    ? straightEdge.FixedLength
                    : Geometry.Dist(vPos, otherPos);

                if (isMovingControlPoint && straightEdge.ConstrainType != ConstrainType.Horizontal
                                          && straightEdge.ConstrainType != ConstrainType.Diagonal45)
                {
                    // rotate the straight edge to follow the handle
                    Point newOther = new Point(vPos.X + dir.X * L, vPos.Y + dir.Y * L);
                    polygon.GetVertexById(otherId).SetPosition(newOther);
                    return true;
                }

                // Fallback: do not move constrained straight edges, just re-aim the handle
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

            // If both are straight, do nothing
            return false;
        }

        // Getting tangent to a circle with a center in 'centerMaybe' toward 'toward'
        private static Vector ArcTangentToward(Point v, Point? centerMaybe, Vector toward)
        {
            // center fallback if null
            Point c = centerMaybe ?? new Point(v.X - toward.Y, v.Y + toward.X);
            Vector r = new Vector(v.X - c.X, v.Y - c.Y); 
            if (r.Length < 1e-9) 
                return new Vector(0, 0);

            Vector t = new Vector(-r.Y, r.X);   // 90° CCW, we are choosing the one that points towards neighbor
                                                                
            if (Vector.Multiply(t, toward) < 0)
                t = -t;

            t.Normalize();
            return t;
        }
    }
}
