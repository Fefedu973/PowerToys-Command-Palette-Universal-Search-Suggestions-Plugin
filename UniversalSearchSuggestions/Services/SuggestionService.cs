using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalSearchSuggestions.Services
{
    internal static class SuggestionService
    {
        private static readonly HttpClient _http = new(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });

        public static async Task<IReadOnlyList<string>> FetchAsync(string query, SuggestionProvider provider, CancellationToken ct)
        {
            var url = provider switch
            {
                SuggestionProvider.Google => $"https://www.google.com/complete/search?client=gws-wiz&q={Uri.EscapeDataString(query)}",
                SuggestionProvider.Bing => $"https://www.bingapis.com/api/v7/suggestions?appid=6D0A9B8C5100E9ECC7E11A104ADD76C10219804B&q={Uri.EscapeDataString(query)}",
                SuggestionProvider.Yahoo => $"https://sugg.search.yahoo.net/sg/?output=json&nresults=10&command={Uri.EscapeDataString(query)}",
                SuggestionProvider.DuckDuckGo => $"https://duckduckgo.com/ac/?type=json&q={Uri.EscapeDataString(query)}",
                SuggestionProvider.Brave => $"https://search.brave.com/api/suggest?rich=true&q={Uri.EscapeDataString(query)}",
                SuggestionProvider.Ecosia => $"https://ac.ecosia.org/?q={Uri.EscapeDataString(query)}",
                _ => throw new NotSupportedException()
            };

            var response = await _http.GetStringAsync(url, ct);
            return provider switch
            {
                SuggestionProvider.Google => GoogleParser.Parse(response),
                SuggestionProvider.Bing => BingParser.Parse(response),
                SuggestionProvider.Yahoo => YahooParser.Parse(response),
                SuggestionProvider.DuckDuckGo => DuckParser.Parse(response),
                SuggestionProvider.Brave => BraveParser.Parse(response),
                SuggestionProvider.Ecosia => EcosiaParser.Parse(response),
                _ => Array.Empty<string>()
            };
        }

        // ---- Individual parsers (trimmed versions, ported from original code) ----
        private static class GoogleParser
        {
            public static IReadOnlyList<string> Parse(string resp)
            {
                const string prefix = "window.google.ac.h(";
                const string suffix = ")";
                var idx = resp.IndexOf(prefix, StringComparison.Ordinal);
                var end = resp.LastIndexOf(suffix, StringComparison.Ordinal);
                if (idx == -1 || end == -1) return Array.Empty<string>();
                var json = resp.Substring(idx + prefix.Length, end - (idx + prefix.Length));
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement[0].EnumerateArray()
                           .Select(e => HtmlDecode(RemoveTags(e[0].GetString()!)))
                           .Distinct().ToArray();
            }
        }
        private static class BingParser
        {
            public static IReadOnlyList<string> Parse(string resp)
            {
                using var doc = JsonDocument.Parse(resp);
                return doc.RootElement.GetProperty("suggestionGroups")[0]
                           .GetProperty("searchSuggestions")
                           .EnumerateArray()
                           .Select(e => e.GetProperty("displayText").GetString()!)
                           .ToArray();
            }
        }
        private static class YahooParser
        {
            public static IReadOnlyList<string> Parse(string resp)
            {
                using var doc = JsonDocument.Parse(resp);
                return doc.RootElement.GetProperty("gossip").GetProperty("results")
                           .EnumerateArray().Select(e => e.GetProperty("key").GetString()!)
                           .ToArray();
            }
        }
        private static class DuckParser
        {
            public static IReadOnlyList<string> Parse(string resp)
            {
                using var doc = JsonDocument.Parse(resp);
                return doc.RootElement.EnumerateArray()
                           .Select(e => e.GetProperty("phrase").GetString()!)
                           .ToArray();
            }
        }
        private static class BraveParser
        {
            public static IReadOnlyList<string> Parse(string resp)
            {
                using var doc = JsonDocument.Parse(resp);
                var root = doc.RootElement[1];
                var list = new List<string>();
                foreach (var item in root.EnumerateArray())
                {
                    list.Add(item.TryGetProperty("name", out var n) ? n.GetString()! : item.GetProperty("q").GetString()!);
                }
                return list;
            }
        }
        private static class EcosiaParser
        {
            public static IReadOnlyList<string> Parse(string resp)
            {
                using var doc = JsonDocument.Parse(resp);
                return doc.RootElement.GetProperty("suggestions").EnumerateArray().Select(e => e.GetString()!).ToArray();
            }
        }

        private static string RemoveTags(string input) => input.Replace("<b>", string.Empty).Replace("</b>", string.Empty);
        private static string HtmlDecode(string s) => System.Net.WebUtility.HtmlDecode(s);
    }
}
