using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DurLineScanner.Scanning;

namespace DurLineScanner.Controls;

public sealed class GradeBrushConverter : IValueConverter
{
    public static readonly GradeBrushConverter Instance = new();

    private static readonly Brush None = Palette.Brush(Color.FromRgb(0xB0, 0xB0, 0xB0));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Grade grade ? Palette.Brush(Palette.For(grade)) : None;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class GradeTextConverter : IValueConverter
{
    public static readonly GradeTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Grade grade ? SignalLimits.GradeText(grade) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
