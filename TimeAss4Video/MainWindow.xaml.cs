using AutoMapper;
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
using static TimeAss4Video.AssUtility;

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
                        if (AutoGenerateVideoInfo(ViewModel, file) == false)
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
                    Export(ViewModel.ToAssFormat(), file, GetAssFileName(file.FilePath));
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
            if (string.IsNullOrEmpty(ViewModel.ExportPath))
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
                Export(ViewModel, ViewModel.Files, GetAssFileName(ViewModel.ExportPath));
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
                ViewModel.ExportPath = dialog.FileName;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.ImportPath))
            {
                await ShowMessageAsync("请先选择导入文件");
                return;
            }
            try
            {
                var (files, format) = ImportFromAss(ViewModel.ImportPath);
                if (ViewModel.ImportIncludeFiles)
                {
                    ViewModel.Files.Clear();
                    foreach (var file in files)
                    {
                        ViewModel.Files.Add(file);
                    }
                }
                if (ViewModel.ImportIncludeFormat)
                {
                    var config = new MapperConfiguration(cfg => cfg.CreateMap<AssFormat, MainWindowViewModel>());
                    new Mapper(config).Map(format, ViewModel);
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("导入失败：" + ex.Message);
            }
        }

        private void ClearFiles_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Files.Clear();
        }

        private void DataGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files.Any(p => File.Exists(p)))
                {
                    e.Effects = DragDropEffects.Link;
                };
            }
        }

        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                foreach (var file in files.Where(p => File.Exists(p)))
                {
                    ViewModel.Files.Add(new VideoFileInfo()
                    {
                        FilePath = file
                    });
                }
            }
        }

        private void SelectImportPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "ASS字幕文件|*.ass",
            };
            if (dialog.ShowDialog() == true)
            {
                ViewModel.ImportPath = dialog.FileName;
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            VideoFileInfo file = (sender as FrameworkElement).Tag as VideoFileInfo;
            try
            {
                new Process()
                {
                    StartInfo = new ProcessStartInfo(file.FilePath)
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
            catch(Exception ex)
            {
                await ShowMessageAsync("无法打开该文件：" + ex.Message);
            }
        }
    }
}