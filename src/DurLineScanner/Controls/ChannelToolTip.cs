using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DurLineScanner.Data;

namespace DurLineScanner.Controls;

/// <summary>Tooltip mit den Programmen eines Transponders (aus der importierten lamedb).</summary>
public static class ChannelToolTip
{
    private const int MaxShown = 45;

    /// <summary>Orbitposition (Zehntelgrad) der angezeigten Messreihe.</summary>
    public static int PositionTenths { get; set; } = 192;

    public static FrameworkElement Build(int frequencyMHz, Polarization pol, int symbolRateKs, string? modulation)
    {
        var panel = new StackPanel { MaxWidth = 460, Margin = new Thickness(4) };
        panel.Children.Add(new TextBlock
        {
            Text = $"{frequencyMHz} {(pol == Polarization.Horizontal ? "H" : "V")} {symbolRateKs}" + (string.IsNullOrEmpty(modulation) ? "" : $"  ·  {modulation}"),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
        });

        var list = ChannelList.Current;
        if (list is null)
        {
            panel.Children.Add(Hint(L.T("Keine Senderliste geladen. Über „Senderliste importieren …“ die lamedb eines Enigma2-Receivers " +
                                        "oder ein Senderlisten-Paket (ZIP) laden.")));
            return panel;
        }

        var channels = list.Get(PositionTenths, frequencyMHz, pol);
        if (channels.Count == 0)
        {
            panel.Children.Add(Hint(L.T("In der Senderliste sind für diesen Transponder keine Programme eingetragen.")));
            return panel;
        }

        var tv = channels.Where(c => c.Kind is ChannelKind.Tv or ChannelKind.TvHd or ChannelKind.TvUhd).ToList();
        var radio = channels.Where(c => c.Kind == ChannelKind.Radio).ToList();
        int data = channels.Count(c => c.Kind == ChannelKind.Data);
        panel.Children.Add(Hint($"{tv.Count} TV · {radio.Count} Radio" + (data > 0 ? " · " + L.F("{0} Datendienste", data) : "")));

        int shown = 0;
        foreach (var (title, group) in new[] { (L.T("Fernsehen"), tv), ("Radio", radio) })
        {
            if (group.Count == 0 || shown >= MaxShown) continue;
            panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
            foreach (var c in group.Take(MaxShown - shown))
            {
                var row = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };
                var badge = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 0, 4, 0),
                    Margin = new Thickness(0, 0, 6, 0),
                    MinWidth = 38,
                    Background = Palette.Brush(c.Kind switch
                    {
                        ChannelKind.TvUhd => Color.FromRgb(0x6B, 0x69, 0xD6),
                        ChannelKind.TvHd => Palette.Accent,
                        ChannelKind.Radio => Color.FromRgb(0x00, 0x8B, 0x8B),
                        _ => Color.FromRgb(0x8A, 0x8A, 0x8A),
                    }),
                    Child = new TextBlock { Text = c.KindText, Foreground = Brushes.White, FontSize = 10, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center },
                };
                row.Children.Add(badge);
                if (c.Encrypted)
                {
                    var pay = new TextBlock { Text = L.T("verschlüsselt"), FontSize = 11, Foreground = Palette.Brush(Palette.TextSecondary), Margin = new Thickness(8, 0, 0, 0) };
                    DockPanel.SetDock(pay, Dock.Right);
                    row.Children.Add(pay);
                }
                row.Children.Add(new TextBlock
                {
                    Text = c.Provider.Length > 0 && !c.Name.Contains(c.Provider, StringComparison.OrdinalIgnoreCase) ? $"{c.Name}  ({c.Provider})" : c.Name,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                });
                panel.Children.Add(row);
                shown++;
            }
        }
        if (channels.Count - data > shown)
            panel.Children.Add(Hint(L.F("… und {0} weitere", channels.Count - data - shown)));

        panel.Children.Add(new TextBlock
        {
            Text = L.F("Quelle: {0}", list.Description),
            FontSize = 11,
            Foreground = Palette.Brush(Palette.TextSecondary),
            Margin = new Thickness(0, 8, 0, 0),
        });
        return panel;
    }

    private static TextBlock Hint(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Palette.Brush(Palette.TextSecondary),
        Margin = new Thickness(0, 4, 0, 0),
    };
}
