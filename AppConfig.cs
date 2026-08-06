using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Tounaent_Fixtures
{
    // Drop-in replacement for the small slice of IConfiguration this project actually used
    // (simple "Section:Key" colon-path lookups, e.g. _config["EmailSettings:FromPassword"]).
    // Built on Newtonsoft.Json instead of Microsoft.Extensions.Configuration because the
    // latter's transitive dependencies (Microsoft.Bcl.AsyncInterfaces, System.Text.Json,
    // System.Memory, etc.) kept throwing FileLoadException version-mismatch errors on
    // .NET Framework - a well-documented pain point with no single reliable fix short of
    // avoiding the package family entirely.
    public class AppConfig
    {
        private readonly JObject _root;

        public AppConfig(string basePath)
        {
            var path = Path.Combine(basePath, "appsettings.json");
            var json = File.ReadAllText(path);
            _root = JObject.Parse(json);

            var devPath = Path.Combine(basePath, "appsettings.Development.json");
            if (File.Exists(devPath))
            {
                var devJson = JObject.Parse(File.ReadAllText(devPath));
                _root.Merge(devJson, new Newtonsoft.Json.Linq.JsonMergeSettings
                {
                    MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Replace
                });
            }
        }

        // Supports "Section:Key" the same way IConfiguration's indexer did.
        public string this[string key]
        {
            get
            {
                var parts = key.Split(':');
                JToken current = _root;
                foreach (var part in parts)
                {
                    if (current == null) return null;
                    current = current[part];
                }
                return current?.ToString();
            }
        }
    }
}
