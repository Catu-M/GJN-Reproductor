using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MusicPlayer.Models;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views;

public partial class PlayerView : UserControl
{

    private const double LeadSeconds = 0.15;    
    private const double Tau = 0.22;             
    private const double MinScale = 0.58;        
    private const double InactiveOpacity = 0.30; 
    private const double FadeDistance = 2.5;    
    private const int Window = 5;           

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastFrame;
    private double _focus;
    private double _snapUntil;
    private Song? _lastSong;
    private int _lastCount = -1;

    public PlayerView()
    {
        InitializeComponent();
        CompositionTarget.Rendering += OnRender;
    }

    private void OnRender(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Min(now - _lastFrame, 0.1);
        _lastFrame = now;

        if (DataContext is not PlayerViewModel vm) return;

        var lyrics = vm.Lyrics;
        int n = lyrics.Count;
        if (n == 0) { _lastCount = 0; return; }

        // Canción nueva: colocar el foco de golpe
        if (vm.CurrentSong != _lastSong || n != _lastCount)
        {
            _lastSong = vm.CurrentSong;
            _lastCount = n;
            _snapUntil = now + 0.3;
        }

        // Última línea con texto cuyo tiempo ya pasó
        double t = vm.CurrentPosition.TotalSeconds + LeadSeconds;
        int target = 0;
        for (int i = 0; i < n; i++)
        {
            if (lyrics[i].Time.TotalSeconds > t) break;
            if (!string.IsNullOrWhiteSpace(lyrics[i].Text)) target = i;
        }

        // Si el foco está roto o muy lejos, se corrige de golpe
        if (double.IsNaN(_focus) || double.IsInfinity(_focus) || Math.Abs(target - _focus) > 8)
            _focus = target;

        if (now < _snapUntil)
            _focus = target;
        else
            _focus += (target - _focus) * (1 - Math.Exp(-dt / Tau));

        for (int i = 0; i < n; i++)
            ApplyLine(i);

        ScrollToFocus(n);
    }

    private void ApplyLine(int i)
    {
        if (LyricsList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container) return;
        if (VisualTreeHelper.GetChildrenCount(container) == 0) return;
        if (VisualTreeHelper.GetChild(container, 0) is not TextBlock tb) return;

        double ad = Math.Abs(i - _focus);

        // Meseta: mientras el foco esté cerca de la línea, se queda a tamaño y opacidad completos.
        // Después baja con curva coseno (arranque y frenado suaves) hasta 0 a una línea de distancia.
        const double Plateau = 0.25;
        double e = ad <= Plateau ? 1
                 : ad >= 1 ? 0
                 : 0.5 * (1 + Math.Cos(Math.PI * (ad - Plateau) / (1 - Plateau)));

        double scale = MinScale + (1 - MinScale) * e;

        double opacity;

        if (ad <= 1)
        {
            opacity = InactiveOpacity + (1 - InactiveOpacity) * e;
        }
        else
        {
            double f = Math.Clamp(1 - (ad - 1) / FadeDistance, 0, 1);
            opacity = InactiveOpacity * f * f;
        }

        tb.Opacity = opacity;

        if (tb.RenderTransform is not ScaleTransform st || st.IsFrozen)
        {
            st = new ScaleTransform();
            tb.RenderTransform = st;
        }
        st.ScaleX = scale;
        st.ScaleY = scale;
    }

    // Desplaza la lista para que el foco (incluso entre dos líneas) quede centrado
    private void ScrollToFocus(int n)
    {
        double host = LyricsHost.ActualHeight;
        if (host <= 0) return;

        int i0 = Math.Clamp((int)Math.Floor(_focus), 0, n - 1);
        int i1 = Math.Min(i0 + 1, n - 1);
        double frac = Math.Clamp(_focus - i0, 0, 1);

        double top = 0, c0 = 0, c1 = 0;
        for (int i = 0; i <= i1; i++)
        {
            if (LyricsList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement c)
                return;

            double h = c.ActualHeight;
            if (i == i0) c0 = top + h / 2;
            if (i == i1) c1 = top + h / 2;
            top += h;
        }

        double y = c0 + (c1 - c0) * frac;
        double offset = host / 2 - y;

        if (!double.IsNaN(offset) && !double.IsInfinity(offset))
            ScrollTransform.Y = offset;
    }

    private double? CenterY(int i)
    {
        if (LyricsList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement c) return null;
        if (c.ActualHeight <= 0 || !c.IsDescendantOf(LyricsList)) return null;

        return c.TransformToAncestor(LyricsList)
                .Transform(new Point(0, c.ActualHeight / 2)).Y;
    }
}