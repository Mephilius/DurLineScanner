using DurLineScanner.Scanning;

namespace DurLineScanner.Tests;

public class SignalLimitsTests
{
    [Theory]
    [InlineData("DVB-S2", "8PSK", "3/4", 7.9)]
    [InlineData("DVB-S2", "QPSK", "3/4", 4.0)]
    [InlineData("DVB-S", "QPSK", "5/6", 7.9)]
    [InlineData("DVB-S", "QPSK", null, 8.4)]       // no FEC: highest code rate = conservative
    [InlineData(null, null, null, 7.9)]            // no information at all
    public void Threshold(string? system, string? modulation, string? fec, double expected) =>
        Assert.Equal(expected, SignalLimits.Threshold(system, modulation, fec).Db);

    [Theory]
    [InlineData(7.0, Grade.VeryBad)]     // below the threshold 7.9
    [InlineData(8.5, Grade.Bad)]         // < +1 dB
    [InlineData(10.0, Grade.Marginal)]   // +2.1 dB
    [InlineData(12.0, Grade.Good)]       // +4.1 dB
    [InlineData(17.3, Grade.VeryGood)]   // measured at the LNB port
    public void Mer_8psk34(double mer, Grade expected) =>
        Assert.Equal(expected, SignalLimits.MerScale(7.9, "").Evaluate(mer));

    [Theory]
    [InlineData(38, Grade.VeryBad)]
    [InlineData(45, Grade.Bad)]
    [InlineData(50, Grade.Marginal)]
    [InlineData(55, Grade.Good)]
    [InlineData(61.4, Grade.VeryGood)]
    [InlineData(75, Grade.Marginal)]
    [InlineData(79, Grade.Bad)]
    [InlineData(85, Grade.VeryBad)]
    public void Level(double level, Grade expected) =>
        Assert.Equal(expected, SignalLimits.Level.Evaluate(level));

    [Fact]
    public void Ber_ScaleIsInverted()
    {
        Assert.Equal(Grade.VeryGood, SignalLimits.BerExponent.Evaluate(SignalLimits.BerToExponent(3.6e-5)));
        Assert.Equal(Grade.Bad, SignalLimits.BerExponent.Evaluate(SignalLimits.BerToExponent(5e-2)));
        Assert.Equal("über 1E-1", SignalLimits.BerExponent.RangeText(SignalLimits.BerExponent.Bands[0]));
        Assert.Equal("unter 1E-4", SignalLimits.BerExponent.RangeText(SignalLimits.BerExponent.Bands[^1]));
    }
}
