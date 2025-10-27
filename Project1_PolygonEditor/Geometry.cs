using Project1_PolygonEditor.Enum_classes;
using Project1_PolygonEditor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Project1_PolygonEditor
{
    public static class Geometry
    {
        public static double Dist(Point a, Point b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        public static Point MoveFrom(Point origin, Point toward, double distance)
        {
            double dx = toward.X - origin.X, dy = toward.Y - origin.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-9) return origin;
            double ux = dx / len, uy = dy / len;
            return new Point(origin.X + ux * distance, origin.Y + uy * distance);
        }
        public static Point Mirror(Point center, Point refPoint)
        {
            // Put point opposite to refPoint with the same distance from 'center'
            return new Point(2 * center.X - refPoint.X, 2 * center.Y - refPoint.Y);
        }
        public static Point WithDistance(Point center, Point along, double newDist)
        {
            return MoveFrom(center, along, newDist);
        }
    }
    
    public static class ArcFromColleague
    {
        public struct ArcParams
        {
            public Point Center;
            public double Radius;
            public double ThetaStart; // radians
            public double ThetaEnd;   // radians
            public bool Clockwise;    // true => sweep decreases angle
        }

        private const double EPS = 1e-8;

        // === Public entry point: compute arc parameters for an EdgeType.Arc edge ===
        public static bool TryGetArcParams(Polygon poly, Edge e, out ArcParams arc)
        {
            var A = poly.GetVertexById(e.V1ID).Position;
            var B = poly.GetVertexById(e.V2ID).Position;
            if (Distance(A, B) < EPS) { arc = default; return false; }

            var contA = poly.GetVertexById(e.V1ID).ContinuityType;
            var contB = poly.GetVertexById(e.V2ID).ContinuityType;

            // Treat C1 like G1 for arcs (tangent constrained). If you prefer to forbid C1-at-arc, gate this earlier in UI.
            bool g1A = (contA == ContinuityType.G1 || contA == ContinuityType.C1);
            bool g1B = (contB == ContinuityType.G1 || contB == ContinuityType.C1);

            // If both ends want G1 → not supported (overconstrained)
            if (g1A && g1B) { arc = default; return false; }

            // --- helper local: always build a semicircle over AB with flip flag ---
            ArcParams SemiOverChord()
            {
                var O = Mid(A, B);
                double R = Distance(A, B) * 0.5;
                double thA = Math.Atan2(A.Y - O.Y, A.X - O.X);
                double thB = Math.Atan2(B.Y - O.Y, B.X - O.X);
                return new ArcParams { Center = O, Radius = R, ThetaStart = thA, ThetaEnd = thB, Clockwise = e.ArcFlipSide };
            }

            // G0–G0 → semicircle (always succeeds)
            if (!g1A && !g1B) { arc = SemiOverChord(); return true; }

            // Exactly one tangent-constrained end
            bool g1AtStart = g1A;       // start = V1 if G1/C1 at A, else V2
            int startVid = g1AtStart ? e.V1ID : e.V2ID;
            int endVid = g1AtStart ? e.V2ID : e.V1ID;
            var S = poly.GetVertexById(startVid).Position;
            var E = poly.GetVertexById(endVid).Position;

            if (!TryGetOtherIncidentEdge(poly, e, startVid, out var otherEdge))
            { arc = SemiOverChord(); return true; }

            var tan = GetIncomingTangentAtVertex(poly, otherEdge, startVid);
            double norm = tan.Length;
            if (norm < EPS) { arc = SemiOverChord(); return true; }
            tan /= norm;
            tan = new Vector(-tan.X, -tan.Y);

            // line 1: through S, direction perp to tan
            Vector perp = new Vector(-tan.Y, tan.X);
            // line 2: chord SE perp-bisector
            Vector SE = new Vector(E.X - S.X, E.Y - S.Y);
            Point M = new Point((S.X + E.X) * 0.5, (S.Y + E.Y) * 0.5);
            Vector n = new Vector(-SE.Y, SE.X);

            double dot_d_v = SE.X * perp.X + SE.Y * perp.Y;
            bool flipped = false;
            if (dot_d_v < 0) { perp = new Vector(-perp.X, -perp.Y); dot_d_v = -dot_d_v; flipped = true; }
            if (Math.Abs(dot_d_v) < EPS) { arc = SemiOverChord(); return true; }

            double det = perp.X * n.Y - perp.Y * n.X;
            if (Math.Abs(det) < 1e-10) { arc = SemiOverChord(); return true; }
            Vector rhs = new Vector(M.X - S.X, M.Y - S.Y);
            double s = (rhs.X * n.Y - rhs.Y * n.X) / det;
            Point Oi = new Point(S.X + s * perp.X, S.Y + s * perp.Y);

            double Rcalc = Distance(Oi, S);
            if (Rcalc < EPS) { arc = SemiOverChord(); return true; }

            double thS = Math.Atan2(S.Y - Oi.Y, S.X - Oi.X);
            double thE = Math.Atan2(E.Y - Oi.Y, E.X - Oi.X);

            bool clk = g1AtStart ? flipped : !flipped;
            if (!g1AtStart) { var tmp = thS; thS = thE; thE = tmp; clk = !clk; }
            if (e.ArcFlipSide) clk = !clk;

            arc = new ArcParams { Center = Oi, Radius = Rcalc, ThetaStart = thS, ThetaEnd = thE, Clockwise = clk };
            return true;
        }


        // === draw helper (polyline tessellation, identical behavior to your Bezier) ===
        public static void Tessellate(ArcParams arc, int steps, Action<Point, Point> drawLine)
        {
            double sweep = arc.ThetaEnd - arc.ThetaStart;
            if (arc.Clockwise && sweep > 0) sweep -= 2 * Math.PI;
            if (!arc.Clockwise && sweep < 0) sweep += 2 * Math.PI;

            double dt = sweep / steps;
            double t = arc.ThetaStart;
            Point prev = new Point(arc.Center.X + arc.Radius * Math.Cos(t),
                                   arc.Center.Y + arc.Radius * Math.Sin(t));
            for (int i = 1; i <= steps; i++)
            {
                t += dt;
                Point curr = new Point(arc.Center.X + arc.Radius * Math.Cos(t),
                                       arc.Center.Y + arc.Radius * Math.Sin(t));
                drawLine(prev, curr);
                prev = curr;
            }
        }

        // === tangent at a vertex from “the other” edge (matches Maciek’s) ===
        private static Vector GetIncomingTangentAtVertex(Polygon poly, Edge edge, int vertexId)
        {
            var v = poly.GetVertexById(vertexId).Position;
            int otherId = (edge.V1ID == vertexId) ? edge.V2ID : edge.V1ID;
            var u = poly.GetVertexById(otherId).Position;

            if (edge.EdgeType == EdgeType.BezierCubic)
            {
                bool endHere = (edge.V2ID == vertexId);
                if (endHere)
                {
                    var cp2 = edge.BezierCP2!.Value;
                    return new Vector(v.X - cp2.X, v.Y - cp2.Y);
                }
                else
                {
                    var cp1 = edge.BezierCP1!.Value;
                    return new Vector(v.X - cp1.X, v.Y - cp1.Y);
                }
            }
            if (edge.EdgeType == EdgeType.Arc)
            {
                // derive tangent from the arc itself
                if (TryGetArcParams(poly, edge, out var ap))
                {
                    if (edge.V1ID == vertexId)
                    {
                        // tangent at start angle = perpCCW of radius vector
                        double s = Math.Sin(ap.ThetaStart), c = Math.Cos(ap.ThetaStart);
                        return new Vector(s, -c);
                    }
                    else
                    {
                        double s = Math.Sin(ap.ThetaEnd), c = Math.Cos(ap.ThetaEnd);
                        return new Vector(-s, c);
                    }
                }
                // fallback: straight vector
                return new Vector(v.X - u.X, v.Y - u.Y);
            }
            // Straight edge tangent (from neighbor to v)
            return new Vector(v.X - u.X, v.Y - u.Y);
        }

        private static bool TryGetOtherIncidentEdge(Polygon poly, Edge current, int vertexId, out Edge other)
        {
            var (prev, next) = poly.GetIncidentEdges(vertexId);
            other = (prev.ID == current.ID) ? next : prev;
            return other != null;
        }

        private static double Distance(Point a, Point b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        private static Point Mid(Point a, Point b) => new((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
    }
}
