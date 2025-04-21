using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UniversalSearchSuggestions.Helpers
{
    public sealed class JsonSettingsManager<T> where T : new()
    {
        private readonly string _file;
        private readonly object _lock = new();
        public T Current { get; private set; }

        public JsonSettingsManager(string file)
        {
            _file = file;
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            Current = File.Exists(file)
                ? JsonSerializer.Deserialize<T>(File.ReadAllText(file)) ?? new()
                : new();
        }
        public void Save()
        {
            lock (_lock)
            {
                File.WriteAllText(_file, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }

}
