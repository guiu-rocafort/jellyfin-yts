using Jellyfin.Plugin.YtsTorrents.Yts;

namespace Jellyfin.Plugin.YtsTorrents.Tests;

public class MagnetBuilderTests
{
    [Fact]
    public void Build_ProducesExpectedMagnetUri()
    {
        var hash = "78FA5DA053BC45C9B72A29704DE86353600F7C0F";
        var magnet = MagnetBuilder.Build(hash, "Sintel (2010)");

        Assert.StartsWith("magnet:?xt=urn:btih:" + hash, magnet);
        Assert.Contains("&dn=" + System.Uri.EscapeDataString("Sintel (2010)"), magnet);
        foreach (var tracker in MagnetBuilder.Trackers)
        {
            Assert.Contains("&tr=" + System.Uri.EscapeDataString(tracker), magnet);
        }
    }

    [Theory]
    [InlineData("78FA5DA053BC45C9B72A29704DE86353600F7C0F", true)]
    [InlineData("78fa5da053bc45c9b72a29704de86353600f7c0f", true)]
    [InlineData("too-short", false)]
    [InlineData("", false)]
    [InlineData("78FA5DA053BC45C9B72A29704DE86353600F7C0FF", false)]
    public void IsValidHash_ValidatesFortyCharHex(string hash, bool expected)
    {
        Assert.Equal(expected, MagnetBuilder.IsValidHash(hash));
    }

    [Fact]
    public void Build_RejectsInvalidHash()
    {
        Assert.Throws<System.ArgumentException>(() => MagnetBuilder.Build("not-a-hash", "Title"));
    }
}
