using System.Text.RegularExpressions;
using BokiIPTV.Core.Models;

namespace BokiIPTV.Core.Playlist;

/// Parses an extended M3U (#EXTM3U / #EXTINF) playlist into stream entries.
public static partial class M3uParser
{
    [GeneratedRegex("([\\w-]+)=\"([^\"]*)\"")]
    private static partial Regex AttrRegex();

    public static IReadOnlyList<M3uEntry> Parse(string? content)
    {
        var list = new List<M3uEntry>();
        if (string.IsNullOrEmpty(content)) return list;

        string? name = null, logo = null, group = null;
        bool pending = false;   // an #EXTINF was seen, awaiting its URL line

        using var reader = new StringReader(content);
        for (string? raw; (raw = reader.ReadLine()) is not null;)
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                name = logo = group = null;
                foreach (Match m in AttrRegex().Matches(line))
                {
                    var key = m.Groups[1].Value.ToLowerInvariant();
                    var val = m.Groups[2].Value;
                    if (key == "tvg-logo") logo = val;
                    else if (key == "group-title") group = val;
                    else if (key == "tvg-name") name = val;
                }
                var comma = line.LastIndexOf(',');
                if (comma >= 0)
                {
                    var display = line[(comma + 1)..].Trim();
                    if (display.Length > 0) name = display;   // display name wins
                }
                pending = true;
            }
            else if (line.StartsWith('#'))
            {
                // Other directive (#EXTM3U, #EXTGRP, etc.) — ignore.
            }
            else if (pending)
            {
                list.Add(new M3uEntry
                {
                    Name = name ?? "",
                    Logo = string.IsNullOrWhiteSpace(logo) ? null : logo,
                    Group = string.IsNullOrWhiteSpace(group) ? "Uncategorized" : group!,
                    Url = line
                });
                name = logo = group = null;
                pending = false;
            }
        }
        return list;
    }
}
