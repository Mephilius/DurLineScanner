using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace DurLineScanner.Localization;

public enum AppLanguage
{
    /// <summary>Windows language: German on German Windows, otherwise English.</summary>
    System,
    German,
    English,
}

/// <summary>
/// Bilingual UI (German/English). The German text doubles as the key; the English
/// texts live in the Strings.*.cs files. If a translation is missing, the German text is kept.
/// The language is set at startup; a change takes effect after restart.
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
        // Numbers and dates in bindings (StringFormat) in the format of the UI language
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    /// <summary>Text in the UI language.</summary>
    public static string T(string german) => IsEnglish && En.TryGetValue(german, out var english) ? english : german;

    /// <summary>Formatted text; placeholders as in string.Format, e.g. L.F("{0} von {1} mit Lock", a, b).</summary>
    public static string F(string germanFormat, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(germanFormat), args);

    /// <summary>
    /// Translates the fixed texts of a window (Text, Content, Header, ToolTip, column headers) so the
    /// XAML files stay readable. Call right after InitializeComponent.
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
                // Only set if a translation exists – otherwise hyperlinks in the TextBlock would be lost
                case TextBlock tb when !tb.IsBindingSet(TextBlock.TextProperty) && T(tb.Text) is var t && t != tb.Text:
                    tb.Text = t;
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

    /// <summary>Texts from analysis, data and controls (Strings.Core.cs).</summary>
    static partial void AddCore(Dictionary<string, string> d);

    /// <summary>Window texts (Strings.Ui.cs).</summary>
    static partial void AddUi(Dictionary<string, string> d);
}

/// <summary>Translated text in XAML, e.g. in templates: <c>ToolTip="{l:T 'Zum Vergleich einblenden'}"</c>.</summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TExtension(string text) : MarkupExtension
{
    public string Text { get; set; } = text;

    public override object ProvideValue(IServiceProvider serviceProvider) => L.T(Text);
}
