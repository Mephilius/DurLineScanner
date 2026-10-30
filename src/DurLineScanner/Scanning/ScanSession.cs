using System.Text.Json.Serialization;
using DurLineScanner.Data;

namespace DurLineScanner.Scanning;

/// <summary>Measurement result of one transponder (flat, so it can be saved directly as JSON/CSV).</summary>
public sealed class ScanResult
{
    public int FrequencyMHz { get; set; }
    public Polarization Polarization { get; set; }
    public int SymbolRateKs { get; set; }
    public string? DvbSystem { get; set; }
    public string? Modulation { get; set; }
    public string? Fec { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>Reception threshold for MER/C/N of this transponder.</summary>
    [JsonIgnore]
    public (double Db, string Description) Threshold => SignalLimits.Threshold(DvbSystem, Modulation, Fec);

    /// <summary>MER margin above the reception threshold.</summary>
    [JsonIgnore]
    public double? MerMargin => Locked && MerDb is { } mer ? Math.Round(mer - Threshold.Db, 1) : null;

    [JsonIgnore]
    public Grade MerGrade => Locked ? SignalLimits.MerScale(Threshold.Db, "").Evaluate(MerDb) ?? Grade.VeryBad : Grade.VeryBad;

    [JsonIgnore]
    public Grade? LevelGrade => SignalLimits.Level.Evaluate(LevelDbuV);

    [JsonIgnore]
    public Grade? QualityGrade => TuneAccepted ? SignalLimits.Quality.Evaluate(QualityPercent) : null;

    [JsonIgnore]
    public Grade? CnGrade => Locked ? SignalLimits.CnScale(Threshold.Db, "").Evaluate(CnDb) : Grade.VeryBad;

    [JsonIgnore]
    public Grade? StrengthGrade => TuneAccepted ? SignalLimits.Strength.Evaluate(StrengthPercent) : null;

    [JsonIgnore]
    public string ModulationText => string.Join(" ", new[] { DvbSystem, Modulation, Fec }.Where(s => s is not null));

    /// <summary>false if the device did not acknowledge the tune command or did not respond.</summary>
    public bool TuneAccepted { get; set; }
    public bool Locked { get; set; }

    public int StrengthPercent { get; set; }
    public int QualityPercent { get; set; }
    public double? CnDb { get; set; }
    public double? LevelDbuV { get; set; }
    public double? MerDb { get; set; }
    public double? Ber { get; set; }

    /// <summary>Time to lock in seconds (null = no lock).</summary>
    public double? LockTimeSeconds { get; set; }

    public string PolarizationShort => Polarization == Polarization.Horizontal ? "H" : "V";

    public string Band => FrequencyMHz >= 11700 ? "High" : "Low";

    /// <summary>
    /// No lock although the other transponders of the same plane are received with good level:
    /// then the transponder is probably switched off or changed (outdated list) rather than a reception hole.
    /// </summary>
    public bool ProbablyInactive { get; set; }

    [JsonIgnore]
    public string LockText => !TuneAccepted ? L.T("Fehler") : Locked ? "Lock" : ProbablyInactive ? L.T("vermutl. abgeschaltet") : L.T("kein Lock");
}

public enum LocationKind
{
    /// <summary>Subscriber wall outlet in the apartment.</summary>
    Socket,

    /// <summary>Output directly at the LNB / the dish.</summary>
    LnbPort,
}

public sealed class ScanSession
{
    public string SatelliteName { get; set; } = "";
    public double SatelliteLongitude { get; set; }
    public DateTime Started { get; set; }
    public DateTime? Finished { get; set; }
    public string DeviceModel { get; set; } = "";
    public string Note { get; set; } = "";

    public LocationKind LocationKind { get; set; }

    /// <summary>Display name of the measurement location, e.g. "Wohnzimmer · Anschluss 2" or "Port 1".</summary>
    public string LocationName { get; set; } = "";

    /// <summary>Room of the sat wall outlet (null for LNB port and older measurement series).</summary>
    public string? Room { get; set; }

    /// <summary>Number of the outlet in the room, or of the LNB port.</summary>
    public int? Outlet { get; set; }

    /// <summary>Site at the time of the scan (copy – later changes do not propagate back).</summary>
    public Site? Site { get; set; }

    public Weather? Weather { get; set; }

    [JsonIgnore]
    public string WeatherText => Weather is { } w ? $"{WeatherInfo.Icon(w)} {WeatherInfo.Text(w)}" : L.T("Wetter unbekannt");

    /// <summary>Measured planes: "alle" (all) or a single plane like "Low V" (e.g. quattro LNB port).</summary>
    public string Planes { get; set; } = "alle";

    public List<ScanResult> Results { get; set; } = [];

    public int LockedCount => Results.Count(r => r.Locked);

    public int HoleCount => Results.Count(r => !r.Locked && !r.ProbablyInactive);

    [JsonIgnore]
    public string LocationLabel =>
        L.F(LocationKind == LocationKind.Socket ? "Dose {0}" : "LNB-Port {0}",
            string.IsNullOrWhiteSpace(LocationName) ? "?" : LocationName)
        + (Planes == "alle" ? "" : $" [{Planes}]");

    [JsonIgnore]
    public string DisplayName => L.F("{0:dd.MM.yyyy HH:mm}  {1}\n{2}  ({3}/{4} Lock, {5} Löcher)",
                                     Started, LocationLabel, SatelliteName, LockedCount, Results.Count, HoleCount)
                                 + (string.IsNullOrWhiteSpace(Note) ? "" : $"\n{Note}");

    /// <summary>Marks unlocked transponders as "probably switched off" when their plane is otherwise received well.</summary>
    public void ClassifyMissingTransponders()
    {
        foreach (var group in Results.GroupBy(Plane.Of))
        {
            var locked = group.Where(r => r.Locked).ToList();
            // Three locked transponders are enough evidence that signal arrives on the plane
            bool planeWorks = locked.Count >= 3;
            double? planeLevel = locked.Where(r => r.LevelDbuV.HasValue).Select(r => r.LevelDbuV!.Value).DefaultIfEmpty().Average();
            foreach (var r in group.Where(r => !r.Locked))
            {
                // Level barely weaker than locked neighbours → signal arrives, transponder is missing
                r.ProbablyInactive = planeWorks && r.TuneAccepted
                                     && r.LevelDbuV.HasValue && planeLevel.HasValue
                                     && r.LevelDbuV.Value >= planeLevel.Value - 6;
            }
        }
    }
}
