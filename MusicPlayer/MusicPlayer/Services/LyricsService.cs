using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MusicPlayer.Models;

namespace MusicPlayer.Services;

public partial class LyricsService
{
    [GeneratedRegex(@"\[(\d{2}):(\d{2})(?:\.(\d{2,3}))?\]")] // expresión regular para el archivo.lrc
    private static partial Regex TimeTagRegex();

    public List<LyricLine> LoadLyrics(string lrcPath)
    {
        var lines = new List<LyricLine>();
        if (!File.Exists(lrcPath))
            return lines;
        foreach (var rawLine in File.ReadAllLines(lrcPath))
        {
            var matches = TimeTagRegex().Matches(rawLine);
            if (matches.Count == 0)
                continue;

            var text = TimeTagRegex().Replace(rawLine, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            foreach (Match match in matches)
            {
                var minutes = int.Parse(match.Groups[1].Value);
                var seconds = int.Parse(match.Groups[2].Value);
                var fraction = match.Groups[3].Success
                    ? match.Groups[3].Value.PadRight(3, '0')
                    : "000";
                var milliseconds = int.Parse(fraction[..3]);

                var time = new TimeSpan(0, 0, minutes, seconds, milliseconds);
                lines.Add(new LyricLine { Time = time, Text = text });
            }
        }
        return lines.OrderBy(l => l.Time).ToList();
    }
}
