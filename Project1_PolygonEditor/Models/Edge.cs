using Project1_PolygonEditor.Enum_classes;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1_PolygonEditor.Models
{
    public sealed class Edge
    {
        public int ID { get; }
        public int V1ID { get; private set; }
        public int V2ID { get; private set; }
        public ConstrainType ConstrainType { get; private set; } = ConstrainType.None;
        public EdgeType EdgeType { get; private set; } = EdgeType.Line;

        public Point? BezierCP1 { get; private set; }
        public Point? BezierCP2 { get; private set; }

        public double FixedLength { get; private set; } = 0;

        public Point? ArcCenter { get; private set; }
        public double? ArcRadius { get; private set; }


        public Edge(int id, int v1id, int v2id)
        {
            ID = id;
            V1ID = v1id;
            V2ID = v2id;
        }

        public void SetTypeLine()
        {
            EdgeType = EdgeType.Line;
            ConstrainType = ConstrainType.None;
        }
        public void SetTypeBezier(Point cp1, Point cp2)
        {
            EdgeType = EdgeType.BezierCubic;
            ConstrainType = ConstrainType.None;
            BezierCP1 = cp1;
            BezierCP2 = cp2;
        }
        public void SetTypeArc(Point center, double r)
        {
            EdgeType = EdgeType.Arc;
            ConstrainType = ConstrainType.None;
            ArcCenter = center;
            ArcRadius = r;
        }

        public void SetConstraint(ConstrainType newConstrainType, double fixedLength = 0)
        {
            ConstrainType = newConstrainType;
            FixedLength = (fixedLength > 0) ? fixedLength : 0;
        }
        public void ClearConstraint()
        {
            ConstrainType = ConstrainType.None;
            FixedLength = 0;
        }

        public void SetBezierControlPoints(Point cp1, Point cp2)
        {
            if (EdgeType != EdgeType.BezierCubic)
                throw new ArgumentException($"The edge (id: {ID}) is not of Bezier type!");

            BezierCP1 = cp1;
            BezierCP2 = cp2;
        }
        public void SetArcGeometry(Point center, double r)
        {
            if (EdgeType != EdgeType.Arc)
                throw new ArgumentException($"The edge (id: {ID}) is not of arc type!");

            ArcCenter = center;
            ArcRadius = r;
        }

        public override string ToString() => $"Edge nr. {ID}: {V1ID}->{V2ID}, {EdgeType}, {ConstrainType}";
    }
}
