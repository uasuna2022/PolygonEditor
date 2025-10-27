using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;
using Project1_PolygonEditor.Enum_classes;

namespace Project1_PolygonEditor.Models
{
    // Polygon model class. Contains a lot of getters (for multiple operations inside MainWindow. 
    // Contains many fields for easier work with polygon's fields. 
    public sealed class Polygon 
    {
        private readonly Dictionary<int, Vertex> _verticesByID;
        private readonly Dictionary<int, Edge> _edgesByID;

        private readonly List<int> _vertexOrder;
        private readonly List<int> _edgeOrder;

        private int _nextVertexIdx = 1;
        private int _nextEdgeIdx = 1;

        public bool IsClosed { get; private set; } = false;
        public int VertexCount => _vertexOrder.Count;
        public int EdgeCount => _edgeOrder.Count;   
        public Vertex? FirstVertex => VertexCount > 0 ? _verticesByID[_vertexOrder[0]] : null;
        public Vertex? LastVertex => VertexCount > 0 ? _verticesByID[_vertexOrder[^1]] : null;
        public bool CanClose => !IsClosed && VertexCount >= 3;

        public Polygon()
        {
            _verticesByID = new Dictionary<int, Vertex>();
            _edgesByID = new Dictionary<int, Edge>();
            _vertexOrder = new List<int>();
            _edgeOrder = new List<int>();
        }
        
        public void Clear()
        {
            _verticesByID.Clear();
            _edgesByID.Clear();
            _vertexOrder.Clear();
            _edgeOrder.Clear();
            _nextVertexIdx = 1;
            _nextEdgeIdx = 1;
            IsClosed = false;
        }
        
        public Vertex AddVertex(Point position)
        {
            if (IsClosed)
                throw new InvalidOperationException("The polygon is already closed!");
            Vertex newVertex = new Vertex(_nextVertexIdx, position);
            _verticesByID.Add(newVertex.ID, newVertex);
            _vertexOrder.Add(newVertex.ID);
            _nextVertexIdx++;
            return newVertex;
        }
        public Edge AddEdge(int v1ID, int v2ID)
        {
            if (IsClosed)
                throw new InvalidOperationException("The polygon is already closed!");
            if (!_verticesByID.ContainsKey(v1ID))
                throw new ArgumentException($"Vertex nr. {v1ID} not found!");
            if (!_verticesByID.ContainsKey(v2ID))
                throw new ArgumentException($"Vertex nr. {v2ID} not found!");

            Edge newEdge = new Edge(_nextEdgeIdx, v1ID, v2ID);
            _edgesByID.Add(newEdge.ID, newEdge);
            _edgeOrder.Add(newEdge.ID);
            _nextEdgeIdx++;
            return newEdge;
        }

        public int FindHitVertexIdx(Point point, double tolerance = 5.0)
        {
            for (int i = 0; i < VertexCount; i++)
            {
                Vertex v = _verticesByID[_vertexOrder[i]];
                if (tolerance * tolerance >= Math.Pow((point.X - v.Position.X), 2) + Math.Pow((point.Y - v.Position.Y), 2))
                    return i;
            }

            return -1;
        }
        
        public Edge Close()
        {
            if (!CanClose)
                throw new InvalidOperationException("Can't close the polygon!");
            Edge e = AddEdge(_vertexOrder[^1], _vertexOrder[0]);
            IsClosed = true;

            return e;
        }

        public Vertex GetVertexByOrder(int idx) => _verticesByID[_vertexOrder[idx]];
        public Vertex GetVertexById(int id) => _verticesByID[id];
        public int GetVertexOrderIndexById(int id) => _vertexOrder.IndexOf(id);
        public int GetEdgeOrderIndexById(int edgeId) => _edgeOrder.IndexOf(edgeId);
        public Edge GetEdgeByOrderIndex(int edgeOrderIndex) => _edgesByID[_edgeOrder[edgeOrderIndex]];
        public (int prevVID, int nextVID) GetNeighborsOfVertex(int vertexID)
        {
            int i = GetVertexOrderIndexById(vertexID);
            if (i < 0)
                throw new ArgumentException("Vertex not found!");
            int prev = (i != 0) ? _vertexOrder[i - 1] : _vertexOrder[^1];
            int next = (i != VertexCount - 1) ? _vertexOrder[i + 1] : _vertexOrder[0];
            return (prev, next);
        }
        public (Edge prev, Edge next) GetIncidentEdges(int vertexID)
        {
            var (prevID, nextID) = GetNeighborsOfVertex(vertexID);
            Edge prevEdge = _edgesByID[_edgeOrder.First(eid => _edgesByID[eid].V2ID == vertexID)];
            Edge nextEdge = _edgesByID[_edgeOrder.First(eid => _edgesByID[eid].V1ID == vertexID)];
            return (prevEdge, nextEdge);
        }
        public void DeleteVertex(int vertexId)
        {
            int idx = GetVertexOrderIndexById(vertexId);
            var (prevVID, nextVID) = GetNeighborsOfVertex(vertexId);

            int prevEid = _edgeOrder.First(eid => _edgesByID[eid].V1ID == prevVID && _edgesByID[eid].V2ID == vertexId);
            int nextEid = _edgeOrder.First(eid => _edgesByID[eid].V1ID == vertexId && _edgesByID[eid].V2ID == nextVID);

            _edgeOrder.Remove(prevEid);
            _edgeOrder.Remove(nextEid);
            _edgesByID.Remove(prevEid);
            _edgesByID.Remove(nextEid);

            _vertexOrder.RemoveAt(idx);
            _verticesByID.Remove(vertexId);

            if (VertexCount < 3)
            {
                Clear();
                return;
            }

            int newEid = _nextEdgeIdx++;
            Edge newE = new Edge(newEid, prevVID, nextVID);
            _edgesByID.Add(newEid, newE);

            _edgeOrder.Insert(Math.Min(_edgeOrder.Count, idx == 0 ? _edgeOrder.Count : idx - 1), newEid);
        }

