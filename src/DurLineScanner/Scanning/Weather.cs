namespace DurLineScanner.Scanning;

public enum Weather
{
    Sunny,
    PartlyCloudy,
    Overcast,
    Rain,
    Snow,
    Thunderstorm,
}

public static class WeatherInfo
{
    public static readonly Weather[] All = Enum.GetValues<Weather>();

    public static string Text(Weather w) => w switch
    {
        Weather.Sunny => L.T("Sonnig"),
        Weather.PartlyCloudy => L.T("Leicht bewölkt"),
        Weather.Overcast => L.T("Geschlossene Wolkendecke"),
        Weather.Rain => L.T("Regen"),
        Weather.Snow => L.T("Schnee"),
        _ => L.T("Gewitter"),
    };

    public static string Icon(Weather w) => w switch
    {
        Weather.Sunny => "☀",
        Weather.PartlyCloudy => "⛅",
        Weather.Overcast => "☁",
        Weather.Rain => "🌧",
        Weather.Snow => "❄",
        _ => "⛈",
    };

    /// <summary>
    /// Typische Zusatzdämpfung im Ku-Band bei 30° Elevation in dB. Grobe Richtwerte (ITU-R P.618/P.840):
    /// Wolken einige Zehntel dB, Regen 1–2 dB, nasser Schnee auf Schüssel und LNB-Abdeckung 2–3 dB,
    /// Starkregen bei Gewitter 5 dB und mehr.
    /// </summary>
    public static double FadeDb(Weather w) => w switch
    {
        Weather.Sunny => 0,
        Weather.PartlyCloudy => 0.1,
        Weather.Overcast => 0.4,
        Weather.Rain => 1.5,
        Weather.Snow => 2.5,
        _ => 5,
    };

    /// <summary>Wetter, bei dem Dämpfung durch Niederschlag schon in der Messung steckt.</summary>
    public static bool IsWet(Weather w) => w is Weather.Rain or Weather.Snow or Weather.Thunderstorm;
}

/// <summary>Auswahl in der Oberfläche.</summary>
public sealed record WeatherOption(Weather Weather)
{
    public override string ToString() => $"{WeatherInfo.Icon(Weather)}  {WeatherInfo.Text(Weather)}";
}
