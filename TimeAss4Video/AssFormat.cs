using System.Windows.Media;

namespace TimeAss4Video
{
    public class AssFormat : IAssFormat
    {
        public int Alignment { get; set; }
        public bool Bold { get; set; }
        public Color BorderColor { get; set; }
        public int BorderWidth { get; set; }
        public FontFamily Font { get; set; }
        public Color FontColor { get; set; }
        public string Format { get; set; }
        public int Interval { get; set; }
        public bool Italic { get; set; }
        public int Margin { get; set; }
        public int Size { get; set; }
        public bool Underline { get; set; }
    }
}