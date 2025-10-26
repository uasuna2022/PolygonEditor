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
                for (int i = 0; i < _polygon.VertexCount - 1; i++)
                {
                    var a = _polygon.GetVertexByOrder(i).Position;
                    var b = _polygon.GetVertexByOrder(i + 1).Position;
                    _drawStrategy.DrawLine(a, b);
                }
                if (_polygon.IsClosed)
                {
                    var a = _polygon.LastVertex!.Position;
                    var b = _polygon.FirstVertex!.Position;
                    _drawStrategy.DrawLine(a, b);
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

            
            // TODO
            MenuItem miBezier = new MenuItem { Header = "Bezier Curve", IsEnabled = false }; // TODO: toggle edge to Bezier
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

    }
}