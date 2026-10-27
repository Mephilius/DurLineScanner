using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DurLineScanner.Controls;

/// <summary>
/// Wandelt das Benutzerhandbuch (GitHub-Markdown) in ein FlowDocument: Überschriften, Absätze, Listen, Tabellen,
/// Zitate, Trennlinien, Code sowie fett, kursiv, Code und Links im Text. Sprungziele (#abschnitt) folgen den
/// GitHub-Regeln, damit das Inhaltsverzeichnis funktioniert.
/// </summary>
public static partial class MarkdownDocument
{
    private static readonly FontFamily Mono = new("Cascadia Mono, Consolas");
    private static readonly Brush CodeBackground = Palette.Brush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    private static readonly Brush Border = Palette.Brush(Color.FromRgb(0xD8, 0xD8, 0xD8));
    private static readonly Brush HeaderBackground = Palette.Brush(Color.FromRgb(0xF6, 0xF8, 0xFA));
    private static readonly Brush LinkBrush = Palette.Brush(Palette.Accent);

    /// <param name="markdown">Markdown-Text.</param>
    /// <param name="openLink">Wird für Links aufgerufen, die nicht auf einen Abschnitt im Dokument zeigen.</param>
    public static FlowDocument Build(string markdown, Action<string> openLink)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            PagePadding = new Thickness(28, 20, 28, 28),
            TextAlignment = TextAlignment.Left,
            ColumnWidth = double.PositiveInfinity,
        };
        var anchors = new Dictionary<string, Block>();
        var lines = markdown.Replace("\r", "").Split('\n');

        void Navigate(string target)
        {
            if (target.StartsWith('#') && anchors.TryGetValue(target[1..], out var block)) block.BringIntoView();
            else openLink(target);
        }

        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                i++;
            }
            else if (trimmed.StartsWith("```"))
            {
                var code = new List<string>();
                for (i++; i < lines.Length && !lines[i].TrimStart().StartsWith("```"); i++) code.Add(lines[i]);
                i++;
                doc.Blocks.Add(new Paragraph(new Run(string.Join("\n", code)))
                {
                    FontFamily = Mono, FontSize = 13, Background = CodeBackground, Padding = new Thickness(10, 8, 10, 8),
                });
            }
            else if (HeadingRegex().Match(trimmed) is { Success: true } heading)
            {
                int level = heading.Groups[1].Length;
                var text = heading.Groups[2].Value;
                var paragraph = new Paragraph
                {
                    FontSize = level switch { 1 => 26, 2 => 20, 3 => 16, _ => 15 },
                    FontWeight = level == 1 ? FontWeights.SemiBold : FontWeights.SemiBold,
                    Margin = new Thickness(0, level == 1 ? 0 : 22, 0, 8),
                };
                AddInlines(paragraph.Inlines, text, Navigate);
                anchors.TryAdd(Anchor(text), paragraph);
                doc.Blocks.Add(paragraph);
                i++;
            }
            else if (trimmed is "---" or "***")
            {
                doc.Blocks.Add(new BlockUIContainer(new Border
                {
                    Height = 1, Background = Border, Margin = new Thickness(0, 8, 0, 8),
                }));
                i++;
            }
            else if (trimmed.StartsWith('|'))
            {
                var rows = new List<string>();
                for (; i < lines.Length && lines[i].Trim().StartsWith('|'); i++) rows.Add(lines[i].Trim());
                doc.Blocks.Add(BuildTable(rows, Navigate));
            }
            else if (trimmed.StartsWith('>'))
            {
                var quote = new List<string>();
                for (; i < lines.Length && lines[i].Trim().StartsWith('>'); i++) quote.Add(lines[i].Trim().TrimStart('>').Trim());
                var paragraph = new Paragraph { Margin = new Thickness(0, 4, 0, 10) };
                AddInlines(paragraph.Inlines, string.Join(" ", quote), Navigate);
                doc.Blocks.Add(new Section(paragraph)
                {
                    BorderBrush = LinkBrush, BorderThickness = new Thickness(4, 0, 0, 0),
                    Background = Palette.Brush(Color.FromRgb(0xEE, 0xF4, 0xFB)), Padding = new Thickness(12, 4, 8, 0),
                    Margin = new Thickness(0, 4, 0, 12),
                });
            }
            else if (ListItemRegex().IsMatch(line))
            {
                doc.Blocks.Add(BuildList(lines, ref i, Navigate));
            }
            else
            {
                // Absatz: aufeinanderfolgende Zeilen bis zur nächsten Leerzeile oder einem anderen Block
                var text = new List<string>();
                for (; i < lines.Length; i++)
                {
                    var t = lines[i].Trim();
                    if (t.Length == 0 || t.StartsWith('#') || t.StartsWith('|') || t.StartsWith('>') || t.StartsWith("```")
                        || t is "---" or "***" || ListItemRegex().IsMatch(lines[i])) break;
                    text.Add(t);
                }
                var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
                AddInlines(paragraph.Inlines, string.Join(" ", text), Navigate);
                doc.Blocks.Add(paragraph);
            }
        }
        return doc;
    }

    /// <summary>Liste mit Fortsetzungszeilen und einer Ebene Unterpunkten.</summary>
    private static List BuildList(string[] lines, ref int i, Action<string> navigate)
    {
        var first = ListItemRegex().Match(lines[i]);
        int indent = first.Groups[1].Length;
        bool numbered = char.IsDigit(first.Groups[2].Value[0]);
        var list = new List
        {
            MarkerStyle = numbered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(24, 0, 0, 0),
        };

        while (i < lines.Length)
        {
            var match = ListItemRegex().Match(lines[i]);
            if (!match.Success || match.Groups[1].Length < indent) break;
            if (match.Groups[1].Length > indent)
            {
                // Unterpunkte an den letzten Eintrag hängen
                if (list.ListItems.LastListItem is { } last) last.Blocks.Add(BuildList(lines, ref i, navigate));
                else i++;
                continue;
            }

            var text = new List<string> { match.Groups[3].Value };
            for (i++; i < lines.Length; i++)
            {
                var next = lines[i];
                if (next.Trim().Length == 0 || ListItemRegex().IsMatch(next) || !next.StartsWith(' ')) break;
                text.Add(next.Trim());
            }
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 3) };
            AddInlines(paragraph.Inlines, string.Join(" ", text), navigate);
            list.ListItems.Add(new ListItem(paragraph));

            // Leerzeile innerhalb der Liste überspringen, wenn es danach weitergeht
            if (i + 1 < lines.Length && lines[i].Trim().Length == 0 && ListItemRegex().Match(lines[i + 1]) is { Success: true } m
                && m.Groups[1].Length >= indent)
                i++;
        }
        return list;
    }

    private static Table BuildTable(List<string> rows, Action<string> navigate)
    {
        static string[] Cells(string row) => row.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();

        var header = Cells(rows[0]);
        var table = new Table { CellSpacing = 0, BorderBrush = Border, BorderThickness = new Thickness(1, 1, 0, 0), Margin = new Thickness(0, 4, 0, 14) };
        for (int c = 0; c < header.Length; c++)
            table.Columns.Add(new TableColumn { Width = c == header.Length - 1 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        bool isHeader = true;
        foreach (var row in rows)
        {
            var cells = Cells(row);
            if (cells.All(c => c.Trim('-', ':', ' ').Length == 0)) continue; // Trennzeile |---|---|
            var tableRow = new TableRow { Background = isHeader ? HeaderBackground : null };
            for (int c = 0; c < header.Length; c++)
            {
                var paragraph = new Paragraph { Margin = new Thickness(0), FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal };
                AddInlines(paragraph.Inlines, c < cells.Length ? cells[c] : "", navigate);
                tableRow.Cells.Add(new TableCell(paragraph)
                {
                    BorderBrush = Border, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(8, 5, 8, 5),
                });
            }
            group.Rows.Add(tableRow);
            isHeader = false;
        }
        return table;
    }

    /// <summary>Fett, kursiv, Code und Links innerhalb einer Zeile.</summary>
    private static void AddInlines(InlineCollection target, string text, Action<string> navigate)
    {
        int pos = 0;
        foreach (Match m in InlineRegex().Matches(text))
        {
            if (m.Index > pos) target.Add(new Run(text[pos..m.Index]));
            if (m.Groups["code"].Success)
            {
                target.Add(new Run(m.Groups["code"].Value) { FontFamily = Mono, Background = CodeBackground, FontSize = 13 });
            }
            else if (m.Groups["bold"].Success)
            {
                var bold = new Bold();
                AddInlines(bold.Inlines, m.Groups["bold"].Value, navigate);
                target.Add(bold);
            }
            else if (m.Groups["italic"].Success)
            {
                var italic = new Italic();
                AddInlines(italic.Inlines, m.Groups["italic"].Value, navigate);
                target.Add(italic);
            }
            else
            {
                var url = m.Groups["url"].Value;
                var link = new Hyperlink { Foreground = LinkBrush, Cursor = System.Windows.Input.Cursors.Hand, ToolTip = url.StartsWith('#') ? null : url };
                AddInlines(link.Inlines, m.Groups["label"].Value, navigate);
                link.Click += (_, _) => navigate(url);
                target.Add(link);
            }
            pos = m.Index + m.Length;
        }
        if (pos < text.Length) target.Add(new Run(text[pos..]));
    }

    /// <summary>Sprungziel nach GitHub-Regeln: klein, Satzzeichen weg, Leerzeichen zu Bindestrichen.</summary>
    public static string Anchor(string heading)
    {
        var plain = InlineMarkupRegex().Replace(heading, "$1").Trim().ToLowerInvariant();
        var chars = plain.Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-' or '_').ToArray();
        return new string(chars).Replace(' ', '-');
    }

    [GeneratedRegex(@"^(#{1,6})\s+(.*)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^(\s*)([-*]|\d+\.)\s+(.*)$")]
    private static partial Regex ListItemRegex();

    [GeneratedRegex(@"`(?<code>[^`]+)`|\*\*(?<bold>.+?)\*\*|\*(?<italic>[^*\s][^*]*?)\*|\[(?<label>[^\]]+)\]\((?<url>[^)\s]+)\)")]
    private static partial Regex InlineRegex();

    [GeneratedRegex(@"[`*]|\[([^\]]+)\]\([^)]*\)")]
    private static partial Regex InlineMarkupRegex();
}
