using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShotSkiMahiD.Models;
using ShotSkiMahiD.Services;

namespace ShotSkiMahiD.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly PlcService _plcService;
        private readonly ScanService _scan枪Service;
        private readonly MesApiService _mesApiService;
        private readonly ProcessController _processController;
        private readonly LogService _logService;
        private readonly ConfigService _configService;
        private readonly ProductionStatsService _statsService;
        private readonly System.Windows.Threading.DispatcherTimer _timer;
        private readonly HttpClient _mesHealthCheckClient;

        private bool _disposed;
        private string _statusMessage = string.Empty;
        private readonly string _stationId = string.Empty;
        private bool _isEmergencyStopped;

        [ObservableProperty]
        private string _title = "ShotSki Mahi组装系统";

        [ObservableProperty]
        private bool _isPlc1Connected;

        partial void OnIsPlc1ConnectedChanged(bool value)
        {
            OnPropertyChanged(nameof(Plc1ConnectionStatusColor));
        }

        [ObservableProperty]
        private bool _isPlc2Connected;

        partial void OnIsPlc2ConnectedChanged(bool value)
        {
            OnPropertyChanged(nameof(Plc2ConnectionStatusColor));
        }

        [ObservableProperty]
        private bool _isScan1Connected;

        [ObservableProperty]
        private bool _isScan2Connected;

        [ObservableProperty]
        private string _currentSFC = string.Empty;

        [ObservableProperty]
        private string _currentMagnetCode = string.Empty;

        [ObservableProperty]
        private string _currentState = "空闲";

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private int _testValue1;

        [ObservableProperty]
        private int _testValue2;

        [ObservableProperty]
        private string _lastError = string.Empty;

        [ObservableProperty]
        private bool _isMonitoring;

        [ObservableProperty]
        private string _productBarcode = string.Empty;

        [ObservableProperty]
        private string _smallPartBarcode = string.Empty;

        [ObservableProperty]
        private string _assemblyStatusText = "等待中";

        [ObservableProperty]
        private string _assemblyStatusIcon = "\xE7BA";

        [ObservableProperty]
        private string _assemblyStatusDescription = "等待扫码完成";

        [ObservableProperty]
        private string _sfcKeyValidationBackground = "#FFECF0F1";

        [ObservableProperty]
        private string _sfcKeyValidationBorder = "#FFBDC3C7";

        [ObservableProperty]
        private string _sfcKeyValidationForeground = "#FF7F8C8D";

        [ObservableProperty]
        private string _sfcKeyValidationIcon = "\xE946";

        [ObservableProperty]
        private string _sfcKeyValidationText = "等待校验";

        [ObservableProperty]
        private string _scanner1StatusColor = "#FF95A5A6";

        [ObservableProperty]
        private string _scanner1StatusText = "未连接";

        [ObservableProperty]
        private string _scanner2StatusColor = "#FF95A5A6";

        [ObservableProperty]
        private string _scanner2StatusText = "未连接";

        [ObservableProperty]
        private string _plc1IpAddress = "127.0.0.1";

        [ObservableProperty]
        private string _plc2IpAddress = "127.0.0.1";

        [ObservableProperty]
        private int _plc1Port = 503;

        [ObservableProperty]
        private int _plc2Port = 504;

        [ObservableProperty]
        private string _startScanSignalColor = "#FF95A5A6";

        [ObservableProperty]
        private string _startScanSignalText = "OFF";

        [ObservableProperty]
        private string _startPressSignalColor = "#FF95A5A6";

        [ObservableProperty]
        private string _startPressSignalText = "OFF";

        [ObservableProperty]
        private string _completePressSignalColor = "#FF95A5A6";

        [ObservableProperty]
        private string _completePressSignalText = "OFF";

        [ObservableProperty]
        private string _assemblyStatusColor = "#FF95A5A6";

        [ObservableProperty]
        private string _assemblyStatusDetailText = "等待中";

        [ObservableProperty]
        private string _pressTimeValue = "0";

        [ObservableProperty]
        private string _pressTemperatureValue = "0";

        [ObservableProperty]
        private string _startApiDetail = "未调用";

        [ObservableProperty]
        private string _startApiStatusColor = "#FF95A5A6";

        [ObservableProperty]
        private string _getSfcKeyApiDetail = "未调用";

        [ObservableProperty]
        private string _getSfcKeyApiStatusColor = "#FF95A5A6";

        [ObservableProperty]
        private string _addSfcKeyApiDetail = "未调用";

        [ObservableProperty]
        private string _addSfcKeyApiStatusColor = "#FF95A5A6";

        [ObservableProperty]
        private string _dataCollectApiDetail = "未调用";

        [ObservableProperty]
        private string _dataCollectApiStatusColor = "#FF95A5A6";

        [ObservableProperty]
        private string _completeApiDetail = "未调用";

        [ObservableProperty]
        private string _completeApiStatusColor = "#FF95A5A6";

        [ObservableProperty]
        private string _currentTimeText = string.Empty;

        [ObservableProperty]
        private string _stationIdText = "工作站: --";

        [ObservableProperty]
        private bool _isMesConnected;

        public string CurrentStateText => GetStateDescription(CurrentState);
        public string StatusMessage => _statusMessage;
        public string Plc1ConnectionStatusColor => IsPlc1Connected ? "#FF27AE60" : "#FFE74C3C";
        public string Plc2ConnectionStatusColor => IsPlc2Connected ? "#FF27AE60" : "#FFE74C3C";
        public string MesConnectionStatusColor => IsMesConnected ? "#FF27AE60" : "#FFE74C3C";
        public int PassCount => _statsService.CurrentStats.PassCount;
        public int FailCount => _statsService.CurrentStats.FailCount;
        public int TotalCount => PassCount + FailCount;
        public string PassRateText => TotalCount > 0 ? $"合格率: {(PassCount * 100.0 / TotalCount):F1}%" : "合格率: --";
        public string FailRateText => TotalCount > 0 ? $"不合格率: {(FailCount * 100.0 / TotalCount):F1}%" : "不合格率: --";
        public string StatsLastUpdated => _statsService.CurrentStats.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");

        [ObservableProperty]
        private int _editPassCount;

        [ObservableProperty]
        private int _editFailCount;

        [ObservableProperty]
        private bool _isEditingStats;
        public ObservableCollection<string> SystemLogs { get; } = new();

        public MainViewModel(
            PlcService plcService,
            ScanService scanService,
            MesApiService mesApiService,
            ProcessController processController,
            LogService logService,
            ConfigService configService,
            ProductionStatsService statsService)
        {
            _plcService = plcService;
            _scan枪Service = scanService;
            _mesApiService = mesApiService;
            _processController = processController;
            _logService = logService;
            _configService = configService;
            _statsService = statsService;

            _mesHealthCheckClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            Initialize();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            CurrentTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            if (IsMonitoring && IsPlc1Connected)
            {
                ReadD3010AssemblyStatus();
            }
            
            CheckMesConnectionAsync().ConfigureAwait(false);
        }

        private async Task CheckMesConnectionAsync()
        {
            try
            {
                var wasConnected = IsMesConnected;
                var response = await _mesHealthCheckClient.GetAsync(_configService.GetMesBaseUrl());
                IsMesConnected = response.IsSuccessStatusCode;

                if (wasConnected != IsMesConnected)
                {
                    _logService.LogInfo(IsMesConnected ? "MES服务已重新连接" : "MES服务连接断开");
                }
            }
            catch
            {
                if (IsMesConnected)
                {
                    IsMesConnected = false;
                    _logService.LogWarning("MES服务连接异常");
                }
            }
        }

        private void ReadD3010AssemblyStatus()
        {
            try
            {
                var d3010Value = _plcService.ReadPlc1Register("D3010");
                
                switch (d3010Value)
                {
                    case 1:
                        AssemblyStatusColor = "#FF27AE60";
                        AssemblyStatusDetailText = "允许组装";
                        break;
                    case 2:
                        AssemblyStatusColor = "#FFE74C3C";
                        AssemblyStatusDetailText = "禁止组装";
                        break;
                    default:
                        AssemblyStatusColor = "#FF95A5A6";
                        AssemblyStatusDetailText = "等待中";
                        break;
                }
            }
            catch
            {
            }
        }

        private void Initialize()
        {
            _plcService.OnLog += OnServiceLog;
            _plcService.OnConnectionChanged += OnPlcConnectionChanged;
            _scan枪Service.OnLog += OnServiceLog;
            _mesApiService.OnLog += OnServiceLog;
            _logService.OnLogAdded += OnLogAdded;

            _processController.OnProcessStateChanged += OnProcessStateChanged;
            _processController.OnError += OnProcessError;

            _statsService.OnStatsChanged += OnStatsChanged;

            LoadRecentLogs();

            _logService.LogInfo("系统初始化完成");
            _logService.LogInfo($"已加载生产统计: 合格={PassCount}, 不合格={FailCount}");
        }

        private void LoadRecentLogs()
        {
            var recentLogs = _logService.GetRecentSystemLogs(100);
            foreach (var log in recentLogs)
                SystemLogs.Add(log);
        }

        private void OnStatsChanged(ProductionStats stats)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(PassCount));
                OnPropertyChanged(nameof(FailCount));
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(PassRateText));
                OnPropertyChanged(nameof(FailRateText));
                OnPropertyChanged(nameof(StatsLastUpdated));
            });
        }

        private void OnPlcConnectionChanged(object? sender, bool isConnected)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(Plc1ConnectionStatusColor));
                OnPropertyChanged(nameof(Plc2ConnectionStatusColor));
            });
        }

        [RelayCommand]
        private async Task StartAsync()
        {
            if (_isEmergencyStopped)
            {
                _statusMessage = "请先复位紧急停止状态";
                OnPropertyChanged(nameof(StatusMessage));
                return;
            }

            if (IsMonitoring)
            {
                _logService.LogInfo("系统已在运行中，跳过重复启动");
                return;
            }

            var allConnected = IsPlc1Connected && IsPlc2Connected && IsScan1Connected && IsScan2Connected;

            if (!allConnected)
            {
                await Task.Run(() =>
                {
                    var result1 = _plcService.ConnectPlc1();
                    var result2 = _plcService.ConnectPlc2();
                    IsPlc1Connected = result1;
                    IsPlc2Connected = result2;
                });

                await Task.Run(() =>
                {
                    var result1 = _scan枪Service.ConnectScan1();
                    var result2 = _scan枪Service.ConnectScan2();
                    IsScan1Connected = result1;
                    IsScan2Connected = result2;
                });
            }

            UpdateScannerStatus();

            if (IsPlc1Connected && IsPlc2Connected && IsScan1Connected && IsScan2Connected)
            {
                _processController.StartMonitoring();
                IsMonitoring = true;
                _statusMessage = "系统已启动，等待扫码";
                OnPropertyChanged(nameof(StatusMessage));
                _logService.LogInfo("系统启动成功");
            }
            else
            {
                _statusMessage = "设备连接失败，请检查配置";
                OnPropertyChanged(nameof(StatusMessage));
                _logService.LogError("设备连接失败");
            }
        }

        [RelayCommand]
        private void Stop()
        {
            _processController.StopMonitoring();
            IsMonitoring = false;
            _statusMessage = "系统已停止";
            OnPropertyChanged(nameof(StatusMessage));
            _logService.LogInfo("系统已手动停止");
        }

        [RelayCommand]
        private void Reset()
        {
            _isEmergencyStopped = false;
            _processController.StopMonitoring();
            IsMonitoring = false;
            
            ProductBarcode = string.Empty;
            SmallPartBarcode = string.Empty;
            CurrentSFC = string.Empty;
            CurrentMagnetCode = string.Empty;
            CurrentState = "空闲";
            IsProcessing = false;
            TestValue1 = 0;
            TestValue2 = 0;
            LastError = string.Empty;
            
            StartScanSignalColor = "#FF95A5A6";
            StartScanSignalText = "OFF";
            StartPressSignalColor = "#FF95A5A6";
            StartPressSignalText = "OFF";
            CompletePressSignalColor = "#FF95A5A6";
            CompletePressSignalText = "OFF";
            AssemblyStatusColor = "#FF95A5A6";
            AssemblyStatusDetailText = "等待中";
            PressTimeValue = "0";
            PressTemperatureValue = "0";
            
            AssemblyStatusText = "等待中";
            AssemblyStatusIcon = "\xE7BA";
            AssemblyStatusDescription = "等待扫码完成";
            
            SfcKeyValidationBackground = "#FFECF0F1";
            SfcKeyValidationBorder = "#FFBDC3C7";
            SfcKeyValidationForeground = "#FF7F8C8D";
            SfcKeyValidationIcon = "\xE946";
            SfcKeyValidationText = "等待校验";
            
            StartApiDetail = "未调用";
            StartApiStatusColor = "#FF95A5A6";
            GetSfcKeyApiDetail = "未调用";
            GetSfcKeyApiStatusColor = "#FF95A5A6";
            AddSfcKeyApiDetail = "未调用";
            AddSfcKeyApiStatusColor = "#FF95A5A6";
            DataCollectApiDetail = "未调用";
            DataCollectApiStatusColor = "#FF95A5A6";
            CompleteApiDetail = "未调用";
            CompleteApiStatusColor = "#FF95A5A6";
            
            _statusMessage = "系统已复位";
            OnPropertyChanged(nameof(StatusMessage));
            _logService.LogInfo("系统已复位");
        }

        [RelayCommand]
        private void EmergencyStop()
        {
            _isEmergencyStopped = true;
            _processController.StopMonitoring();
            IsMonitoring = false;
            _statusMessage = "紧急停止已触发！";
            OnPropertyChanged(nameof(StatusMessage));
            _logService.LogError("紧急停止已触发");
        }

        [RelayCommand]
        private async Task ConnectPlc1Async()
        {
            await Task.Run(() =>
            {
                var result = _plcService.ConnectPlc1();
                IsPlc1Connected = result;
            });
        }

        [RelayCommand]
        private async Task ConnectPlc2Async()
        {
            await Task.Run(() =>
            {
                var result = _plcService.ConnectPlc2();
                IsPlc2Connected = result;
            });
        }

        [RelayCommand]
        private async Task ConnectScan1Async()
        {
            await Task.Run(() =>
            {
                var result = _scan枪Service.ConnectScan1();
                IsScan1Connected = result;
            });
            UpdateScannerStatus();
        }

        [RelayCommand]
        private async Task ConnectScan2Async()
        {
            await Task.Run(() =>
            {
                var result = _scan枪Service.ConnectScan2();
                IsScan2Connected = result;
            });
            UpdateScannerStatus();
        }

        [RelayCommand]
        private void StartMonitoring()
        {
            if (!IsPlc1Connected || !IsPlc2Connected)
            {
                LastError = "请先连接PLC设备";
                _logService.LogError(LastError);
                return;
            }

            if (!IsScan1Connected || !IsScan2Connected)
            {
                LastError = "请先连接扫码枪设备";
                _logService.LogError(LastError);
                return;
            }

            _processController.StartMonitoring();
            IsMonitoring = true;
            _logService.LogInfo("监控已启动");
        }

        [RelayCommand]
        private void StopMonitoring()
        {
            _processController.StopMonitoring();
            IsMonitoring = false;
            _logService.LogInfo("监控已停止");
        }

        [RelayCommand]
        private void ClearLogs()
        {
            SystemLogs.Clear();
        }

        [RelayCommand]
        private void ViewLog()
        {
            var logViewerVm = new LogViewerViewModel(_logService);
            var logViewer = new Views.LogViewerWindow(logViewerVm);
            logViewer.Owner = Application.Current.MainWindow;
            logViewer.Show();
        }

        [RelayCommand]
        private void EditStats()
        {
            EditPassCount = PassCount;
            EditFailCount = FailCount;
            IsEditingStats = true;
        }

        [RelayCommand]
        private void SaveStats()
        {
            if (EditPassCount < 0 || EditFailCount < 0)
            {
                MessageBox.Show("统计数值不能为负数", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"确认修改生产统计？\n\n合格数量: {PassCount} → {EditPassCount}\n不合格数量: {FailCount} → {EditFailCount}",
                "确认修改",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _statsService.Reset(EditPassCount, EditFailCount, "User");
                _logService.LogInfo($"用户手动修改统计: 合格={EditPassCount}, 不合格={EditFailCount}");
            }
            IsEditingStats = false;
        }

        [RelayCommand]
        private void CancelEditStats()
        {
            IsEditingStats = false;
        }

        [RelayCommand]
        private void ResetStats()
        {
            var result = MessageBox.Show(
                $"确认重置所有生产统计数据为0？\n\n当前: 合格={PassCount}, 不合格={FailCount}",
                "确认重置",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _statsService.Reset(0, 0, "User");
                _logService.LogWarning("用户已重置生产统计数据");
            }
        }

        private void UpdateScannerStatus()
        {
            Scanner1StatusColor = IsScan1Connected ? "#FF27AE60" : "#FF95A5A6";
            Scanner1StatusText = IsScan1Connected ? "已连接" : "未连接";
            Scanner2StatusColor = IsScan2Connected ? "#FF27AE60" : "#FF95A5A6";
            Scanner2StatusText = IsScan2Connected ? "已连接" : "未连接";
        }

        private void OnServiceLog(object? sender, string message)
        {
            if (sender is PlcService)
            {
                return;
            }
            _logService.LogInfo(message);
        }

        private void OnLogAdded(object? sender, string logEntry)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (SystemLogs.Count > 1000)
                {
                    SystemLogs.RemoveAt(0);
                }
                SystemLogs.Add(logEntry);
            });
        }

        private void OnProcessStateChanged(object? sender, ProcessStatus status)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentSFC = status.CurrentSFC;
                CurrentMagnetCode = status.CurrentMagnetCode;
                CurrentState = GetStateDescription(status.CurrentState.ToString());
                IsProcessing = status.IsProcessing;
                TestValue1 = status.TestValue1;
                TestValue2 = status.TestValue2;
                
                ProductBarcode = status.CurrentSFC;
                SmallPartBarcode = status.CurrentMagnetCode;
                
                PressTimeValue = status.TestValue1.ToString();
                PressTemperatureValue = status.TestValue2.ToString();
                
                UpdateSignalStatus(status);
                UpdateApiStatus(status);
                UpdateAssemblyStatus(status);
            });
        }

        private void UpdateSignalStatus(ProcessStatus status)
        {
            switch (status.CurrentState)
            {
                case ProcessState.WaitingForD3003:
                    StartScanSignalColor = "#FF27AE60";
                    StartScanSignalText = "ON";
                    break;
                case ProcessState.WaitingForD3005:
                    StartPressSignalColor = "#FF27AE60";
                    StartPressSignalText = "ON";
                    break;
                case ProcessState.WaitingForD3007:
                    CompletePressSignalColor = "#FF27AE60";
                    CompletePressSignalText = "ON";
                    break;
                case ProcessState.Completed:
                    AssemblyStatusColor = "#FF27AE60";
                    AssemblyStatusDetailText = "完成";
                    break;
            }
        }

        private void UpdateApiStatus(ProcessStatus status)
        {
            switch (status.CurrentState)
            {
                case ProcessState.StartApiCalled:
                    StartApiDetail = "调用中...";
                    StartApiStatusColor = "#FFF39C12";
                    break;
                case ProcessState.GetSfcKeyCalled:
                    GetSfcKeyApiDetail = "调用中...";
                    GetSfcKeyApiStatusColor = "#FFF39C12";
                    break;
                case ProcessState.AddSfcKeyCalled:
                    AddSfcKeyApiDetail = "调用中...";
                    AddSfcKeyApiStatusColor = "#FFF39C12";
                    break;
                case ProcessState.TestDataCollectCalled:
                    DataCollectApiDetail = "调用中...";
                    DataCollectApiStatusColor = "#FFF39C12";
                    break;
                case ProcessState.CompleteCalled:
                    CompleteApiDetail = "调用中...";
                    CompleteApiStatusColor = "#FFF39C12";
                    break;
                case ProcessState.Completed:
                    StartApiDetail = "成功";
                    StartApiStatusColor = "#FF27AE60";
                    GetSfcKeyApiDetail = "成功";
                    GetSfcKeyApiStatusColor = "#FF27AE60";
                    AddSfcKeyApiDetail = "成功";
                    AddSfcKeyApiStatusColor = "#FF27AE60";
                    DataCollectApiDetail = "成功";
                    DataCollectApiStatusColor = "#FF27AE60";
                    CompleteApiDetail = "成功";
                    CompleteApiStatusColor = "#FF27AE60";
                    break;
                case ProcessState.Error:
                    MarkFailedStep(status.FailedStep);
                    break;
            }
        }

        private void MarkFailedStep(ProcessState? failedStep)
        {
            var failColor = "#FFE74C3C";
            var failText = "失败";

            switch (failedStep)
            {
                case ProcessState.StartApiCalled:
                    StartApiDetail = failText; StartApiStatusColor = failColor; break;
                case ProcessState.GetSfcKeyCalled:
                    GetSfcKeyApiDetail = failText; GetSfcKeyApiStatusColor = failColor; break;
                case ProcessState.AddSfcKeyCalled:
                    AddSfcKeyApiDetail = failText; AddSfcKeyApiStatusColor = failColor; break;
                case ProcessState.TestDataCollectCalled:
                    DataCollectApiDetail = failText; DataCollectApiStatusColor = failColor; break;
                case ProcessState.CompleteCalled:
                    CompleteApiDetail = failText; CompleteApiStatusColor = failColor; break;
                default:
                    StartApiDetail = failText; StartApiStatusColor = failColor;
                    GetSfcKeyApiDetail = failText; GetSfcKeyApiStatusColor = failColor;
                    AddSfcKeyApiDetail = failText; AddSfcKeyApiStatusColor = failColor;
                    DataCollectApiDetail = failText; DataCollectApiStatusColor = failColor;
                    CompleteApiDetail = failText; CompleteApiStatusColor = failColor;
                    break;
            }
        }

        private void UpdateAssemblyStatus(ProcessStatus status)
        {
            switch (status.CurrentState)
            {
                case ProcessState.Idle:
                    AssemblyStatusText = "等待中";
                    AssemblyStatusIcon = "\xE7BA";
                    AssemblyStatusDescription = "等待扫码完成";
                    SfcKeyValidationText = "等待校验";
                    SfcKeyValidationIcon = "\xE946";
                    SfcKeyValidationForeground = "#FF7F8C8D";
                    SfcKeyValidationBackground = "#FFECF0F1";
                    SfcKeyValidationBorder = "#FFBDC3C7";
                    break;
                case ProcessState.WaitingForD3003:
                case ProcessState.WaitingForScan2:
                case ProcessState.WaitingForD3005:
                case ProcessState.WaitingForD3007:
                    AssemblyStatusText = "处理中";
                    AssemblyStatusIcon = "\xE895";
                    AssemblyStatusDescription = "正在处理组装流程";
                    SfcKeyValidationText = "校验中...";
                    SfcKeyValidationIcon = "\xE895";
                    SfcKeyValidationForeground = "#FFF39C12";
                    SfcKeyValidationBackground = "#FFFDF2E1";
                    SfcKeyValidationBorder = "#FFF39C12";
                    break;
                case ProcessState.Completed:
                    AssemblyStatusText = "可组装";
                    AssemblyStatusIcon = "\xE73E";
                    AssemblyStatusDescription = "组装流程完成";
                    SfcKeyValidationText = "校验通过";
                    SfcKeyValidationIcon = "\xE73E";
                    SfcKeyValidationForeground = "#FF27AE60";
                    SfcKeyValidationBackground = "#FFE8F8F0";
                    SfcKeyValidationBorder = "#FF27AE60";
                    _statsService.IncrementPass();
                    break;
                case ProcessState.Error:
                    AssemblyStatusText = "不可组装";
                    AssemblyStatusIcon = "\xEA39";
                    AssemblyStatusDescription = "组装流程异常";
                    SfcKeyValidationText = "校验失败";
                    SfcKeyValidationIcon = "\xEA39";
                    SfcKeyValidationForeground = "#FFE74C3C";
                    SfcKeyValidationBackground = "#FFFAE8E8";
                    SfcKeyValidationBorder = "#FFE74C3C";
                    _statsService.IncrementFail();
                    break;
            }
        }

        private void OnProcessError(object? sender, string error)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LastError = error;
                _statusMessage = $"错误: {error}";
                OnPropertyChanged(nameof(StatusMessage));
            });
        }

        private static string GetStateDescription(string state)
        {
            return state switch
            {
                "空闲" => "系统空闲",
                "等待扫码枪1" => "等待产品条码扫描",
                "调用Start接口" => "正在调用Start API",
                "等待D3003信号" => "等待PLC扫码信号",
                "等待扫码枪2" => "等待小件条码扫描",
                "调用GetSfcKey接口" => "正在获取SFC Key",
                "等待D3005信号" => "等待压合开始信号",
                "等待D3007信号" => "等待压合完成信号",
                "读取测试数据" => "正在读取压合参数",
                "调用AddSfcKey接口" => "正在绑定SFC Key",
                "调用TestDataCollect接口" => "正在上传测试数据",
                "调用Complete接口" => "正在完成流程",
                "流程完成" => "组装流程已完成",
                "错误" => "流程异常",
                _ => state
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _timer.Stop();
                    _mesHealthCheckClient.Dispose();
                    _plcService.OnLog -= OnServiceLog;
                    _scan枪Service.OnLog -= OnServiceLog;
                    _mesApiService.OnLog -= OnServiceLog;
                    _logService.OnLogAdded -= OnLogAdded;
                    _processController.OnProcessStateChanged -= OnProcessStateChanged;
                    _processController.OnError -= OnProcessError;

                    _processController.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
