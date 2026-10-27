using System.Text.Json.Serialization;
using DurLineScanner.Data;

namespace DurLineScanner.Scanning;

/// <summary>Messergebnis eines Transponders (flach, damit es sich direkt als JSON/CSV speichern lässt).</summary>
public sealed class ScanResult
{
    public int FrequencyMHz { get; set; }
    public Polarization Polarization { get; set; }
    public int SymbolRateKs { get; set; }
    public string? DvbSystem { get; set; }
    public string? Modulation { get; set; }
    public string? Fec { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>Empfangsschwelle für MER/C/N dieses Transponders.</summary>
    [JsonIgnore]
    public (double Db, string Description) Threshold => SignalLimits.Threshold(DvbSystem, Modulation, Fec);

    /// <summary>MER-Reserve über der Empfangsschwelle.</summary>
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

    /// <summary>false, wenn das Gerät den Einstellbefehl nicht bestätigt hat oder nicht antwortete.</summary>
    public bool TuneAccepted { get; set; }
    public bool Locked { get; set; }

    public int StrengthPercent { get; set; }
    public int QualityPercent { get; set; }
    public double? CnDb { get; set; }
    public double? LevelDbuV { get; set; }
    public double? MerDb { get; set; }
    public double? Ber { get; set; }

    /// <summary>Zeit bis zum Lock in Sekunden (null = kein Lock).</summary>
    public double? LockTimeSeconds { get; set; }

    public string PolarizationShort => Polarization == Polarization.Horizontal ? "H" : "V";

    public string Band => FrequencyMHz >= 11700 ? "High" : "Low";

    /// <summary>
    /// Kein Lock, obwohl die übrigen Transponder derselben Ebene mit gutem Pegel empfangen werden:
    /// dann ist der Transponder vermutlich abgeschaltet oder geändert (veraltete Liste) und kein Empfangsloch.
    /// </summary>
    public bool ProbablyInactive { get; set; }

    [JsonIgnore]
    public string LockText => !TuneAccepted ? L.T("Fehler") : Locked ? "Lock" : ProbablyInactive ? L.T("vermutl. abgeschaltet") : L.T("kein Lock");
}

public enum LocationKind
{
    /// <summary>Teilnehmerdose in der Wohnung.</summary>
    Socket,

    /// <summary>Ausgang direkt am LNB / an der Schüssel.</summary>
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

    /// <summary>Anzeigename des Messorts, z.B. "Wohnzimmer · Anschluss 2" oder "Port 1".</summary>
    public string LocationName { get; set; } = "";

    /// <summary>Raum der Sat-Dose (null bei LNB-Port und älteren Messreihen).</summary>
    public string? Room { get; set; }

    /// <summary>Nummer des Anschlusses im Raum bzw. des LNB-Ports.</summary>
    public int? Outlet { get; set; }

    /// <summary>Standort zum Zeitpunkt des Scans (Kopie – spätere Änderungen wirken nicht zurück).</summary>
    public Site? Site { get; set; }

    public Weather? Weather { get; set; }

    [JsonIgnore]
    public string WeatherText => Weather is { } w ? $"{WeatherInfo.Icon(w)} {WeatherInfo.Text(w)}" : L.T("Wetter unbekannt");

    /// <summary>Gemessene Ebenen: "alle" oder eine Ebene wie "Low V" (z.B. Quattro-LNB-Port).</summary>
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

    /// <summary>Markiert nicht gelockte Transponder als "vermutlich abgeschaltet", wenn ihre Ebene sonst gut empfangen wird.</summary>
    public void ClassifyMissingTransponders()
    {
        foreach (var group in Results.GroupBy(Plane.Of))
        {
            var locked = group.Where(r => r.Locked).ToList();
            // Drei gelockte Transponder genügen als Beleg, dass Signal auf der Ebene ankommt
            bool planeWorks = locked.Count >= 3;
            double? planeLevel = locked.Where(r => r.LevelDbuV.HasValue).Select(r => r.LevelDbuV!.Value).DefaultIfEmpty().Average();
            foreach (var r in group.Where(r => !r.Locked))
            {
                // Pegel kaum schwächer als bei gelockten Nachbarn → Signal kommt an, Transponder fehlt
                r.ProbablyInactive = planeWorks && r.TuneAccepted
                                     && r.LevelDbuV.HasValue && planeLevel.HasValue
                                     && r.LevelDbuV.Value >= planeLevel.Value - 6;
            }
        }
    }
}
