using System;
using HslCommunication.Profinet.Omron;
using HslCommunication;

namespace ShotSkiMahiD.Services
{
    public class PlcService : IDisposable
    {
        private OmronFinsNet? _plc1;
        private OmronFinsNet? _plc2;
        private readonly Models.PlcConfig _plc1Config;
        private readonly Models.PlcConfig _plc2Config;
        private bool _disposed;
        private bool _isPlc1Connected;
        private bool _isPlc2Connected;

        public event EventHandler<string>? OnLog;
        public event EventHandler<bool>? OnConnectionChanged;

        public bool IsPlc1Connected => _isPlc1Connected;
        public bool IsPlc2Connected => _isPlc2Connected;

        public PlcService(Models.PlcConfig plc1Config, Models.PlcConfig plc2Config)
        {
            _plc1Config = plc1Config;
            _plc2Config = plc2Config;
        }

        public bool ConnectPlc1()
        {
            if (_isPlc1Connected && _plc1 != null)
            {
                Log("PLC1已连接，跳过重复连接");
                return true;
            }

            try
            {
                _plc1?.ConnectClose();
                _plc1 = new OmronFinsNet(_plc1Config.IP, _plc1Config.Port);
                var result = _plc1.ConnectServer();

                if (result.IsSuccess)
                {
                    _isPlc1Connected = true;
                    Log($"PLC1连接成功: {_plc1Config.IP}:{_plc1Config.Port}");
                    OnConnectionChanged?.Invoke(this, true);
                    return true;
                }
                else
                {
                    _isPlc1Connected = false;
                    Log($"PLC1连接失败: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _isPlc1Connected = false;
                Log($"PLC1连接异常: {ex.Message}");
                return false;
            }
        }

        public bool ConnectPlc2()
        {
            if (_isPlc2Connected && _plc2 != null)
            {
                Log("PLC2已连接，跳过重复连接");
                return true;
            }

            try
            {
                _plc2?.ConnectClose();
                _plc2 = new OmronFinsNet(_plc2Config.IP, _plc2Config.Port);
                var result = _plc2.ConnectServer();

                if (result.IsSuccess)
                {
                    _isPlc2Connected = true;
                    Log($"PLC2连接成功: {_plc2Config.IP}:{_plc2Config.Port}");
                    OnConnectionChanged?.Invoke(this, true);
                    return true;
                }
                else
                {
                    _isPlc2Connected = false;
                    Log($"PLC2连接失败: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _isPlc2Connected = false;
                Log($"PLC2连接异常: {ex.Message}");
                return false;
            }
        }

        public void DisconnectAll()
        {
            try
            {
                _plc1?.ConnectClose();
                _plc2?.ConnectClose();
                _isPlc1Connected = false;
                _isPlc2Connected = false;
                Log("所有PLC连接已关闭");
                OnConnectionChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                Log($"关闭PLC连接异常: {ex.Message}");
            }
        }

        public int ReadPlc1Register(string address)
        {
            if (_plc1 == null || !_isPlc1Connected)
            {
                Log("PLC1未连接，无法读取");
                return -1;
            }

            try
            {
                var result = _plc1.ReadInt16(address);
                if (result.IsSuccess)
                {
                    Log($"读取PLC1 [{address}] = {result.Content}");
                    return result.Content;
                }
                else
                {
                    Log($"读取PLC1 [{address}] 失败: {result.Message}");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Log($"读取PLC1 [{address}] 异常: {ex.Message}");
                return -1;
            }
        }

        public int ReadPlc2Register(string address)
        {
            if (_plc2 == null || !_isPlc2Connected)
            {
                Log("PLC2未连接，无法读取");
                return -1;
            }

            try
            {
                var result = _plc2.ReadInt16(address);
                if (result.IsSuccess)
                {
                    Log($"读取PLC2 [{address}] = {result.Content}");
                    return result.Content;
                }
                else
                {
                    Log($"读取PLC2 [{address}] 失败: {result.Message}");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Log($"读取PLC2 [{address}] 异常: {ex.Message}");
                return -1;
            }
        }

        public bool WritePlc1Register(string address, short value)
        {
            if (_plc1 == null || !_isPlc1Connected)
            {
                Log("PLC1未连接，无法写入");
                return false;
            }

            try
            {
                var result = _plc1.Write(address, value);
                if (result.IsSuccess)
                {
                    Log($"写入PLC1 [{address}] = {value} 成功");
                    return true;
                }
                else
                {
                    Log($"写入PLC1 [{address}] = {value} 失败: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"写入PLC1 [{address}] 异常: {ex.Message}");
                return false;
            }
        }

        public bool WritePlc2Register(string address, short value)
        {
            if (_plc2 == null || !_isPlc2Connected)
            {
                Log("PLC2未连接，无法写入");
                return false;
            }

            try
            {
                var result = _plc2.Write(address, value);
                if (result.IsSuccess)
                {
                    Log($"写入PLC2 [{address}] = {value} 成功");
                    return true;
                }
                else
                {
                    Log($"写入PLC2 [{address}] = {value} 失败: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"写入PLC2 [{address}] 异常: {ex.Message}");
                return false;
            }
        }

        private void Log(string message)
        {
            OnLog?.Invoke(this, $"[PLC] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DisconnectAll();
                _plc1?.Dispose();
                _plc2?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
