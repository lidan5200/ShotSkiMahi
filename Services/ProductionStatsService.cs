using System.IO;
using Newtonsoft.Json;
using ShotSkiMahiD.Models;

namespace ShotSkiMahiD.Services
{
    public class ProductionStatsService
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private ProductionStats _stats;

        public event Action<ProductionStats>? OnStatsChanged;

        public ProductionStats CurrentStats => _stats;

        public ProductionStatsService(string dataDirectory)
        {
            if (!Directory.Exists(dataDirectory))
                Directory.CreateDirectory(dataDirectory);

            _filePath = Path.Combine(dataDirectory, "production_stats.json");
            _stats = Load();
        }

        public ProductionStats Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonConvert.DeserializeObject<ProductionStats>(json) ?? CreateDefault();
                }
            }
            catch { }

            return CreateDefault();
        }

        public void Save(ProductionStats stats, string updatedBy = "System")
        {
            lock (_lock)
            {
                stats.LastUpdated = DateTime.Now;
                stats.LastUpdatedBy = updatedBy;
                _stats = stats;

                try
                {
                    var json = JsonConvert.SerializeObject(_stats, Formatting.Indented);
                    File.WriteAllText(_filePath, json);
                }
                catch { }

                OnStatsChanged?.Invoke(_stats);
            }
        }

        public void IncrementPass()
        {
            lock (_lock)
            {
                _stats.PassCount++;
                _stats.LastUpdated = DateTime.Now;
                Persist();
                OnStatsChanged?.Invoke(_stats);
            }
        }

        public void IncrementFail()
        {
            lock (_lock)
            {
                _stats.FailCount++;
                _stats.LastUpdated = DateTime.Now;
                Persist();
                OnStatsChanged?.Invoke(_stats);
            }
        }

        public void Reset(int passCount = 0, int failCount = 0, string updatedBy = "User")
        {
            Save(new ProductionStats
            {
                PassCount = passCount,
                FailCount = failCount
            }, updatedBy);
        }

        private void Persist()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_stats, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }

        private static ProductionStats CreateDefault() => new();
    }
}
