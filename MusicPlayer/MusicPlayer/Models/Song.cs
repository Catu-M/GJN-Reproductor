using System;

namespace MusicPlayer.Models;

/// <summary>
/// Representa una canción encontrada en la biblioteca local (un archivo .mp3),
/// junto con su metadata leída con TagLibSharp y la ruta a su letra .lrc, si existe.
/// </summary>
public class Song
{
    public required string FilePath { get; init; }

    public string Title { get; set; } = "Título desconocido";

    public string Artist { get; set; } = "Artista desconocido";

    public string Album { get; set; } = "Álbum desconocido";

    /// <summary>
    /// Bytes de la carátula embebida en el mp3 (si tiene). Puede ser null.
    /// </summary>
    public byte[]? CoverArt { get; set; }

    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Ruta al archivo .lrc con el mismo nombre que el mp3. Null si no existe.
    /// </summary>
    public string? LyricsPath { get; set; }
}
