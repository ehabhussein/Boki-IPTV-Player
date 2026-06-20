namespace BokiIPTV.Core.Xtream;

public static class StreamUrlBuilder
{
    private static string Root(XtreamCredentials c) => c.BaseUrl.TrimEnd('/');

    public static string Live(XtreamCredentials c, int streamId)
        => $"{Root(c)}/live/{c.Username}/{c.Password}/{streamId}.ts";

    public static string Movie(XtreamCredentials c, int streamId, string? ext)
        => $"{Root(c)}/movie/{c.Username}/{c.Password}/{streamId}.{Ext(ext)}";

    public static string Episode(XtreamCredentials c, string episodeId, string? ext)
        => $"{Root(c)}/series/{c.Username}/{c.Password}/{episodeId}.{Ext(ext)}";

    private static string Ext(string? ext) => string.IsNullOrWhiteSpace(ext) ? "mp4" : ext;
}
