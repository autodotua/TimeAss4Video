using AutoMapper;
using FzLib.Extension;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace TimeAss4Video
{
    public class MainWindowViewModel : ViewModelBase, IAssFormat
    {
        private Timer timer;

        public MainWindowViewModel()
        {
            timer = new Timer(Interval);
            timer.Elapsed += (p1, p2) =>
            {
                this.Notify(nameof(ReviewContent));
            };
            timer.Start();
            Task.Run(() =>
            {
                Fonts = new System.Drawing.Text.InstalledFontCollection()
                    .Families
                    .Select(p => new FontFamily(p.Name))
                    .ToList();
                this.Notify(nameof(Fonts));
            });

            Files.CollectionChanged += Files_CollectionChanged;
        }

        private void Files_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var path = (e.NewItems[0] as VideoFileInfo).FilePath;
                ExportPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".ass");
            }

            for (int i = 0; i < Files.Count; i++)
            {
                Files[i].Index = i + 1;
            }
        }

        public string ReviewContent => DateTime.Now.ToString(Format);

        public ObservableCollection<VideoFileInfo> Files { get; } = new ObservableCollection<VideoFileInfo>();

        private string exportPath = "";

        [JsonIgnore]
        public string ExportPath
        {
            get => exportPath;
            set => this.SetValueAndNotify(ref exportPath, value, nameof(ExportPath));
        }

        private string importPath = "";

        [JsonIgnore]
        public string ImportPath
        {
            get => importPath;
            set => this.SetValueAndNotify(ref importPath, value, nameof(ImportPath));
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

        public List<FontFamily> Fonts { get; private set; }

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

        private bool bold = false;

        public bool Bold
        {
            get => bold;
            set => this.SetValueAndNotify(ref bold, value, nameof(Bold));
        }

        private bool italic = false;

        public bool Italic
        {
            get => italic;
            set => this.SetValueAndNotify(ref italic, value, nameof(Italic));
        }

        private bool underline = false;

        public bool Underline
        {
            get => underline;
            set => this.SetValueAndNotify(ref underline, value, nameof(Underline));
        }

        private bool importIncludeFiles = true;

        [JsonIgnore]
        public bool ImportIncludeFiles
        {
            get => importIncludeFiles;
            set => this.SetValueAndNotify(ref importIncludeFiles, value, nameof(ImportIncludeFiles));
        }

        private bool importIncludeFormatold = true;

        [JsonIgnore]
        public bool ImportIncludeFormat
        {
            get => importIncludeFormatold;
            set => this.SetValueAndNotify(ref importIncludeFormatold, value, nameof(ImportIncludeFormat));
        }

        public AssFormat ToAssFormat()
        {
            var config = new MapperConfiguration(cfg => cfg.CreateMap<MainWindowViewModel, AssFormat>());
            return new Mapper(config).Map<AssFormat>(this);
        }
    }
}