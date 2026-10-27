using System.Diagnostics;
using DurLineScanner.Data;
using DurLineScanner.Device;

namespace DurLineScanner.Scanning;

public sealed class ScanOptions
{
    /// <summary>Wartezeit nach dem Einstellen, bevor die erste Abfrage kommt.</summary>
    public TimeSpan SettleTime { get; set; } = TimeSpan.FromMilliseconds(800);

    /// <summary>Maximale Zeit bis zum Lock; danach gilt der Transponder als nicht empfangbar.</summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Anzahl gelockter Messungen, die gemittelt werden.</summary>
    public int LockedSamples { get; set; } = 2;
}

/// <summary>Fährt eine Transponderliste ab und misst jeden Transponder.</summary>
public sealed class Scanner(Sf4000Client client, ScanOptions options)
{
    public async Task<ScanResult> MeasureAsync(Transponder tp, Satellite satellite, CancellationToken ct)
    {
        var result = new ScanResult
        {
            FrequencyMHz = tp.FrequencyMHz,
            Polarization = tp.Polarization,
            SymbolRateKs = tp.SymbolRateKs,
            DvbSystem = tp.DvbSystem,
            Modulation = tp.Modulation,
            Fec = tp.Fec,
            Timestamp = DateTime.Now,
        };

        try
        {
            result.TuneAccepted = await client.TuneAsync(tp, satellite, ct);
        }
        catch (TimeoutException)
        {
            result.TuneAccepted = false;
        }
        if (!result.TuneAccepted) return result;

        var stopwatch = Stopwatch.StartNew();
        await Task.Delay(options.SettleTime, ct);

        var locked = new List<StatusReading>();
        StatusReading? last = null;
        while (stopwatch.Elapsed < options.LockTimeout || locked.Count > 0)
        {
            StatusReading reading;
            try
            {
                reading = await client.GetStatusAsync(ct);
            }
            catch (TimeoutException)
            {
                continue;
            }
            last = reading;
            if (reading.IsLocked)
            {
                result.LockTimeSeconds ??= stopwatch.Elapsed.TotalSeconds;
                locked.Add(reading);
                if (locked.Count >= options.LockedSamples) break;
            }
            else if (locked.Count > 0)
            {
                // Lock wieder verloren: instabiler Empfang, bisherige Werte reichen
                break;
            }
        }

        var samples = locked.Count > 0 ? locked : last is null ? [] : [last];
        if (samples.Count == 0) return result;

        result.Locked = locked.Count > 0;
        result.StrengthPercent = (int)Math.Round(samples.Average(s => s.StrengthPercent));
        result.QualityPercent = (int)Math.Round(samples.Average(s => s.Quality));
        result.CnDb = Average(samples, s => s.CnDb);
        result.LevelDbuV = Average(samples, s => s.LevelDbuV);
        result.MerDb = Average(samples, s => s.MerDb);
        result.Ber = samples.Max(s => s.Ber);
        return result;
    }

    public async Task RunAsync(
        ScanSession session,
        Satellite satellite,
        IReadOnlyList<Transponder> transponders,
        IProgress<(int Done, int Total, ScanResult Result)> progress,
        CancellationToken ct)
    {
        await client.SetDetailModeAsync(true, ct);
        try
        {
            for (int i = 0; i < transponders.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var result = await MeasureAsync(transponders[i], satellite, ct);
                session.Results.Add(result);
                session.ClassifyMissingTransponders();
                progress.Report((i + 1, transponders.Count, result));
            }
        }
        finally
        {
            session.Finished = DateTime.Now;
            try
            {
                await client.SetDetailModeAsync(false, CancellationToken.None);
            }
            catch (Exception ex) when (ex is TimeoutException or InvalidOperationException)
            {
                // Detailmodus bleibt an; stört beim nächsten Verbinden nicht
            }
        }
    }

    private static double? Average(List<StatusReading> samples, Func<StatusReading, double?> selector)
    {
        var values = samples.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return values.Count > 0 ? Math.Round(values.Average(), 1) : null;
    }
}
