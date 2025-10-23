using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Project1_PolygonEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public DrawingAlgorithm CurrentSectionDrawingAlgorithm { get; private set; } = DrawingAlgorithm.Library;

        private bool _isClosed = false;
        private const double _vertexRadius = 4.0;
        private readonly List<Point> _vertices = new();
        private readonly List<Ellipse> _vertexDots = new();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isClosed)
                return;
            Point position = e.GetPosition(DrawingCanvas);

            _vertices.Add(position);

            var dot = new Ellipse
            {
                Width = 2 * _vertexRadius,
                Height = 2 * _vertexRadius,
                Fill = Brushes.DodgerBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 0.5
            };
            Canvas.SetLeft(dot, position.X - _vertexRadius);
            Canvas.SetTop(dot, position.Y - _vertexRadius);

            DrawingCanvas.Children.Add(dot);
            _vertexDots.Add(dot);

            e.Handled = true;
        }
    }
}