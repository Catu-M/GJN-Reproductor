using System;
using System.Collections.Generic;
using System.IO;
using MusicPlayer.Models;

namespace MusicPlayer.Services;
/// <summary>
/// Escanea una carpeta buscando archivos .mp3, lee su metadata (título, artista,
/// álbum, carátula) con TagLibSharp, y detecta si existe un .lrc con el mismo nombre.
/// </summary>
public class MusicLibraryService
{
    public List<Song> LoadSongsFromFolder(string folderPath)
    {
        var songs = new List<Song>();

        if (!Directory.Exists(folderPath))
            return songs;

        var mp3Files = Directory.GetFiles(folderPath, "*.mp3", SearchOption.AllDirectories);

        foreach (var filePath in mp3Files)
        {
            var song = new Song
            {
                FilePath = filePath,
                Title = Path.GetFileNameWithoutExtension(filePath)
            };

            var lrcPath = Path.ChangeExtension(filePath, ".lrc");
            song.LyricsPath = File.Exists(lrcPath) ? lrcPath : null;

            try
            {
                using var file = TagLib.File.Create(filePath);

                if (!string.IsNullOrWhiteSpace(file.Tag.Title))
                    song.Title = file.Tag.Title;

                if (!string.IsNullOrWhiteSpace(file.Tag.FirstPerformer))
                    song.Artist = file.Tag.FirstPerformer;

                if (!string.IsNullOrWhiteSpace(file.Tag.Album))
                    song.Album = file.Tag.Album;

                song.Duration = file.Properties.Duration;

                if (file.Tag.Pictures.Length > 0)
                    song.CoverArt = file.Tag.Pictures[0].Data.Data;
            }
            catch
            {
                // Si la metadata no se puede leer (tags corruptos, formato raro, etc.)
                // igual mostramos la canción con el nombre del archivo, en vez de ocultarla.
            }

            songs.Add(song);
        }

        return songs;
    }
}
