using BokiIPTV.Core.Xtream;
using Xunit;

public class StreamUrlBuilderTests
{
    private static readonly XtreamCredentials Cred = new("http://egysatv.com:8080", "ehussein", "975284");

    [Fact]
    public void Live_builds_ts_url()
        => Assert.Equal("http://egysatv.com:8080/live/ehussein/975284/341925.ts",
                        StreamUrlBuilder.Live(Cred, 341925));

    [Fact]
    public void Movie_uses_container_extension_or_defaults_to_mp4()
    {
        Assert.Equal("http://egysatv.com:8080/movie/ehussein/975284/12.mkv", StreamUrlBuilder.Movie(Cred, 12, "mkv"));
        Assert.Equal("http://egysatv.com:8080/movie/ehussein/975284/12.mp4", StreamUrlBuilder.Movie(Cred, 12, null));
    }

    [Fact]
    public void BaseUrl_trailing_slash_is_normalized()
        => Assert.Equal("http://x/live/u/p/5.ts",
                        StreamUrlBuilder.Live(new("http://x/", "u", "p"), 5));
}
