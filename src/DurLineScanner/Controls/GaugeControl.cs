using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DurLineScanner.Scanning;

namespace DurLineScanner.Controls;

/// <summary>
/// Needle gauge: 240° scale with colored grade bands, needle, peak marker
/// and value with grade in the center. The tooltip explains the metric.
/// </summary>
public sealed class GaugeControl : FrameworkElement
{
    private const double StartAngle = 150;  // degrees, clockwise from 3 o'clock
    private const double SweepAngle = 240;

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(MetricScale), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnScaleChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double?), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PeakProperty = DependencyProperty.Register(
        nameof(Peak), typeof(double?), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RangeMinProperty = DependencyProperty.Register(
        nameof(RangeMin), typeof(double?), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RangeMaxProperty = DependencyProperty.Register(
        nameof(RangeMax), typeof(double?), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RangeTextProperty = DependencyProperty.Register(
        nameof(RangeText), typeof(string), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText), typeof(string), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TooltipExtraProperty = DependencyProperty.Register(
        nameof(TooltipExtra), typeof(string), typeof(GaugeControl),
        new FrameworkPropertyMetadata(null, OnScaleChanged));

    private static readonly Typeface Regular = new("Segoe UI");
    private static readonly Typeface SemiBold = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Brush TextBrush = Palette.Brush(Palette.Text);
    private static readonly Brush SecondaryBrush = Palette.Brush(Palette.TextSecondary);
    private static readonly Brush NeedleBrush = Palette.Brush(Color.FromRgb(0x33, 0x33, 0x33));
    private static readonly Brush PeakBrush = Palette.Brush(Palette.Accent);
    private static readonly Brush RangeBrush = Palette.Brush(Color.FromRgb(0x5C, 0x2D, 0x91));

    public MetricScale? Scale
    {
        get => (MetricScale?)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    /// <summary>Value on the scale (the exponent for BER).</summary>
    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Best value so far (peak marker).</summary>
    public double? Peak
    {
        get => (double?)GetValue(PeakProperty);
        set => SetValue(PeakProperty, value);
    }

    /// <summary>Smallest value of the mini scan (outer range arc).</summary>
    public double? RangeMin
    {
        get => (double?)GetValue(RangeMinProperty);
        set => SetValue(RangeMinProperty, value);
    }

    /// <summary>Largest value of the mini scan.</summary>
    public double? RangeMax
    {
        get => (double?)GetValue(RangeMaxProperty);
        set => SetValue(RangeMaxProperty, value);
    }

    /// <summary>Label of the range, e.g. "Mini-Scan 12,1 … 15,4".</summary>
    public string? RangeText
    {
        get => (string?)GetValue(RangeTextProperty);
        set => SetValue(RangeTextProperty, value);
    }

    /// <summary>Display text instead of the numeric value, e.g. "3,6E-5".</summary>
    public string? ValueText
    {
        get => (string?)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    /// <summary>Extra line below the value, e.g. "Reserve +11,8 dB".</summary>
    public string? Caption
    {
        get => (string?)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public string? TooltipExtra
    {
        get => (string?)GetValue(TooltipExtraProperty);
        set => SetValue(TooltipExtraProperty, value);
    }

    private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (GaugeControl)d;
        if (gauge.Scale is { } scale) MetricToolTip.Attach(gauge, scale, gauge.TooltipExtra);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 220 : availableSize.Height);

    protected override void OnRender(DrawingContext dc)
    {
        // Transparent background so the tooltip responds on the whole area
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        if (Scale is not { } scale || ActualWidth < 60 || ActualHeight < 60) return;

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        double titleHeight = 24;
        // Below the center the text block (value, unit, grade, extra line) needs about 0.95 r
        double radius = Math.Min(ActualWidth / 2 - 16, (ActualHeight - titleHeight - 16) / 1.95);
        if (radius < 30) return;
        var center = new Point(ActualWidth / 2, titleHeight + radius + 12);
        double band = Math.Max(8, radius * 0.13);

        // Title
        DrawText(dc, scale.Title, SemiBold, 14, TextBrush, new Point(ActualWidth / 2, 2), dpi, center: true);

        // Base track and colored bands (ungraded scales stay gray)
        double arcRadius = radius - band / 2;
        dc.DrawGeometry(null, new Pen(Palette.Brush(Palette.Track), band), Arc(center, arcRadius, 0, 1));
        foreach (var b in scale.Graded ? scale.Bands : [])
        {
            double f0 = Fraction(scale, b.From), f1 = Fraction(scale, b.To);
            if (f1 - f0 <= 0.001) continue;
            var pen = new Pen(Palette.Brush(Palette.For(b.Grade)), band) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
            dc.DrawGeometry(null, pen, Arc(center, arcRadius, f0, f1));
        }

        // Tick marks and labels
        var ticks = Ticks(scale);
        var tickPen = new Pen(SecondaryBrush, 1);
        foreach (var t in ticks)
        {
            double angle = Angle(Fraction(scale, t));
            var inner = Polar(center, radius - band - 2, angle);
            var outer = Polar(center, radius - band - 7, angle);
            dc.DrawLine(tickPen, inner, outer);
            var labelPos = Polar(center, radius - band - 18, angle);
            DrawText(dc, scale.Format(t), Regular, Math.Max(9, radius * 0.085), SecondaryBrush, labelPos, dpi, center: true, vCenter: true);
        }

        // Mini scan range as an outer arc, with end caps
        if (RangeMin is { } rMin && RangeMax is { } rMax)
        {
            double f0 = Fraction(scale, Math.Min(rMin, rMax)), f1 = Fraction(scale, Math.Max(rMin, rMax));
            if (f1 - f0 < 0.004) f1 = Math.Min(1, f0 + 0.004);
            var rangePen = new Pen(RangeBrush, 5) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
            dc.DrawGeometry(null, rangePen, Arc(center, radius + 5, f0, f1));
            var capPen = new Pen(RangeBrush, 1.5);
            foreach (var f in new[] { f0, f1 })
                dc.DrawLine(capPen, Polar(center, radius + 1, Angle(f)), Polar(center, radius + 10, Angle(f)));
        }

        // Peak marker outside
        if (Peak is { } peak)
        {
            double a = Angle(Fraction(scale, peak));
            var tip = Polar(center, radius + 1, a);
            var l = Polar(center, radius + 9, a - 3);
            var r = Polar(center, radius + 9, a + 3);
            dc.DrawGeometry(PeakBrush, null, Triangle(tip, l, r));
        }

        // Needle
        if (Value is { } value)
        {
            double a = Angle(Fraction(scale, value));
            var tip = Polar(center, radius - band - 4, a);
            var left = Polar(center, radius * 0.06, a - 90);
            var right = Polar(center, radius * 0.06, a + 90);
            dc.DrawGeometry(NeedleBrush, null, Triangle(tip, left, right));
        }
        dc.DrawEllipse(NeedleBrush, null, center, radius * 0.07, radius * 0.07);

        // Value with unit, below it grade and extra line
        double valueSize = Math.Max(14, radius * 0.27);
        double smallSize = Math.Max(10, radius * 0.095);
        string valueText = ValueText ?? (Value is { } v ? v.ToString("0.0", CultureInfo.CurrentCulture) : "–");
        var valueFt = Text(valueText, SemiBold, valueSize, TextBrush, dpi);
        var unitFt = Text(scale.Unit, Regular, smallSize * 1.15, SecondaryBrush, dpi);
        double unitGap = scale.Unit.Length > 0 ? 4 : 0;
        double totalWidth = valueFt.Width + unitGap + unitFt.Width;
        double y = center.Y + radius * 0.14;
        dc.DrawText(valueFt, new Point(center.X - totalWidth / 2, y));
        if (scale.Unit.Length > 0)
            dc.DrawText(unitFt, new Point(center.X - totalWidth / 2 + valueFt.Width + unitGap, y + valueFt.Baseline - unitFt.Baseline));
        y += valueFt.Height + 2;

        if (scale.Evaluate(Value) is { } grade)
        {
            var text = Text(SignalLimits.GradeText(grade), SemiBold, smallSize, Brushes.White, dpi);
            var pill = new Rect(center.X - text.Width / 2 - 9, y, text.Width + 18, text.Height + 4);
            dc.DrawRoundedRectangle(Palette.Brush(Palette.For(grade)), null, pill, pill.Height / 2, pill.Height / 2);
            dc.DrawText(text, new Point(pill.X + 9, pill.Y + 2));
            y = pill.Bottom + 3;
        }

        // Extra line and mini scan range side by side if there is enough width
        if (!string.IsNullOrEmpty(Caption) && !string.IsNullOrEmpty(RangeText))
        {
            var captionFt = Text(Caption + "  ·  ", Regular, smallSize, SecondaryBrush, dpi);
            var rangeFt = Text(RangeText, SemiBold, smallSize, RangeBrush, dpi);
            double width = captionFt.WidthIncludingTrailingWhitespace + rangeFt.Width;
            if (width <= ActualWidth - 8)
            {
                double x = center.X - width / 2;
                dc.DrawText(captionFt, new Point(x, y));
                dc.DrawText(rangeFt, new Point(x + captionFt.WidthIncludingTrailingWhitespace, y));
                return;
            }
        }
        if (!string.IsNullOrEmpty(Caption))
        {
            DrawText(dc, Caption, Regular, smallSize, SecondaryBrush, new Point(center.X, y), dpi, center: true);
            y += smallSize * 1.4;
        }
        if (!string.IsNullOrEmpty(RangeText))
            DrawText(dc, RangeText, SemiBold, smallSize, RangeBrush, new Point(center.X, y), dpi, center: true);
    }

    private static double Fraction(MetricScale scale, double value) =>
        Math.Clamp((value - scale.Min) / (scale.Max - scale.Min), 0, 1);

    private static double Angle(double fraction) => StartAngle + SweepAngle * fraction;

    private static Point Polar(Point center, double r, double angleDeg)
    {
        double rad = angleDeg * Math.PI / 180;
        return new Point(center.X + r * Math.Cos(rad), center.Y + r * Math.Sin(rad));
    }

    private static Geometry Arc(Point center, double r, double f0, double f1)
    {
        double a0 = Angle(f0), a1 = Angle(f1);
        var figure = new PathFigure { StartPoint = Polar(center, r, a0), IsClosed = false };
        figure.Segments.Add(new ArcSegment(Polar(center, r, a1), new Size(r, r), 0, a1 - a0 > 180, SweepDirection.Clockwise, true));
        var geometry = new PathGeometry([figure]);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry Triangle(Point a, Point b, Point c)
    {
        var figure = new PathFigure { StartPoint = a, IsClosed = true, IsFilled = true };
        figure.Segments.Add(new LineSegment(b, false));
        figure.Segments.Add(new LineSegment(c, false));
        var geometry = new PathGeometry([figure]);
        geometry.Freeze();
        return geometry;
    }

    private static IEnumerable<double> Ticks(MetricScale scale)
    {
        double range = scale.Max - scale.Min;
        double step = range switch
        {
            <= 8 => 1,
            <= 20 => 2,
            <= 40 => 5,
            <= 60 => 10,
            _ => 20,
        };
        if (range / step > 10) step *= 2;
        for (double t = Math.Ceiling(scale.Min / step) * step; t <= scale.Max + 1e-9; t += step)
            yield return t;
    }

    private static FormattedText Text(string text, Typeface face, double size, Brush brush, double dpi) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, size, brush, dpi);

    private static void DrawText(DrawingContext dc, string text, Typeface face, double size, Brush brush, Point at,
        double dpi, bool center = false, bool vCenter = false)
    {
        var ft = Text(text, face, size, brush, dpi);
        var origin = new Point(center ? at.X - ft.Width / 2 : at.X, vCenter ? at.Y - ft.Height / 2 : at.Y);
        dc.DrawText(ft, origin);
    }
}
