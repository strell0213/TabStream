using System;
using System.IO;
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
using TabStream2.Model;

namespace TabStream2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Playhead playhead;
        List<AudioTrack> audioTracks;
        List<Track> listTrack;

        bool isDraggingPlayhead=false;
        double zoomScale = 1.0;
        const double MinZoom = 0.2;
        const double MaxZoom = 8.0;
        const double PixelsPerSecondBase = 20.0;
        public MainWindow()
        {
            InitializeComponent();
            DrawTimeRuler();

            playhead = new Playhead(0, PlayheadLine.Stroke);
            audioTracks = new List<AudioTrack>();
            listTrack = new List<Track>();
            GenerateTracks();
        }

        public void DrawTimeRuler()
        {
            TimeRuler.Children.Clear();

            double pixelsPerSecond = PixelsPerSecondBase; // базовая шкала, масштабируется через LayoutTransform
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

            Rectangle rec = new Rectangle()
            {
                Width = 10,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Fill = Brushes.Blue,
            };
            Canvas.SetLeft(rec, -5);
            rec.MouseDown += RCurPos_MouseDown;
            rec.MouseMove += RCurPos_MouseMove;
            rec.MouseUp += RCurPos_MouseUp;
            TimeRuler.Children.Add(rec);
        }


        public void GenerateTracks()
        {
            int trackCount = 100;
            double trackHeight = 80;
            double trackWidth = 150;

            for (int i = 1; i <= trackCount; i++)
            {
                // Создание трека и добавление в список
                var track = new Track(i);
                listTrack.Add(track);

                // === Левая часть: TrackNameList ===
                Border nameBorder = new Border
                {
                    Width = trackWidth,
                    Height = trackHeight,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                };

                TextBlock nameText = new TextBlock
                {
                    Text = track.Name,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(10, 0, 0, 0),
                    FontWeight = FontWeights.SemiBold
                };

                nameBorder.Child = nameText;
                TrackNameList.Children.Add(nameBorder);

                // Вложенный Canvas внутри Border
                Canvas trackCanvas = new Canvas
                {
                    Background = Brushes.Transparent,
                    Width = double.NaN, // Автоматическая ширина
                    Height = trackHeight
                };

                Border trackRow = new Border
                {
                    Height = trackHeight,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Background = new SolidColorBrush(Color.FromRgb(37, 37, 37)),
                    Child = trackCanvas
                };

                TracksContainer.Children.Add(trackRow);
            }
        }

        private void TimeRuler_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //isDraggingPlayhead = true;
            //TimeRuler.CaptureMouse(); // захват мыши
            
            //// Получаем позицию мыши относительно TimeRuler
            //double mouseX = e.GetPosition(TimeRuler).X;
            
            //// Преобразуем в позицию на немасштабированном TimeRuler
            //double absoluteX = mouseX / zoomScale;
            
            //// Добавляем смещение прокрутки
            //double scrollOffset = TimeRulerScrollViewer.HorizontalOffset;
            //double finalAbsoluteX = absoluteX + scrollOffset;
            
            //// Сохраняем абсолютную позицию в playhead
            //playhead.CurrentPos = finalAbsoluteX;
            
            //// Отладочная информация
            //System.Diagnostics.Debug.WriteLine($"MouseClick - mouseX: {mouseX:F2}, zoomScale: {zoomScale:F2}, absoluteX: {absoluteX:F2}, scrollOffset: {scrollOffset:F2}, finalAbsoluteX: {finalAbsoluteX:F2}");
            
            //// Устанавливаем PlayHead в позицию мыши
            //Canvas.SetLeft(PlayheadLine, mouseX);
        }

        private void TimeRuler_MouseMove(object sender, MouseEventArgs e)
        {
            //if (isDraggingPlayhead)
            //{
            //    double mouseX = e.GetPosition(TimeRuler).X;
                
            //    // Преобразуем в абсолютную позицию
            //    double absoluteX = mouseX / zoomScale;
            //    double scrollOffset = TimeRulerScrollViewer.HorizontalOffset;
            //    double finalAbsoluteX = absoluteX + scrollOffset;
                
            //    // Сохраняем абсолютную позицию
            //    playhead.CurrentPos = finalAbsoluteX;
                
            //    // Перемещаем PlayHead за мышью
            //    Canvas.SetLeft(PlayheadLine, mouseX);
            //}
        }

        private void TimeRuler_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //isDraggingPlayhead = false;
            //TimeRuler.ReleaseMouseCapture(); // отпускаем мышь
        }

        private void UpdatePlayheadPosition()
        {
            if (playhead.CurrentPos < 0) return;

            // Преобразуем абсолютную позицию в позицию на масштабированном TimeRuler
            double absoluteX = playhead.CurrentPos;
            double scrollOffset = TimeRulerScrollViewer.HorizontalOffset;
            double relativeX = absoluteX - scrollOffset;
            double scaledX = relativeX * zoomScale;

            // Отладочная информация
            System.Diagnostics.Debug.WriteLine($"UpdatePlayhead - absoluteX: {absoluteX:F2}, scrollOffset: {scrollOffset:F2}, relativeX: {relativeX:F2}, zoomScale: {zoomScale:F2}, scaledX: {scaledX:F2}");

            // Устанавливаем PlayHead в правильную позицию
            Canvas.SetLeft(PlayheadLine, scaledX);
        }


        

        private void TrackNamesScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true; // блокируем прокрутку вручную
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    string extension = System.IO.Path.GetExtension(file).ToLower();
                    bool v = (extension == ".mp3" || extension == ".wav");
                    if (v)
                    {
                        // Пример создания AudioTrack
                        var id = audioTracks.Count + 1;
                        var audioTrack = new AudioTrack(
                            iDAudioTrack: id,
                            fileName: System.IO.Path.GetFileName(file),
                            audioPath: new Uri(file),
                            startMs: 0,
                            endMs: 5000,
                            iDTrack: 1 // по умолчанию первая дорожка
                        );

                        audioTracks.Add(audioTrack);
                        RenderAudioClip(audioTrack);
                    }
                }
            }
        }

        public void RenderAudioClip(AudioTrack track)
        {
            // Найдём нужный контейнер по IDTrack
            if (track.IDTrack - 1 < 0 || track.IDTrack - 1 >= TracksContainer.Children.Count)
                return;

            var border = TracksContainer.Children[track.IDTrack - 1] as Border;
            var targetRow = border?.Child as Canvas;
            if (targetRow == null)
                return;

            double totalMs = 60000; // Например, длина таймлайна — 1 минута
            double canvasWidth = TimeRuler.ActualWidth;

            double left = (track.StartMs / totalMs) * canvasWidth;
            double width = ((track.EndMs - track.StartMs) / totalMs) * canvasWidth;

            var clipRect = new Border
            {
                Width = width,
                Height = 60,
                Background = Brushes.SteelBlue,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Child = new TextBlock
                {
                    Text = track.FileName,
                    Foreground = Brushes.White,
                    Margin = new Thickness(5, 2, 5, 2),
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            Canvas.SetLeft(clipRect, left);
            Canvas.SetTop(clipRect, 10);

            targetRow.Children.Add(clipRect);
        }

        private void TimeRulerScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //if (TimeRuler == null || TracksContainer == null)
            //    return;

            //double oldScale = zoomScale;
            //double zoomDelta = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
            //double newScale = Math.Max(MinZoom, Math.Min(MaxZoom, oldScale * zoomDelta));
            //if (Math.Abs(newScale - oldScale) < 0.0001)
            //{
            //    e.Handled = true;
            //    return;
            //}

            //Point mousePosInViewer = e.GetPosition(TimeRulerScrollViewer);
            //double mouseX = mousePosInViewer.X;
            //double oldOffset = TimeRulerScrollViewer.HorizontalOffset;

            //zoomScale = newScale;
            //ApplyZoom();

            //double newOffset = ((oldOffset + mouseX) / oldScale) * newScale - mouseX;
            //TimeRulerScrollViewer.ScrollToHorizontalOffset(newOffset);
            //TracksScrollViewer.ScrollToHorizontalOffset(newOffset);

            //// Обновляем позицию PlayHead при изменении зума
            //UpdatePlayheadPosition();
            //e.Handled = true;
        }

        private void ApplyZoom()
        {
            var scaleX = zoomScale;
            if (TimeRulerScale != null)
            {
                TimeRulerScale.ScaleX = scaleX;
            }
            if (TracksScale != null)
            {
                TracksScale.ScaleX = scaleX;
            }
            
            // PlayHead теперь не зависит от масштаба - он всегда в позиции мыши
        }

        private void TimeRulerScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            TrackNamesScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            if (!Double.IsNaN(e.HorizontalChange) && Math.Abs(e.HorizontalChange) > 0)
            {
                TracksScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
                // Обновляем позицию PlayHead при прокрутке
                UpdatePlayheadPosition();
            }
        }

        private void TracksScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            TrackNamesScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            if (!Double.IsNaN(e.HorizontalChange) && Math.Abs(e.HorizontalChange) > 0)
            {
                TimeRulerScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
                // Обновляем позицию PlayHead при прокрутке
                UpdatePlayheadPosition();
            }
        }

        private void RCurPos_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var rect = (Rectangle)sender;
            isDraggingPlayhead = true;
            playhead.PCurPos = e.GetPosition(rect);
            rect.CaptureMouse();
        }

        private void RCurPos_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDraggingPlayhead) return;

            var rect = (Rectangle)sender;
            var canvas = (Canvas)rect.Parent;

            Point mousePos = e.GetPosition(canvas);
            double x = mousePos.X - playhead.PCurPos.X;
            double y = mousePos.Y - playhead.PCurPos.Y;

            // --- ГРАНИЦЫ CANVAS ---
            // Минимум (верхний левый угол)
            double minX = 0;
            double minY = 0;

            // Максимум (нижний правый угол)
            double maxX = canvas.ActualWidth - rect.Width;
            double maxY = canvas.ActualHeight - rect.Height;

            // Ограничиваем координаты, чтобы фигура не выходила за Canvas
            x = Math.Max(minX, Math.Min(x, maxX));
            y = Math.Max(minY, Math.Min(y, maxY));

            // --- Применяем скорректированные координаты ---
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);

            foreach (UIElement element in CPlayheadCanvas.Children)
            {
                if (element is Line line)
                {
                    double xLine = Canvas.GetLeft(line);
                    xLine = x+5;
                    Canvas.SetLeft(line, xLine);
                }
            }
        }

        private void RCurPos_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var rect = (Rectangle)sender;
            isDraggingPlayhead = false;
            rect.ReleaseMouseCapture();
        }
    }
}
