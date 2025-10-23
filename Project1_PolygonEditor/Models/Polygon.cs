using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace Project1_PolygonEditor.Models
{
    public sealed class Polygon
    {
        private readonly Dictionary<int, Vertex> _vertices;
        private readonly Dictionary<int, Edge> _edges;

        private int _nextVertexIdx = 1;
        private int _nextEdgeIdx = 1;

        public bool IsClosed() => _edges.Count >= 3 && _edges.Count == _vertices.Count;
        
        public Vertex AddVertex(Point position)
        {
            Vertex newVertex = new Vertex(_nextVertexIdx, position);
            _vertices.Add(newVertex.ID, newVertex);
            _nextVertexIdx++;
            return newVertex;
        }
        public Edge AddEdge(int v1ID, int v2ID)
        {
            if (!_vertices.ContainsKey(v1ID))
                throw new ArgumentException($"Vertex nr. {v1ID} not found!");
            if (!_vertices.ContainsKey(v2ID))
                throw new ArgumentException($"Vertex nr. {v2ID} not found!");

            Edge newEdge = new Edge(_nextEdgeIdx, v1ID, v2ID);
            _edges.Add(newEdge.ID, newEdge);
            _nextEdgeIdx++;
            return newEdge;
        }

        
    }
}
