using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MusicPlayer.Views;
public class MarqueeText : Canvas
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MarqueeText),
        new PropertyMetadata(string.Empty, (d, _) => ((MarqueeText)d).OnTextChanged()));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double Speed { get; set; } = 45;
    public double Gap { get; set; } = 60;
    private readonly StackPanel _strip = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock _first = new();
    private readonly TextBlock _second = new();
    private readonly TranslateTransform _shift = new();
    private readonly BitmapCache _cache = new() { SnapsToDevicePixels = false };
    public MarqueeText()
    {
        ClipToBounds = true;
        UseLayoutRounding = false;
        SnapsToDevicePixels = false;

        _strip.RenderTransform = _shift;
        _strip.CacheMode = _cache;
        _strip.Children.Add(_first);
        _strip.Children.Add(_second);
        Children.Add(_strip);

        SizeChanged += (_, _) => Refresh();
        Loaded += (_, _) => _cache.RenderAtScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
    }

    private void OnTextChanged()
    {
        var text = Text ?? string.Empty;
        _first.Text = text;
        _second.Text = text;
        InvalidateMeasure();
        Refresh();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _strip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        double width = double.IsInfinity(availableSize.Width)
            ? _first.DesiredSize.Width
            : availableSize.Width;

        return new Size(width, _strip.DesiredSize.Height);
    }

    private void Refresh()
    {
        double available = ActualWidth;
        if (available <= 0)
            return;

        _first.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double textWidth = _first.DesiredSize.Width;
        _shift.BeginAnimation(TranslateTransform.XProperty, null);
        _shift.X = 0;

        if (textWidth <= available)
        {
            _second.Visibility = Visibility.Collapsed;
            SetLeft(_strip, (available - textWidth) / 2);
            return;
        }

        _second.Visibility = Visibility.Visible;
        _second.Margin = new Thickness(Gap, 0, 0, 0);
        SetLeft(_strip, 0);

        double distance = textWidth + Gap;
        var animation = new DoubleAnimation(0, -distance, TimeSpan.FromSeconds(distance / Speed))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };
        Timeline.SetDesiredFrameRate(animation, 60);
        _shift.BeginAnimation(TranslateTransform.XProperty, animation);
    }
}