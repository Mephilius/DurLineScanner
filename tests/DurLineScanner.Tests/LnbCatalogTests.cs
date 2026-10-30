using System.Globalization;
using DurLineScanner.Data;

namespace DurLineScanner.Tests;

public class LnbCatalogTests
{
    [Fact]
    public void Catalog_NoDuplicatesAndPlausibleValues()
    {
        Assert.Equal(LnbCatalog.All.Count, LnbCatalog.All.Select(m => (m.Manufacturer, m.Model)).Distinct().Count());
        Assert.All(LnbCatalog.All, m =>
        {
            Assert.NotEqual(LnbType.Unknown, m.Type);
            if (m.Gain is { } gain) Assert.InRange(gain, 40, 75);
            if (m.GainMin is { } min && m.GainMax is { } max) Assert.True(min <= max);
            if (m.NoiseFigure.Length > 0)
            {
                var number = m.NoiseFigure.Replace("max.", "").Replace("<", "").Trim();
                Assert.InRange(double.Parse(number, CultureInfo.InvariantCulture), 0.05, 1.5);
            }
        });
    }

    [Theory]
    [InlineData("Inverto", "IDLT-QTL412-ULTRA-OPN", 65)] // 60–70 → middle
    [InlineData("Wisi", "OC 01 E", 55)]                   // at least 55
    [InlineData("Polytron", "OSP-AP 40", 60)]             // at most 60
    public void Gain_MiddleOrSingleValue(string manufacturer, string model, double expected) =>
        Assert.Equal(expected, LnbCatalog.Find(manufacturer, model)!.Gain);
}
