using DurLineScanner.Data;

namespace DurLineScanner.Scanning;

/// <summary>
/// Blickrichtung zum Satelliten und überschlägige Empfangserwartung aus Standort, Schüsselgröße und Wetter.
/// Die Erwartung ist ein Richtwert: Die Sendeleistung (EIRP) hängt von Satellit und Ausleuchtzone ab,
/// angenommen werden 51 dBW (Astra/Hotbird in Mitteleuropa).
/// </summary>
public static class LinkBudget
{
    private const double EarthRadiusKm = 6378.137;
    private const double GeoRadiusKm = 42164.2;
    private const double BoltzmannDb = -228.6;

    public const double EirpDbw = 51;
    public const double DishEfficiency = 0.65;

    /// <summary>Rauschtemperatur bei klarem Himmel (LNB ca. 0,3 dB NF, Himmel, Erdboden über die Nebenkeulen) in K.</summary>
    public const double ClearSkyNoiseK = 90;

    /// <summary>MER, die der Transponder selbst liefert (Wanderfeldröhre, Filter) – auch eine riesige Schüssel misst kaum mehr.</summary>
    public const double SatelliteMerDb = 18;

    public sealed record LookAngles(double AzimuthDeg, double ElevationDeg, double SkewDeg, double DistanceKm)
    {
        public bool Visible => ElevationDeg > 0;
    }

    public static LookAngles Look(double latitude, double longitude, double satelliteLongitude)
    {
        double phi = Rad(latitude);
        double delta = Rad(satelliteLongitude - longitude);
        double cosBeta = Math.Cos(phi) * Math.Cos(delta);
        double sinBeta = Math.Sqrt(1 - cosBeta * cosBeta);
        double elevation = Math.Atan2(cosBeta - EarthRadiusKm / GeoRadiusKm, sinBeta);
        double azimuth = Math.Atan2(Math.Sin(delta), -Math.Sin(phi) * Math.Cos(delta));
        double skew = Math.Atan2(Math.Sin(delta), Math.Tan(phi));
        double distance = Math.Sqrt(EarthRadiusKm * EarthRadiusKm + GeoRadiusKm * GeoRadiusKm - 2 * EarthRadiusKm * GeoRadiusKm * cosBeta);
        return new LookAngles((Deg(azimuth) + 360) % 360, Deg(elevation), Deg(skew), distance);
    }

    /// <summary>Wetterdämpfung am Standort: der Regenweg wird bei flacher Elevation länger.</summary>
    public static double FadeDb(Weather weather, double elevationDeg) =>
        WeatherInfo.FadeDb(weather) * Math.Clamp(Math.Sin(Rad(30)) / Math.Sin(Rad(Math.Max(elevationDeg, 5))), 0.6, 2.5);

    public sealed record Expectation(double CnDb, double MerDb, double LevelDbuV, double FadeDb, double ElevationDeg);

    /// <summary>
    /// Erwartete C/N, MER und Pegel am LNB-Ausgang für einen Transponder. Ohne Koordinaten wird
    /// Mitteleuropa angenommen (38 600 km, 30° Elevation).
    /// </summary>
    public static Expectation Expect(Site site, double satelliteLongitude, int frequencyMHz, int symbolRateKs, Weather weather)
    {
        var look = site.HasCoordinates ? Look(site.Latitude!.Value, site.Longitude!.Value, satelliteLongitude) : null;
        double distanceKm = look?.DistanceKm ?? 38600;
        double elevation = look?.ElevationDeg ?? 30;
        double fade = FadeDb(weather, elevation);

        double lambda = 299.792458 / frequencyMHz; // m
        double gain = 10 * Math.Log10(DishEfficiency * Math.Pow(Math.PI * site.DishCm / 100 / lambda, 2));
        double pathLoss = 20 * Math.Log10(4 * Math.PI * distanceKm * 1000 / lambda);
        // Regen dämpft das Signal und strahlt zugleich Rauschen ein
        double noiseK = ClearSkyNoiseK + 275 * (1 - Math.Pow(10, -fade / 10));
        double carrierDbw = EirpDbw - fade - pathLoss + gain;
        double cn = carrierDbw - BoltzmannDb - 10 * Math.Log10(noiseK) - 10 * Math.Log10(symbolRateKs * 1000.0);

        // Messbare MER: Funkfeld und Transponder addieren sich
        double mer = -10 * Math.Log10(Math.Pow(10, -cn / 10) + Math.Pow(10, -SatelliteMerDb / 10));
        double levelDbuV = carrierDbw + 30 + site.LnbGainDb + 108.75; // dBm an 75 Ω → dBµV
        return new Expectation(Math.Round(cn, 1), Math.Round(mer, 1), Math.Round(levelDbuV, 1), Math.Round(fade, 1), elevation);
    }

    /// <summary>Wie viel dB C/N ein Wetter gegenüber dem aktuellen zusätzlich kostet.</summary>
    public static double ExtraLossDb(Site site, double satelliteLongitude, int frequencyMHz, int symbolRateKs, Weather now, Weather worse)
    {
        var a = Expect(site, satelliteLongitude, frequencyMHz, symbolRateKs, now);
        var b = Expect(site, satelliteLongitude, frequencyMHz, symbolRateKs, worse);
        return Math.Max(0, Math.Round(a.CnDb - b.CnDb, 1));
    }

    private static double Rad(double deg) => deg * Math.PI / 180;

    private static double Deg(double rad) => rad * 180 / Math.PI;
}
