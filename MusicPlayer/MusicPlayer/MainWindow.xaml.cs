using System.Windows;
using MusicPlayer.ViewModels;

namespace MusicPlayer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Un único PlayerViewModel compartido entre ambas pestañas:
        // la biblioteca le dice qué reproducir, la vista de reproducción
        // simplemente refleja su estado.
        var playerViewModel = new PlayerViewModel();
        var libraryViewModel = new LibraryViewModel(playerViewModel);

        LibraryTabView.DataContext = libraryViewModel;
        PlayerTabView.DataContext = playerViewModel;
    }
}
