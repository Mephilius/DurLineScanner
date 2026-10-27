using DurLineScanner.Data;

namespace DurLineScanner.Tests;

public class ChannelListTests
{
    // Aufbau wie eine Enigma2-lamedb (Format 4); Werte angelehnt an Astra 19,2° O
    private const string LamedbV4 = """
        eDVB services /4/
        transponders
        00c00000:0437:0001
        	s 11494000:22000000:0:2:192:2:0
        /
        00c00000:03fb:0001
        	s 10744000:22000000:0:4:192:2:0
        /
        00c00000:0001:0002
        	s 12188000:27500000:1:2:192:2:0:1:2:0:2
        /
        end
        services
        283d:00c00000:0437:0001:25:0
        Das Erste HD
        p:ARD
        2b66:00c00000:0437:0001:25:0
        ZDF HD
        p:ZDF,C:0000
        0060:00c00000:0437:0001:2:0
        Bayern 3
        p:ARD
        0700:00c00000:03fb:0001:1:0
        Sky Cinema
        p:SKY,C:098c,C:1833
        end
        """;

    private const string LamedbV5 = """
        eDVB services /5/
        # Kommentar
        t:00c00000:0437:0001,s:11494000:22000000:0:2:192:2:0
        s:283d:00c00000:0437:0001:25:0,"Das Erste HD",p:ARD
        s:2b66:c00000:437:1:25:0,"ZDF HD",p:ZDF
        """;

    [Fact]
    public void ParseV4_GroupsServicesByTransponder()
    {
        var list = ChannelList.Parse(LamedbV4, new DateTime(2026, 10, 27));
        Assert.Equal("lamedb vom 27.10.2026 (4 Sender)", list.Description);

        var tp = list.Get(192, 11494, Polarization.Horizontal);
        Assert.Equal(["Das Erste HD", "ZDF HD", "Bayern 3"], tp.Select(c => c.Name));
        Assert.Equal(ChannelKind.TvHd, tp[0].Kind);
        Assert.Equal(ChannelKind.Radio, tp[2].Kind);
        Assert.True(tp[1].Encrypted);

        var sky = Assert.Single(list.Get(192, 10744, Polarization.Horizontal));
        Assert.True(sky.Encrypted);
        Assert.Empty(list.Get(192, 11494, Polarization.Vertical));
        Assert.Empty(list.Get(130, 11494, Polarization.Horizontal));
    }

    [Fact]
    public void ParseV5_KeysWithoutLeadingZerosMatch()
    {
        var list = ChannelList.Parse(LamedbV5, DateTime.Today);
        Assert.Equal(["Das Erste HD", "ZDF HD"], list.Get(192, 11494, Polarization.Horizontal).Select(c => c.Name));
    }

    [Fact]
    public void AllAt_ListsChannelsWithTransponder()
    {
        var all = ChannelList.Parse(LamedbV4, DateTime.Today).AllAt(192);
        var zdf = all.Single(c => c.Channel.Name == "ZDF HD");
        Assert.Equal(11494, zdf.FrequencyMHz);
        Assert.Equal(Polarization.Horizontal, zdf.Pol);
        Assert.Equal("Bayern 3", all[0].Channel.Name); // alphabetisch
    }

    [Fact]
    public void Parse_RejectsOtherFiles() =>
        Assert.Throws<InvalidDataException>(() => ChannelList.Parse("<satellites/>", DateTime.Today));

