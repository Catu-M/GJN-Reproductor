using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MusicPlayer.Views;

/// <summary>
/// Fondo animado tipo "Apple Music": burbujas de color con degradado radial
/// (color -> transparente) que flotan por toda la superficie.
/// </summary>
public class AnimatedBackground : Canvas
{
    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette),
        typeof(IReadOnlyList<Color>),
        typeof(AnimatedBackground),
        new PropertyMetadata(null, (d, _) => ((AnimatedBackground)d).ApplyPalette()));

    public IReadOnlyList<Color>? Palette
    {
        get => (IReadOnlyList<Color>?)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    private sealed class Blob
    {
        public Ellipse Shape = null!;
        public GradientStop[] Stops = null!;
        public TranslateTransform Move = new();
        public double AnchorX, AnchorY, Size, AmpX, AmpY, SpeedX, SpeedY, PhaseX, PhaseY;
    }

    // Offsets y opacidades del degradado: el centro es opaco y el borde queda en 0
    // (esto es lo que hace que las burbujas se fundan con el fondo).
    private static readonly double[] StopOffsets = { 0.0, 0.35, 0.70, 1.0 };
    private static readonly byte[] StopAlphas = { 210, 150, 50, 0 };

    // anchorX, anchorY, tamaño, ampX, ampY, velX, velY, faseX, faseY
    // Repartidas por TODA la pantalla, no en una esquina.
    private static readonly double[][] Layout =
    {
        new[] { 0.15, 0.20, 0.85, 0.12, 0.10, 0.23, 0.31, 0.0, 1.2 },
        new[] { 0.85, 0.15, 0.80, 0.10, 0.14, 0.29, 0.19, 2.1, 0.4 },
        new[] { 0.50, 0.50, 0.90, 0.18, 0.16, 0.17, 0.25, 4.0, 2.5 },
        new[] { 0.15, 0.85, 0.80, 0.14, 0.10, 0.27, 0.21, 1.0, 3.3 },
        new[] { 0.85, 0.85, 0.85, 0.10, 0.12, 0.20, 0.33, 3.2, 5.0 },
        new[] { 0.50, 0.05, 0.70, 0.20, 0.08, 0.31, 0.22, 5.0, 0.9 },
        new[] { 0.50, 0.98, 0.70, 0.20, 0.08, 0.24, 0.28, 0.7, 4.4 },
    };

    private static readonly Color[] FallbackPalette =
    {
        Color.FromRgb(60, 60, 70), Color.FromRgb(40, 40, 50), Color.FromRgb(30, 30, 30)
    };

    private readonly List<Blob> _blobs = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _paletteApplied;
    private bool _rendering;

    public AnimatedBackground()
    {
        ClipToBounds = true;
        IsHitTestVisible = false;
        Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));

        foreach (var l in Layout)
        {
            var blob = new Blob
            {
                AnchorX = l[0],
                AnchorY = l[1],
                Size = l[2],
                AmpX = l[3],
                AmpY = l[4],
                SpeedX = l[5],
                SpeedY = l[6],
                PhaseX = l[7],
                PhaseY = l[8],
            };

            blob.Stops = new GradientStop[StopOffsets.Length];
            var brush = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
            };
            for (int i = 0; i < StopOffsets.Length; i++)
            {
                blob.Stops[i] = new GradientStop(Colors.Transparent, StopOffsets[i]);
                brush.GradientStops.Add(blob.Stops[i]);
            }

            blob.Shape = new Ellipse
            {
                Fill = brush,
                IsHitTestVisible = false,
                RenderTransform = blob.Move,
            };

            _blobs.Add(blob);
            Children.Add(blob.Shape);
        }

        Loaded += (_, _) => StartRendering();
        Unloaded += (_, _) => StopRendering();
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible) StartRendering(); else StopRendering();
        };

        ApplyPalette();
    }

    private void StartRendering()
    {
        if (_rendering) return;
        CompositionTarget.Rendering += OnFrame;
        _rendering = true;
    }

    private void StopRendering()
    {
        if (!_rendering) return;
        CompositionTarget.Rendering -= OnFrame;
        _rendering = false;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        double w = sizeInfo.NewSize.Width;
        double h = sizeInfo.NewSize.Height;
        double baseSize = Math.Max(w, h) * 0.8;

        foreach (var b in _blobs)
        {
            double d = b.Size * baseSize;
            b.Shape.Width = d;
            b.Shape.Height = d;
            SetLeft(b.Shape, b.AnchorX * w - d / 2);
            SetTop(b.Shape, b.AnchorY * h - d / 2);
        }
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        double t = _clock.Elapsed.TotalSeconds;
        double w = ActualWidth;
        double h = ActualHeight;

        foreach (var b in _blobs)
        {
            b.Move.X = Math.Sin(t * b.SpeedX + b.PhaseX) * b.AmpX * w;
            b.Move.Y = Math.Cos(t * b.SpeedY + b.PhaseY) * b.AmpY * h;
        }
    }

    private void ApplyPalette()
    {
        var palette = Palette is { Count: > 0 } ? Palette : FallbackPalette;
        bool animate = _paletteApplied && IsLoaded;
        var duration = new Duration(TimeSpan.FromSeconds(1.2));

        // Fondo base: el color principal, oscurecido
        var c0 = palette[0];
        var baseColor = Color.FromRgb((byte)(c0.R * 0.40), (byte)(c0.G * 0.40), (byte)(c0.B * 0.40));
        var bg = (SolidColorBrush)Background;
        if (animate) bg.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(baseColor, duration));
        else { bg.BeginAnimation(SolidColorBrush.ColorProperty, null); bg.Color = baseColor; }

        // Cada burbuja toma un color de la paleta (cíclico)
        for (int i = 0; i < _blobs.Count; i++)
        {
            var c = palette[i % palette.Count];
            var stops = _blobs[i].Stops;
            for (int s = 0; s < stops.Length; s++)
            {
                var target = Color.FromArgb(StopAlphas[s], c.R, c.G, c.B);
                if (animate)
                    stops[s].BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(target, duration));
                else
                {
                    stops[s].BeginAnimation(GradientStop.ColorProperty, null);
                    stops[s].Color = target;
                }
            }
        }

        _paletteApplied = true;
    }
}