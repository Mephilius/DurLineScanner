using System.Windows.Media;
using DurLineScanner.Scanning;

namespace DurLineScanner.Controls;

/// <summary>Einheitliche Farben der App (Fluent-angelehnt) für Bewertungen und Akzente.</summary>
public static class Palette
{
    public static readonly Color VeryBad = Color.FromRgb(0xA4, 0x26, 0x2C);
    public static readonly Color Bad = Color.FromRgb(0xE0, 0x46, 0x3C);
    public static readonly Color Marginal = Color.FromRgb(0xF7, 0x8C, 0x1F);
    public static readonly Color Good = Color.FromRgb(0x7F, 0xBA, 0x3B);
    public static readonly Color VeryGood = Color.FromRgb(0x10, 0x8C, 0x3E);

    public static readonly Color Accent = Color.FromRgb(0x00, 0x5F, 0xB8);
    public static readonly Color Text = Color.FromRgb(0x1B, 0x1B, 0x1B);
    public static readonly Color TextSecondary = Color.FromRgb(0x61, 0x61, 0x61);
    public static readonly Color Track = Color.FromRgb(0xE6, 0xE6, 0xE6);

    public static Color For(Grade grade) => grade switch
    {
        Grade.VeryBad => VeryBad,
        Grade.Bad => Bad,
        Grade.Marginal => Marginal,
        Grade.Good => Good,
        _ => VeryGood,
    };

    public static SolidColorBrush Brush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static ScottPlot.Color ToPlot(Color c) => new(c.R, c.G, c.B, c.A);
}