        public bool TryFindNearestEdge(Point p, out int edgeOrderIndex, out Point projPoint, double tolerance = 5.0)
        {
            edgeOrderIndex = -1;
            double best = double.MaxValue;
            projPoint = default;

            for (int i = 0; i < _edgeOrder.Count; i++)
            {
                Edge e = _edgesByID[_edgeOrder[i]];
                Point a = _verticesByID[e.V1ID].Position;
                Point b = _verticesByID[e.V2ID].Position;

                var d2 = Geometry.DistSqPointToSeg(p, a, b, out Point proj);
                if (d2 < best)
                {
                    best = d2;
                    edgeOrderIndex = i;
                    projPoint = proj;
                }
            }
            return best <= tolerance * tolerance;
        }

        public Point GetEdgeMidpointByOrderIndex(int edgeOrderIndex)
        {
            if (edgeOrderIndex < 0 || edgeOrderIndex >= _edgeOrder.Count)
                throw new ArgumentOutOfRangeException(nameof(edgeOrderIndex));
            Edge e = _edgesByID[_edgeOrder[edgeOrderIndex]];
            Point a = _verticesByID[e.V1ID].Position;
            Point b = _verticesByID[e.V2ID].Position;
            return new Point((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
        }

        public Vertex InsertVertexOnEdge(int edgeOrderIndex, Point pos)
        {
            if (edgeOrderIndex < 0 || edgeOrderIndex >= _edgeOrder.Count)
                throw new ArgumentOutOfRangeException(nameof(edgeOrderIndex));

            int oldEid = _edgeOrder[edgeOrderIndex];
            Edge oldE = _edgesByID[oldEid];

            Vertex newVertex = new Vertex(_nextVertexIdx, pos);
            _verticesByID.Add(newVertex.ID, newVertex);
            _nextVertexIdx++;

            int v1Index = _vertexOrder.IndexOf(oldE.V1ID);
            _vertexOrder.Insert(v1Index + 1, newVertex.ID);

            _edgesByID.Remove(oldEid);
            _edgeOrder.RemoveAt(edgeOrderIndex);

            Edge e1 = new Edge(_nextEdgeIdx, oldE.V1ID, newVertex.ID); 
            _edgesByID.Add(e1.ID, e1); _nextEdgeIdx++;
            Edge e2 = new Edge(_nextEdgeIdx, newVertex.ID, oldE.V2ID); 
            _edgesByID.Add(e2.ID, e2); _nextEdgeIdx++;


            _edgeOrder.Insert(edgeOrderIndex, e2.ID);
            _edgeOrder.Insert(edgeOrderIndex, e1.ID);

            return newVertex;
        }

        public Vertex InsertVertexAtEdgeMidpoint(int edgeOrderIndex)
        {
            var mid = GetEdgeMidpointByOrderIndex(edgeOrderIndex);
            return InsertVertexOnEdge(edgeOrderIndex, mid);
        }

        public void SetEdgeConstraintByOrderIndex(int edgeOrderIndex, ConstrainType t, double fixedLen = 0)
        {
            Edge e = GetEdgeByOrderIndex(edgeOrderIndex);
            e.SetConstraint(t, fixedLen);
        }
        public void ClearEdgeConstraintByOrderIndex(int edgeOrderIndex)
        {
            GetEdgeByOrderIndex(edgeOrderIndex).ClearConstraint();
        }
        public int GetOtherVertexIdOfEdge(Edge e, int knownVertexId)
        {
            if (knownVertexId == e.V1ID) return e.V2ID;
            if (knownVertexId == e.V2ID) return e.V1ID;
            throw new ArgumentException("Vertex is not incident to this edge.");
        }
        public void SetEdgeTypeBezierByOrderIndex(int edgeOrderIndex, Point cp1, Point cp2)
        {
            Edge e = GetEdgeByOrderIndex(edgeOrderIndex);
            e.SetTypeBezier(cp1, cp2);
        }

        public void SetBezierControlPointsByOrderIndex(int edgeOrderIndex, Point cp1, Point cp2)
        {
            Edge e = GetEdgeByOrderIndex(edgeOrderIndex);
            e.SetBezierControlPoints(cp1, cp2);
        }

        public (Point a, Point b) GetEdgeEndpointsByOrderIndex(int i)
        {
            Edge e = GetEdgeByOrderIndex(i);
            var a = _verticesByID[e.V1ID].Position;
            var b = _verticesByID[e.V2ID].Position;
            return (a, b);
        }
    }
}
