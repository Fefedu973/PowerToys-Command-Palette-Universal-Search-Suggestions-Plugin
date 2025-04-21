using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalSearchSuggestions
{
    public sealed class Settings
    {
        public SearchEngine SelectedEngine { get; set; } = SearchEngine.Google;
        public SuggestionProvider SelectedProvider { get; set; } = SuggestionProvider.Google;
        public string CustomEngineUrl { get; set; } = string.Empty;
        public bool AlwaysShowQueryCard { get; set; } = true;
        public bool EnableSerpMarkdown { get; set; } = false;
    }

    public enum SearchEngine
    {
        Google,
        Bing,
        Yahoo,
        DuckDuckGo,
        Brave,
        Ecosia,
        Custom
    }

    public enum SuggestionProvider
    {
        Google,
        Bing,
        Yahoo,
        DuckDuckGo,
        Brave,
        Ecosia
    }
}
