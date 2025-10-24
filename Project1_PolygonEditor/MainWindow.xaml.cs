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

namespace Project1_PolygonEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WriteableBitmap? _bitmap;
        private IDrawStrategy _drawStrategy;
        public DrawingAlgorithm CurrentSectionDrawingAlgorithm { get; private set; } = DrawingAlgorithm.Library;

        private Polygon _polygon;
        public MainWindow()
        {
            InitializeComponent();
            _drawStrategy = new LibraryLineStrategy(DrawingCanvas);
            _polygon = new Polygon();

            DrawingCanvas.MouseLeftButtonDown += DrawingCanvas_MouseLeftButtonDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int w = Math.Max(1, (int)Math.Ceiling(DrawingCanvas.ActualWidth));
            int h = Math.Max(1, (int)Math.Ceiling(DrawingCanvas.ActualHeight));
            
            _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
            RasterImage.Source = _bitmap;
        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Children.Clear();
            _polygon.Clear();
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

                return;
            }

            if (idx > 0)
                return;

            Vertex v = _polygon.AddVertex(p);
            new VertexFigure(v).DrawVertex(DrawingCanvas);

            if (_polygon.VertexCount >= 2)
            {
                Point p1 = _polygon.GetVertexByOrder(_polygon.VertexCount - 2).Position;
                Point p2 = _polygon.LastVertex!.Position;
                _drawStrategy.DrawLine(p1, p2);
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
            _drawStrategy = new BresenhamLineStrategy(_bitmap!);
        } 
    }
}