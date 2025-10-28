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
    public static class RelationPropagationResolver
    {
        /// <summary>
        /// Propagate a vertex movement throughout the polygon to preserve constraints and continuity.
        /// Adjusts connected edges and neighboring vertices as needed.
        /// </summary>
        /// <param name="vertexId">The ID of the vertex that was moved.</param>
        /// <param name="polygon">The polygon containing the vertex.</param>
        /// <param name="oldPos">The original position of the vertex before the move.</param>
        /// <param name="newPos">The new position of the vertex after the move.</param>
        public static void PropagateVertexMove(int vertexId, Polygon polygon, Point oldPos, Point newPos)
        {
            // Calculate the movement vector of the vertex (for potential whole-polygon shifting).
            Vector moveVector = new Vector(newPos.X - oldPos.X, newPos.Y - oldPos.Y);

            // Track visited vertices to detect if propagation loops around the entire polygon.
            HashSet<int> visited = new HashSet<int>();

            // Local helper to enforce continuity (G1 or C1) at a vertex if applicable.
            void EnforceContinuityAtVertex(int vId)
            {
                Vertex v = polygon.GetVertexById(vId);
                if (v.ContinuityType != ContinuityType.G0)
                {
                    // Use continuity resolver with isMovingControlPoint=false since this is a vertex move.
                    Continuity.ContinuityResolver.EnforceAt(vId, polygon, v.ContinuityType, false);
                }
            }

            void PropagateDirection(int startVertexId, bool clockwise)
            {
                int currentId = startVertexId;
                int hops = 0;
                int maxHops = polygon.VertexCount;

                while (hops++ < maxHops)
                {
                    // Detect loop: all vertices adjusted, fallback to whole-polygon move.
                    var incidentEdges = polygon.GetIncidentEdges(currentId);
                    Edge edge = clockwise ? incidentEdges.next : incidentEdges.prev;

                    int neighborId = (edge.V1ID == currentId) ? edge.V2ID : edge.V1ID;

                    // Check if the neighbor is already visited
                    if (visited.Contains(neighborId))
                    {
                        // If the neighbor is the *original* start vertex, we've looped.
                        if (neighborId == vertexId) // 'vertexId' from outer PropagateVertexMove scope
                        {
                            // This implies a fully constrained, closed loop. Fallback.
                            TranslatePolygon(polygon, moveVector);
                        }
                        // If it's the start OR any other visited vertex (meeting the other propagation),
                        // we must stop this direction.
                        return;
                    }

                    Vertex currentVertex = polygon.GetVertexById(currentId);
                    Vertex neighborVertex = polygon.GetVertexById(neighborId);
                    Point curPos = currentVertex.Position;
                    Point neiPos = neighborVertex.Position;

                    bool neighborWasMovedByConstraint = false;
                    switch (edge.ConstrainType)
                    {
                        case ConstrainType.Horizontal:
                            neighborVertex.SetPosition(new Point(neiPos.X, curPos.Y));
                            neighborWasMovedByConstraint = true;
                            break;

                        case ConstrainType.Diagonal45:
                            {
                                Vector d = neiPos - curPos;
                                if (d.Length < 1e-9) break;
                                Vector u1 = new Vector(1, 1); u1.Normalize();
                                Vector u2 = new Vector(1, -1); u2.Normalize();
                                double s1 = Vector.Multiply(d, u1);
                                double s2 = Vector.Multiply(d, u2);
                                Vector u = (Math.Abs(s1) >= Math.Abs(s2)) ? u1 : u2;
                                double s = (Math.Abs(s1) >= Math.Abs(s2)) ? s1 : s2;
                                neighborVertex.SetPosition(curPos + u * s);
                                neighborWasMovedByConstraint = true;
                                break;
                            }

                        case ConstrainType.FixedLength:
                            {
                                double L = edge.FixedLength;
                                if (L <= 0) break;
                                Vector dir = neiPos - curPos;
                                double dist = dir.Length;
                                if (dist < 1e-9)
                                    dir = new Vector(1, 0);
                                else
                                    dir /= dist;
                                neighborVertex.SetPosition(curPos + dir * L);
                                neighborWasMovedByConstraint = true;
                                break;
                            }

                        default:
                            neighborWasMovedByConstraint = false;
                            break;
                    }

                    EnforceContinuityAtVertex(neighborId);

                    // Jeśli sąsiad nie został przesunięty przez ograniczenie,
                    // łańcuch *ruchu* jest przerwany. Kończymy propagację w tym kierunku.
                    if (!neighborWasMovedByConstraint)
                    {
                        return;
                    }

                    // Jeśli tu dotarliśmy, sąsiad ZOSTAŁ przesunięty.
                    // Dodajemy go do odwiedzonych i kontynuujemy pętlę.
                    visited.Add(neighborId);
                    currentId = neighborId;
                }
            }


            visited.Add(vertexId);

            // 1. Enforce continuity at the initially moved vertex (adjust outgoing control handles if needed).
            EnforceContinuityAtVertex(vertexId);

            // 2. Propagate outwards clockwise and counterclockwise from the moved vertex.
            PropagateDirection(vertexId, clockwise: true);
            PropagateDirection(vertexId, clockwise: false);
        }

        /// <summary>
        /// Propagate a Bezier control-point movement, enforcing continuity and any resulting constraint propagation.
        /// </summary>
        /// <param name="edgeId">ID of the edge whose control point is moved.</param>
        /// <param name="isFirstControl">True if the first control point (near V1) is moved; false if the second (near V2).</param>
        /// <param name="newControlPos">The new position of the control point under the cursor.</param>
        /// <param name="polygon">The polygon containing this edge.</param>
        public static void PropagateControlPointMove(int edgeId, bool isFirstControl, Point newControlPos, Polygon polygon)
        {
            Edge edge = polygon.GetEdgeByOrderIndex(polygon.GetEdgeOrderIndexById(edgeId));
            if (edge.EdgeType != EdgeType.BezierCubic)
            {
                // Only handle Bezier cubic edges for control point dragging.
                return;
            }

            // 1. Update the control point to the new position (leave the other control point unchanged).
            if (isFirstControl)
            {
                // Moving CP1 (attached to V1)
                Point otherCP = edge.BezierCP2 ?? polygon.GetVertexById(edge.V2ID).Position;
                edge.SetBezierControlPoints(newControlPos, otherCP);
            }
            else
            {
                // Moving CP2 (attached to V2)
                Point otherCP = edge.BezierCP1 ?? polygon.GetVertexById(edge.V1ID).Position;
                edge.SetBezierControlPoints(otherCP, newControlPos);
            }

            // 2. Enforce continuity at the vertex corresponding to this control point.
            int vertexId = isFirstControl ? edge.V1ID : edge.V2ID;
            Vertex v = polygon.GetVertexById(vertexId);
            if (v.ContinuityType != ContinuityType.G0)
            {
                // Use isMovingControlPoint=true since the user is dragging a handle (not moving the vertex itself).
                Continuity.ContinuityResolver.EnforceAt(vertexId, polygon, v.ContinuityType, true);
            }

            // 3. Check if continuity enforcement moved a neighboring vertex (e.g., the far end of an adjacent line).
            // G1/C1 continuity code can move the opposite vertex of a connected line segment if unconstrained (to keep tangents aligned).
            var incidentEdges = polygon.GetIncidentEdges(vertexId);
            foreach (Edge incEdge in new Edge[] { incidentEdges.prev, incidentEdges.next })
            {
                if (incEdge.EdgeType == EdgeType.Line)
                {
                    // Identify the far end of this line (the vertex opposite 'v').
                    int farVertexId = (incEdge.V1ID == vertexId) ? incEdge.V2ID : incEdge.V1ID;
                    // If the continuity logic moved this far vertex (which it will do only if the line was free of constraints),
                    // we should propagate any constraints or continuities from that vertex outward.
                    Vertex farV = polygon.GetVertexById(farVertexId);
                    // We call PropagateVertexMove on the far vertex with no net initial movement (old==new position).
                    // This will enforce constraints/continuity involving 'farV' that might have been violated by its move.
                    PropagateVertexMove(farVertexId, polygon, farV.Position, farV.Position);
                }
            }
        }

        /// <summary>
        /// Handle propagation when a new constraint is applied to an edge.
        /// Should be called after the constraint is initially enforced on that edge.
        /// </summary>
        /// <param name="edge">The edge that received a new constraint.</param>
        /// <param name="polygon">The polygon containing the edge.</param>
        public static void OnConstraintApplied(Edge edge, Polygon polygon)
        {
            // After adding a constraint and using ConstraintResolver to set the edge, 
            // propagate from both endpoints in case their movement affected other relations.
            Vertex v1 = polygon.GetVertexById(edge.V1ID);
            Vertex v2 = polygon.GetVertexById(edge.V2ID);
            // Treat each endpoint as having been "moved" to its new position.
            PropagateVertexMove(v1.ID, polygon, v1.Position, v1.Position);
            PropagateVertexMove(v2.ID, polygon, v2.Position, v2.Position);
        }

        /// <summary>
        /// Utility: Check if an edge's constraint is already satisfied by the current vertex positions.
        /// </summary>
        private static bool IsConstraintSatisfied(Edge edge, Polygon polygon)
        {
            Vertex v1 = polygon.GetVertexById(edge.V1ID);
            Vertex v2 = polygon.GetVertexById(edge.V2ID);
            Point p1 = v1.Position;
            Point p2 = v2.Position;
            switch (edge.ConstrainType)
            {
                case ConstrainType.Horizontal:
                    // Horizontal if Y coordinates are (nearly) equal.
                    return Math.Abs(p1.Y - p2.Y) < 1e-6;
                case ConstrainType.Diagonal45:
                    // Diagonal if slope is ±1 (within a tolerance).
                    double dx = p2.X - p1.X;
                    double dy = p2.Y - p1.Y;
                    if (Math.Abs(dx) < 1e-6 || Math.Abs(dy) < 1e-6) return false;
                    double slope = dy / dx;
                    return Math.Abs(Math.Abs(slope) - 1.0) < 1e-3;
                case ConstrainType.FixedLength:
                    if (edge.FixedLength <= 0) return true;
                    double currentLen = Geometry.Dist(p1, p2);
                    return Math.Abs(currentLen - edge.FixedLength) < 1e-6;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Utility: Translate (move) the entire polygon by a given vector, including vertices and curve control points.
        /// </summary>
        private static void TranslatePolygon(Polygon polygon, Vector moveVec)
        {
            // Move all vertices.
            for (int i = 0; i < polygon.VertexCount; i++)
            {
                Vertex v = polygon.GetVertexByOrder(i);
                v.SetPosition(new Point(v.Position.X + moveVec.X, v.Position.Y + moveVec.Y));
            }
            // Move all Bezier control points and arc centers by the same vector, preserving relative shape.
            for (int j = 0; j < polygon.EdgeCount; j++)
            {
                Edge e = polygon.GetEdgeByOrderIndex(j);
                if (e.EdgeType == EdgeType.BezierCubic)
                {
                    if (e.BezierCP1.HasValue || e.BezierCP2.HasValue)
                    {
                        Point cp1 = e.BezierCP1 ?? polygon.GetVertexById(e.V1ID).Position;
                        Point cp2 = e.BezierCP2 ?? polygon.GetVertexById(e.V2ID).Position;
                        e.SetBezierControlPoints(
                            new Point(cp1.X + moveVec.X, cp1.Y + moveVec.Y),
                            new Point(cp2.X + moveVec.X, cp2.Y + moveVec.Y));
                    }
                }
                else if (e.EdgeType == EdgeType.Arc)
                {
                    if (e.ArcCenter.HasValue)
                    {
                        Point c = e.ArcCenter.Value;
                        e.SetArcGeometry(new Point(c.X + moveVec.X, c.Y + moveVec.Y), e.ArcRadius ?? 0);
                    }
                }
            }
        }
    }
}