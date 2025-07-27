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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TabStream2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DrawTimeRuler();
        }

        public void DrawTimeRuler()
        {
            TimeRuler.Children.Clear();

            double pixelsPerSecond = 20; // задаём фиксированную шкалу
            int maxSeconds = 10000;
            double rulerWidth = pixelsPerSecond * maxSeconds;
            TimeRuler.Width = rulerWidth;

            for (int i = 0; i <= maxSeconds; i += 5)
            {
                double x = i * pixelsPerSecond;

                Line marker = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = 20,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };

                TextBlock label = new TextBlock
                {
                    Text = TimeSpan.FromSeconds(i).ToString(@"mm\:ss"),
                    Foreground = Brushes.White,
                    FontSize = 9
                };

                Canvas.SetLeft(label, x + 2);
                Canvas.SetTop(label, 20);

                TimeRuler.Children.Add(marker);
                TimeRuler.Children.Add(label);
            }
        }

        private void TracksScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            TrackNamesScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
        }

    }
}
