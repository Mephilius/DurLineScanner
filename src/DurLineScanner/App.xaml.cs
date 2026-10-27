using System.Windows;
using DurLineScanner.Data;
using DurLineScanner.Localization;

namespace DurLineScanner;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Sprache vor dem ersten Fenster festlegen; "--lang=en" bzw. "--lang=de" gilt nur für diesen Start
        var language = e.Args.FirstOrDefault(a => a.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))?[7..].ToLowerInvariant() switch
        {
            "en" => AppLanguage.English,
            "de" => AppLanguage.German,
            _ => AppSettings.Load().Language,
        };
        L.Init(language);
        base.OnStartup(e);
    }
}
