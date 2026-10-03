using System;
using System.Windows;
using MusicPlayer.ViewModels;

namespace MusicPlayer;

public partial class MainWindow : Window
{
    private readonly PlayerViewModel _playerViewModel;
    private PlayerWindow? _playerWindow;

    public MainWindow()
    {
        InitializeComponent();
        _playerViewModel = new PlayerViewModel();
        var libraryViewModel = new LibraryViewModel(_playerViewModel);
        LibraryTabView.DataContext = libraryViewModel;
        Loaded += (_, _) => ShowPlayerWindow();
    }
    public void ShowPlayerWindow()
    {
        if (_playerWindow is null)
        {
            _playerWindow = new PlayerWindow(_playerViewModel)
            {
                Left = Left + 60,
                Top = Top + 60
            };
            _playerWindow.Closed += (_, _) => _playerWindow = null;
            _playerWindow.Show();
            Activate(); 
        }
        else
        {
            _playerWindow.Activate();
        }
    }

    public void TogglePlayerFullscreen()
    {
        ShowPlayerWindow();
        _playerWindow?.ToggleFullscreen();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Application.Current.Shutdown(); 
    }
}