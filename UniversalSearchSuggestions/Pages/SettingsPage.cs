using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;


namespace UniversalSearchSuggestions.Pages
{
    internal sealed class SettingsPage : ListPage
    {
        private readonly JsonSettingsManager<Settings> _manager;

        public SettingsPage(JsonSettingsManager<Settings> manager)
        {
            _manager = manager;
            Title = "Universal Search Suggestions – settings";
            Name = "Open";
            Icon = new("\uE713"); // Settings icon
        }

        public override IListItem[] GetItems() =>
        [
            new ListItem(new ToggleSettingCommand(_manager, s => s.AlwaysShowQueryCard, "Always show explicit search result"))
        {
            Title = "Always show search with \"Enter\"",
            Subtitle = _manager.Current.AlwaysShowQueryCard ? "Enabled" : "Disabled"
        },
        new ListItem(new ToggleSettingCommand(_manager, s => s.EnableSerpMarkdown, "Render Google Knowledge Graph as markdown"))
        {
            Title = "Show Knowledge‑Graph markdown preview (beta)",
            Subtitle = _manager.Current.EnableSerpMarkdown ? "Enabled" : "Disabled"
        },
        new ListItem(new CycleSettingCommand<SearchEngine>(_manager, s => s.SelectedEngine, "Search engine"))
        {
            Title = $"Selected engine: {_manager.Current.SelectedEngine}"
        },
        new ListItem(new CycleSettingCommand<SuggestionProvider>(_manager, s => s.SelectedProvider, "Suggestion API"))
        {
            Title = $"Suggestion provider: {_manager.Current.SelectedProvider}"
        },
        new ListItem(new EditTextSettingCommand(_manager, s => s.CustomEngineUrl, "Custom engine base URL"))
        {
            Title = "Custom search engine URL",
            Subtitle = string.IsNullOrWhiteSpace(_manager.Current.CustomEngineUrl) ? "(not set)" : _manager.Current.CustomEngineUrl
        }
        ];

        // Helper commands --------------------------------------------------------
        private sealed class ToggleSettingCommand : InvokableCommand
        {
            private readonly JsonSettingsManager<Settings> _mgr;
            private readonly Func<Settings, bool> _getter;
            private readonly Action<Settings, bool> _setter;
            private readonly string _name;

            public ToggleSettingCommand(JsonSettingsManager<Settings> mgr, Func<Settings, bool> expr, string name)
            {
                _mgr = mgr; _getter = expr; _name = name;
                _setter = (s, v) => typeof(Settings).GetProperty(expr.Method.Name.Substring(4))!.SetValue(s, v);
                Name = name; Icon = new("\uE8E5");
            }
            public override ICommandResult Invoke()
            {
                var cur = _mgr.Current; _setter(cur, !_getter(cur)); _mgr.Save();
                return CommandResult.ShowToast($"{_name}: {(_getter(cur) ? "On" : "Off")}");
            }
        }

        private sealed class CycleSettingCommand<T> : InvokableCommand where T : struct, Enum
        {
            private readonly JsonSettingsManager<Settings> _mgr;
            private readonly Func<Settings, T> _getter;
            private readonly Action<Settings, T> _setter;
            public CycleSettingCommand(JsonSettingsManager<Settings> mgr, Func<Settings, T> expr, string name)
            {
                _mgr = mgr; _getter = expr; _setter = (s, v) => typeof(Settings).GetProperty(expr.Method.Name.Substring(4))!.SetValue(s, v);
                Name = name; Icon = new("\uE8D4");
            }
            public override ICommandResult Invoke()
            {
                var curSettings = _mgr.Current;
                var vals = Enum.GetValues<T>();
                var idx = Array.IndexOf(vals, _getter(curSettings));
                var next = vals[(idx + 1) % vals.Length];
                _setter(curSettings, next); _mgr.Save();
                return CommandResult.ShowToast($"Set to {next}");
            }
        }

        private sealed class EditTextSettingCommand : InvokableCommand
        {
            private readonly JsonSettingsManager<Settings> _mgr; private readonly Func<Settings, string> _getter; private readonly Action<Settings, string> _setter; private readonly string _name;
            public EditTextSettingCommand(JsonSettingsManager<Settings> mgr, Func<Settings, string> expr, string name)
            {
                _mgr = mgr; _getter = expr; _setter = (s, v) => typeof(Settings).GetProperty(expr.Method.Name.Substring(4))!.SetValue(s, v);
                Name = name; _name = name; Icon = new("\uE70F");
            }
            public override ICommandResult Invoke()
            {
                // For now, inform user to edit JSON – a proper FormPage could be wired later.
                return CommandResult.ShowToast("Edit in settings.json – form UI TBD");
            }
        }
    }
}
