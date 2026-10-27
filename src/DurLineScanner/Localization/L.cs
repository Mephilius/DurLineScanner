using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace DurLineScanner.Localization;

public enum AppLanguage
{
    /// <summary>Sprache von Windows: Deutsch bei deutschem Windows, sonst Englisch.</summary>
    System,
    German,
    English,
}

/// <summary>
/// Zweisprachige Oberfläche (Deutsch/Englisch). Der deutsche Text ist zugleich der Schlüssel; die englischen
/// Texte stehen in den Strings.*.cs-Dateien. Fehlt eine Übersetzung, bleibt der deutsche Text stehen.
/// Die Sprache wird beim Start festgelegt, ein Wechsel wirkt nach dem Neustart.
/// </summary>
public static partial class L
{
    private static readonly Dictionary<string, string> En = Build();

    public static bool IsEnglish { get; private set; }

    public static void Init(AppLanguage language)
    {
        IsEnglish = language switch
        {
            AppLanguage.German => false,
            AppLanguage.English => true,
            _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "de",
        };
        var culture = IsEnglish ? CultureInfo.GetCultureInfo("en-GB") : CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
        // Zahlen und Datumsangaben in Bindungen (StringFormat) im Format der Oberflächensprache
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    /// <summary>Text in der Oberflächensprache.</summary>
    public static string T(string german) => IsEnglish && En.TryGetValue(german, out var english) ? english : german;

    /// <summary>Formatierter Text; Platzhalter wie bei string.Format, z.B. L.F("{0} von {1} mit Lock", a, b).</summary>
    public static string F(string germanFormat, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(germanFormat), args);

    /// <summary>
    /// Übersetzt die festen Texte eines Fensters (Text, Content, Header, ToolTip, Spaltenköpfe), damit die
    /// XAML-Dateien lesbar bleiben. Aufruf direkt nach InitializeComponent.
    /// </summary>
    public static void Translate(DependencyObject root)
    {
        if (!IsEnglish) return;
        Visit(root);

        static void Visit(DependencyObject o)
        {
            switch (o)
            {
                case Window w:
                    w.Title = T(w.Title);
                    break;
                case TextBlock tb when !tb.IsBindingSet(TextBlock.TextProperty):
                    tb.Text = T(tb.Text);
                    break;
                case HeaderedContentControl { Header: string header } hc:
                    hc.Header = T(header);
                    break;
                case DataGrid grid:
                    foreach (var column in grid.Columns)
                        if (column.Header is string h) column.Header = T(h);
                    break;
            }
            if (o is ContentControl { Content: string content } cc && o is not Window)
                cc.Content = T(content);
            if (o is FrameworkElement { ToolTip: string tip } fe)
                fe.ToolTip = T(tip);

            foreach (var child in LogicalTreeHelper.GetChildren(o).OfType<DependencyObject>())
                Visit(child);
        }
    }

    private static bool IsBindingSet(this DependencyObject o, DependencyProperty p) =>
        System.Windows.Data.BindingOperations.IsDataBound(o, p);

    private static Dictionary<string, string> Build()
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        AddCore(d);
        AddUi(d);
        return d;
    }

    /// <summary>Texte aus Auswertung, Daten und Steuerelementen (Strings.Core.cs).</summary>
    static partial void AddCore(Dictionary<string, string> d);

    /// <summary>Texte der Fenster (Strings.Ui.cs).</summary>
    static partial void AddUi(Dictionary<string, string> d);
}

/// <summary>Übersetzter Text in XAML, z.B. in Vorlagen: <c>ToolTip="{l:T 'Zum Vergleich einblenden'}"</c>.</summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TExtension(string text) : MarkupExtension
{
    public string Text { get; set; } = text;

    public override object ProvideValue(IServiceProvider serviceProvider) => L.T(Text);
}
