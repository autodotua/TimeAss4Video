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
            if (new[] { "jpg", "tif", "raw", "png", "dng", "arw", "nef", "cr2", "rw2" }.Contains(file.File.Extension.ToLower().TrimStart('.')))
            {
                file.StartTime = file.File.LastWriteTime;
                return true;
            }
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

        public static void Export(IAssFormat format, IVideoFileInfo file, string exportPath)
        {
            Export(format, new[] { file }, exportPath);
        }

        public static void Export(IAssFormat format, IList<IVideoFileInfo> files, string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
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
                 .Append((file.StartTime.Value + currentTime * file.Ratio).ToString(format.Format))
                 .AppendLine();
                    if (nextTime >= file.Length)
                    {
                        break;
                    }
                    currentTime = nextTime;
                }
                totalTime += file.Length;
            }
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllText(path, outputs.ToString());
        }

        public static string GetAssFileName(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".ass");
        }

        public static StringBuilder GetAssHead(IAssFormat format, IList<IVideoFileInfo> files)
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
          .AppendLine($"; {Parameters.AssFiles }={JsonConvert.SerializeObject(files)}")
          .AppendLine($"; {Parameters.AssFormat }={JsonConvert.SerializeObject(format)}")
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

        public static (List<VideoFileInfo> files, AssFormat format) ImportFromAss(string path)
        {
            string assText = File.ReadAllText(path);
            if (!assText.Contains(Parameters.AssSoftware))
            {
                throw new Exception("非本软件生成的ASS不可导入");
            }
            var match = Regex.Match(assText, $@"{Parameters.AssFiles }=(?<Json>.+){Environment.NewLine}");
            if (match == null || match.Success == false)
            {
                throw new Exception("ASS格式错误");
            }
            string json = match.Groups["Json"].Value;

            var files = JsonConvert.DeserializeObject<List<VideoFileInfo>>(json);

            match = Regex.Match(assText, $@"{Parameters.AssFormat }=(?<Json>.+){Environment.NewLine}");
            if (match == null || match.Success == false)
            {
                throw new Exception("ASS格式错误");
            }
            json = match.Groups["Json"].Value;

            var format = JsonConvert.DeserializeObject<AssFormat>(json);
            return (files, format);
        }
    }
}