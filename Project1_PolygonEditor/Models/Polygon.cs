using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace Project1_PolygonEditor.Models
{
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
    }
}
