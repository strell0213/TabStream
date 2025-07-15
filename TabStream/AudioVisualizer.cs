using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TabStream
{
    public class AudioVisualizer
    {
        private Canvas canvas;
        private List<Rectangle> waveformBars;
        private Random random;

        public AudioVisualizer(Canvas canvas)
        {
            this.canvas = canvas;
            this.waveformBars = new List<Rectangle>();
            this.random = new Random();
        }

        public void GenerateWaveform(double duration = 60.0)
        {
            ClearWaveform();
            
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            
            if (canvasWidth <= 0 || canvasHeight <= 0) return;
            
            int barCount = (int)(canvasWidth / 2); // 2px per bar
            if (barCount <= 0) barCount = 1;
            
            for (int i = 0; i < barCount; i++)
            {
                // Generate random amplitude based on time position
                double timePosition = (double)i / barCount;
                double amplitude = GenerateAmplitude(timePosition);
                
                double barHeight = Math.Max(5, amplitude * canvasHeight * 0.8);
                double x = i * 2;
                double y = (canvasHeight - barHeight) / 2;
                
                Rectangle bar = new Rectangle
                {
                    Width = 1,
                    Height = barHeight,
                    Fill = new SolidColorBrush(GetBarColor(timePosition)),
                    Opacity = 0.8
                };
                
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                
                canvas.Children.Add(bar);
                waveformBars.Add(bar);
            }
        }

        private double GenerateAmplitude(double timePosition)
        {
            // Create a more realistic waveform pattern
            double baseAmplitude = 0.3 + 0.4 * Math.Sin(timePosition * Math.PI * 4);
            double noise = (random.NextDouble() - 0.5) * 0.2;
            double beat = Math.Sin(timePosition * Math.PI * 8) * 0.1;
            
            return Math.Max(0.1, Math.Min(1.0, baseAmplitude + noise + beat));
        }

        private Color GetBarColor(double timePosition)
        {
            // Create a gradient effect based on time position
            byte r = (byte)(100 + timePosition * 100);
            byte g = (byte)(150 + timePosition * 50);
            byte b = (byte)(255 - timePosition * 100);
            
            return Color.FromRgb(r, g, b);
        }

        public void UpdatePlayhead(double position, double totalDuration)
        {
            if (totalDuration <= 0) return;
            
            double playheadX = (position / totalDuration) * canvas.ActualWidth;
            
            // Highlight bars near the playhead
            for (int i = 0; i < waveformBars.Count; i++)
            {
                double barX = i * 2;
                double distance = Math.Abs(barX - playheadX);
                
                if (distance < 20)
                {
                    waveformBars[i].Opacity = 1.0;
                    waveformBars[i].Fill = new SolidColorBrush(Colors.Yellow);
                }
                else
                {
                    waveformBars[i].Opacity = 0.8;
                    double timePos = (double)i / waveformBars.Count;
                    waveformBars[i].Fill = new SolidColorBrush(GetBarColor(timePos));
                }
            }
        }

        public void ClearWaveform()
        {
            foreach (var bar in waveformBars)
            {
                canvas.Children.Remove(bar);
            }
            waveformBars.Clear();
        }

        public void ResizeWaveform()
        {
            GenerateWaveform();
        }
    }
} 