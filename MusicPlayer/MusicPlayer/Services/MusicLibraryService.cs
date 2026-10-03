using System;
using System.Collections.Generic;
using System.IO;
using MusicPlayer.Models;

namespace MusicPlayer.Services;
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
                // por si no hay metadatos no de error 
            }
            songs.Add(song);
        }
        return songs;
    }
}
