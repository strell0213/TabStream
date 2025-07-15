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
using System.Windows.Threading;
using System.Media;
using System.IO;
using Microsoft.Win32;

namespace TabStream
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer;
        private DispatcherTimer timer;
        private bool isPlaying = false;
        private bool isPaused = false;
        private double totalDuration = 0;
        private double currentPosition = 0;
        private bool isUserSeeking = false;
        private List<AudioTrack> audioTracks;
        private int trackCounter = 1;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAudioPlayer();
            SetupTimer();
            audioTracks = new List<AudioTrack>();
            DrawTimeRuler();
            AddDefaultTrack();
        }

        private void InitializeAudioPlayer()
        {
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            
            // Set initial volume
            mediaPlayer.Volume = VolumeSlider.Value / 100.0;
        }

        private void SetupTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
        }

        private void DrawTimeRuler()
        {
            TimeRuler.Children.Clear();
            
            // Calculate ruler width based on zoom level
            double zoomLevel = ZoomSlider?.Value ?? 1.0;
            double rulerWidth = Math.Max(800 * zoomLevel, TimeRuler.ActualWidth);
            TimeRuler.Width = rulerWidth;
            
            // Draw time markers every 5 seconds
            int maxSeconds = (int)(60 * zoomLevel);
            for (int i = 0; i <= maxSeconds; i += 5)
            {
                double x = (i / (double)maxSeconds) * rulerWidth;
                
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
                    FontSize = 10
                };
                
                Canvas.SetLeft(label, x + 2);
                Canvas.SetTop(label, 20);
                
                TimeRuler.Children.Add(marker);
                TimeRuler.Children.Add(label);
            }
        }

        private void AddDefaultTrack()
        {
            AddTrack($"Track {trackCounter}");
        }

        private void AddTrack(string trackName)
        {
            AudioTrack newTrack = new AudioTrack(trackName);
            newTrack.DeleteTrackRequested += OnDeleteTrack;
            audioTracks.Add(newTrack);
            TracksContainer.Children.Add(newTrack);
            trackCounter++;
            
            // Update all tracks with current duration
            UpdateAllTracks();
        }

        private void OnDeleteTrack(object sender, EventArgs e)
        {
            if (sender is AudioTrack track)
            {
                audioTracks.Remove(track);
                TracksContainer.Children.Remove(track);
                
                // Don't allow deleting the last track
                if (audioTracks.Count == 0)
                {
                    AddDefaultTrack();
                }
            }
        }

        private void UpdateAllTracks()
        {
            if (audioTracks == null) return;

            foreach (var track in audioTracks)
            {
                track.UpdatePlayhead(currentPosition, totalDuration);
                track.SetZoom(ZoomSlider.Value);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                if (mediaPlayer.Source == null)
                {
                    LoadAudioFile();
                    return;
                }
                
                StartPlayback();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopPlayback();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                if (isPaused)
                {
                    ResumePlayback();
                }
                else
                {
                    PausePlayback();
                }
            }
        }

        private void LoadAudioFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files (*.mp3;*.wav;*.m4a;*.flac)|*.mp3;*.wav;*.m4a;*.flac|All Files (*.*)|*.*",
                Title = "Select Audio File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    mediaPlayer.Open(new Uri(openFileDialog.FileName));
                    PlayButton.Content = "▶ Play";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StartPlayback()
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Play();
                isPlaying = true;
                isPaused = false;
                timer.Start();
                PlayButton.Content = "⏸ Pause";
                UpdatePlayhead();
            }
        }

        private void StopPlayback()
        {
            mediaPlayer.Stop();
            isPlaying = false;
            isPaused = false;
            timer.Stop();
            currentPosition = 0;
            PlayButton.Content = "▶ Play";
            UpdateProgress();
            UpdatePlayhead();
        }

        private void PausePlayback()
        {
            mediaPlayer.Pause();
            isPaused = true;
            timer.Stop();
            PlayButton.Content = "▶ Resume";
        }

        private void ResumePlayback()
        {
            mediaPlayer.Play();
            isPaused = false;
            timer.Start();
            PlayButton.Content = "⏸ Pause";
        }

        private void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            totalDuration = mediaPlayer.NaturalDuration.HasTimeSpan?
                mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds: 0;

            if (ProgressSlider != null)
            {
                ProgressSlider.Maximum = totalDuration;
            }
            if (TotalTimeText != null)
            {
                TotalTimeText.Text = TimeSpan.FromSeconds(totalDuration).ToString(@"mm\:ss");
            }
            
            // Start playback automatically after loading
            StartPlayback();
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isPlaying && !isPaused)
            {
                currentPosition = mediaPlayer.Position.TotalSeconds;
                UpdateProgress();
                UpdatePlayhead();
            }
        }

        private void UpdateProgress()
        {
            if (!isUserSeeking)
            {
                if (ProgressSlider != null)
                {
                    ProgressSlider.Value = currentPosition;
                }
                if (CurrentTimeText != null)
                {
                    CurrentTimeText.Text = TimeSpan.FromSeconds(currentPosition).ToString(@"mm\:ss");
                }
            }
        }

        private void UpdatePlayhead()
        {
            if (totalDuration > 0)
            {
                UpdateAllTracks();
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUserSeeking && mediaPlayer.Source != null)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(e.NewValue);
                mediaPlayer.Position = newPosition;
                currentPosition = e.NewValue;
                if (CurrentTimeText != null)
                {
                    CurrentTimeText.Text = TimeSpan.FromSeconds(currentPosition).ToString(@"mm\:ss");
                }
                UpdatePlayhead();
            }
        }

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isUserSeeking = true;
        }

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isUserSeeking = false;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.Volume = e.NewValue / 100.0;
            }
            if (VolumeText != null)
            {
                VolumeText.Text = $"{e.NewValue:F0}%";
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedText != null)
            {
                SpeedText.Text = $"{e.NewValue:F1}x";
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ZoomText != null)
            {
                ZoomText.Text = $"{(e.NewValue * 100):F0}%";
            }
            UpdateAllTracks();
        }

        private void AddTrackButton_Click(object sender, RoutedEventArgs e)
        {
            AddTrack($"Track {trackCounter}");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Draw initial visualizations after window is loaded
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DrawTimeRuler();
                UpdateAllTracks();
            }));
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            
            // Redraw visualizations when window is resized
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DrawTimeRuler();
                UpdateAllTracks();
            }));
        }

        protected override void OnClosed(EventArgs e)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.Close();
                mediaPlayer = null;
            }
            
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
            
            base.OnClosed(e);
        }
    }
}
