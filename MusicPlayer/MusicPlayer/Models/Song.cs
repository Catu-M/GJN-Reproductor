using System;

namespace MusicPlayer.Models;
public class Song
{
    public required string FilePath { get; init; }
    public string Title { get; set; } = "Título desconocido";
    public string Artist { get; set; } = "Artista desconocido";
    public string Album { get; set; } = "Álbum desconocido";
    public byte[]? CoverArt { get; set; }
    public TimeSpan Duration { get; set; }
    public string? LyricsPath { get; set; }
}
