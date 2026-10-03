using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer.Models;

public enum LyricLineState
{
    Past,
    Current,
    Future
}

public partial class LyricLine : ObservableObject
{
    public TimeSpan Time { get; init; }
    public string Text { get; init; } = string.Empty;
    [ObservableProperty]
    private LyricLineState state = LyricLineState.Future;
    [ObservableProperty]
    private double fontSize = 18;
    [ObservableProperty]
    private double opacity = 0.3;
    [ObservableProperty]
    private bool isVisible;
}