namespace ShotSkiMahiD.Models
{
    public class ProductionStats
    {
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string LastUpdatedBy { get; set; } = "System";
    }
}
