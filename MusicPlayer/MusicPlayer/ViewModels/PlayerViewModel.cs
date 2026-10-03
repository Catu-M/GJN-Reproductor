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
using System.Threading.Tasks;
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
    private bool hasSong;
    [ObservableProperty]
    private bool isPlaying;
    [ObservableProperty]
    private string playPauseLabel = "▶";
    [ObservableProperty]
    private TimeSpan currentPosition;
    [ObservableProperty]
    private TimeSpan totalDuration;
    [ObservableProperty]
    private double positionSeconds;
    [ObservableProperty]
    private double durationSeconds;
    [ObservableProperty]
    private string positionText = "00:00.000";
    [ObservableProperty]
    private string durationText = "00:00.000";
    [ObservableProperty]
    private bool isSeeking;
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
        TotalDuration = _audioFile.TotalTime;
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
        CurrentPosition = TimeSpan.Zero;
        TotalDuration = TimeSpan.Zero;
        CurrentSong = null;
    }
    //barra de progreso de la melodía 
    public void Seek(TimeSpan position)
    {
        if (_audioFile is null)
            return;

        if (position < TimeSpan.Zero)
            position = TimeSpan.Zero;
        else if (position > TotalDuration)
            position = TotalDuration;

        _audioFile.CurrentTime = position;
        CurrentPosition = position;
    }

    // colocar un tiempo exacto arrastrando la barra de progreso 
    public void BeginSeek() => IsSeeking = true;

    // mueve el audio en vivo 
    public void DragSeek(double seconds)
    {
        if (IsSeeking)
            Seek(TimeSpan.FromSeconds(seconds));
    }
    public void EndSeek(double seconds)
    {
        Seek(TimeSpan.FromSeconds(seconds));
        IsSeeking = false;
    }
    partial void OnIsPlayingChanged(bool value)
    {
        PlayPauseLabel = value ? "⏸" : "▶";
    }
    partial void OnCurrentSongChanged(Song? value)
    {
        HasSong = value is not null;
    }
    partial void OnCurrentPositionChanged(TimeSpan value)
    {
        PositionSeconds = value.TotalSeconds;
        PositionText = FormatTime(value);
    }
    partial void OnTotalDurationChanged(TimeSpan value)
    {
        DurationSeconds = value.TotalSeconds;
        DurationText = FormatTime(value);
    }
    private static string FormatTime(TimeSpan t) => t.ToString(@"mm\:ss\.fff");

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_audioFile is null)
            return;

        if (!IsSeeking)
            CurrentPosition = _audioFile.CurrentTime;

        if (_audioFile.CurrentTime >= _audioFile.TotalTime)
            Stop();
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
    private async void LoadBackgroundColors(byte[]? coverArt)
    {
        var song = CurrentSong;

        var palette = await Task.Run(() => _colorService.GetPalette(coverArt, 6));

        if (CurrentSong != song) return;

        BackgroundPrimary = palette[0];
        BackgroundSecondary = palette.Count > 1 ? palette[1] : palette[0];
        Palette = palette;
    }
}