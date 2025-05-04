using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PuppeteerAot;
using PuppeteerExtraSharp.Plugins.Recaptcha;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalSearchSuggestions;

internal sealed partial class UniversalSearchSuggestionsPage : DynamicListPage, IAsyncDisposable
{
    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0 Safari/123.0" } }
    };

    private CancellationTokenSource _cts = new();
    private IListItem[] _items = Array.Empty<IListItem>();
    private IBrowser? _browser;

    public UniversalSearchSuggestionsPage()
    {
        Icon = new("\uE8B6");
        Title = "Search Google";
        Name = "Search Google";
        PlaceholderText = "Start typing…";
        ShowDetails = true;
    }

    public override IListItem[] GetItems() => _items;

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new();
        _ = RefreshAsync(newSearch, _cts.Token);
    }

    private async Task RefreshAsync(string query, CancellationToken token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _items = [BuildInfoItem("Start typing to search…")];
                RaiseItemsChanged();
                return;
            }

            IsLoading = true;

            var suggestions = await GetGoogleSuggestionsAsync(query, token);
            var all = new List<string> { query };
            all.AddRange(suggestions.Where(s => !string.Equals(s, query, StringComparison.OrdinalIgnoreCase)));
            if (all.Count > 10) all.RemoveRange(10, all.Count - 10);

            _items = all.Select(BuildPlaceholderItem).ToArray();
            RaiseItemsChanged();

            await EnsureBrowserAsync();

            // Parallel capture of screenshots
            var captureTasks = all.Select((text, idx) => CaptureAndInjectAsync(text, idx)).ToArray();
            await Task.WhenAll(captureTasks);
        }
        catch (OperationCanceledException) { /* typing continued */ }
        finally { IsLoading = false; }
    }

    private async Task CaptureAndInjectAsync(string query, int index)
    {
        try
        {
            var path = await CaptureScreenshotAsync(query, CancellationToken.None);
            if (_items.Length > index && _items[index] is ListItem li)
            {
                li.Details = new Details
                {
                    Body = $"![Google result for '{EscapeAlt(query)}']({path.Replace("\\", "/")})"
                };
                RaiseItemsChanged();
            }
        }
        catch { /* swallow individual errors */ }
    }

    #region Google Suggest API

    private static async Task<IEnumerable<string>> GetGoogleSuggestionsAsync(string query, CancellationToken token)
    {
        var uri = $"https://suggestqueries.google.com/complete/search?client=firefox&q={Uri.EscapeDataString(query)}";
        try
        {
            using var resp = await _http.GetAsync(uri, token);
            if (!resp.IsSuccessStatusCode) return Enumerable.Empty<string>();
            var json = await resp.Content.ReadAsStringAsync(token);
            return ParseSuggestions(json);
        }
        catch { return Enumerable.Empty<string>(); }
    }

    #endregion

    #region Item builders

    private static ListItem BuildInfoItem(string text) => new(new NoOpCommand()) { Title = text };

    private ListItem BuildPlaceholderItem(string suggestion) => new(new OpenUrlCommand(GenerateSearchUrl(suggestion)))
    {
        Title = suggestion,
        Subtitle = "Open Google search result",
        Details = new Details { Body = "_Generating preview…_" }
    };

    #endregion

    #region Screenshots

    private async Task EnsureBrowserAsync()
    {
        if (_browser != null) return;
        var fetcher = new BrowserFetcher();
        await fetcher.DownloadAsync();
        _browser = await PuppeteerAot.Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--disable-gpu",
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--disable-setuid-sandbox",
                "--disable-blink-features=AutomationControlled"
            }
        });
    }

    private async Task<string> CaptureScreenshotAsync(string query, CancellationToken token)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"GoogleThumb_{Guid.NewGuid():N}.png");
        var page = await _browser!.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = 350, Height = 450 });
        var url = GenerateSearchUrl(query) + "&hl=en&igu=1";
        await page.GoToAsync(url, WaitUntilNavigation.Networkidle2);
        await TryDismissOverlaysAsync(page);
        await page.ScreenshotAsync(tempFile, new ScreenshotOptions { FullPage = false });
        await page.CloseAsync();
        return $"file:///{tempFile}";
    }

    private static async Task TryDismissOverlaysAsync(PuppeteerAot.IPage page)
    {
        var accept = await page.QuerySelectorAsync("button[aria-label='Accept all']");
        if (accept != null) await accept.ClickAsync();
    }

    private static string GenerateSearchUrl(string q) => $"https://www.google.com/search?q={Uri.EscapeDataString(q)}";

    #endregion

    #region Utilities

    private static IEnumerable<string> ParseSuggestions(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() < 2)
            yield break;
        foreach (var el in doc.RootElement[1].EnumerateArray())
        {
            var s = el.GetString();
            if (!string.IsNullOrWhiteSpace(s)) yield return s;
        }
    }

    private static string EscapeAlt(string text) => text.Replace("[", "&#91;").Replace("]", "&#93;").Replace("(", "&#40;").Replace(")", "&#41;");

    #endregion

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        if (_browser != null) await _browser.CloseAsync();
    }
}