    [Fact]
    public void ToLamedb5_RoundTripsWithSource()
    {
        var text = ChannelList.ToLamedb5(
        [
            (192, 11494, Polarization.Horizontal, 22000, new Channel("ZDF HD", "ZDF", ChannelKind.TvHd, false)),
            (192, 11494, Polarization.Horizontal, 22000, new Channel("Bayern 3", "ARD", ChannelKind.Radio, false)),
            (-8, 10722, Polarization.Vertical, 27500, new Channel("Sky \"Test\"", "SKY", ChannelKind.Tv, true)),
        ]);
        var lines = text.Split('\n').ToList();
        lines.Insert(1, "# Quelle: TVHeadend server");
        var list = ChannelList.Parse(string.Join("\n", lines), new DateTime(2026, 10, 27));

        Assert.Equal("TVHeadend server vom 27.10.2026 (3 Sender)", list.Description);
        Assert.Equal(["ZDF HD", "Bayern 3"], list.Get(192, 11494, Polarization.Horizontal).Select(c => c.Name));
        var sky = Assert.Single(list.Get(-8, 10722, Polarization.Vertical));
        Assert.True(sky.Encrypted);
        Assert.Equal(ChannelKind.Tv, sky.Kind);
    }

    [Theory]
    [InlineData("19.2E", 192)]
    [InlineData("Astra 19,2° Ost", 192)]
    [InlineData("0.8W", -8)]
    [InlineData("Hotbird 13E", 130)]
    [InlineData("DVB-T", null)]
    public void ParseOrbital(string text, int? expected) =>
        Assert.Equal(expected, ReceiverChannelSource.ParseOrbital(text));

    [Theory]
    [InlineData(ReceiverKind.Tvheadend, "192.168.1.30", false, null, "http://192.168.1.30:9981/")]
    [InlineData(ReceiverKind.Tvheadend, "tv.example.de", true, null, "https://tv.example.de/")]
    [InlineData(ReceiverKind.Tvheadend, "tvh", false, 9982, "http://tvh:9982/")]
    [InlineData(ReceiverKind.Tvheadend, "server/tvheadend", true, 8443, "https://server:8443/tvheadend/")]
    [InlineData(ReceiverKind.Enigma2, "vuplus.local", false, null, "http://vuplus.local/")]
    [InlineData(ReceiverKind.Enigma2, "box.example.de", true, null, "https://box.example.de/")]
    [InlineData(ReceiverKind.Enigma2, "192.168.1.20", false, 8080, "http://192.168.1.20:8080/")]
    public void BaseUri_FromFields(ReceiverKind kind, string address, bool https, int? port, string expected) =>
        Assert.Equal(expected, ReceiverChannelSource.BaseUri(
            new AppSettings { ReceiverKind = kind, ReceiverAddress = address, ReceiverHttps = https, ReceiverPort = port }).ToString());

    [Fact]
    public void ReceiverPassword_IsStoredEncryptedAndRestored()
    {
        var s = new AppSettings { ReceiverPassword = "geheim123" };
        Assert.DoesNotContain("geheim", s.ReceiverPasswordProtected);
        Assert.NotEmpty(s.ReceiverPasswordProtected);

        // Wie nach dem Speichern und erneuten Laden: nur der verschlüsselte Wert wird übernommen
        var reloaded = new AppSettings { ReceiverPasswordProtected = s.ReceiverPasswordProtected };
        Assert.Equal("geheim123", reloaded.ReceiverPassword);
        Assert.Equal(9, reloaded.ReceiverPassword.Length);
    }

    [Theory]
    [InlineData("https://tv.example.de", "tv.example.de", true, null)]
    [InlineData("http://tvh:9982", "tvh", false, 9982)]
    [InlineData("tvh.local:9982", "tvh.local", false, 9982)]
    [InlineData("https://server:8443/tvheadend/", "server/tvheadend", true, 8443)]
    [InlineData("192.168.1.20", "192.168.1.20", false, null)]
    public void SplitReceiverAddress_FullAddressToFields(string input, string address, bool https, int? port)
    {
        var s = new AppSettings { ReceiverKind = ReceiverKind.Tvheadend, ReceiverAddress = input };
        s.SplitReceiverAddress();
        Assert.Equal(address, s.ReceiverAddress);
        Assert.Equal(https, s.ReceiverHttps);
        Assert.Equal(port, s.ReceiverPort);
    }
}
