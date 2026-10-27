using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DurLineScanner.Scanning;

namespace DurLineScanner.Controls;

/// <summary>Tooltip mit Erklärung eines Messwerts und seinen Bewertungsbereichen von sehr gut bis sehr schlecht.</summary>
public static class MetricToolTip
{
    public static ToolTip Build(MetricScale scale, string? extra = null)
    {
        var panel = new StackPanel { MaxWidth = 440, Margin = new Thickness(4) };
        panel.Children.Add(new TextBlock
        {
            Text = scale.Unit.Length > 0 ? $"{scale.Title} ({scale.Unit})" : scale.Title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4),
        });
        panel.Children.Add(new TextBlock { Text = scale.Explanation, TextWrapping = TextWrapping.Wrap });
        if (extra is not null)
            panel.Children.Add(new TextBlock { Text = extra, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });

        var grid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Von gut nach schlecht sortiert anzeigen
        var bands = scale.Bands.OrderByDescending(b => b.Grade).ThenBy(b => b.From).ToList();
        for (int i = 0; i < bands.Count; i++)
        {
            var band = bands[i];
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var swatch = new Rectangle
            {
                Width = 12, Height = 12, RadiusX = 2, RadiusY = 2,
                Fill = Palette.Brush(Palette.For(band.Grade)),
                Margin = new Thickness(0, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
            };
            AddCell(grid, swatch, i, 0);
            AddCell(grid, new TextBlock { Text = SignalLimits.GradeText(band.Grade), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 1, 12, 1) }, i, 1);
            AddCell(grid, new TextBlock { Text = scale.RangeText(band), Margin = new Thickness(0, 1, 12, 1) }, i, 2);
            AddCell(grid, new TextBlock { Text = band.Meaning, TextWrapping = TextWrapping.Wrap, Foreground = Palette.Brush(Palette.TextSecondary), Margin = new Thickness(0, 1, 0, 1) }, i, 3);
        }
        panel.Children.Add(grid);

        panel.Children.Add(new TextBlock
        {
            Text = L.F("Grundlage: {0}", scale.Basis),
            FontSize = 11,
            Foreground = Palette.Brush(Palette.TextSecondary),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        });

        return new ToolTip { Content = panel };
    }

    /// <summary>Hängt den Tooltip an ein Element; bleibt sichtbar, solange die Maus darauf steht.</summary>
    public static void Attach(FrameworkElement owner, MetricScale scale, string? extra = null)
    {
        owner.ToolTip = Build(scale, extra);
        ToolTipService.SetShowDuration(owner, int.MaxValue);
        ToolTipService.SetInitialShowDelay(owner, 300);
    }

    private static void AddCell(Grid grid, UIElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        grid.Children.Add(element);
    }
}
