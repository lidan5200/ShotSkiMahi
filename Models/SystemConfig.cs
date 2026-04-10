namespace ShotSkiMahiD.Models
{
    public class SystemConfig
    {
        public MagnetConfig MagnetConfig { get; set; } = new();
        public ScanConfig ScanConfig { get; set; } = new();
        public PlcConfig Plc1Config { get; set; } = new();
        public PlcConfig Plc2Config { get; set; } = new();
        public ApiConfig ApiConfig { get; set; } = new();
    }

    public class MagnetConfig
    {
        public string RMRule { get; set; } = "H?????";
        public string RMName { get; set; } = "Magenet01";
        public string RMCode { get; set; } = "H11123";
    }

    public class ScanConfig
    {
        public string RMRule { get; set; } = "H?????";
        public string Scan1IP { get; set; } = "127.0.0.1";
        public int Scan1Port { get; set; } = 501;
        public string Scan2IP { get; set; } = "127.0.0.1";
        public int Scan2Port { get; set; } = 502;
        public string Send2Command { get; set; } = "T";
        public int GlueScanMinLength { get; set; } = 10;
        public int GlueScanMaxLength { get; set; } = 20;
        public int AssemblyScanMinLength { get; set; } = 8;
        public int AssemblyScanMaxLength { get; set; } = 20;
    }

    public class PlcConfig
    {
        public string IP { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 503;
    }

    public class ApiConfig
    {
        public bool StartEnabled { get; set; } = false;
        public bool GetSfcKeyEnabled { get; set; } = true;
        public bool AddSfcKeyEnabled { get; set; } = true;
        public bool TestDataCollect2MainChildEnabled { get; set; } = true;
        public bool CompleteEnabled { get; set; } = true;
    }
}
