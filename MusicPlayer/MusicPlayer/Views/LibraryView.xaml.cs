using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }

    private void OpenPlayer_Click(object sender, RoutedEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.ShowPlayerWindow();
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.TogglePlayerFullscreen();
    }
}