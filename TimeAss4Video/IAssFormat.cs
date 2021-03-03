using System.Windows.Media;

namespace TimeAss4Video
{
    public interface IAssFormat
    {
        int Alignment { get; set; }
        bool Bold { get; set; }
        Color BorderColor { get; set; }
        int BorderWidth { get; set; }
        FontFamily Font { get; set; }
        Color FontColor { get; set; }
        string Format { get; set; }
        int Interval { get; set; }
        bool Italic { get; set; }
        int Margin { get; set; }
        int Size { get; set; }
        bool Underline { get; set; }
    }
}