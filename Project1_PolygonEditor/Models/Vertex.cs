using Project1_PolygonEditor.Enum_classes;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1_PolygonEditor.Models
{
    public sealed class Vertex
    {
        public int ID { get; }
        public Point Position { get; private set; }
        public ContinuityType ContinuityType { get; private set; } = ContinuityType.G0;

        public Vertex(int id, Point position, ContinuityType continuityType)
        {
            ID = id;
            Position = position;
            ContinuityType = continuityType;
        }
        public Vertex(int id, Point position)
        {
            ID = id;
            Position = position;
        }

        public void SetPosition(Point p) => Position = p;
        public void SetContinuityType(ContinuityType type) => ContinuityType = type;
    }
}
