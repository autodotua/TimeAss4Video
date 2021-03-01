using FzLib.Extension;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace TimeAss4Video
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Timer timer;

        public MainWindowViewModel(Window win) : base(win)
        {
            timer = new Timer(Interval);
            timer.Elapsed += (p1, p2) =>
            {
                this.Notify(nameof(ReviewContent));
            };
            timer.Start();
            Fonts = new System.Drawing.Text.InstalledFontCollection()
                .Families
                .Select(p => new FontFamily(p.Name))
                .ToList();
        }

        public string ReviewContent => DateTime.Now.ToString(Format);

        private string filePath = @"C:\Users\admin\Desktop\YDXJ0053.MP4";

        public string FilePath
        {
            get => filePath;
            set => this.SetValueAndNotify(ref filePath, value, nameof(FilePath));
        }

        private TimeSpan length = TimeSpan.Zero;

        public TimeSpan Length
        {
            get => length;
            set => this.SetValueAndNotify(ref length, value, nameof(Length));
        }

        private DateTime? startTime = null;

        public DateTime? StartTime
        {
            get => startTime;
            set => this.SetValueAndNotify(ref startTime, value, nameof(StartTime));
        }

        private string format = "HH:mm:ss";

        public string Format
        {
            get => format;
            set => this.SetValueAndNotify(ref format, value, nameof(Format));
        }

        private int interval = 1000;

        public int Interval
        {
            get => interval;
            set
            {
                this.SetValueAndNotify(ref interval, value, nameof(Interval));
                timer.Interval = value;
            }
        }

        private int size = 32;

        public int Size
        {
            get => size;
            set => this.SetValueAndNotify(ref size, value, nameof(Size));
        }

        private int margin = 20;

        public int Margin
        {
            get => margin;
            set => this.SetValueAndNotify(ref margin, value, nameof(Margin));
        }

        private int alignment = 3;

        public int Alignment
        {
            get => alignment;
            set => this.SetValueAndNotify(ref alignment, value, nameof(Alignment));
        }

        private FontFamily font = SystemFonts.MessageFontFamily;

        public FontFamily Font
        {
            get => font;
            set => this.SetValueAndNotify(ref font, value, nameof(Font));
        }

        private Color fontColor = Colors.White;

        public Color FontColor
        {
            get => fontColor;
            set => this.SetValueAndNotify(ref fontColor, value, nameof(FontColor));
        }

        private Color borderColor = Colors.Black;

        public List<FontFamily> Fonts { get; }

        public Color BorderColor
        {
            get => borderColor;
            set => this.SetValueAndNotify(ref borderColor, value, nameof(BorderColor));
        }

        private int borderWidth = 3;

        public int BorderWidth
        {
            get => borderWidth;
            set => this.SetValueAndNotify(ref borderWidth, value, nameof(BorderWidth));
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel(this);
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateReview();
        }

        private void UpdateReview()
        {
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        public async Task ShowMessageAsync(string message)
        {
            tbkDialogMessage.Text = message;
            await dialog.ShowAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "视频文件|*.mp4;*.mov;*.mkv;*.avi|所有文件|*.*",
            };
            if (dialog.ShowDialog() == true)
            {
                ViewModel.FilePath = dialog.FileName;
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.FilePath))
            {
                await ShowMessageAsync("请先指定视频文件");
                return;
            }
            if (!File.Exists(ViewModel.FilePath))
            {
                await ShowMessageAsync("指定的文件不存在");
                return;
            }
            (sender as Button).IsEnabled = false;

            try
            {
                await Task.Run(() =>
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        FileName = "ffprobe",
                        Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{ViewModel.FilePath}\""
                    };

                    Process p = new Process() { StartInfo = startInfo };
                    p.Start();
                    p.WaitForExit();
                    var output = p.StandardOutput.ReadToEnd().Trim();
                    if (double.TryParse(output, out double length))
                    {
                        ViewModel.Length = TimeSpan.FromSeconds(length);
                    }
                    else
                    {
                        throw new Exception("无法获取视频长度");
                    }
                    var modifiedTime = new FileInfo(ViewModel.FilePath).LastWriteTime;
                    ViewModel.StartTime = modifiedTime - ViewModel.Length;
                });
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("自动检测失败：" + ex.Message);
            }
            (sender as Button).IsEnabled = true;
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.StartTime.HasValue)
            {
                await ShowMessageAsync("请设置视频开始时间");
                return;
            }
            if (string.IsNullOrWhiteSpace(ViewModel.FilePath))
            {
                await ShowMessageAsync("请先指定视频文件");
                return;
            }
            var btn = sender as Button;
            btn.IsEnabled = false;
            await Task.Run(() =>
            {
                int size = ViewModel.Size;
                int margin = ViewModel.Margin;
                int al = ViewModel.Alignment;
                int bw = ViewModel.BorderWidth;
                var c = (byte.MaxValue - ViewModel.FontColor.A).ToString("X2") + ViewModel.FontColor.ToString()[3..];
                var bc = (byte.MaxValue - ViewModel.BorderColor.A).ToString("X2") + ViewModel.BorderColor.ToString()[3..];
                StringBuilder outputs = new StringBuilder();
                outputs.AppendLine("[Script Info]")
              .AppendLine("ScriptType: v4.00+")
               .AppendLine("Collisions: Normal")
               .AppendLine("PlayResX: 1920")
               .AppendLine("PlayResY: 1080")
               .AppendLine()
               .AppendLine("[V4+ Styles]")
               .AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding")
               .AppendLine($"Style: Default, Microsoft YaHei, {size}, &H{c}, &H{c}, &H{bc}, &H00000000, 0, 0, 0, 0, 100, 100, 0.00, 0.00, 1, {bw}, 0, {al}, {margin}, {margin}, {margin}, 0")
               .AppendLine()
               .AppendLine("[Events]")
               .AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");
                string timespanFormat = "hh\\:mm\\:ss\\:ff";
                for (TimeSpan time = TimeSpan.Zero; time <= ViewModel.Length; time = time.Add(TimeSpan.FromMilliseconds(ViewModel.Interval)))
                {
                    outputs.Append($"Dialogue: 3,")
                        .Append(time.ToString(timespanFormat))
                        .Append(",")
                        .Append(time.Add(TimeSpan.FromMilliseconds(ViewModel.Interval)).ToString(timespanFormat))
                        .Append(",Default,,0000,0000,0000,,")
                        .Append((ViewModel.StartTime.Value + time).ToString(ViewModel.Format))
                        .AppendLine();
                }

                string path = ViewModel.FilePath;
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(ViewModel.FilePath) + ".ass"), outputs.ToString());
            });
            btn.Content = "导出成功";
            await Task.Delay(1000);
            btn.Content = "导出";
            btn.IsEnabled = true;
        }
    }

    public class AlignmentRadioButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool?)value == true)
            {
                return int.Parse(parameter as string);
            }
            else
            {
                return 0;
            }
        }
    }
}