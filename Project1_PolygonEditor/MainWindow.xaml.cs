using Project1_PolygonEditor.StrategyPattern;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Project1_PolygonEditor.Models;
using Project1_PolygonEditor.View;
using System.Runtime.ConstrainedExecution;
using Project1_PolygonEditor.Enum_classes;
using System.Windows.Controls.Primitives;



namespace Project1_PolygonEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IDrawStrategy _drawStrategy;
        public DrawingAlgorithm CurrentSectionDrawingAlgorithm { get; private set; } = DrawingAlgorithm.Library;

        private Polygon _polygon;
        private System.Windows.Shapes.Line? _rubberLine;
        private List<VertexFigure> _vertexFigures;

        private VertexFigure? _draggingVertex;
        private Point _dragCaptureOffset;

        private int _edgeCtxIndex = -1;
        private Point _edgeCtxPoint;

        // NEW
        private List<BezierControlPointFigure> _cpFigures = new();
        private BezierControlPointFigure? _draggingCP;

        public MainWindow()
        {
            InitializeComponent();
            _drawStrategy = new LibraryLineStrategy(DrawingCanvas);
            _polygon = new Polygon();
            _vertexFigures = new List<VertexFigure>();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Children.Clear();
            _polygon.Clear();
            _vertexFigures.Clear();
            _rubberLine = null;
            _draggingVertex = null;
            _cpFigures.Clear();
            _draggingCP = null;

            if (DrawingCanvas.IsMouseCaptured) 
                DrawingCanvas.ReleaseMouseCapture();
        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_polygon.IsClosed)
                return;

            Point p = e.GetPosition(DrawingCanvas);
            int idx = _polygon.FindHitVertexIdx(p);

            if (idx == 0 && _polygon.CanClose)
            {
                _drawStrategy.DrawLine(_polygon.LastVertex!.Position, _polygon.FirstVertex!.Position);
                Edge closingEdge = _polygon.Close();
                
                if (_rubberLine != null)
                {
                    DrawingCanvas.Children.Remove(_rubberLine);
                    _rubberLine = null;
                }
                
                return;
            }

            if (idx == 0 && !_polygon.CanClose)
                return;

            if (idx > 0)
                return;

            Vertex v = _polygon.AddVertex(p);
            VertexFigure vf = new VertexFigure(v);
            vf.DrawVertex(DrawingCanvas);
            _vertexFigures.Add(vf);
            AttachVertexContextMenu(vf);

            if (_polygon.VertexCount >= 2)
            {
                Point p1 = _polygon.GetVertexByOrder(_polygon.VertexCount - 2).Position;
                Point p2 = _polygon.LastVertex!.Position;
                _drawStrategy.DrawLine(p1, p2);

                int v1Id = _polygon.GetVertexByOrder(_polygon.VertexCount - 2).ID;
                int v2Id = v.ID;
                _polygon.AddEdge(v1Id, v2Id);
            }
        }

        private void LibraryAlgorithmRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentSectionDrawingAlgorithm = DrawingAlgorithm.Library;
            _drawStrategy = new LibraryLineStrategy(DrawingCanvas);
            RedrawAll();
        }

        private void BresenhamAlgorithmRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentSectionDrawingAlgorithm = DrawingAlgorithm.Bresenham;
            _drawStrategy = new BresenhamLineStrategy(DrawingCanvas);
            RedrawAll();
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingVertex != null)
            {
                Point cursor = e.GetPosition(DrawingCanvas);
                Point newPos = new Point(cursor.X - _dragCaptureOffset.X, cursor.Y - _dragCaptureOffset.Y);

                int draggedID = _draggingVertex.Model.ID;

                _draggingVertex.Model.SetPosition(newPos);

                RedrawAll();
                _draggingVertex = _vertexFigures.First(vf => vf.Model.ID == draggedID);
                _draggingVertex.SyncToModel();

                if (_rubberLine != null)
                {
                    DrawingCanvas.Children.Remove(_rubberLine);
                    _rubberLine = null;
                }

                return;
            }

            if (_draggingCP != null)
            {
                Point cursor = e.GetPosition(DrawingCanvas);
                // Update model CP position
                int eid = _draggingCP.EdgeId;
                var edgeIdx = _polygon.GetEdgeOrderIndexById(eid);
                var edge = _polygon.GetEdgeByOrderIndex(edgeIdx);

                if (edge.EdgeType == EdgeType.BezierCubic)
                {
                    if (_draggingCP.IsFirst)
                        edge.SetBezierControlPoints(new Point(cursor.X, cursor.Y), edge.BezierCP2!.Value);
                    else
                        edge.SetBezierControlPoints(edge.BezierCP1!.Value, new Point(cursor.X, cursor.Y));
                }

                RedrawAll(); // rebuild handles and geometry
                return;
            }


            if (_polygon.IsClosed || _polygon.VertexCount == 0)
                return;

            Point last = _polygon.LastVertex!.Position;
            Point current = e.GetPosition(DrawingCanvas);

            if (_rubberLine == null)
            {
                _rubberLine = new System.Windows.Shapes.Line
                {
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1.0,
                    StrokeDashArray = new DoubleCollection { 4, 4 }, 
                    IsHitTestVisible = false                          
                };
                DrawingCanvas.Children.Add(_rubberLine);
            }

            _rubberLine.X1 = last.X; 
            _rubberLine.Y1 = last.Y;
            _rubberLine.X2 = current.X; 
            _rubberLine.Y2 = current.Y;

        }
        private void DrawingCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_rubberLine != null)
            {
                DrawingCanvas.Children.Remove(_rubberLine);
                _rubberLine = null;
            }
        }

        private void RedrawAll()
        {
            if (DrawingCanvas == null)
                return;

            DrawingCanvas.Children.Clear();

            if (_polygon.EdgeCount > 0)
            {
                for (int i = 0; i < _polygon.EdgeCount; i++)
                {
                    var e = _polygon.GetEdgeByOrderIndex(i);
                    var a = _polygon.GetVertexById(e.V1ID).Position;
                    var b = _polygon.GetVertexById(e.V2ID).Position;

                    switch (e.EdgeType)
                    {
                        case EdgeType.Line:
                            _drawStrategy.DrawLine(a, b);
                            break;

                        case EdgeType.BezierCubic:
                            if (e.BezierCP1 == null || e.BezierCP2 == null)
                            {
                                // fallback: draw chord
                                _drawStrategy.DrawLine(a, b);
                            }
                            else
                            {
                                DrawCubicBezier(a, e.BezierCP1.Value, e.BezierCP2.Value, b);
                                DrawBezierControlsAndHandles(i);
                            }
                            break;

                        case EdgeType.Arc:
                            // (Later) – for now, chord fallback:
                            _drawStrategy.DrawLine(a, b);
                            break;
                    }
                }
            }


            _vertexFigures.Clear();
            for (int i = 0; i < _polygon.VertexCount; i++)
            {
                Vertex v = _polygon.GetVertexByOrder(i);
                VertexFigure vf = new VertexFigure(v);
                vf.DrawVertex(DrawingCanvas);
                AttachVertexContextMenu(vf);
                _vertexFigures.Add(vf);
            }

            _rubberLine = null;

            // Setting an appropriate badge to all edges (if necessary)
            for (int i = 0; i < _polygon.EdgeCount; i++)
            {
                Edge e = _polygon.GetEdgeByOrderIndex(i);
                if (e.ConstrainType == ConstrainType.None) continue;

                Point midPoint = _polygon.GetEdgeMidpointByOrderIndex(i);

                string badgeText = e.ConstrainType switch
                {
                    ConstrainType.Horizontal => "H",
                    ConstrainType.Diagonal45 => "D",
                    ConstrainType.FixedLength => $"{e.FixedLength:0.#} 🔒",
                    _ => ""
                };

                FrameworkElement badge = CreateConstraintBadge(e, badgeText);
                DrawingCanvas.Children.Add(badge);

                // This function measures the desired size of the element (an argument is a maximum available size)
                badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Size sz = badge.DesiredSize;

                Canvas.SetLeft(badge, midPoint.X - sz.Width / 2);
                Canvas.SetTop(badge, midPoint.Y - sz.Height / 2);
            }

        }
        private void AttachVertexContextMenu(VertexFigure vf)
        {
            ContextMenu cm = new ContextMenu();

            MenuItem miDelete = new MenuItem 
            { 
                Header = "Delete vertex"
            };
            miDelete.Click += (s, e) =>
            {
                int vertexID = vf.Model.ID;
                _polygon.DeleteVertex(vertexID);
                _vertexFigures.Remove(vf);
                RedrawAll();
            };

            cm.Items.Add(miDelete);
            cm.Items.Add(new Separator());

            MenuItem miSetContinuityType = new MenuItem
            {
                Header = "Set continuity type",
                IsEnabled = true
            };

            foreach (ContinuityType t in Enum.GetValues(typeof(ContinuityType)))
            {
                var item = new MenuItem 
                { 
                    Header = t.ToString(), 
                    IsCheckable = true 
                };
                item.IsChecked = (vf.Model.ContinuityType == t);
                item.Click += (s, e) =>
                {
                    vf.Model.SetContinuityType(t);
                    RedrawAll();
                };
                miSetContinuityType.Items.Add(item);
            }

            cm.Items.Add(miSetContinuityType);

            vf.Shape.ContextMenu = cm;
            vf.Shape.MouseRightButtonDown += (s, e) =>
            {
                SyncContinuityMenuState(vf.Model.ID, miSetContinuityType, vf.Model.ContinuityType);
                miDelete.IsEnabled = (_polygon.VertexCount >= 4) ? true : false;
                vf.Shape.ContextMenu.IsOpen = true;
                e.Handled = true;
            };

            vf.Shape.MouseLeftButtonDown += (s, e) =>
            {
                if (!_polygon.IsClosed)
                {
                    Point cursor = e.GetPosition(DrawingCanvas);
                    if (_polygon.FirstVertex != null && vf.Model.ID == _polygon.FirstVertex.ID && _polygon.VertexCount >= 3)
                    {
                        _drawStrategy.DrawLine(_polygon.LastVertex!.Position, _polygon.FirstVertex!.Position);
                        Edge closingEdge = _polygon.Close();

                        if (_rubberLine != null)
                        {
                            DrawingCanvas.Children.Remove(_rubberLine);
                            _rubberLine = null;
                        }

                        RedrawAll();
                        e.Handled = true;
                    }

                    return;
                }

                Point cursorPos = e.GetPosition(DrawingCanvas);
                Point center = vf.Model.Position;                     
                _draggingVertex = vf;
                _dragCaptureOffset = new Point(cursorPos.X - center.X, cursorPos.Y - center.Y);

                DrawingCanvas.CaptureMouse();
                e.Handled = true;                                   
            };

            vf.Shape.MouseLeftButtonUp += (s, e) =>
            {
                if (_draggingVertex == vf)
                {
                    _draggingVertex = null;
                    vf.Shape.ReleaseMouseCapture();
                    e.Handled = true;
                }
            };

        }

        private void SyncContinuityMenuState(int vertexId, MenuItem continuityMenu, ContinuityType current)
        {
            var (prevEdge, nextEdge) = _polygon.GetIncidentEdges(vertexId);
            bool bothLines = prevEdge.EdgeType == EdgeType.Line && nextEdge.EdgeType == EdgeType.Line;

            foreach (MenuItem mi in continuityMenu.Items)
            {
                if (Enum.TryParse(mi.Header.ToString(), out ContinuityType t))
                {
                    mi.IsChecked = (t == current);

                    if (t == ContinuityType.G0) mi.IsEnabled = true;
                    else mi.IsEnabled = !bothLines;
                }
            }
        }

        private void DrawingCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(DrawingCanvas);

            if (_polygon.TryFindNearestEdge(p, out int edgeIdx, out Point proj))
            {
                _edgeCtxIndex = edgeIdx;
                _edgeCtxPoint = proj;
                ShowEdgeContextMenu();
                RedrawAll();

                e.Handled = true;
                return;
            }

            // optionally something here 
        }

        private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingVertex != null)
            {
                DrawingCanvas.ReleaseMouseCapture();
                _draggingVertex = null;
                e.Handled = true;
            }

            if (_draggingCP != null)
            {
                DrawingCanvas.ReleaseMouseCapture();
                _draggingCP = null;
                e.Handled = true;
                return;
            }
        }

        private void ShowEdgeContextMenu()
        {
            ContextMenu cm = new ContextMenu();

            MenuItem miAddVertex = new MenuItem 
            { 
                Header = "Add new vertex" 
            };

            miAddVertex.Click += (s, _) =>
            {
                if (_edgeCtxIndex >= 0)
                {
                    _polygon.InsertVertexAtEdgeMidpoint(_edgeCtxIndex);
                    RedrawAll();
                }
            };

            cm.Items.Add(miAddVertex);
            cm.Items.Add(new Separator());

            if (_edgeCtxIndex < 0)
                return;

            Edge edge = _polygon.GetEdgeByOrderIndex(_edgeCtxIndex);
            int eid = edge.ID;

            bool NeighborHas(ConstrainType t)
            {
                int prevIdx = (_edgeCtxIndex - 1 + _polygon.EdgeCount) % _polygon.EdgeCount;
                int nextIdx = (_edgeCtxIndex + 1) % _polygon.EdgeCount;
                var prev = _polygon.GetEdgeByOrderIndex(prevIdx);
                var next = _polygon.GetEdgeByOrderIndex(nextIdx);
                return prev.ConstrainType == t || next.ConstrainType == t;
            }

            MenuItem miConstraint = new MenuItem 
            { 
                Header = "Add constraint" 
            };

            // Horizontal constraint
            MenuItem miH = new MenuItem 
            { 
                Header = "Horizontal",
                IsCheckable = true 
            };
            miH.IsChecked = edge.ConstrainType == ConstrainType.Horizontal;
            miH.IsEnabled = edge.EdgeType == EdgeType.Line && !NeighborHas(ConstrainType.Horizontal);
            miH.Click += (s, _) =>
            {
                _polygon.SetEdgeConstraintByOrderIndex(_edgeCtxIndex, ConstrainType.Horizontal);
                RedrawAll();
            };
            miConstraint.Items.Add(miH);

            // Diagonal constraint
            MenuItem mi45 = new MenuItem 
            {
                Header = "45°",
                IsCheckable = true 
            };
            mi45.IsChecked = edge.ConstrainType == ConstrainType.Diagonal45;
            mi45.IsEnabled = edge.EdgeType == EdgeType.Line && !NeighborHas(ConstrainType.Diagonal45);
            mi45.Click += (s, _) =>
            {
                _polygon.SetEdgeConstraintByOrderIndex(_edgeCtxIndex, ConstrainType.Diagonal45);
                RedrawAll();
            };
            miConstraint.Items.Add(mi45);


            // Fixed Length
            MenuItem miFixedLength = new MenuItem 
            {
                Header = "Fixed length…",
                IsCheckable = true
            };
            miFixedLength.IsChecked = edge.ConstrainType == ConstrainType.FixedLength;
            miFixedLength.IsEnabled = edge.EdgeType == EdgeType.Line;
            miFixedLength.Click += (s, _) =>
            {
                double currentLength = Polygon.Distance(_polygon.GetVertexById(edge.V1ID).Position,
                    _polygon.GetVertexById(edge.V2ID).Position);

                InputDoubleWindow w = new InputDoubleWindow("Fixed length", "Please enter a fixed value of this edge [10, 1000]:",
                    currentLength);
                w.Owner = this;
                bool? result = w.ShowDialog();

                if (result == true && w.Length.HasValue)
                {
                    _polygon.SetEdgeConstraintByOrderIndex(_edgeCtxIndex, ConstrainType.FixedLength, w.Length.Value);
                    RedrawAll();
                }
              
            };
            miConstraint.Items.Add(miFixedLength);


            cm.Items.Add(miConstraint);

            MenuItem miRemoveConstraint = new MenuItem
            {
                Header = "Remove constraint",
            };
            miRemoveConstraint.IsEnabled = edge.ConstrainType != ConstrainType.None;
            miRemoveConstraint.Click += (s, _) =>
            {
                _polygon.ClearEdgeConstraintByOrderIndex(_edgeCtxIndex);
                RedrawAll();
            };
            cm.Items.Add(miRemoveConstraint);


            // NEW
            MenuItem miBezier = new MenuItem 
            { 
                Header = "Bezier Curve", 
                IsCheckable = true, 
                IsChecked = edge.EdgeType == EdgeType.BezierCubic 
            };
            miBezier.Click += (s, _) =>
            {
                var (p0, p3) = _polygon.GetEdgeEndpointsByOrderIndex(_edgeCtxIndex);
                var (cp1, cp2) = DefaultBezierCPs(p0, p3);
                _polygon.SetEdgeTypeBezierByOrderIndex(_edgeCtxIndex, cp1, cp2);
                RedrawAll();
            };
            cm.Items.Add(miBezier);

            MenuItem miArc = new MenuItem { Header = "Arc", IsEnabled = false }; // TODO: toggle edge to Arc
            cm.Items.Add(miArc);
            
            cm.PlacementTarget = DrawingCanvas;
            cm.Placement = PlacementMode.MousePoint;
            cm.IsOpen = true;
        }

        private FrameworkElement CreateConstraintBadge(Edge e, string text)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            Border border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = (Brush)new BrushConverter().ConvertFrom("#F0F3F7")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#9AA7B0")!,
                BorderThickness = new Thickness(1),
                Child = tb,
                Padding = new Thickness(2, 0, 2, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    ShadowDepth = 0,
                    BlurRadius = 2,
                    Opacity = 0.25
                },
                IsHitTestVisible = false 
            };

            return border;
        }
        // NEW
        private (Point cp1, Point cp2) DefaultBezierCPs(Point p0, Point p3)
        {
            // Default placement of Bezier Control Points
            Point cp1 = new Point(p0.X + (p3.X - p0.X) / 3, p0.Y + (p3.Y - p0.Y) / 3);
            Point cp2 = new Point(p0.X + 2 * (p3.X - p0.X) / 3, p0.Y + 2 * (p3.Y - p0.Y) / 3);
            return (cp1, cp2);
        }
        private void DrawCubicBezier(Point p0, Point p1, Point p2, Point p3)
        {
            const int STEPS = 100; // Defining accuracy of the curve (amount of steps)
            Point prevPoint = p0;

            for (int i = 1; i <= STEPS; i++)
            {
                double t = (double)i / STEPS;
                double mt = 1 - t;

                // Bernstein basis
                double b0 = mt * mt * mt;
                double b1 = 3 * mt * mt * t;
                double b2 = 3 * mt * t * t;
                double b3 = t * t * t;

                // Overall formula for every point:
                // P(t) = p0(1-t)^3 + p1 * 3t(1-t)^2 + p2 * 3t^2(1-t) + p3t^3

                Point currPoint = new Point(b0 * p0.X + b1 * p1.X + b2 * p2.X + b3 * p3.X, 
                    b0 * p0.Y + b1 * p1.Y + b2 * p2.Y + b3 * p3.Y);

                _drawStrategy.DrawLine(prevPoint, currPoint);
                prevPoint = currPoint;
            }
        }
        private void DrawBezierControlsAndHandles(int edgeOrderIndex)
        {
            var e = _polygon.GetEdgeByOrderIndex(edgeOrderIndex);
            if (e.BezierCP1 == null || e.BezierCP2 == null) return;

            var (p0, p3) = _polygon.GetEdgeEndpointsByOrderIndex(edgeOrderIndex);
            Point p1 = e.BezierCP1.Value;
            Point p2 = e.BezierCP2.Value;

            // Define of dash line for control polygon
            void drawDashedLine(Point a, Point b)
            {
                System.Windows.Shapes.Line l = new System.Windows.Shapes.Line
                {
                    X1 = a.X,
                    Y1 = a.Y,
                    X2 = b.X,
                    Y2 = b.Y,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1.0,
                    StrokeDashArray = new DoubleCollection { 3, 3 },
                    IsHitTestVisible = false
                };
                DrawingCanvas.Children.Add(l);
            }

            drawDashedLine(p0, p1);
            drawDashedLine(p1, p2);
            drawDashedLine(p2, p3);

            // Define of control points' figures
            BezierControlPointFigure cp1Fig = new BezierControlPointFigure(e.ID, true, p1);
            BezierControlPointFigure cp2Fig = new BezierControlPointFigure(e.ID, false, p2);
            cp1Fig.DrawFigure(DrawingCanvas);
            cp2Fig.DrawFigure(DrawingCanvas);

            // Attach handlers
            AttachControlPointHandlers(cp1Fig);
            AttachControlPointHandlers(cp2Fig);

            _cpFigures.Add(cp1Fig);
            _cpFigures.Add(cp2Fig);
        }

        private void AttachControlPointHandlers(BezierControlPointFigure cpf)
        {
            cpf.Shape.MouseLeftButtonDown += (s, ev) =>
            {
                _draggingCP = cpf;

                DrawingCanvas.CaptureMouse();
                ev.Handled = true;
            };

            cpf.Shape.MouseLeftButtonUp += (s, ev) =>
            {
                if (_draggingCP == cpf)
                {
                    _draggingCP = null;
                    DrawingCanvas.ReleaseMouseCapture();
                    ev.Handled = true;
                }
            };
        }

    }
}