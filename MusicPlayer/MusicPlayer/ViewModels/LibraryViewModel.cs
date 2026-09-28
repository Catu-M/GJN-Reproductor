using System.Collections.ObjectModel;
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
    public PlayerViewModel Player => _playerViewModel;

    [ObservableProperty]
    private string? libraryFolder;

    [ObservableProperty]
    private string statusMessage = "Elige una carpeta para empezar.";

    public LibraryViewModel(PlayerViewModel playerViewModel)
    {
        _playerViewModel = playerViewModel;
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
}
