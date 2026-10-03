using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MusicPlayer.Views;
public class AnimatedBackground : Canvas
{
    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette),
        typeof(IReadOnlyList<Color>),
        typeof(AnimatedBackground),
        new PropertyMetadata(null, (d, _) => ((AnimatedBackground)d).SetTargets(instant: false))
    );

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
        public Vector3 Cur, Target;
    }
    private const double ColorTau = 0.7;
    private const double MaxDt = 0.05;
    private const float BaseDarken = 0.40f;
    private static readonly double[] StopOffsets = { 0.0, 0.35, 0.70, 1.0 };
    private static readonly byte[] StopAlphas = { 210, 150, 50, 0 };
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
    private readonly SolidColorBrush _bgBrush = new(Color.FromRgb(20, 20, 20));

    private Vector3 _bgCur, _bgTarget;
    private double _time;       
    private double _lastFrame;
    private bool _settled = true; 
    private bool _rendering;

    public AnimatedBackground()
    {
        ClipToBounds = true;
        IsHitTestVisible = false;
        Background = _bgBrush;

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

        SetTargets(instant: true);
    }

    private void StartRendering()
    {
        if (_rendering) return;
        _lastFrame = _clock.Elapsed.TotalSeconds; 
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
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Min(now - _lastFrame, MaxDt);
        _lastFrame = now;
        if (dt <= 0) return;

        _time += dt;

        // Movimiento continuo, nunca se reinicia
        double w = ActualWidth;
        double h = ActualHeight;
        foreach (var b in _blobs)
        {
            b.Move.X = Math.Sin(_time * b.SpeedX + b.PhaseX) * b.AmpX * w;
            b.Move.Y = Math.Cos(_time * b.SpeedY + b.PhaseY) * b.AmpY * h;
        }

        if (_settled) return;

        float k = (float)(1 - Math.Exp(-dt / ColorTau));
        float maxDiff = 0;

        _bgCur += (_bgTarget - _bgCur) * k;
        maxDiff = Math.Max(maxDiff, (_bgTarget - _bgCur).Length());

        foreach (var b in _blobs)
        {
            b.Cur += (b.Target - b.Cur) * k;
            maxDiff = Math.Max(maxDiff, (b.Target - b.Cur).Length());
        }

        if (maxDiff < 0.4f)
        {
            _bgCur = _bgTarget;
            foreach (var b in _blobs) b.Cur = b.Target;
            _settled = true;
        }

        ApplyColors();
    }

    private void SetTargets(bool instant)
    {
        var palette = Palette is { Count: > 0 } ? Palette : FallbackPalette;

        _bgTarget = ToVec(palette[0]) * BaseDarken;
        for (int i = 0; i < _blobs.Count; i++)
            _blobs[i].Target = ToVec(palette[i % palette.Count]);

        if (instant)
        {
            _bgCur = _bgTarget;
            foreach (var b in _blobs) b.Cur = b.Target;
            _settled = true;
            ApplyColors();
        }
        else
        {
            _settled = false; // el siguiente fotograma empieza a transicionar
        }
    }
    private void ApplyColors()
    {
        _bgBrush.Color = ToColor(255, _bgCur);

        foreach (var b in _blobs)
        {
            for (int s = 0; s < b.Stops.Length; s++)
                b.Stops[s].Color = ToColor(StopAlphas[s], b.Cur);
        }
    }
    private static Vector3 ToVec(Color c) => new(c.R, c.G, c.B);

    private static Color ToColor(byte alpha, Vector3 v) => Color.FromArgb(
        alpha,
        (byte)Math.Clamp(MathF.Round(v.X), 0, 255),
        (byte)Math.Clamp(MathF.Round(v.Y), 0, 255),
        (byte)Math.Clamp(MathF.Round(v.Z), 0, 255));
}