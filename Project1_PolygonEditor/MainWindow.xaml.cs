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

            VertexContinuityBadge.DrawNearVertex(DrawingCanvas, v);

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
                            if (ArcFromColleague.TryGetArcParams(_polygon, e, out var ap))
                            {
                                ArcFromColleague.Tessellate(ap, 72, (p, q) => _drawStrategy.DrawLine(p, q));
                            }
                            else
                            {
                                var A = _polygon.GetVertexById(e.V1ID).Position;
                                var B = _polygon.GetVertexById(e.V2ID).Position;
                                var O = new Point((A.X + B.X) * 0.5, (A.Y + B.Y) * 0.5);
                                double R = Polygon.Distance(A, B) * 0.5;
                                double thA = Math.Atan2(A.Y - O.Y, A.X - O.X);
                                double thB = Math.Atan2(B.Y - O.Y, B.X - O.X);
                                var ap2 = new ArcFromColleague.ArcParams
                                {
                                    Center = O,
                                    Radius = R,
                                    ThetaStart = thA,
                                    ThetaEnd = thB,
                                    Clockwise = e.ArcFlipSide
                                };
                                ArcFromColleague.Tessellate(ap2, 72, (p, q) => _drawStrategy.DrawLine(p, q));
                            }
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

                VertexContinuityBadge.DrawNearVertex(DrawingCanvas, v);
            }

            _rubberLine = null;

            // Setting an appropriate badge to all edges (if necessary)
            for (int i = 0; i < _polygon.EdgeCount; i++)
            {
                Edge e = _polygon.GetEdgeByOrderIndex(i);
                if (e.ConstrainType == ConstrainType.None) continue;

                Point midPoint = _polygon.GetEdgeMidpointByOrderIndex(i);

                EdgeConstraintBadge.DrawAt(DrawingCanvas, e, midPoint);
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
                    var (prevE, nextE) = _polygon.GetIncidentEdges(vf.Model.ID);
                    bool incidentHasArc = (prevE.EdgeType == EdgeType.Arc) || (nextE.EdgeType == EdgeType.Arc);

                    if (incidentHasArc && t == ContinuityType.C1)
                    {
                        MessageBox.Show("C1 is not supported on vertices adjacent to arcs.");
                        return;
                    }
                    if (t == ContinuityType.G1)
                    {
                        // if the opposite endpoint of an incident Arc already has G1 → block
                        bool oppositeG1 = false;
                        foreach (var ed in new[] { prevE, nextE })
                        {
                            if (ed.EdgeType != EdgeType.Arc) continue;
                            int otherId = (ed.V1ID == vf.Model.ID) ? ed.V2ID : ed.V1ID;
                            if (_polygon.GetVertexById(otherId).ContinuityType == ContinuityType.G1)
                                oppositeG1 = true;
                        }
                        if (oppositeG1)
                        {
                            MessageBox.Show("At most one endpoint of an arc may have G1 continuity.");
                            return;
                        }
                    }
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
                int prevIdx = (_edgeCtxIndex - 1 + _polygon.EdgeCount) % _polygon.EdgeCount;
                int nextIdx = (_edgeCtxIndex + 1) % _polygon.EdgeCount;
                var prev = _polygon.GetEdgeByOrderIndex(prevIdx);
                var next = _polygon.GetEdgeByOrderIndex(nextIdx);

                if (prev.EdgeType == EdgeType.Arc || next.EdgeType == EdgeType.Arc)
                {
                    MessageBox.Show("Adjacent arcs are not supported. Turn off the neighboring arc first.");
                    return;
                }
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
    }
}