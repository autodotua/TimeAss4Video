using System;

namespace TimeAss4Video
{
    public interface IVideoFileInfo
    {
        DateTime? EndTime { get; }
        string FilePath { get; set; }
        TimeSpan Length { get; set; }
        DateTime? StartTime { get; set; }
    }
}