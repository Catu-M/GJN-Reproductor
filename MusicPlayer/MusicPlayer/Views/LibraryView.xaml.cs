using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MusicPlayer.ViewModels;

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
    private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is LibraryViewModel vm && sender is Slider slider)
        {
            vm.BeginSeek();
            vm.DragSeek(GetSecondsAtClick(slider, e));
        }
    }
    private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is LibraryViewModel vm && sender is Slider slider)
            vm.EndSeek(slider.Value);
    }
    private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DataContext is LibraryViewModel vm)
            vm.DragSeek(e.NewValue);
    }

    private static double GetSecondsAtClick(Slider slider, MouseButtonEventArgs e)
    {
        var width = slider.ActualWidth;
        if (width <= 0)
            return slider.Value;

        var x = e.GetPosition(slider).X;
        var fraction = Math.Clamp(x / width, 0, 1);

        return slider.Minimum + fraction * (slider.Maximum - slider.Minimum);
    }
}