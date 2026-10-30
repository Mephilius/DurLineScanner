using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using DurLineScanner.Controls;

namespace DurLineScanner;

/// <summary>Help: user guide (in the UI language), about (version, license, sources) and license text.</summary>
public partial class HelpWindow : Window
{
    private const string GermanGuide = "Help.BENUTZERHANDBUCH.md";
    private const string EnglishGuide = "Help.USER_GUIDE.md";

    private bool _english = L.IsEnglish;

    public HelpWindow(string versionLabel, bool showAbout = false)
    {
        InitializeComponent();
        L.Translate(this);
        AboutVersionText.Text = versionLabel;
        FooterText.Text = "DUR-line Scanner " + versionLabel + " · GPL-3.0";
        LicenseBox.Text = ReadResource("Help.LICENSE");
        ShowGuide();
        if (showAbout) Tabs.SelectedItem = AboutTab;
    }

    private void ShowGuide()
    {
        GuideReader.Document = MarkdownDocument.Build(ReadResource(_english ? EnglishGuide : GermanGuide), OpenLink);
        GuideLanguageButton.Content = _english ? "Deutsche Fassung" : "English version";
    }

    private void GuideLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        _english = !_english;
        ShowGuide();
    }

    /// <summary>Links from the guide: switch to the other language version, otherwise open in the browser.</summary>
    private void OpenLink(string target)
    {
        if (target.EndsWith("USER_GUIDE.md", StringComparison.OrdinalIgnoreCase) || target.EndsWith("BENUTZERHANDBUCH.md", StringComparison.OrdinalIgnoreCase))
        {
            _english = target.EndsWith("USER_GUIDE.md", StringComparison.OrdinalIgnoreCase);
            ShowGuide();
        }
        else if (target.StartsWith("https://") || target.StartsWith("http://"))
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
    }

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        OpenLink(e.Uri.AbsoluteUri);
        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private static string ReadResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream is null) return "";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
