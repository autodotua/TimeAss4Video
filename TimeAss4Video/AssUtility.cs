using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TimeAss4Video;
using System.Text.RegularExpressions;

namespace TimeAss4Video
{
    public static class AssUtility
    {
        public static bool AutoGenerateVideoInfo(IAssFormat format, VideoFileInfo file)
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

        public static void Export(IAssFormat format, VideoFileInfo file)
        {
            Export(format, new[] { file });
        }

        public static void Export(IAssFormat format, IList<VideoFileInfo> files)
        {
            Export(format, files, format.OutputPath);
        }

        public static void Export(IAssFormat format, IList<VideoFileInfo> files, string outputPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(outputPath));
            StringBuilder outputs = GetAssHead(format, files);

            string timespanFormat = "hh\\:mm\\:ss\\:ff";
            var interval = TimeSpan.FromMilliseconds(format.Interval);
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
                 .Append((file.StartTime.Value + currentTime).ToString(format.Format))
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
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllText(path, outputs.ToString());
        }

        public static StringBuilder GetAssHead(IAssFormat format, IList<VideoFileInfo> files)
        {
            int size = format.Size;
            int margin = format.Margin;
            int al = format.Alignment;
            int bw = format.BorderWidth;
            var c = (byte.MaxValue - format.FontColor.A).ToString("X2") + format.FontColor.ToString()[3..];
            var bc = (byte.MaxValue - format.BorderColor.A).ToString("X2") + format.BorderColor.ToString()[3..];
            int bold = format.Bold ? 1 : 0;
            int italic = format.Italic ? 1 : 0;
            int underline = format.Underline ? 1 : 0;

            StringBuilder outputs = new StringBuilder();
            outputs.AppendLine("[Script Info]")
          .AppendLine("; " + Parameters.AssSoftware)
          .AppendLine("; " + Parameters.AssAuthor)
          .AppendLine($"; {Parameters.AssFilesInfo }={JsonConvert.SerializeObject(files)}")
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

        public static List<VideoFileInfo> ImportFromAss(string path)
        {
            string assText = File.ReadAllText(path);
            if (!assText.Contains(Parameters.AssSoftware))
            {
                throw new Exception("非本软件生成的ASS不可导入");
            }
            var match = Regex.Match(assText, $@"{Parameters.AssFilesInfo }=(?<Json>.+){Environment.NewLine}");
            if(match==null || match.Success==false)
            {
                throw new Exception("找不到文件信息");
            }
            string json = match.Groups["Json"].Value;

            return JsonConvert.DeserializeObject<List<VideoFileInfo>>(json);
        }
    }
}