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
        public MainWindow()
        {
            InitializeComponent();
            _drawStrategy = new LibraryLineStrategy(DrawingCanvas);
            _polygon = new Polygon();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (_polygon.VertexCount >= 2)
            {
                _polygon.DeleteVertex(_polygon.GetVertexByOrder(2).ID);
                RedrawAll();
                return;
            }
            
            DrawingCanvas.Children.Clear();
            _polygon.Clear();
            _rubberLine = null;
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
            new VertexFigure(v).DrawVertex(DrawingCanvas);

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
        }

        private void BresenhamAlgorithmRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentSectionDrawingAlgorithm = DrawingAlgorithm.Bresenham;
            _drawStrategy = new BresenhamLineStrategy(DrawingCanvas);
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
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

            for (int i = 0; i < _polygon.VertexCount; i++)
            {
                Vertex v = _polygon.GetVertexByOrder(i);
                new VertexFigure(v).DrawVertex(DrawingCanvas);
                //AttachVertexContextMenu(vf);
            }

            _rubberLine = null;
        }
    }
}