using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using ShotSkiMahiD.Models;
using ShotSkiMahiD.Validators;

namespace ShotSkiMahiD.Services
{
    public class ProcessController : IDisposable
    {
        private readonly PlcService _plcService;
        private readonly ScanService _scan枪Service;
        private readonly MesApiService _mesApiService;
        private readonly LogService _logService;
        private readonly ScanConfig _scanConfig;
        private readonly MagnetConfig _magnetConfig;
        
        private CancellationTokenSource? _monitoringCts;
        private ProcessStatus _currentProcess;
        private bool _disposed;

        public event EventHandler<ProcessStatus>? OnProcessStateChanged;
        public event EventHandler<string>? OnError;

        public ProcessStatus CurrentProcess => _currentProcess;

        public ProcessController(
            PlcService plcService,
            ScanService scan枪Service,
            MesApiService mesApiService,
            LogService logService,
            ScanConfig scanConfig,
            MagnetConfig magnetConfig)
        {
            _plcService = plcService;
            _scan枪Service = scan枪Service;
            _mesApiService = mesApiService;
            _logService = logService;
            _scanConfig = scanConfig;
            _magnetConfig = magnetConfig;

            _currentProcess = new ProcessStatus();
            
            _scan枪Service.OnScan1DataReceived += OnScan1DataReceived;
            _scan枪Service.OnScan2DataReceived += OnScan2DataReceived;
        }

        public void StartMonitoring()
        {
            _monitoringCts = new CancellationTokenSource();
            Task.Run(() => MonitorPlcSignals(_monitoringCts.Token));
            _logService.LogInfo("PLC信号监控已启动");
        }

        public void StopMonitoring()
        {
            _monitoringCts?.Cancel();
            _logService.LogInfo("PLC信号监控已停止");
        }

        private async Task MonitorPlcSignals(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_currentProcess.CurrentState == ProcessState.WaitingForD3003)
                    {
                        var d3003Value = _plcService.ReadPlc1Register(PlcRegisterAddress.D3003);
                        if (d3003Value == 1)
                        {
                            // D3003=0表示扫码枪1已完成扫描，触发扫码枪2
                            _plcService.WritePlc1Register(PlcRegisterAddress.D3003, 0);
                            _logService.LogInfo("检测到D3003=1，触发扫码枪2");
                            await TriggerScan2Async();
                        }
                    }

                    if (_currentProcess.CurrentState == ProcessState.WaitingForD3005)
                    {
                        var d3005Value = _plcService.ReadPlc2Register(PlcRegisterAddress.D3005);
                        if (d3005Value == 1)
                        {
                            _logService.LogInfo("检测到D3005=1，记录开始压合状态");
                            _plcService.WritePlc2Register(PlcRegisterAddress.D3005, 0);
                            UpdateProcessState(ProcessState.WaitingForD3007);
                        }
                    }

                    if (_currentProcess.CurrentState == ProcessState.WaitingForD3007)
                    {
                        var d3007Value = _plcService.ReadPlc2Register(PlcRegisterAddress.D3007);
                        if (d3007Value == 1)
                        {
                            _logService.LogInfo("检测到D3007=1，确认压合完成");
                            _plcService.WritePlc2Register(PlcRegisterAddress.D3007, 0);
                            await ReadTestDataAndCompleteAsync();
                        }
                    }

