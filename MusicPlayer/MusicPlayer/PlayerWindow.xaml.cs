using System.Windows;
using System.Windows.Input;

namespace MusicPlayer;

public partial class PlayerWindow : Window
{
    private bool _isFullscreen;

    public PlayerWindow(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel; 
    }
    public void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            WindowState = WindowState.Normal;  
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
        }

        _isFullscreen = !_isFullscreen;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.F11 || (e.Key == Key.Escape && _isFullscreen))
            ToggleFullscreen();
    }

    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        ToggleFullscreen();
    }
}