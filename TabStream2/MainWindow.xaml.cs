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
using NAudio.Wave;

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
        static IWavePlayer outputDevice;

        bool isDraggingPlayhead=false;
        double zoomScale = 1.0;
        const double MinZoom = 0.2;
        const double MaxZoom = 8.0;
        const double PixelsPerSecondBase = 20.0;
            // Смещение временной шкалы в секундах (окно просмотра)
            double timeOffsetSeconds = 0.0;
            // Максимальная длительность: 10:00:00 = 36000 сек
            const double MaxTimelineSeconds = 36000.0;
        public MainWindow()
        {
            InitializeComponent();
            DrawTimeRuler();

            outputDevice = new WaveOutEvent();
            playhead = new Playhead(0, PlayheadLine.Stroke);
            audioTracks = new List<AudioTrack>();
            listTrack = new List<Track>();
            GenerateTracks();
        }

        public void DrawTimeRuler()
        {
            TimeRuler.Children.Clear();

            double pixelsPerSecond = PixelsPerSecondBase; // базовая шкала, масштабируется через LayoutTransform

            // Полная ширина по всей таймлинейке (до 10:00:00)
            double rulerWidth = pixelsPerSecond * MaxTimelineSeconds;
            TimeRuler.Width = rulerWidth;
            CPlayheadCanvas.Width = rulerWidth;
            // ВАЖНО: ширину ScrollViewer не увеличиваем, он должен оставаться в размере колонки.
            // Контент внутри должен быть широким, чтобы появилась горизонтальная прокрутка.
            TracksContainer.Width = rulerWidth;

            // Метки каждые 5 секунд по всей длине
            int tickStep = 5;
            for (int s = 0; s <= MaxTimelineSeconds; s += tickStep)
            {
                double x = s * pixelsPerSecond;

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
                    Text = TimeSpan.FromSeconds(s).ToString(@"mm\:ss"),
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
                    Width = TracksContainer.Width, // Автоматическая ширина
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

        }

        private void TimeRuler_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void TimeRuler_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

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
                        double totalSeconds = 0;
                        using (var audioFile = new AudioFileReader(file))
                        {
                            totalSeconds = audioFile.TotalTime.TotalSeconds;
                        }

                            // Пример создания AudioTrack
                            var id = audioTracks.Count + 1;
                        var audioTrack = new AudioTrack(
                            iDAudioTrack: id,
                            fileName: System.IO.Path.GetFileName(file),
                            audioPath: new Uri(file),
                            startMs: 0,
                            endMs: totalSeconds,
                            iDTrack: 1 // по умолчанию первая дорожка 
                        ); //TODO При Drag Drop учитывать дорожку на позиции курсора

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

            // Перевод секунд в пиксели согласно шкале (20 px/сек)
            double pixelsPerSecond = PixelsPerSecondBase;
            double left = Calculate.SSToX(track.StartMs);
            double width = Calculate.SSToX(Math.Max(0, track.EndMs - track.StartMs));

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

        private void TimeRulerScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            TrackNamesScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            if (!Double.IsNaN(e.HorizontalChange) && Math.Abs(e.HorizontalChange) > 0)
            {
                TracksScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
                // Обновляем позицию PlayHead при прокрутке
                if (!isDraggingPlayhead)
                {
                    UpdatePlayheadPosition();
                }
            }
        }

        private void TracksScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            TrackNamesScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            if (!Double.IsNaN(e.HorizontalChange) && Math.Abs(e.HorizontalChange) > 0)
            {
                TimeRulerScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
                // Обновляем позицию PlayHead при прокрутке
                if (!isDraggingPlayhead)
                {
                    UpdatePlayheadPosition();
                }
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
            // Используем известный Canvas напрямую, чтобы избежать NRE при перерисовке
            var canvas = TimeRuler;

            Point mousePos = e.GetPosition(canvas);

            // Позиция курсора относительно левого края rect внутри canvas => желаемая позиция rect по X
            double desiredRectX = mousePos.X - playhead.PCurPos.X;

            // Текущее окно/вьюпорт
            double viewportWidth = TimeRulerScrollViewer?.ViewportWidth > 0
                ? TimeRulerScrollViewer.ViewportWidth
                : (TimeRuler.ActualWidth > 0 ? TimeRuler.ActualWidth : 1200);
            double pixelsPerSecond = PixelsPerSecondBase;

            // Рассчитываем абсолютную Х-позицию в пикселях по всей шкале
            double rectX = desiredRectX;
            // Кламп по полной ширине TimeRuler
            double maxX = TimeRuler.Width - rect.Width;
            rectX = Math.Max(0.0, Math.Min(rectX, maxX));

            // По вертикали — в пределах канвы
            double y = mousePos.Y - playhead.PCurPos.Y;
            double maxY = canvas.ActualHeight - rect.Height;
            y = Math.Max(0.0, Math.Min(y, maxY));

            Canvas.SetLeft(rect, rectX);
            Canvas.SetTop(rect, y);

            // Автопрокрутка ScrollViewer, чтобы RCurPos оставался видимым
            if (TimeRulerScrollViewer != null)
            {
                double currentOffset = TimeRulerScrollViewer.HorizontalOffset;
                double rightEdge = currentOffset + viewportWidth;
                double targetLeft = rectX;
                double targetRight = rectX + rect.Width;

                if (targetLeft < currentOffset)
                {
                    TimeRulerScrollViewer.ScrollToHorizontalOffset(targetLeft);
                    TracksScrollViewer.ScrollToHorizontalOffset(targetLeft);
                }
                else if (targetRight > rightEdge)
                {
                    double newOffset = targetRight - viewportWidth;
                    TimeRulerScrollViewer.ScrollToHorizontalOffset(newOffset);
                    TracksScrollViewer.ScrollToHorizontalOffset(newOffset);
                }
            }

            // Абсолютная позиция плейхеда в пикселях (центр RCurPos)
            playhead.CurrentPos = rectX + 5.0;
            // Обновим положение линии поверх треков с учётом текущего HorizontalOffset
            UpdatePlayheadPosition();
        }

        private void RCurPos_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var rect = (Rectangle)sender;
            isDraggingPlayhead = false;
            rect.ReleaseMouseCapture();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            double startSeconds = Calculate.XToSS(playhead.CurrentPos);
            string path = audioTracks[0].AudioPath.OriginalString;
            using (var audioFile = new AudioFileReader(path))
            {
                // Проверяем, не превышает ли указанное время длину файла
                if (startSeconds < audioFile.TotalTime.TotalSeconds)
                {
                    // Устанавливаем позицию (время начала)
                    audioFile.CurrentTime = TimeSpan.FromSeconds(startSeconds);
                }
                else
                {
                    return;
                }
                
                outputDevice.Init(audioFile);
                outputDevice.Play();

            }
        }
    }
}
