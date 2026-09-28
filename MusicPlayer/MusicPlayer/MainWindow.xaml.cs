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

        // Un único PlayerViewModel compartido entre ambas ventanas
        _playerViewModel = new PlayerViewModel();
        var libraryViewModel = new LibraryViewModel(_playerViewModel);
        LibraryTabView.DataContext = libraryViewModel;

        // Al abrir la app, se abre también la ventana de pantalla
        Loaded += (_, _) => ShowPlayerWindow();
    }

    /// <summary>Abre la ventana de pantalla (o la trae al frente si ya existe).</summary>
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
            Activate(); // el control se queda con el foco
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
        Application.Current.Shutdown(); // al cerrar el control, se cierra todo
    }
}