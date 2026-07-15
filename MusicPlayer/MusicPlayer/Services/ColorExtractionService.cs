using System.IO;
using System.Windows.Media;

namespace MusicPlayer.Services;

/// <summary>
/// Extrae los colores dominantes de la carátula de un álbum para usarlos
/// como fondo dinámico en la vista de reproducción.
/// </summary>
public class ColorExtractionService
{
    private static readonly Color FallbackPrimary = Color.FromRgb(30, 30, 30);
    private static readonly Color FallbackSecondary = Color.FromRgb(10, 10, 10);

    public (Color Primary, Color Secondary) GetDominantColors(byte[]? coverArt)
    {
        if (coverArt is null || coverArt.Length == 0)
            return (FallbackPrimary, FallbackSecondary);

        try
        {
            using var stream = new MemoryStream(coverArt);
            using var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(stream);

            var colorThief = new ColorThiefDotNet.ColorThief();
            var palette = colorThief.GetPalette(bitmap, 5);

            if (palette.Count == 0)
                return (FallbackPrimary, FallbackSecondary);

            var primary = palette[0].Color;
            var secondary = palette.Count > 1 ? palette[1].Color : primary;

            return (
                Color.FromRgb((byte)primary.R, (byte)primary.G, (byte)primary.B),
                Color.FromRgb((byte)secondary.R, (byte)secondary.G, (byte)secondary.B)
            );
        }
        catch
        {
            return (FallbackPrimary, FallbackSecondary);
        }
    }
}
