using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1_PolygonEditor.StrategyPattern
{
    public interface IDrawStrategy
    {
        void DrawLine(System.Windows.Point p1, System.Windows.Point p2);
    }
}
