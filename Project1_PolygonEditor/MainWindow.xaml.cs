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

        private List<BezierControlPointFigure> _cpFigures;
        private BezierControlPointFigure? _draggingCP;

        public MainWindow()
        {
            InitializeComponent();
            _drawStrategy = new LibraryLineStrategy(DrawingCanvas);
            _polygon = new Polygon();
            _vertexFigures = new List<VertexFigure>();
            _cpFigures = new List<BezierControlPointFigure>();
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
                Continuity.ContinuityResolver.EnforceAt(_draggingVertex.Model.ID, _polygon,
                    _draggingVertex.Model.ContinuityType, false);

                // Enforce all constraints on edges connected to this vertex
                var (prev, next) = _polygon.GetIncidentEdges(_draggingVertex.Model.ID);
                EdgeConstraints.ConstraintResolver.EnforceAtEdge(prev, _polygon, true);
                EdgeConstraints.ConstraintResolver.EnforceAtEdge(next, _polygon, true);


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

                int eid = _draggingCP.EdgeId;
                var edgeIdx = _polygon.GetEdgeOrderIndexById(eid);
                var edge = _polygon.GetEdgeByOrderIndex(edgeIdx);

                if (edge.EdgeType == EdgeType.BezierCubic)
                {
                    // 1) put the dragged CP exactly under the cursor
                    if (_draggingCP.IsFirst)
                        edge.SetBezierControlPoints(new Point(cursor.X, cursor.Y), edge.BezierCP2!.Value);
                    else
                        edge.SetBezierControlPoints(edge.BezierCP1!.Value, new Point(cursor.X, cursor.Y));

                    // 2) enforce continuity at THIS end (this may move the opposite handle)
                    int vertexIdAtThisEnd = _draggingCP.IsFirst ? edge.V1ID : edge.V2ID;
                    var vType = _polygon.GetVertexById(vertexIdAtThisEnd).ContinuityType;

                    // (the isMovingControlPoint flag is optional in our latest G1/C1; safe to keep true)
                    Continuity.ContinuityResolver.EnforceAt(vertexIdAtThisEnd, _polygon, vType, true);
                }

                // 3) redraw with adjusted handles
                RedrawAll();
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
                                throw new ArgumentException("No control points defined!");
                            else
                            {
                                DrawCubicBezier(a, e.BezierCP1.Value, e.BezierCP2.Value, b);
                                DrawBezierControlsAndHandles(i);
                            }
                            break;

                        case EdgeType.Arc:
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
                EnforceAllConstraints();   
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
                    Continuity.ContinuityResolver.EnforceAt(
                        vf.Model.ID,
                        _polygon,
                        vf.Model.ContinuityType,
                        false   // we are not dragging a control point
                    );
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
                    EnforceAllConstraints();
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
                var e = _polygon.GetEdgeByOrderIndex(_edgeCtxIndex);
                EdgeConstraints.ConstraintResolver.EnforceAtEdge(e, _polygon);
                RedrawAll();

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
                var e = _polygon.GetEdgeByOrderIndex(_edgeCtxIndex);
                EdgeConstraints.ConstraintResolver.EnforceAtEdge(e, _polygon);
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
                    var e = _polygon.GetEdgeByOrderIndex(_edgeCtxIndex);
                    EdgeConstraints.ConstraintResolver.EnforceAtEdge(e, _polygon);
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

            MenuItem miArc = new MenuItem
            {
                Header = "Arc",
                IsCheckable = true,
                IsChecked = edge.EdgeType == EdgeType.Arc
            };
            miArc.Click += (s, _) =>
            {
                edge.SetTypeArc();
                RedrawAll();
            };
            cm.Items.Add(miArc);

            MenuItem miFlip = new MenuItem { Header = "Flip arc side" };
            miFlip.IsEnabled = edge.EdgeType == EdgeType.Arc;
            miFlip.Click += (s, _) =>
            {
                edge.SwitchArcSide();
                RedrawAll();
            };
            cm.Items.Add(miFlip);


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

        private void EnforceAllConstraints()
        {
            for (int i = 0; i < _polygon.EdgeCount; i++)
            {
                var e = _polygon.GetEdgeByOrderIndex(i);
                EdgeConstraints.ConstraintResolver.EnforceAtEdge(e, _polygon);
            }
        }

        /*
        // NEW
        private static double AngleOf(Point p) => Math.Atan2(p.Y, p.X);

        private void DrawArcPolyline(Point center, double radius, double startAngle, double endAngle, bool clockwise)
        {
            const int STEPS = 72; // smooth enough; increase if needed
            double sweep = endAngle - startAngle;
            if (clockwise && sweep > 0) sweep -= 2 * Math.PI;
            if (!clockwise && sweep < 0) sweep += 2 * Math.PI;

            double dt = sweep / STEPS;
            double t = startAngle;
            Point prev = new Point(center.X + radius * Math.Cos(t), center.Y + radius * Math.Sin(t));

            for (int i = 1; i <= STEPS; i++)
            {
                t += dt;
                Point curr = new Point(center.X + radius * Math.Cos(t), center.Y + radius * Math.Sin(t));
                _drawStrategy.DrawLine(prev, curr);
                prev = curr;
            }
        }
        private bool TryComputeArc(Edge e, out Point O, out double R, out double a0, out double a1, out bool clockwise)
        {
            var A = _polygon.GetVertexById(e.V1ID).Position;
            var B = _polygon.GetVertexById(e.V2ID).Position;

            // Continuity at ends (your vertex menu already sets this)
            var vA = _polygon.GetVertexById(e.V1ID).ContinuityType;
            var vB = _polygon.GetVertexById(e.V2ID).ContinuityType;

            // Enforce "only one end may be G1"
            bool g1A = vA == ContinuityType.G1;
            bool g1B = vB == ContinuityType.G1;
            if (g1A && g1B) g1B = false; // prefer start end

            // Helper lines
            Point M = new Point((A.X + B.X) * 0.5, (A.Y + B.Y) * 0.5);
            Vector AB = new Vector(B.X - A.X, B.Y - A.Y);
            double L = Math.Sqrt(AB.X * AB.X + AB.Y * AB.Y);
            if (L < 1e-6) { O = default; R = 0; a0 = a1 = 0; clockwise = false; return false; }

            // Perpendicular-bisector of AB: through M with direction n = perp(AB)
            Vector n = new Vector(-AB.Y, AB.X);

            // Default: G0–G0 → semicircle with diameter AB
            if (!g1A && !g1B)
            {
                O = M;
                R = L * 0.5;
                // start/end angles
                a0 = Math.Atan2(A.Y - O.Y, A.X - O.X);
                a1 = Math.Atan2(B.Y - O.Y, B.X - O.X);

                // choose side using Edge.ArcFlipSide
                clockwise = e.ArcFlipSide; // false = CCW (bulge left of AB), true = CW
                return true;
            }

            // Build line through the G1 end, perpendicular to tangent there
            // Tangent at A (incoming from previous edge) ~ normalize( A - Prev(A) )
            Point? prevOfA = TryPrevVertexAround(A, e.V1ID);
            Point? nextOfB = TryNextVertexAround(B, e.V2ID);

            Vector tangent; // unit
            Point anchor;   // A or B

            if (g1A && prevOfA.HasValue)
            {
                Vector tA = new Vector(A.X - prevOfA.Value.X, A.Y - prevOfA.Value.Y);
                if (tA.Length < 1e-6) { g1A = false; } else { tA.Normalize(); tangent = tA; anchor = A; goto Solve; }
            }
            if (g1B && nextOfB.HasValue)
            {
                Vector tB = new Vector(nextOfB.Value.X - B.X, nextOfB.Value.Y - B.Y);
                if (tB.Length < 1e-6) { g1B = false; } else { tB.Normalize(); tangent = tB; anchor = B; goto Solve; }
            }

            // Degenerate → fallback to semicircle
            O = M; R = L * 0.5;
            a0 = Math.Atan2(A.Y - O.Y, A.X - O.X);
            a1 = Math.Atan2(B.Y - O.Y, B.X - O.X);
            clockwise = e.ArcFlipSide;
            return true;

        Solve:
            // Center must lie on:
            // 1) line through 'anchor' with direction perp(tangent)
            // 2) perpendicular bisector of AB: through M, direction n
            Vector d1 = new Vector(-tangent.Y, tangent.X); // perp to tangent
            if (d1.Length < 1e-9 || n.Length < 1e-9) { O = default; R = 0; a0 = a1 = 0; clockwise = false; return false; }

            // Solve intersection: anchor + s*d1 = M + t*n
            // 2x2 system [d1  -n][s;t] = (M - anchor)
            double det = d1.X * (-n.Y) - d1.Y * (-n.X);
            if (Math.Abs(det) < 1e-8)
            {
                // Nearly parallel → fallback semicircle
                O = M; R = L * 0.5;
                a0 = Math.Atan2(A.Y - O.Y, A.X - O.X);
                a1 = Math.Atan2(B.Y - O.Y, B.X - O.X);
                clockwise = e.ArcFlipSide;
                return true;
            }
            Vector rhs = new Vector(M.X - anchor.X, M.Y - anchor.Y);
            double s = (rhs.X * (-n.Y) - rhs.Y * (-n.X)) / det;
            O = new Point(anchor.X + s * d1.X, anchor.Y + s * d1.Y);

            R = Math.Sqrt((A.X - O.X) * (A.X - O.X) + (A.Y - O.Y) * (A.Y - O.Y));
            a0 = Math.Atan2(A.Y - O.Y, A.X - O.X);
            a1 = Math.Atan2(B.Y - O.Y, B.X - O.X);

            // Choose direction: if G1 at A, the **outgoing** arc tangent at A must match 'tangent'
            // Arc tangent at angle θ is perpendicular to (O->point). For CCW, tangent direction at A is perp((A - O)) rotating CCW.
            // We’ll check both directions and pick the one whose tangent at A matches best; then allow Flip to invert.
            bool ccwPreferred = true;
            {
                // CCW tangent at A is perpCCW(A-O) = (- (A.Y - O.Y), (A.X - O.X))
                Vector r = new Vector(A.X - O.X, A.Y - O.Y);
                Vector t_ccw = new Vector(-r.Y, r.X); t_ccw.Normalize();
                Vector t_cw = new Vector(r.Y, -r.X); t_cw.Normalize();
                double dCCW = Math.Abs(1 - Math.Abs(t_ccw.X * tangent.X + t_ccw.Y * tangent.Y));
                double dCW = Math.Abs(1 - Math.Abs(t_cw.X * tangent.X + t_cw.Y * tangent.Y));
                ccwPreferred = dCCW <= dCW;
            }

            clockwise = !ccwPreferred;
            // Apply user flip
            if (e.ArcFlipSide) clockwise = !clockwise;

            return true;
        }

        // Helpers to fetch neighbors needed for tangents
        private Point? TryPrevVertexAround(Point A, int vId)
        {
            try { var (prevId, _) = _polygon.GetNeighborsOfVertex(vId); return _polygon.GetVertexById(prevId).Position; }
            catch { return null; }
        }
        private Point? TryNextVertexAround(Point B, int vId)
        {
            try { var (_, nextId) = _polygon.GetNeighborsOfVertex(vId); return _polygon.GetVertexById(nextId).Position; }
            catch { return null; }
        }

        */

    }
}