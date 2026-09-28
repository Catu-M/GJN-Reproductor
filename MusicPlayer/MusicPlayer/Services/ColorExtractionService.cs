using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace MusicPlayer.Services;

public class ColorExtractionService
{
    private static readonly Color FallbackPrimary = Color.FromRgb(30, 30, 30);
    private static readonly Color FallbackSecondary = Color.FromRgb(10, 10, 10);

    public (Color Primary, Color Secondary) GetDominantColors(byte[]? coverArt)
    {
        var p = GetPalette(coverArt, 2);
        return (p[0], p.Count > 1 ? p[1] : p[0]);
    }

    /// <summary>Devuelve varios colores dominantes de la carátula.</summary>
    public IReadOnlyList<Color> GetPalette(byte[]? coverArt, int count = 6)
    {
        var fallback = new List<Color> { FallbackPrimary, Color.FromRgb(45, 45, 55), FallbackSecondary };

        if (coverArt is null || coverArt.Length == 0)
            return fallback;

        try
        {
            using var stream = new MemoryStream(coverArt);
            using var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(stream);

            var colorThief = new ColorThiefDotNet.ColorThief();
            var palette = colorThief.GetPalette(bitmap, count);

            var list = palette
                .Select(p => Boost(Color.FromRgb((byte)p.Color.R, (byte)p.Color.G, (byte)p.Color.B), 1.25))
                .ToList();

            return list.Count > 0 ? list : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    // Sube la saturación para que las burbujas se vean más vivas
    private static Color Boost(Color c, double satFactor)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double d = max - min;

        double h = 0;
        if (d > 0)
        {
            if (max == r) h = ((g - b) / d) % 6;
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60;
            if (h < 0) h += 360;
        }

        double s = max == 0 ? 0 : d / max;
        double v = max;
        s = Math.Min(1.0, s * satFactor);

        double cc = v * s;
        double x = cc * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - cc;

        (double R, double G, double B) rgb =
            h < 60 ? (cc, x, 0) :
            h < 120 ? (x, cc, 0) :
            h < 180 ? (0, cc, x) :
            h < 240 ? (0, x, cc) :
            h < 300 ? (x, 0, cc) : (cc, 0, x);

        return Color.FromRgb(
            (byte)Math.Clamp((rgb.R + m) * 255, 0, 255),
            (byte)Math.Clamp((rgb.G + m) * 255, 0, 255),
            (byte)Math.Clamp((rgb.B + m) * 255, 0, 255));
    }
}