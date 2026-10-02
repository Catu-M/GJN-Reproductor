using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Models;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly MusicLibraryService _libraryService = new();
    private readonly PlayerViewModel _playerViewModel;

    public ObservableCollection<Song> Songs { get; } = new();
    public ICollectionView SongsView { get; }

    public PlayerViewModel Player => _playerViewModel;

    [ObservableProperty]
    private string? libraryFolder;

    [ObservableProperty]
    private string statusMessage = "Elige una carpeta para empezar.";

    [ObservableProperty]
    private string searchText = string.Empty;

    public LibraryViewModel(PlayerViewModel playerViewModel)
    {
        _playerViewModel = playerViewModel;

        SongsView = CollectionViewSource.GetDefaultView(Songs);
        SongsView.Filter = FilterSong;
    }

    partial void OnSearchTextChanged(string value) => SongsView.Refresh();

    private bool FilterSong(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        if (obj is not Song song)
            return false;

        return song.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || song.Artist.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void ChooseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Selecciona la carpeta de tu música"
        };

        if (dialog.ShowDialog() == true)
        {
            LibraryFolder = dialog.FolderName;
            LoadSongs();
        }
    }

    private void LoadSongs()
    {
        Songs.Clear();

        if (string.IsNullOrEmpty(LibraryFolder))
            return;

        foreach (var song in _libraryService.LoadSongsFromFolder(LibraryFolder))
            Songs.Add(song);

        StatusMessage = Songs.Count == 0
            ? "No se encontraron archivos .mp3 en esta carpeta ni en sus subcarpetas."
            : $"{Songs.Count} canción(es) encontrada(s).";
    }

    [RelayCommand]
    private void PlaySong(Song? song)
    {
        if (song is not null)
            _playerViewModel.Play(song);
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _playerViewModel.Stop();
    }

    public void BeginSeek() => _playerViewModel.BeginSeek();

 
    public void DragSeek(double seconds) => _playerViewModel.DragSeek(seconds);

    /// <summary>Llamar cuando el usuario suelta el mouse; fija la posición final.</summary>
    public void EndSeek(double seconds) => _playerViewModel.EndSeek(seconds);

    [RelayCommand]
    private void NextSong() => PlayAdjacent(1);

    [RelayCommand]
    private void PreviousSong() => PlayAdjacent(-1);

    /// <summary>Reproduce la canción anterior/siguiente dentro del orden visible (respeta el buscador).</summary>
    private void PlayAdjacent(int offset)
    {
        var current = _playerViewModel.CurrentSong;
        if (current is null)
            return;

        var visible = SongsView.Cast<Song>().ToList();
        var index = visible.IndexOf(current);
        if (index < 0)
            return;

        var newIndex = index + offset;
        if (newIndex < 0 || newIndex >= visible.Count)
            return;

        _playerViewModel.Play(visible[newIndex]);
    }
}