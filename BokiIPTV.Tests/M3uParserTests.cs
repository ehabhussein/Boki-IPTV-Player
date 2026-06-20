using BokiIPTV.Core.Playlist;
using Xunit;

public class M3uParserTests
{
    private const string Sample =
        "#EXTM3U\n" +
        "#EXTINF:-1 tvg-id=\"bbc1\" tvg-name=\"BBC One\" tvg-logo=\"http://logo/bbc.png\" group-title=\"UK\",BBC One HD\n" +
        "http://host:8080/live/u/p/101.ts\n" +
        "\n" +
        "#EXTINF:-1 tvg-logo=\"\" ,Random Movie\n" +
        "http://host:8080/movie/u/p/55.mp4\n";

    [Fact]
    public void Parses_name_logo_group_and_url()
    {
        var list = M3uParser.Parse(Sample);
        Assert.Equal(2, list.Count);

        var first = list[0];
        Assert.Equal("BBC One HD", first.Name);          // display name (after comma) wins over tvg-name
        Assert.Equal("http://logo/bbc.png", first.Logo);
        Assert.Equal("UK", first.Group);
        Assert.Equal("http://host:8080/live/u/p/101.ts", first.Url);
    }

    [Fact]
    public void Missing_group_defaults_to_uncategorized()
        => Assert.Equal("Uncategorized", M3uParser.Parse(Sample)[1].Group);

    [Fact]
    public void Empty_or_null_content_returns_empty()
    {
        Assert.Empty(M3uParser.Parse(""));
        Assert.Empty(M3uParser.Parse(null!));
    }

    [Fact]
    public void Skips_entries_without_a_following_url()
    {
        var list = M3uParser.Parse("#EXTM3U\n#EXTINF:-1,Dangling\n");
        Assert.Empty(list);
    }
}
