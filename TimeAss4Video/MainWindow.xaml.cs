using FzLib.Extension;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace TimeAss4Video
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(Window win) : base(win)
        {
        }

        private string filePath = @"Z:\待处理\YDXJ0699.MP4";

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

        private DateTime startTime = DateTime.Now;

        public DateTime StartTime
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
            set => this.SetValueAndNotify(ref interval, value, nameof(Interval));
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel(this);
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
            (sender as Button).IsEnabled = false;
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

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            StringBuilder outputs = new StringBuilder();
            outputs.AppendLine("[Script Info]")
          .AppendLine("ScriptType: v4.00+")
           .AppendLine("Collisions: Normal")
           .AppendLine("PlayResX: 1920")
           .AppendLine("PlayResY: 1080")
           .AppendLine()
           .AppendLine("[V4+ Styles]")
           .AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding")
           .AppendLine("Style: Default, Microsoft YaHei, 64, &H00FFFFFF, &H00FFFFFF, &H00000000, &H00000000, 0, 0, 0, 0, 100, 100, 0.00, 0.00, 1, 1, 0, 3, 20, 20, 20, 0")
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
                    .Append((ViewModel.StartTime + time).ToString(ViewModel.Format))
                    .AppendLine();
            }

            string path = ViewModel.FilePath;
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(ViewModel.FilePath) + ".ass"), outputs.ToString());
        }
    }
}