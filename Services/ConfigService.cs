using System.IO;
using System.Threading;

namespace ShotSkiMahiD.Services
{
    public class ConfigService : IDisposable
    {
        private readonly string _settingIniPath;
        private readonly string _mesConfigIniPath;
        private FileSystemWatcher? _mesConfigWatcher;
        private Timer? _debounceTimer;
        private bool _disposed;

        public event Action<Models.MesConfig>? OnMesConfigChanged;

        public ConfigService(string settingIniPath, string mesConfigIniPath)
        {
            _settingIniPath = settingIniPath;
            _mesConfigIniPath = mesConfigIniPath;

            StartMesConfigWatcher();
        }

        private void StartMesConfigWatcher()
        {
            var directory = Path.GetDirectoryName(_mesConfigIniPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            _mesConfigWatcher = new FileSystemWatcher
            {
                Path = directory,
                Filter = Path.GetFileName(_mesConfigIniPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _mesConfigWatcher.Changed += OnMesConfigFileChanged;
            _mesConfigWatcher.Created += OnMesConfigFileChanged;
        }

        private void OnMesConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(DebounceReload, null, 1000, Timeout.Infinite);
        }

        private void DebounceReload(object? state)
        {
            try
            {
                var newConfig = LoadMesConfig();
                OnMesConfigChanged?.Invoke(newConfig);
            }
            catch
            {
            }
        }

        public Models.SystemConfig LoadSystemConfig()
        {
            var config = new Models.SystemConfig();
            
            if (!File.Exists(_settingIniPath))
            {
                throw new FileNotFoundException($"配置文件未找到: {_settingIniPath}");
            }

            var lines = File.ReadAllLines(_settingIniPath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("----"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "RMRule":
                            config.MagnetConfig.RMRule = value;
                            config.ScanConfig.RMRule = value;
                            break;
                        case "RMName":
                            config.MagnetConfig.RMName = value;
                            break;
                        case "RMCode":
                            config.MagnetConfig.RMCode = value;
                            break;
                        case "Scan1_IP":
                            config.ScanConfig.Scan1IP = value;
                            break;
                        case "Scan1_Port":
                            if (int.TryParse(value, out int scan1Port))
                                config.ScanConfig.Scan1Port = scan1Port;
                            break;
                        case "Scan2_IP":
                            config.ScanConfig.Scan2IP = value;
                            break;
                        case "Scan2_Port":
                            if (int.TryParse(value, out int scan2Port))
                                config.ScanConfig.Scan2Port = scan2Port;
                            break;
                        case "Send2":
                            config.ScanConfig.Send2Command = value;
                            break;
                        case "GlueScanMinLength":
                            if (int.TryParse(value, out int glueMin))
                                config.ScanConfig.GlueScanMinLength = glueMin;
                            break;
                        case "GlueScanMaxLength":
                            if (int.TryParse(value, out int glueMax))
                                config.ScanConfig.GlueScanMaxLength = glueMax;
                            break;
                        case "AssemblyScanMinLength":
                            if (int.TryParse(value, out int assemblyMin))
                                config.ScanConfig.AssemblyScanMinLength = assemblyMin;
                            break;
                        case "AssemblyScanMaxLength":
                            if (int.TryParse(value, out int assemblyMax))
                                config.ScanConfig.AssemblyScanMaxLength = assemblyMax;
                            break;
                        case "PLC1_IP":
                            config.Plc1Config.IP = value;
                            break;
                        case "PLC1_Port":
                            if (int.TryParse(value, out int plc1Port))
                                config.Plc1Config.Port = plc1Port;
                            break;
                        case "PLC2_IP":
                            config.Plc2Config.IP = value;
                            break;
                        case "PLC2_Port":
                            if (int.TryParse(value, out int plc2Port))
                                config.Plc2Config.Port = plc2Port;
                            break;
                        case "API_Start_Enabled":
                            config.ApiConfig.StartEnabled = bool.TryParse(value, out bool startEnabled) && startEnabled;
                            break;
                        case "API_GetSfcKey_Enabled":
                            config.ApiConfig.GetSfcKeyEnabled = bool.TryParse(value, out bool getSfcEnabled) && getSfcEnabled;
                            break;
                        case "API_AddSfcKey_Enabled":
                            config.ApiConfig.AddSfcKeyEnabled = bool.TryParse(value, out bool addSfcEnabled) && addSfcEnabled;
                            break;
                        case "API_TestDataCollect2MainChild_Enabled":
                            config.ApiConfig.TestDataCollect2MainChildEnabled = bool.TryParse(value, out bool testEnabled) && testEnabled;
                            break;
                        case "API_Complete_Enabled":
                            config.ApiConfig.CompleteEnabled = bool.TryParse(value, out bool completeEnabled) && completeEnabled;
                            break;
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// 使用INIFile（kernel32 API）读取mes_config.ini，正确处理中文编码
        /// </summary>
        public Models.MesConfig LoadMesConfig()
        {
            var config = new Models.MesConfig();
            
            if (!File.Exists(_mesConfigIniPath))
            {
                throw new FileNotFoundException($"MES配置文件未找到: {_mesConfigIniPath}");
            }

            var ini = new INIFile(_mesConfigIniPath);

            config.System.JSONURL = ini.IniReadValue("SYSTEM", "JSONURL");
            config.System.Factory = ini.IniReadValue("SYSTEM", "Factory");
            config.System.URLIP = ini.IniReadValue("SYSTEM", "URLIP");
            config.System.ShareIP = ini.IniReadValue("SYSTEM", "ShareIP");
            config.System.FtpIP = ini.IniReadValue("SYSTEM", "FtpIP");
            config.System.CloudIP = ini.IniReadValue("SYSTEM", "CloudIP");
            config.System.DownloadType = ini.IniReadValue("SYSTEM", "DownloadType");
            config.System.RunEXE = ini.IniReadValue("SYSTEM", "runEXE");
            config.System.IP = ini.IniReadValue("SYSTEM", "IP");

            var logKeepDays = ini.IniReadValue("Config", "LogKeepDays");
            if (int.TryParse(logKeepDays, out int logDays))
                config.Config.LogKeepDays = logDays;

            config.Config.LoginUser = ini.IniReadValue("Config", "LoginUser");
            config.Config.LoginPassword = ini.IniReadValue("Config", "LoginPassword");
            config.Config.LoginID = ini.IniReadValue("Config", "LoginID");
            config.Config.Client = ini.IniReadValue("Config", "Client");

            var clientId = ini.IniReadValue("Config", "ClientID");
            if (int.TryParse(clientId, out int cId))
                config.Config.ClientID = cId;

            config.Config.Section = ini.IniReadValue("Config", "Section");

            var sectionId = ini.IniReadValue("Config", "SectionID");
            if (int.TryParse(sectionId, out int sId))
                config.Config.SectionID = sId;

            config.Config.Line = ini.IniReadValue("Config", "Line");
            config.Config.Project = ini.IniReadValue("Config", "PROJECT");
            config.Config.Product = ini.IniReadValue("Config", "PRODUCT");
            config.Config.ClientPN = ini.IniReadValue("Config", "ClientPN");
            config.Config.Resource = ini.IniReadValue("Config", "Resource");

            var projectId = ini.IniReadValue("Config", "PROJECT_ID");
            if (int.TryParse(projectId, out int pId))
                config.Config.ProjectID = pId;

            var productId = ini.IniReadValue("Config", "PRODUCT_ID");
            if (int.TryParse(productId, out int prodId))
                config.Config.ProductID = prodId;

            var shopOrderId = ini.IniReadValue("Config", "SHOPORDER_ID");
            if (int.TryParse(shopOrderId, out int soId))
                config.Config.ShopOrderID = soId;

            var schedulingId = ini.IniReadValue("Config", "SchedulingID");
            if (int.TryParse(schedulingId, out int schId))
                config.Config.SchedulingID = schId;

            var loadId = ini.IniReadValue("Config", "Load_ID");
            if (int.TryParse(loadId, out int lId))
                config.Config.LoadID = lId;

            var schedulingQty = ini.IniReadValue("Config", "SchedulingQty");
            if (int.TryParse(schedulingQty, out int sqty))
                config.Config.SchedulingQty = sqty;

            var shoporderQty = ini.IniReadValue("Config", "ShoporderQty");
            if (int.TryParse(shoporderQty, out int oqty))
                config.Config.ShoporderQty = oqty;

            var stationId = ini.IniReadValue("Config", "StationID");
            if (int.TryParse(stationId, out int stId))
                config.Config.StationID = stId;

            config.Config.ModelID = ini.IniReadValue("Config", "Model_ID");

            var firstStation = ini.IniReadValue("Config", "FristStation");
            if (int.TryParse(firstStation, out int fId))
                config.Config.FirstStation = fId;

            config.Config.Operation = ini.IniReadValue("Config", "Operation");
            config.Config.SapShoporder = ini.IniReadValue("Config", "SapShoporder");
            config.Config.Remark = ini.IniReadValue("Config", "Remark");
            config.Config.TraceStationId = ini.IniReadValue("Config", "TraceStationId");

            return config;
        }

        public void UpdateMesConfig(string section, string key, string value)
        {
            var ini = new INIFile(_mesConfigIniPath);
            ini.IniWriteValue(section, key, value);
        }

        public string GetMesBaseUrl()
        {
            if (!File.Exists(_mesConfigIniPath))
                return string.Empty;

            try
            {
                var ini = new INIFile(_mesConfigIniPath);
                return ini.IniReadValue("SYSTEM", "JSONURL");
            }
            catch
            {
                return string.Empty;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _debounceTimer?.Dispose();
                _mesConfigWatcher?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