                    await Task.Delay(100, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logService.LogError($"监控PLC信号异常: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }

        private async void OnScan1DataReceived(object? sender, string sfc)
        {
            try
            {
                _logService.LogInfo($"收到扫码枪1数据: {sfc}");
                
                var validator = new SfcValidator(_scanConfig);
                var result = validator.Validate(sfc);
                
                if (!result.IsValid)
                {
                    var errors = string.Join(", ", result.Errors);
                    _logService.LogError($"SFC验证失败: {errors}");
                    OnError?.Invoke(this, $"SFC验证失败: {errors}");
                    return;
                }

                _currentProcess.CurrentSFC = sfc;
                _currentProcess.StartTime = DateTime.Now;
                _currentProcess.IsProcessing = true;
                
                UpdateProcessState(ProcessState.StartApiCalled);
                
                var startResponse = await _mesApiService.StartAsync(sfc);
                
                if (startResponse.Success && startResponse.Result == "PASS")
                {
                    _logService.LogInfo($"Start接口返回PASS，写入D3010=1");
                    _plcService.WritePlc1Register(PlcRegisterAddress.D3010, PlcWriteValue.Pass);
                    UpdateProcessState(ProcessState.WaitingForD3003);
                }
                else
                {
                    _logService.LogError($"Start接口返回FAIL: {startResponse.Message}");
                    _plcService.WritePlc1Register(PlcRegisterAddress.D3010, PlcWriteValue.Fail);
                    UpdateProcessState(ProcessState.Error);
                    ResetProcess();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"处理扫码枪1数据异常: {ex.Message}");
                UpdateProcessState(ProcessState.Error);
                OnError?.Invoke(this, ex.Message);
                ResetProcess();
            }
        }

        private async Task TriggerScan2Async()
        {
            try
            {
                UpdateProcessState(ProcessState.WaitingForScan2);
                await Task.Run(() => _scan枪Service.SendCommandToScan2());
            }
            catch (Exception ex)
            {
                _logService.LogError($"触发扫码枪2异常: {ex.Message}");
                UpdateProcessState(ProcessState.Error);
                OnError?.Invoke(this, ex.Message);
                ResetProcess();
            }
        }       
        private async void OnScan2DataReceived(object? sender, string magnetCode)
        {
            try
            {
                _logService.LogInfo($"收到扫码枪2数据: {magnetCode}");
                
                var validator = new MagnetCodeValidator(_scanConfig);
                var result = validator.Validate(magnetCode);
                
                if (!result.IsValid)
                {
                    var errors = string.Join(", ", result.Errors);
                    _logService.LogError($"磁铁码验证失败: {errors}");
                    OnError?.Invoke(this, $"磁铁码验证失败: {errors}");
                    return;
                }

                _currentProcess.CurrentMagnetCode = magnetCode;
                UpdateProcessState(ProcessState.GetSfcKeyCalled);
                
                var getSfcKeyResponse = await _mesApiService.GetSfcKeyAsync(magnetCode);
                
                if (getSfcKeyResponse.Success && getSfcKeyResponse.Result == "PASS")
                {
                    _logService.LogInfo($"GetSfcKey接口返回PASS，写入D3000=2");
                    _plcService.WritePlc1Register(PlcRegisterAddress.D3000, PlcWriteValue.Fail);
                    UpdateProcessState(ProcessState.Error);
                    ResetProcess();
                }
                else
                {
                    _logService.LogInfo($"GetSfcKey接口返回FAIL，写入D3000=1");
                    _plcService.WritePlc1Register(PlcRegisterAddress.D3000, PlcWriteValue.Pass);
                    UpdateProcessState(ProcessState.WaitingForD3005);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"处理扫码枪2数据异常: {ex.Message}");
                UpdateProcessState(ProcessState.Error);
                OnError?.Invoke(this, ex.Message);
                ResetProcess();
            }
        }

        private async Task ReadTestDataAndCompleteAsync()
        {
            try
            {
                UpdateProcessState(ProcessState.ReadingTestData);
                
                var testValue1 = _plcService.ReadPlc2Register(PlcRegisterAddress.D3105);
                var testValue2 = _plcService.ReadPlc2Register(PlcRegisterAddress.D3107);
                
                _currentProcess.TestValue1 = testValue1;
                _currentProcess.TestValue2 = testValue2;
                
                _logService.LogInfo($"读取测试数据: D3105={testValue1}, D3107={testValue2}");
                
                UpdateProcessState(ProcessState.AddSfcKeyCalled);
                var addSfcKeyResponse = await _mesApiService.AddSfcKeyAsync(
                    _currentProcess.CurrentSFC, 
                    _currentProcess.CurrentMagnetCode);
                
                if (!addSfcKeyResponse.Success)
                {
                    _logService.LogError($"AddSfcKey接口调用失败: {addSfcKeyResponse.Message}");
                    UpdateProcessState(ProcessState.Error);
                    OnError?.Invoke(this, addSfcKeyResponse.Message);
                    ResetProcess();
                    return;
                }
                
                UpdateProcessState(ProcessState.TestDataCollectCalled);
                var testDataResponse = await _mesApiService.TestDataCollect2MainChildAsync(
                    _currentProcess.CurrentSFC, testValue1, testValue2);
                
                if (!testDataResponse.Success)
                {
                    _logService.LogError($"TestDataCollect接口调用失败: {testDataResponse.Message}");
                    UpdateProcessState(ProcessState.Error);
                    OnError?.Invoke(this, testDataResponse.Message);
                    ResetProcess();
                    return;
                }
                
                UpdateProcessState(ProcessState.CompleteCalled);
                var completeResponse = await _mesApiService.CompleteAsync(_currentProcess.CurrentSFC);
                
                if (completeResponse.Success)
                {
                    _logService.LogInfo("业务流程完成");
                    UpdateProcessState(ProcessState.Completed);
                }
                else
                {
                    _logService.LogError($"Complete接口调用失败: {completeResponse.Message}");
                    UpdateProcessState(ProcessState.Error);
                    OnError?.Invoke(this, completeResponse.Message);
                }
                
                ResetProcess();
            }
            catch (Exception ex)
            {
                _logService.LogError($"读取测试数据并完成流程异常: {ex.Message}");
                UpdateProcessState(ProcessState.Error);
                OnError?.Invoke(this, ex.Message);
                ResetProcess();
            }
        }

        private void UpdateProcessState(ProcessState newState)
        {
            if (newState == ProcessState.Error)
                _currentProcess.FailedStep = _currentProcess.CurrentState;

            _currentProcess.CurrentState = newState;
            OnProcessStateChanged?.Invoke(this, _currentProcess);
            _logService.LogInfo($"流程状态变更: {newState}");
        }

        private void ResetProcess()
        {
            _currentProcess.EndTime = DateTime.Now;
            _currentProcess.IsProcessing = false;
            
            _currentProcess = new ProcessStatus();
            UpdateProcessState(ProcessState.Idle);
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
                    StopMonitoring();
                    _scan枪Service.OnScan1DataReceived -= OnScan1DataReceived;
                    _scan枪Service.OnScan2DataReceived -= OnScan2DataReceived;
                }

                _disposed = true;
            }
        }
    }
}
