using FzLib.Extension;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace TimeAss4Video
{
    public class VideoFileInfo : INotifyPropertyChanged, IVideoFileInfo
    {
        private FileInfo file;

        [JsonIgnore]
        public FileInfo File
        {
            get => file;
            set => this.SetValueAndNotify(ref file, value, nameof(File), nameof(FilePath));
        }

        public string FilePath
        {
            get => file == null ? "" : file.FullName;
            set => file = string.IsNullOrWhiteSpace(value) ? null : new FileInfo(value);
        }

        private TimeSpan length = TimeSpan.Zero;

        public TimeSpan Length
        {
            get => length;
            set => this.SetValueAndNotify(ref length, value, nameof(Length), nameof(EndTime));
        }

        private DateTime? startTime = null;

        public DateTime? StartTime
        {
            get => startTime;
            set => this.SetValueAndNotify(ref startTime, value, nameof(StartTime), nameof(EndTime));
        }

        private int index = 0;

        public int Index
        {
            get => index;
            set => this.SetValueAndNotify(ref index, value, nameof(Index));
        }

        [JsonIgnore]
        public DateTime? EndTime => StartTime.HasValue ? StartTime + Length : null;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}