using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Models;
using MusicPlayer.Services;
using NAudio.Wave;
using System.Collections.Generic;

namespace MusicPlayer.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly LyricsService _lyricsService = new();
    private readonly ColorExtractionService _colorService = new();
    private readonly DispatcherTimer _timer;

    private IWavePlayer? _outputDevice;
    private AudioFileReader? _audioFile;

    [ObservableProperty]
    private Song? currentSong;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private string playPauseLabel = "▶";

    [ObservableProperty]
    private TimeSpan currentPosition;

    [ObservableProperty]
    private ImageSource? albumArt;

    [ObservableProperty]
    private Color backgroundPrimary = Color.FromRgb(20, 20, 20);

    [ObservableProperty]
    private Color backgroundSecondary = Color.FromRgb(10, 10, 10);

    [ObservableProperty]
    private IReadOnlyList<Color> palette = Array.Empty<Color>();

    [ObservableProperty]
    private Color backgroundBase = Color.FromRgb(12, 14, 22);

    [ObservableProperty]
    private Color blobColor = Color.FromArgb(120, 160, 30, 40);

    [ObservableProperty]
    private Color blobSoftColor = Color.FromArgb(190, 40, 44, 56);

    [ObservableProperty]
    private int activeLyricIndex = -1;

    public ObservableCollection<LyricLine> Lyrics { get; } = new();

    public PlayerViewModel()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;
    }

    public void Play(Song song)
    {
        Stop();

        CurrentSong = song;

        _outputDevice = new WaveOutEvent();
        _audioFile = new AudioFileReader(song.FilePath);
        _outputDevice.Init(_audioFile);
        _outputDevice.Play();

        IsPlaying = true;
        _timer.Start();

        LoadAlbumArt(song.CoverArt);
        LoadLyricsFor(song);
        LoadBackgroundColors(song.CoverArt);
    }

    [RelayCommand]
    private void TogglePause()
    {
        if (_outputDevice is null)
            return;

        if (IsPlaying)
        {
            _outputDevice.Pause();
            IsPlaying = false;
        }
        else
        {
            _outputDevice.Play();
            IsPlaying = true;
        }
    }

    public void Stop()
    {
        _timer.Stop();
        _outputDevice?.Stop();
        _outputDevice?.Dispose();
        _audioFile?.Dispose();
        _outputDevice = null;
        _audioFile = null;
        IsPlaying = false;
        ActiveLyricIndex = -1;
    }

    partial void OnIsPlayingChanged(bool value)
    {
        PlayPauseLabel = value ? "⏸" : "▶";
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_audioFile is null)
            return;

        CurrentPosition = _audioFile.CurrentTime;
        UpdateActiveLyric();

        if (_audioFile.CurrentTime >= _audioFile.TotalTime)
            Stop();
    }
    private const int LyricWindowRadius = 2;

    private void UpdateActiveLyric()
    {
        if (Lyrics.Count == 0)
            return;

        var index = -1;
        for (var i = 0; i < Lyrics.Count; i++)
        {
            if (Lyrics[i].Time <= CurrentPosition)
                index = i;
            else
                break;
        }

        ActiveLyricIndex = index;

        for (var i = 0; i < Lyrics.Count; i++)
        {
            var distance = i - index;       // negativo = ya pasó, positivo = falta
            var absDistance = Math.Abs(distance);
            var line = Lyrics[i];

            line.State = distance < 0
                ? LyricLineState.Past
                : distance == 0
                    ? LyricLineState.Current
                    : LyricLineState.Future;

            line.IsVisible = absDistance <= LyricWindowRadius;

            line.FontSize = absDistance switch
            {
                0 => 160,
                1 => 80,
                _ => 40
            };

            line.Opacity = absDistance switch
            {
                0 => 1.0,
                1 => 0.6,
                2 => 0.3,
                _ => 0.0
            };
        }
    }

    private void LoadLyricsFor(Song song)
    {
        Lyrics.Clear();

        if (string.IsNullOrEmpty(song.LyricsPath))
            return;

        foreach (var line in _lyricsService.LoadLyrics(song.LyricsPath))
            Lyrics.Add(line);
    }

    private void LoadAlbumArt(byte[]? coverArt)
    {
        if (coverArt is null || coverArt.Length == 0)
        {
            AlbumArt = null;
            return;
        }

        using var stream = new MemoryStream(coverArt);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        AlbumArt = image;
    }

    private void LoadBackgroundColors(byte[]? coverArt)
    {
        var (primary, secondary) = _colorService.GetDominantColors(coverArt);
        BackgroundPrimary = primary;
        BackgroundSecondary = secondary;

        Palette = _colorService.GetPalette(coverArt, 6);
    }

    private static Color Lighten(Color color, double amount, byte alpha)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            alpha,
            (byte)(color.R + (255 - color.R) * amount),
            (byte)(color.G + (255 - color.G) * amount),
            (byte)(color.B + (255 - color.B) * amount));
    }

    private static Color Darken(Color color, double factor)
    {
        factor = Math.Clamp(factor, 0, 1);
        return Color.FromRgb(
            (byte)(color.R * factor),
            (byte)(color.G * factor),
            (byte)(color.B * factor));
    }
}