using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TimeAss4Video
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }
        private const string ConfigPath = "config.json";

        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(ConfigPath))
            {
                try
                {
                    ViewModel = JsonConvert.DeserializeObject<MainWindowViewModel>(File.ReadAllText(ConfigPath));
                }
                catch
                {
                    ViewModel = new MainWindowViewModel();
                }
            }
            else
            {
                ViewModel = new MainWindowViewModel();
            }
            DataContext = ViewModel;
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
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(ViewModel));
        }

        public async Task ShowMessageAsync(string message)
        {
            tbkDialogMessage.Text = message;
            await dialog.ShowAsync();
        }

        private void AddNewFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "视频文件|*.mp4;*.mov;*.mkv;*.avi|所有文件|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (var path in dialog.FileNames)
                {
                    ViewModel.Files.Add(new VideoFileInfo() { File = new FileInfo(path) });
                }
            }
        }

        private async void GenerateVideoInfosButton_Click(object sender, RoutedEventArgs e)
        {
            PreprocessFiles();
            if (ViewModel.Files.Count == 0)
            {
                await ShowMessageAsync("没有任何文件");
                return;
            }
            if (ViewModel.Files.Any(p => p.File == null))
            {
                await ShowMessageAsync("存在空的文件地址");
                return;
            }
            if (ViewModel.Files.Any(p => !p.File.Exists))
            {
                await ShowMessageAsync("有的文件不存在");
                return;
            }
            (sender as Button).IsEnabled = false;
            List<VideoFileInfo> failedFiles = new List<VideoFileInfo>();
            foreach (var file in ViewModel.Files)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        if (AutoGenerateVideoInfo(file) == false)
                        {
                            failedFiles.Add(file);
                        }
                    });
                }
                catch (Exception ex)
                {
                    await ShowMessageAsync("自动检测失败：" + ex.Message);
                }
            }
            if (failedFiles.Count > 0)
            {
                await ShowMessageAsync("部分文件检测失败：" + string.Join('、', failedFiles.Select(p => p.File.Name)));
            }
            (sender as Button).IsEnabled = true;
        }

        private async void ExportSingleButton_Click(object sender, RoutedEventArgs e)
        {
            PreprocessFiles();
            if (ViewModel.Files.Any(p => !p.StartTime.HasValue))
            {
                await ShowMessageAsync("请设置视频开始时间");
                return;
            }
            if (ViewModel.Files.Any(p => p.Length.Equals(TimeSpan.Zero)))
            {
                await ShowMessageAsync("请设置视频长度");
                return;
            }
            var btn = sender as Button;
            btn.IsEnabled = false;
            await Task.Run(() =>
            {
                foreach (var file in ViewModel.Files)
                {
                    Export(file);
                }
            });
            btn.Content = "导出成功";
            await Task.Delay(1000);
            btn.Content = "导出";
            btn.IsEnabled = true;
        }

        private void PreprocessFiles()
        {
            foreach (var file in ViewModel.Files.ToArray())
            {
                if (file.File == null && file.Length.Equals(TimeSpan.Zero) && !file.StartTime.HasValue)
                {
                    ViewModel.Files.Remove(file);
                }
            }
        }

        private async void ExportMergeButton_Click(object sender, RoutedEventArgs e)
        {
            PreprocessFiles();
            if (string.IsNullOrEmpty(ViewModel.OutputPath))
            {
                await ShowMessageAsync("请设置输出路径");
                return;
            }
            if (ViewModel.Files.Any(p => !p.StartTime.HasValue))
            {
                await ShowMessageAsync("请设置视频开始时间");
                return;
            }
            if (ViewModel.Files.Any(p => p.Length.Equals(TimeSpan.Zero)))
            {
                await ShowMessageAsync("请设置视频长度");
                return;
            }
            var btn = sender as Button;
            btn.IsEnabled = false;
            await Task.Run(() =>
            {
                Export(ViewModel.Files);
            });
            btn.Content = "导出成功";
            await Task.Delay(1000);
            btn.Content = "导出";
            btn.IsEnabled = true;
        }

        private void SelectSavePathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "ASS字幕文件|*.ass",
            };
            if (dialog.ShowDialog() == true)
            {
                ViewModel.OutputPath = dialog.FileName;
            }
        }

        private bool AutoGenerateVideoInfo(VideoFileInfo file)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{file.FilePath}\""
            };

            Process p = new Process() { StartInfo = startInfo };
            p.Start();
            p.WaitForExit();
            var output = p.StandardOutput.ReadToEnd().Trim();
            if (double.TryParse(output, out double length))
            {
                file.Length = TimeSpan.FromSeconds(length);
            }
            else
            {
                return false;
            }
            var modifiedTime = file.File.LastWriteTime;
            file.StartTime = modifiedTime - file.Length;
            return true;
        }

        private void Export(VideoFileInfo file)
        {
            Export(new[] { file });
        }

        private void Export(IEnumerable<VideoFileInfo> files)
        {
            Export(files, ViewModel.OutputPath);
        }

        private void Export(IEnumerable<VideoFileInfo> files, string outputPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(outputPath));
            StringBuilder outputs = GetAssHead();

            string timespanFormat = "hh\\:mm\\:ss\\:ff";
            var interval = TimeSpan.FromMilliseconds(ViewModel.Interval);
            TimeSpan totalTime = TimeSpan.Zero;
            foreach (var file in files)
            {
                TimeSpan currentTime = TimeSpan.Zero;
                while (true)
                {
                    var nextTime = currentTime.Add(interval);
                    if (nextTime > file.Length)
                    {
                        nextTime = file.Length;
                    }
                    outputs.Append($"Dialogue: 3,")
                 .Append((totalTime + currentTime).ToString(timespanFormat))
                 .Append(",")
                 .Append((totalTime + nextTime).ToString(timespanFormat))
                 .Append(",Default,,0000,0000,0000,,")
                 .Append((file.StartTime.Value + currentTime).ToString(ViewModel.Format))
                 .AppendLine();
                    if (nextTime == file.Length)
                    {
                        break;
                    }
                    currentTime = nextTime;
                }
                totalTime += file.Length;
            }
            string path = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + ".ass");
            File.WriteAllText(path, outputs.ToString());
        }

        private StringBuilder GetAssHead()
        {
            int size = ViewModel.Size;
            int margin = ViewModel.Margin;
            int al = ViewModel.Alignment;
            int bw = ViewModel.BorderWidth;
            var c = (byte.MaxValue - ViewModel.FontColor.A).ToString("X2") + ViewModel.FontColor.ToString()[3..];
            var bc = (byte.MaxValue - ViewModel.BorderColor.A).ToString("X2") + ViewModel.BorderColor.ToString()[3..];
            int bold = ViewModel.Bold ? 1 : 0;
            int italic = ViewModel.Italic ? 1 : 0;
            int underline = ViewModel.Underline ? 1 : 0;

            StringBuilder outputs = new StringBuilder();
            outputs.AppendLine("[Script Info]")
          .AppendLine("ScriptType: v4.00+")
           .AppendLine("Collisions: Normal")
           .AppendLine("PlayResX: 1920")
           .AppendLine("PlayResY: 1080")
           .AppendLine()
           .AppendLine("[V4+ Styles]")
           .AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding")
           .AppendLine($"Style: Default, Microsoft YaHei, {size}, &H{c}, &H{c}, &H{bc}, &H00000000, {bold}, {italic}, {underline}, 0, 100, 100, 0.00, 0.00, 1, {bw}, 0, {al}, {margin}, {margin}, {margin}, 0")
           .AppendLine()
           .AppendLine("[Events]")
           .AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");
            return outputs;
        }
    }
}