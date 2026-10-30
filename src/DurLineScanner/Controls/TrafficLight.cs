using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DurLineScanner.Scanning;

namespace DurLineScanner.Controls;

/// <summary>Traffic light (red/yellow/green) with headline and reasons; all reasons also as a tooltip.</summary>
public sealed class TrafficLight : Border
{
    private static readonly Color Housing = Color.FromRgb(0x2B, 0x2B, 0x2B);
    private static readonly Color Off = Color.FromRgb(0x4A, 0x4A, 0x4A);
    private static readonly Color Yellow = Color.FromRgb(0xF2, 0xB1, 0x0C);

    private readonly Ellipse _red = Lamp(), _yellow = Lamp(), _green = Lamp();
    private readonly TextBlock _headline = new() { FontSize = 15, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly StackPanel _reasons = new();

    public TrafficLight()
    {
        Background = Palette.Brush(Colors.White);
        BorderBrush = Palette.Brush(Color.FromRgb(0xE5, 0xE5, 0xE5));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        Padding = new Thickness(10, 8, 12, 8);

        var lamps = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 4, 6, 4) };
        lamps.Children.Add(_red);
        lamps.Children.Add(_yellow);
        lamps.Children.Add(_green);
        var housing = new Border
        {
            Background = Palette.Brush(Housing),
            CornerRadius = new CornerRadius(14),
            Child = lamps,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 14, 0),
        };

        var text = new StackPanel();
        text.Children.Add(_headline);
        text.Children.Add(_reasons);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.Children.Add(housing);
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        Child = grid;

        ToolTipService.SetShowDuration(this, int.MaxValue);
        Show(Verdict.Empty);
    }

    /// <summary>Number of reasons shown directly.</summary>
    public int VisibleReasons { get; set; } = 3;

    /// <summary>Reasons on a single line with ellipsis instead of wrapped (little space).</summary>
    public bool Compact { get; set; }

    public void Show(Verdict verdict)
    {
        _red.Fill = Palette.Brush(verdict.Light == Light.Red ? Palette.Bad : Off);
        _yellow.Fill = Palette.Brush(verdict.Light == Light.Yellow ? Yellow : Off);
        _green.Fill = Palette.Brush(verdict.Light == Light.Green ? Palette.VeryGood : Off);
        foreach (var (lamp, on) in new[] { (_red, Light.Red), (_yellow, Light.Yellow), (_green, Light.Green) })
            lamp.Effect = verdict.Light == on
                ? new System.Windows.Media.Effects.DropShadowEffect { Color = ((SolidColorBrush)lamp.Fill).Color, BlurRadius = 12, ShadowDepth = 0, Opacity = 0.9 }
                : null;

        _headline.Text = verdict.Headline;
        _reasons.Children.Clear();
        foreach (var reason in verdict.Reasons.Take(VisibleReasons))
            _reasons.Children.Add(new TextBlock
            {
                Text = "• " + reason,
                Foreground = Palette.Brush(Palette.TextSecondary),
                TextWrapping = Compact ? TextWrapping.NoWrap : TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 0),
            });
        if (verdict.Reasons.Count > VisibleReasons)
            _reasons.Children.Add(new TextBlock { Text = L.F("… {0} weitere (Maus darüber)", verdict.Reasons.Count - VisibleReasons), Foreground = Palette.Brush(Palette.TextSecondary), FontStyle = FontStyles.Italic });

        ToolTip = verdict.Reasons.Count == 0 ? null : new ToolTip
        {
            Content = new TextBlock
            {
                Text = verdict.Headline + "\n\n" + string.Join("\n", verdict.Reasons.Select(r => "• " + r))
                       + "\n\n" + L.T("Rot = Empfang gestört, Gelb = verbesserungswürdig, Grün = in Ordnung. Grundlage: MER-Reserve über der "
                                      + "Empfangsschwelle, Reserve für Regen/Gewitter und Erwartung aus Schüsselgröße, Standort und Wetter."),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 460,
            },
        };
    }

    private static Ellipse Lamp() => new() { Width = 18, Height = 18, Margin = new Thickness(3, 0, 3, 0) };
}
