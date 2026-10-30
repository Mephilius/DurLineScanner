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
    /// Typical extra attenuation in Ku band at 30° elevation in dB. Rough guideline values (ITU-R P.618/P.840):
    /// clouds a few tenths of a dB, rain 1–2 dB, wet snow on dish and LNB cover 2–3 dB,
    /// heavy rain in thunderstorms 5 dB and more.
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

    /// <summary>Weather where attenuation from precipitation is already included in the measurement.</summary>
    public static bool IsWet(Weather w) => w is Weather.Rain or Weather.Snow or Weather.Thunderstorm;
}

/// <summary>Selection item in the UI.</summary>
public sealed record WeatherOption(Weather Weather)
{
    public override string ToString() => $"{WeatherInfo.Icon(Weather)}  {WeatherInfo.Text(Weather)}";
}
