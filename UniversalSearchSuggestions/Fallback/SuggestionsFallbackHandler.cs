using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;


namespace UniversalSearchSuggestions.Fallback
{
    internal sealed class SuggestionsFallbackHandler : IFallbackHandler
    {
        private readonly Settings _settings;
        private readonly UniversalSearchSuggestionsCommandsProvider _provider;
        private readonly List<ListItem> _items = new();
        private string _lastQuery = string.Empty;
        private CancellationTokenSource? _cts;

        public SuggestionsFallbackHandler(Settings settings, UniversalSearchSuggestionsCommandsProvider provider)
        {
            _settings = settings; _provider = provider;
        }

        public void UpdateQuery(string query)
        {
            if (string.Equals(query, _lastQuery, StringComparison.Ordinal)) return;
            _lastQuery = query;
            _cts?.Cancel();
            _cts = new();
            _ = UpdateAsync(query, _cts.Token);
        }

        private async Task UpdateAsync(string query, CancellationToken token)
        {
            try
            {
                _items.Clear();
                if (string.IsNullOrWhiteSpace(query)) { _provider.Refresh(); return; }

                var suggestions = await SuggestionService.FetchAsync(query, _settings.SelectedProvider, token);
                foreach (var s in suggestions)
                {
                    _items.Add(CreateSearchItem(s));
                }

                if (_items.Count == 0 && _settings.AlwaysShowQueryCard)
                {
                    _items.Add(CreateSearchItem(query));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "Error fetching suggestions",
                    Subtitle = ex.Message
                });
            }
            finally { _provider.Refresh(); }
        }

        // Called by the fallback item wrapper to expose the Items list
        public IListItem[] Items() => _items.ToArray();

        private ListItem CreateSearchItem(string text) => new(new OpenUrlCommand(SearchEngineUrls.Resolve(_settings, text)))
        {
            Title = text,
            Icon = new("\uE721") // Search icon
        };
    }
}
