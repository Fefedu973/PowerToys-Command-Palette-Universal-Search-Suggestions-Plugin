using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalSearchSuggestions.Services
{
    internal static class SearchEngineUrls
    {
        public static string Resolve(Settings s, string query)
        {
            var q = Uri.EscapeDataString(query);
            return s.SelectedEngine switch
            {
                SearchEngine.Google => $"https://www.google.com/search?q={q}",
                SearchEngine.Bing => $"https://www.bing.com/search?q={q}",
                SearchEngine.Yahoo => $"https://search.yahoo.com/search?p={q}",
                SearchEngine.DuckDuckGo => $"https://duckduckgo.com/?q={q}",
                SearchEngine.Brave => $"https://search.brave.com/search?q={q}",
                SearchEngine.Ecosia => $"https://www.ecosia.org/search?q={q}",
                SearchEngine.Custom => string.IsNullOrWhiteSpace(s.CustomEngineUrl) ? q : s.CustomEngineUrl + q,
                _ => q
            };
        }
    }
}
