using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TabStream
{
    public class AudioTrack : UserControl
    {
        public Grid TrackGrid { get; private set; }
        public Canvas TrackCanvas { get; private set; }
        public Canvas WaveformCanvas { get; private set; }
        public Line Playhead { get; private set; }
        public TextBlock TrackNameLabel { get; private set; }
        public AudioVisualizer Visualizer { get; private set; }
        
        public string TrackName { get; set; }
        public double ZoomLevel { get; set; } = 1.0;
        public double CurrentPosition { get; set; } = 0;
        public double TotalDuration { get; set; } = 0;
        
        private bool isDraggingPlayhead = false;
        private Point lastMousePosition;

        public AudioTrack(string trackName = "Track")
        {
            TrackName = trackName;
            InitializeTrack();
        }

        private void InitializeTrack()
        {
            TrackGrid = new Grid();
            TrackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            TrackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Track header panel
            StackPanel trackHeader = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            // Track name label
            TrackNameLabel = new TextBlock
            {
                Text = TrackName,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0)
            };
            trackHeader.Children.Add(TrackNameLabel);

            // Delete track button
            Button deleteButton = new Button
            {
                Content = "✕",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                Foreground = Brushes.Red,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0)
            };
            deleteButton.Click += (s, e) => OnDeleteTrack();
            trackHeader.Children.Add(deleteButton);

            Grid.SetColumn(trackHeader, 0);
            TrackGrid.Children.Add(trackHeader);

            // Track canvas container
            ScrollViewer trackScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            Grid.SetColumn(trackScrollViewer, 1);
            TrackGrid.Children.Add(trackScrollViewer);

            // Main track canvas
            TrackCanvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Height = 80,
                MinWidth = 800
            };
            trackScrollViewer.Content = TrackCanvas;

            // Border for track
            Border trackBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
            };

            // Waveform canvas
            WaveformCanvas = new Canvas
            {
                Background = Brushes.Transparent
            };
            TrackCanvas.Children.Add(WaveformCanvas);

            // Playhead
            Playhead = new Line
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = 80
            };
            TrackCanvas.Children.Add(Playhead);

            // Add mouse events for playhead dragging
            TrackCanvas.MouseLeftButtonDown += TrackCanvas_MouseLeftButtonDown;
            TrackCanvas.MouseLeftButtonUp += TrackCanvas_MouseLeftButtonUp;
            TrackCanvas.MouseMove += TrackCanvas_MouseMove;

            // Initialize visualizer
            Visualizer = new AudioVisualizer(WaveformCanvas);
            Visualizer.GenerateWaveform();

            Content = TrackGrid;
        }

        private void TrackCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(TrackCanvas);
            
            // Check if click is near playhead
            if (Math.Abs(mousePos.X - Playhead.X1) < 10)
            {
                isDraggingPlayhead = true;
                lastMousePosition = mousePos;
                TrackCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void TrackCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingPlayhead)
            {
                isDraggingPlayhead = false;
                TrackCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void TrackCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingPlayhead)
            {
                Point mousePos = e.GetPosition(TrackCanvas);
                double newX = Math.Max(0, mousePos.X);
                
                // Update playhead position
                Playhead.X1 = newX;
                Playhead.X2 = newX;
                
                // Calculate new position based on zoom level
                double canvasWidth = TrackCanvas.ActualWidth;
                if (canvasWidth > 0)
                {
                    CurrentPosition = (newX / canvasWidth) * TotalDuration;
                    CurrentPosition = Math.Max(0, Math.Min(TotalDuration, CurrentPosition));
                }
                
                // Update visualizer
                Visualizer.UpdatePlayhead(CurrentPosition, TotalDuration);
                
                e.Handled = true;
            }
        }

        public void UpdatePlayhead(double position, double totalDuration)
        {
            CurrentPosition = position;
            TotalDuration = totalDuration;
            
            if (TotalDuration > 0)
            {
                double playheadX = (CurrentPosition / TotalDuration) * TrackCanvas.ActualWidth;
                Playhead.X1 = playheadX;
                Playhead.X2 = playheadX;
                
                Visualizer.UpdatePlayhead(CurrentPosition, TotalDuration);
            }
        }

        public void SetZoom(double zoomLevel)
        {
            ZoomLevel = zoomLevel;
            
            // Update canvas width based on zoom
            double baseWidth = 800;
            double newWidth = baseWidth * zoomLevel;
            TrackCanvas.MinWidth = newWidth;
            
            // Regenerate waveform with new zoom
            Visualizer.GenerateWaveform();
            
            // Update playhead position
            UpdatePlayhead(CurrentPosition, TotalDuration);
        }

        public void SetTrackName(string name)
        {
            TrackName = name;
            TrackNameLabel.Text = name;
        }

        public event EventHandler DeleteTrackRequested;

        private void OnDeleteTrack()
        {
            DeleteTrackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
} 