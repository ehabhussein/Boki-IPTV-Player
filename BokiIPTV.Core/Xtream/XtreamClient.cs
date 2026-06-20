using System.Text;
using System.Text.Json;
using BokiIPTV.Core.Models;

namespace BokiIPTV.Core.Xtream;

public sealed class XtreamClient(HttpClient http, XtreamCredentials cred) : IXtreamClient
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private string Url(string? action, params (string k, string v)[] extra)
    {
        var sb = new StringBuilder(cred.BaseUrl.TrimEnd('/'))
            .Append("/player_api.php?username=").Append(cred.Username)
            .Append("&password=").Append(cred.Password);
        if (action is not null) sb.Append("&action=").Append(action);
        foreach (var (k, v) in extra) sb.Append('&').Append(k).Append('=').Append(v);
        return sb.ToString();
    }

    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        await using var s = await http.GetStreamAsync(url, ct);
        return (await JsonSerializer.DeserializeAsync<T>(s, Json, ct))!;
    }

    public async Task<UserInfo> AuthenticateAsync(CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(await http.GetStringAsync(Url(null), ct));
        var ui = doc.RootElement.GetProperty("user_info");
        return ui.Deserialize<UserInfo>(Json)!;
    }

    public Task<IReadOnlyList<Category>> GetLiveCategoriesAsync(CancellationToken ct)
        => GetListAsync<Category>(Url("get_live_categories"), ct);
    public Task<IReadOnlyList<Channel>> GetLiveStreamsAsync(string categoryId, CancellationToken ct)
        => GetListAsync<Channel>(Url("get_live_streams", ("category_id", categoryId)), ct);
    public Task<IReadOnlyList<Channel>> GetAllLiveStreamsAsync(CancellationToken ct)
        => GetListAsync<Channel>(Url("get_live_streams"), ct);
    public Task<IReadOnlyList<Category>> GetVodCategoriesAsync(CancellationToken ct)
        => GetListAsync<Category>(Url("get_vod_categories"), ct);
    public Task<IReadOnlyList<Movie>> GetVodStreamsAsync(string categoryId, CancellationToken ct)
        => GetListAsync<Movie>(Url("get_vod_streams", ("category_id", categoryId)), ct);
    public Task<IReadOnlyList<Movie>> GetAllVodStreamsAsync(CancellationToken ct)
        => GetListAsync<Movie>(Url("get_vod_streams"), ct);
    public Task<IReadOnlyList<Category>> GetSeriesCategoriesAsync(CancellationToken ct)
        => GetListAsync<Category>(Url("get_series_categories"), ct);
    public Task<IReadOnlyList<Series>> GetSeriesAsync(string categoryId, CancellationToken ct)
        => GetListAsync<Series>(Url("get_series", ("category_id", categoryId)), ct);
    public Task<IReadOnlyList<Series>> GetAllSeriesAsync(CancellationToken ct)
        => GetListAsync<Series>(Url("get_series"), ct);
    public Task<SeriesInfo> GetSeriesInfoAsync(int seriesId, CancellationToken ct)
        => GetAsync<SeriesInfo>(Url("get_series_info", ("series_id", seriesId.ToString())), ct);
    public Task<VodInfo> GetVodInfoAsync(int vodId, CancellationToken ct)
        => GetAsync<VodInfo>(Url("get_vod_info", ("vod_id", vodId.ToString())), ct);

    public async Task<IReadOnlyList<EpgEntry>> GetShortEpgAsync(int streamId, CancellationToken ct)
    {
        // EPG is optional per channel; on any failure or empty payload return empty list, never throw.
        try
        {
            using var doc = JsonDocument.Parse(
                await http.GetStringAsync(Url("get_short_epg", ("stream_id", streamId.ToString())), ct));
            if (!doc.RootElement.TryGetProperty("epg_listings", out var arr)) return [];
            var list = new List<EpgEntry>();
            foreach (var e in arr.EnumerateArray())
            {
                list.Add(new EpgEntry
                {
                    Title = Decode(e, "title"),
                    Description = Decode(e, "description"),
                    Start = ParseUnix(e, "start_timestamp"),
                    End = ParseUnix(e, "stop_timestamp")
                });
            }
            return list;
        }
        catch { return []; }
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string url, CancellationToken ct)
        => await GetAsync<List<T>>(url, ct);

    private static string Decode(JsonElement e, string prop)
    {
        if (!e.TryGetProperty(prop, out var v) || v.ValueKind != JsonValueKind.String) return "";
        var s = v.GetString() ?? "";
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); } catch { return s; }
    }

    private static DateTimeOffset ParseUnix(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) && long.TryParse(v.GetString(), out var ts)
            ? DateTimeOffset.FromUnixTimeSeconds(ts) : DateTimeOffset.MinValue;
}
