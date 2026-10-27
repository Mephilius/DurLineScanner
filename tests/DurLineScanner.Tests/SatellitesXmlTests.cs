using DurLineScanner.Data;

namespace DurLineScanner.Tests;

public class SatellitesXmlTests
{
    // Ausschnitt aus der OpenPLi-Datei vom 27.10.2026
    private const string Sample = """
        <?xml version="1.0" encoding="UTF-8"?>
        <!--
        lyngsat grabber 1.0.5
        File created on 2026-10-27T08:33:39
        -->
        <satellites>
        	<sat name="177.0W C-band NSS 9" flags="0" position="-1771">
        		<transponder frequency="3840000" symbol_rate="27500000" polarization="2" fec_inner="3" system="0" modulation="1"/>
        	</sat>
        	<sat name="19.2E Astra 1N/1P" flags="1" position="192">
        		<transponder frequency="10729000" symbol_rate="23500000" polarization="1" fec_inner="3" system="1" modulation="2"/>
        		<transponder frequency="10788000" symbol_rate="22000000" polarization="1" fec_inner="4" system="0" modulation="1"/>
        		<transponder frequency="10714250" symbol_rate="22000000" polarization="0" fec_inner="4" system="0" modulation="1"/>
        	</sat>
        </satellites>
        """;

    [Fact]
    public void Load_ParsesTranspondersByPosition()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, Sample);
        try
        {
            var xml = SatellitesXml.Load(path);
            Assert.Equal("satellites.xml vom 27.10.2026", xml.Description);
            Assert.Equal(2, xml.SatelliteCount);

            // 19,2° als 192 – auch 193 muss noch passen (±0,2°)
            var astra = xml.GetTransponders(193)!;
            Assert.Equal(3, astra.Count);
            Assert.Equal(new Transponder(10729, Polarization.Vertical, 23500, "DVB-S2", "8PSK", "3/4"), astra[0]);
            Assert.Equal(Polarization.Horizontal, astra[2].Polarization);
            Assert.Equal(10714, astra[2].FrequencyMHz);

            // Zirkulare Polarisation wird übersprungen – NSS 9 hat dann keinen empfangbaren Transponder
            Assert.Null(xml.GetTransponders(-1770));
            var sat = Assert.Single(xml.Satellites);
            Assert.Equal(new Satellite(1920, "19.2E Astra 1N/1P", 9750, 10600, 19.2), sat);
            Assert.Null(xml.GetTransponders(130));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_RejectsOtherXml()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "<foo/>");
        try
        {
            Assert.Throws<InvalidDataException>(() => SatellitesXml.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
