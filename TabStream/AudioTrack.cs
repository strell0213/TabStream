using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Composition; // Added for CompositionTarget.Rendering
using System.Windows.Media.Imaging; // Added for BitmapSource
using System.Windows.Data; // Added for Binding
using System.Windows.Markup; // Added for XAML
using System.Windows.Threading; // Added for DispatcherTimer
using System.Windows.Media.Animation; // Added for DoubleAnimation
using System.Windows.Media.Effects; // Added for BlurEffect
using System.Windows.Media.Media3D; // Added for Matrix3DProjection
using System.Windows.Media.TextFormatting; // Added for TextFormatter
using System.Windows.Media.Converters; // Added for ImageSourceConverter
using System.Windows.Media; // Added for MediaPlayer

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

        // === ДОБАВЛЕНО: новые поля для индивидуального аудио ===
        private string audioFilePath; // ДОБАВЛЕНО
        private double trimStartMs = 0; // ДОБАВЛЕНО
        private double trimEndMs = 0; // ДОБАВЛЕНО
        private TextBox trimStartBox; // ДОБАВЛЕНО
        private TextBox trimEndBox; // ДОБАВЛЕНО
        private Button loadTrackButton; // ДОБАВЛЕНО

        // === ДОБАВЛЕНО: поле для MediaPlayer индивидуального трека ===
        private MediaPlayer trackPlayer; // ДОБАВЛЕНО

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

            // === ДОБАВЛЕНО: UI для загрузки и trim ===
            loadTrackButton = new Button
            {
                Content = "Загрузить трек",
                Width = 90,
                Height = 22,
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 11
            };
            loadTrackButton.Click += LoadTrackButton_Click; // ДОБАВЛЕНО
            ((StackPanel)TrackGrid.Children[0]).Children.Add(loadTrackButton);

            ((StackPanel)TrackGrid.Children[0]).Children.Add(new TextBlock { Text = "Старт:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
            trimStartBox = new TextBox { Width = 40, Height = 20, Margin = new Thickness(2,0,5,0), FontSize = 11, Text = "0" };
            trimStartBox.LostFocus += TrimBox_LostFocus; // ДОБАВЛЕНО
            ((StackPanel)TrackGrid.Children[0]).Children.Add(trimStartBox);

            ((StackPanel)TrackGrid.Children[0]).Children.Add(new TextBlock { Text = "Конец:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
            trimEndBox = new TextBox { Width = 40, Height = 20, Margin = new Thickness(2,0,5,0), FontSize = 11, Text = "0" };
            trimEndBox.LostFocus += TrimBox_LostFocus; // ДОБАВЛЕНО
            ((StackPanel)TrackGrid.Children[0]).Children.Add(trimEndBox);

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

            // === ДОБАВЛЕНО: Drag & Drop ===
            TrackCanvas.AllowDrop = true;
            TrackCanvas.Drop += TrackCanvas_Drop;
            TrackCanvas.DragOver += TrackCanvas_DragOver;

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

        // === ДОБАВЛЕНО: обработчики ===
        private void LoadTrackButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Audio Files (*.mp3;*.wav;*.m4a;*.flac)|*.mp3;*.wav;*.m4a;*.flac|All Files (*.*)|*.*",
                Title = "Выберите аудиофайл для дорожки"
            };
            if (dlg.ShowDialog() == true)
            {
                audioFilePath = dlg.FileName;
                TrackNameLabel.Text = System.IO.Path.GetFileName(audioFilePath);
            }
        }
        private void TrimBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(trimStartBox.Text, out double start))
                trimStartMs = Math.Max(0, start);
            if (double.TryParse(trimEndBox.Text, out double end))
                trimEndMs = Math.Max(0, end);
            if (trimEndMs > 0 && trimEndMs < trimStartMs)
                trimEndMs = trimStartMs;
            trimStartBox.Text = ((int)trimStartMs).ToString();
            trimEndBox.Text = ((int)trimEndMs).ToString();
        }

        // === ДОБАВЛЕНО: Drag & Drop обработчики ===
        private void TrackCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
        private void TrackCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    audioFilePath = files[0];
                    TrackNameLabel.Text = System.IO.Path.GetFileName(audioFilePath);
                }
            }
        }

        // === ДОБАВЛЕНО: методы для воспроизведения индивидуального трека ===
        public void PlayTrack(double volume = 1.0, double speed = 1.0)
        {
            if (string.IsNullOrEmpty(audioFilePath)) return;
            if (trackPlayer != null)
            {
                trackPlayer.Stop();
                trackPlayer.Close();
            }
            trackPlayer = new MediaPlayer();
            trackPlayer.Open(new Uri(audioFilePath));
            trackPlayer.MediaOpened += (s, e) =>
            {
                trackPlayer.Volume = volume;
                trackPlayer.SpeedRatio = speed;
                trackPlayer.Position = TimeSpan.FromMilliseconds(trimStartMs);
                trackPlayer.Play();
                CompositionTarget.Rendering -= CheckTrimEnd;
                CompositionTarget.Rendering += CheckTrimEnd;
            };
        }
        public void StopTrack()
        {
            if (trackPlayer != null)
            {
                trackPlayer.Stop();
                CompositionTarget.Rendering -= CheckTrimEnd;
            }
        }
        public void PauseTrack()
        {
            if (trackPlayer != null)
            {
                trackPlayer.Pause();
            }
        }
        public void ResumeTrack()
        {
            if (trackPlayer != null)
            {
                trackPlayer.Play();
            }
        }
        private void CheckTrimEnd(object sender, EventArgs e)
        {
            if (trackPlayer != null && trimEndMs > trimStartMs && trackPlayer.Position.TotalMilliseconds >= trimEndMs)
            {
                StopTrack();
            }
        }
    }
} 