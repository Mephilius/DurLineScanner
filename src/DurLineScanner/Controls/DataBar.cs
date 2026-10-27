using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DurLineScanner.Controls;

/// <summary>Datenbalken für Tabellenzellen: farbiger Balken proportional zum Wert, Zahl daneben.</summary>
public sealed class DataBar : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double?), typeof(DataBar), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(DataBar), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(DataBar), new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(DataBar), new FrameworkPropertyMetadata(Brushes.SteelBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Typeface Face = new("Segoe UI");
    private static readonly Brush TrackBrush = Palette.Brush(Color.FromRgb(0xF0, 0xF0, 0xF0));
    private static readonly Brush TextBrush = Palette.Brush(Palette.Text);
    private const double TextWidth = 42;

    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 150 : availableSize.Width, 18);

    protected override void OnRender(DrawingContext dc)
    {
        double barWidth = Math.Max(0, ActualWidth - TextWidth - 6);
        var track = new Rect(0, (ActualHeight - 10) / 2, barWidth, 10);
        dc.DrawRoundedRectangle(TrackBrush, null, track, 3, 3);

        if (Value is not { } v) return;
        double fraction = Math.Clamp((v - Minimum) / (Maximum - Minimum), 0, 1);
        if (fraction > 0)
            dc.DrawRoundedRectangle(Fill, null, new Rect(track.X, track.Y, Math.Max(3, barWidth * fraction), track.Height), 3, 3);

        var text = new FormattedText(v.ToString("0.#", CultureInfo.CurrentCulture), CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, Face, 12, TextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point(ActualWidth - text.Width, (ActualHeight - text.Height) / 2));
    }
}
