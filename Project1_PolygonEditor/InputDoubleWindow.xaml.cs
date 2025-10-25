using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Project1_PolygonEditor
{
    /// <summary>
    /// Interaction logic for InputDoubleWindow.xaml
    /// </summary>
    public partial class InputDoubleWindow : Window
    {
        public double? Length { get; private set; }
        public InputDoubleWindow(string title, string label, double defaultValue)
        {
            InitializeComponent();
            Title = title;
            PromptLabel.Text = label;
            InputBox.Text = defaultValue.ToString("0.###");
            InputBox.SelectAll();
            InputBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(InputBox.Text, out double val) && val > 0)
            {
                Length = val;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a positive number.", "Invalid value",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
