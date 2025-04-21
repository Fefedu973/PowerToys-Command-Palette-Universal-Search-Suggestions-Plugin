// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using Windows.UI.ApplicationSettings;

namespace UniversalSearchSuggestions;

public sealed partial class UniversalSearchSuggestionsCommandsProvider : CommandProvider
{
    // ---- Settings -----------------------------------------------------------
    private readonly JsonSettingsManager<Settings> _settings =
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                          "UniversalSearchSuggestions", "settings.json"));

    private Settings Settings => _settings.Current;

    // ---- Icon ---------------------------------------------------------------
    public UniversalSearchSuggestionsCommandsProvider()
    {
        DisplayName = "Universal Search Suggestions";
        Icon = IconHelpers.FromRelativePath("Assets\\Search.png");
    }

    // ---- Top‑level command opens our configuration page --------------------
    private ICommandItem[]? _topLevel;
    public override ICommandItem[] TopLevelCommands()
    {
        _topLevel ??= [
            new CommandItem(new SettingsPage(_settings)) { Title = "Search suggestions settings" }
        ];
        return _topLevel;
    }

    // ---- Fallback – live search suggestions while the user types ------------
    private IFallbackCommandItem[]? _fallback;
    public override IFallbackCommandItem[] FallbackCommands()
    {
        _fallback ??= [new FallbackCommandItem(new SuggestionsFallbackHandler(Settings, this))];
        return _fallback;
    }

    /// <summary>Notify the host that the visible fallback items have changed.</summary>
    internal void Refresh() => RaiseItemsChanged();
}