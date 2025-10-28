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
using Project1_PolygonEditor.Continuity;
using System.Net;




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
        private Point _draggedVertexStartPos;


        private int _edgeCtxIndex = -1;
        private Point _edgeCtxPoint;

        private List<BezierControlPointFigure> _cpFigures;
        private BezierControlPointFigure? _draggingCP;

        private bool _autoRelationsEnabled = false;
        private Dictionary<int, ConstrainType> _inferredConstraints = new Dictionary<int, ConstrainType>();

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
            DrawingCanvas.Children.Clear();
            _polygon.Clear();
            _vertexFigures.Clear();
            _cpFigures.Clear();

            List<Point> pts = new List<Point>
            {
                new Point( 70, 240),   
                new Point(180, 120),   
                new Point(300, 110),   
                new Point(480, 200),   
                new Point(860, 200),   
                new Point(900, 280),   
                new Point(720, 360),   
                new Point(780, 520),   
                new Point(620, 640),   
                new Point(480, 560),   
                new Point(320, 520),   
                new Point(210, 320)    
            };

            var verts = new List<Vertex>();
            foreach (var p in pts) verts.Add(_polygon.AddVertex(p));

            var edgeIds = new List<int>();
            for (int i = 1; i < verts.Count; i++)
                edgeIds.Add(_polygon.AddEdge(verts[i - 1].ID, verts[i].ID).ID);

            edgeIds.Add(_polygon.Close().ID);

            int EO(int id) => _polygon.GetEdgeOrderIndexById(id);

            {
                int eid = edgeIds[1];
                var edge = _polygon.GetEdgeByOrderIndex(EO(eid));
                edge.SetTypeArc();

                _polygon.GetVertexById(verts[2].ID).SetContinuityType(ContinuityType.G1);
            }


            {
                int eid = edgeIds[0]; 
                var p0 = verts[0].Position;
                var p3 = verts[1].Position;
                Point cp1 = new Point(p0.X + 70, p0.Y - 70);
                Point cp2 = new Point(p3.X - 60, p3.Y - 10);
                _polygon.SetEdgeTypeBezierByOrderIndex(EO(eid), cp1, cp2);

                _polygon.GetVertexById(verts[0].ID).SetContinuityType(ContinuityType.G1);
            }

            {
                int eid = edgeIds[6]; 
                var p0 = verts[6].Position;
                var p3 = verts[7].Position;
                Point cp1 = new Point(p0.X + 30, p0.Y + 120);
                Point cp2 = new Point(p3.X - 90, p3.Y - 140);
                _polygon.SetEdgeTypeBezierByOrderIndex(EO(eid), cp1, cp2);

                _polygon.GetVertexById(verts[6].ID).SetContinuityType(ContinuityType.G1);
            }


            {
                int eid = edgeIds[9]; 
                var p0 = verts[10].Position; 
                var p3 = verts[9].Position;
                Point cp1 = new Point(p0.X - 20, p0.Y + 80);
                Point cp2 = new Point(p3.X + 80, p3.Y + 40);
                _polygon.SetEdgeTypeBezierByOrderIndex(EO(eid), cp1, cp2);

                _polygon.GetVertexById(verts[10].ID).SetContinuityType(ContinuityType.C1);
            }

            {
                int eid = edgeIds[3]; 
                _polygon.SetEdgeConstraintByOrderIndex(EO(eid), ConstrainType.Horizontal);
            }


            {
                int eid = edgeIds[4]; 
                _polygon.SetEdgeConstraintByOrderIndex(EO(eid), ConstrainType.Diagonal45);
            }

            {
                int eid = edgeIds[8]; 
                _polygon.SetEdgeConstraintByOrderIndex(EO(eid), ConstrainType.FixedLength, 167); 
            }

            _polygon.GetVertexById(verts[9].ID).SetContinuityType(ContinuityType.G1);

            void SettleAll()
            {
                for (int i = 0; i < _polygon.VertexCount; i++)
                {
                    var v = _polygon.GetVertexByOrder(i);
                    if (v.ContinuityType != ContinuityType.G0)
                        Continuity.ContinuityResolver.EnforceAt(v.ID, _polygon, v.ContinuityType, false);
                }

                for (int i = 0; i < _polygon.EdgeCount; i++)
                {
                    var e = _polygon.GetEdgeByOrderIndex(i);

                    if (e.EdgeType == EdgeType.Arc && ArcClass.TryGetArcParams(_polygon, e, out var ap))
                        e.SetArcGeometry(ap.Center, ap.Radius);

                    EdgeConstraints.ConstraintResolver.EnforceAtEdge(e, _polygon);
                    RelationPropagationResolver.OnConstraintApplied(e, _polygon);
                }

                for (int i = 0; i < _polygon.VertexCount; i++)
                {
                    var v = _polygon.GetVertexByOrder(i);
                    RelationPropagationResolver.PropagateVertexMove(v.ID, _polygon, v.Position, v.Position);
                }
            }

            SettleAll();
            RedrawAll();
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
                RelationPropagationResolver.PropagateVertexMove(draggedID, _polygon, _draggedVertexStartPos, newPos);

                Continuity.ContinuityResolver.EnforceAt(_draggingVertex.Model.ID, _polygon,
                    _draggingVertex.Model.ContinuityType, false);

                // Enforce all constraints on edges connected to this vertex
                var (prev, next) = _polygon.GetIncidentEdges(_draggingVertex.Model.ID);
                EdgeConstraints.ConstraintResolver.EnforceAtEdge(prev, _polygon, true);
                EdgeConstraints.ConstraintResolver.EnforceAtEdge(next, _polygon, true);

                // Automatic relations logic
                _inferredConstraints.Clear(); 

                if (_autoRelationsEnabled)
                {
                    // Get incident edges
                    var (prevEdge, nextEdge) = _polygon.GetIncidentEdges(_draggingVertex.Model.ID);

                    foreach (Edge edge in new[] { prevEdge, nextEdge })
                    {
                        // Automatic relations are being checked only on line edges with no constraints 
                        if (edge.ConstrainType == ConstrainType.None && edge.EdgeType == EdgeType.Line)
                        {
                            Point p1 = _polygon.GetVertexById(edge.V1ID).Position;
                            Point p2 = _polygon.GetVertexById(edge.V2ID).Position;

                            // Check if they follow the rules
                            if (Geometry.IsHorizontal(p1, p2))
                            {
                                _inferredConstraints[edge.ID] = ConstrainType.Horizontal;
                            }
                            else if (Geometry.IsDiagonal45(p1, p2))
                            {
                                _inferredConstraints[edge.ID] = ConstrainType.Diagonal45;
                            }
                        }
                    }
                }

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
                    // put the dragged CP exactly under the cursor
                    if (_draggingCP.IsFirst)
                        edge.SetBezierControlPoints(new Point(cursor.X, cursor.Y), edge.BezierCP2!.Value);
                    else
                        edge.SetBezierControlPoints(edge.BezierCP1!.Value, new Point(cursor.X, cursor.Y));

                    edge.NoteHandleMove(_draggingCP.IsFirst);
                    _polygon.DraggedEdgeId = edge.ID;
                    _polygon.DraggedHandleIsFirst = _draggingCP.IsFirst;

                    // enforce continuity at this end (this may move the opposite handle)
                    int vertexIdAtThisEnd = _draggingCP.IsFirst ? edge.V1ID : edge.V2ID;
                    var vType = _polygon.GetVertexById(vertexIdAtThisEnd).ContinuityType;

                    RelationPropagationResolver.PropagateControlPointMove(edge.ID, _draggingCP.IsFirst,
                        new Point(cursor.X, cursor.Y), _polygon);

                }

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
                            if (ArcClass.TryGetArcParams(_polygon, e, out var ap))
                            {
                                ArcClass.Tessellate(ap, 72, (p, q) => _drawStrategy.DrawLine(p, q));
                            }
                            else
                            {
                                var A = _polygon.GetVertexById(e.V1ID).Position;
                                var B = _polygon.GetVertexById(e.V2ID).Position;
                                var O = new Point((A.X + B.X) * 0.5, (A.Y + B.Y) * 0.5);
                                double R = Geometry.Dist(A, B) * 0.5;
                                double thA = Math.Atan2(A.Y - O.Y, A.X - O.X);
                                double thB = Math.Atan2(B.Y - O.Y, B.X - O.X);
                                var ap2 = new ArcClass.ArcParams
                                {
                                    Center = O,
                                    Radius = R,
                                    ThetaStart = thA,
                                    ThetaEnd = thB,
                                    Clockwise = e.ArcFlipSide
                                };
                                ArcClass.Tessellate(ap2, 72, (p, q) => _drawStrategy.DrawLine(p, q));
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
                Point midPoint = _polygon.GetEdgeMidpointByOrderIndex(i);
                if (e.ConstrainType != ConstrainType.None)
                {
                    EdgeConstraintBadge.DrawAt(DrawingCanvas, e, midPoint);
                }
                else if (_inferredConstraints.ContainsKey(e.ID))
                {
                    DrawTemporaryBadge(_inferredConstraints[e.ID], midPoint);
                }
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
                    // If we can't apply chosen continuity type 
                    if (!Enum.TryParse(item.Header.ToString(), out ContinuityType picked))
                        return;
                    string why;
                    if (!CanApplyContinuity(vf.Model.ID, picked, out why))
                    {
                        MessageBox.Show(why);
                        SyncContinuityMenuState(vf.Model.ID, miSetContinuityType, vf.Model.ContinuityType);
                        return;
                    }
                    vf.Model.SetContinuityType(t);
                    
                    Continuity.ContinuityResolver.EnforceAt(vf.Model.ID, _polygon, vf.Model.ContinuityType,
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

                _draggedVertexStartPos = center;

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
            bool hasArc = prevEdge.EdgeType == EdgeType.Arc || nextEdge.EdgeType == EdgeType.Arc;

            bool oppositeArcEndAlreadyG1 = false;
            if (hasArc)
            {
                foreach (var ed in new[] { prevEdge, nextEdge })
                {
                    if (ed.EdgeType != EdgeType.Arc) continue;
                    int otherId = (ed.V1ID == vertexId) ? ed.V2ID : ed.V1ID;
                    if (_polygon.GetVertexById(otherId).ContinuityType == ContinuityType.G1)
                    {
                        oppositeArcEndAlreadyG1 = true;
                        break;
                    }
                }
            }

            foreach (MenuItem mi in continuityMenu.Items)
            {
                if (!Enum.TryParse(mi.Header.ToString(), out ContinuityType t))
                    continue;

                mi.IsChecked = (t == current);

                if (t == ContinuityType.G0)
                {
                    mi.IsEnabled = true;
                    continue;
                }

                bool enabled = true;
                if (bothLines) 
                    enabled = false;
                if (hasArc && t == ContinuityType.C1) enabled = false; 
                if (hasArc && t == ContinuityType.G1 && oppositeArcEndAlreadyG1) enabled = false; 

                mi.IsEnabled = enabled;
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
                // If there were some inferred relations - set them as constant
                if (_autoRelationsEnabled && _inferredConstraints.Count > 0)
                {
                    foreach (var kvp in _inferredConstraints)
                    {
                        int edgeId = kvp.Key;
                        ConstrainType type = kvp.Value;

                        _polygon.SetEdgeConstraintByOrderIndex(
                            _polygon.GetEdgeOrderIndexById(edgeId),
                            type);
                    }
                }

                _inferredConstraints.Clear(); // Clean the dictionary, as all possible inferred relations are handled

                DrawingCanvas.ReleaseMouseCapture();
                _draggingVertex = null;
                e.Handled = true;
                RedrawAll();
            }

            if (_draggingCP != null)
            {
                DrawingCanvas.ReleaseMouseCapture();
                _draggingCP = null;
                e.Handled = true;
                _polygon.DraggedEdgeId = null;
                _polygon.DraggedHandleIsFirst = null;
                _inferredConstraints.Clear();
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
                RelationPropagationResolver.OnConstraintApplied(e, _polygon);

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
                RelationPropagationResolver.OnConstraintApplied(e, _polygon);
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
                double currentLength = Geometry.Dist(_polygon.GetVertexById(edge.V1ID).Position,
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
                    RelationPropagationResolver.OnConstraintApplied(e, _polygon);
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
                var v1 = _polygon.GetVertexById(edge.V1ID).Position;
                var v2 = _polygon.GetVertexById(edge.V2ID).Position;
                Point mid = new Point((v1.X + v2.X) / 2.0, (v1.Y + v2.Y) / 2.0);
                Vector d = new Vector(v2.X - v1.X, v2.Y - v1.Y);
                if (d.Length > 1e-6)
                {
                    Vector n = new Vector(-d.Y, d.X);
                    n.Normalize();
                    double offset = d.Length / 2.0;
                    edge.ArcCenter = new Point(mid.X + n.X * offset, mid.Y + n.Y * offset);
                    edge.ArcRadius = offset;
                    edge.ArcFlipSide = false;   // or compute based on user choice
                }
                RedrawAll();
            };
            cm.Items.Add(miArc);

            cm.PlacementTarget = DrawingCanvas;
            cm.Placement = PlacementMode.MousePoint;
            cm.IsOpen = true;
        }

        private (Point cp1, Point cp2) DefaultBezierCPs(Point p0, Point p3)   // Default placement of Bezier Control Points
        {
            Point cp1 = new Point(p0.X + (p3.X - p0.X) / 3, p0.Y + (p3.Y - p0.Y) / 3);
            Point cp2 = new Point(p0.X + 2 * (p3.X - p0.X) / 3, p0.Y + 2 * (p3.Y - p0.Y) / 3);
            return (cp1, cp2);
        }
        private void DrawCubicBezier(Point p0, Point p1, Point p2, Point p3)
        {
            double NSteps = 100;
            //double NSteps = Geometry.Dist(p3, p2) + Geometry.Dist(p2, p1) + Geometry.Dist(p1, p0);

            // Changing to polynomial basis: 
            // A0 = V0, A1 = 3(V1-V0), A2 = 3(V2-2V1+V0), A3 = V3-3V2+3V1-V0 
            double a3_x = -p0.X + 3 * p1.X - 3 * p2.X + p3.X;
            double a2_x = 3 * p0.X - 6 * p1.X + 3 * p2.X;
            double a1_x = -3 * p0.X + 3 * p1.X;
            double a0_x = p0.X;

            double a3_y = -p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y;
            double a2_y = 3 * p0.Y - 6 * p1.Y + 3 * p2.Y;
            double a1_y = -3 * p0.Y + 3 * p1.Y;
            double a0_y = p0.Y;

            double dt = 1.0 / NSteps;
            double dt2 = dt * dt;
            double dt3 = dt2 * dt;

            // Forward differences for x
            double d1x = a3_x * dt3 + a2_x * dt2 + a1_x * dt;     
            double d2x = 6 * a3_x * dt3 + 2 * a2_x * dt2;           
            double d3x = 6 * a3_x * dt3;                      

            // Forward differences for y
            double d1y = a3_y * dt3 + a2_y * dt2 + a1_y * dt;      
            double d2y = 6 * a3_y * dt3 + 2 * a2_y * dt2;           
            double d3y = 6 * a3_y * dt3;                   

            // Start at t=0
            double x = a0_x, y = a0_y;
            Point prev = new(Math.Round(x), Math.Round(y));

            // Plot first point
            for (int i = 0; i < NSteps; i++)
            {
                // Advance with only additions (not making multiple multiplications)
                x += d1x; 
                y += d1y;
                d1x += d2x;
                d1y += d2y;
                d2x += d3x;
                d2y += d3y;

                Point curr = new(Math.Round(x), Math.Round(y));
                _drawStrategy.DrawLine(prev, curr);
                prev = curr;
            }

            // Ensure finish to be exactly at P(1) = p3
            _drawStrategy.DrawLine(prev, new Point(p3.X, p3.Y));
            return;

            /* Slow algo without forward-differencing and using Bernstein Basis
             * 
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
            */
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

        // Function serves to check if a user can apply a certain type of continuity to chosen vertex.
        private bool CanApplyContinuity(int vertexId, ContinuityType t, out string reason)
        {
            reason = string.Empty;
            if (t == ContinuityType.G0) 
                return true;

            var (prevE, nextE) = _polygon.GetIncidentEdges(vertexId);
            bool incidentHasArc = (prevE.EdgeType == EdgeType.Arc) || (nextE.EdgeType == EdgeType.Arc);

            // C1 is never allowed on vertices adjacent to arcs.
            if (incidentHasArc && t == ContinuityType.C1)
            {
                reason = "C1 is not supported on vertices adjacent to arcs.";
                return false;
            }

            // For G1 if this vertex touches an arc, the opposite arc end must not be already G1.
            if (t == ContinuityType.G1)
            {
                foreach (var ed in new[] { prevE, nextE })
                {
                    if (ed.EdgeType != EdgeType.Arc) 
                        continue;
                    int otherId = (ed.V1ID == vertexId) ? ed.V2ID : ed.V1ID;
                    if (_polygon.GetVertexById(otherId).ContinuityType == ContinuityType.G1)
                    {
                        reason = "Only one endpoint of an arc may have G1 continuity.";
                        return false;
                    }
                }
            }

            // If both incident edges are straight lines, only G0 is meaningful - disable others.
            bool bothLines = prevE.EdgeType == EdgeType.Line && nextE.EdgeType == EdgeType.Line;
            if (bothLines && t != ContinuityType.G0)
            {
                reason = "Continuity above G0 requires at least one curved segment.";
                return false;
            }

            return true;
        }

        private void AutoRelationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _autoRelationsEnabled = true;
        }

        private void AutoRelationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _autoRelationsEnabled = false;
        }

        // Helper method to draw temporary badge
        private void DrawTemporaryBadge(ConstrainType type, Point midPoint)
        {
            string text = "";
            switch (type)
            {
                case ConstrainType.Horizontal: 
                    text = "H"; 
                    break;
                case ConstrainType.Diagonal45: 
                    text = "D"; 
                    break;
                default: 
                    return; 
            }

            TextBlock badge = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Gray, 
                Opacity = 0.8,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(badge, midPoint.X + 5);
            Canvas.SetTop(badge, midPoint.Y - 10);
            DrawingCanvas.Children.Add(badge);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UserManualMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UserManualWindow w = new UserManualWindow();
            w.Owner = this;
            w.ShowDialog();
        }
    }
}