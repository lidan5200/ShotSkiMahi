using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShotSkiMahiD.Services
{
    public class ScanService : IDisposable
    {
        private TcpClient? _scan1Client;
        private TcpClient? _scan2Client;
        private NetworkStream? _scan1Stream;
        private NetworkStream? _scan2Stream;
        private readonly Models.ScanConfig _config;
        private CancellationTokenSource? _scan1Cts;
        private CancellationTokenSource? _scan2Cts;
        private bool _disposed;

        public event EventHandler<string>? OnLog;
        public event EventHandler<string>? OnScan1DataReceived;
        public event EventHandler<string>? OnScan2DataReceived;

        public bool IsScan1Connected => _scan1Client?.Connected ?? false;
        public bool IsScan2Connected => _scan2Client?.Connected ?? false;

        public ScanService(Models.ScanConfig config)
        {
            _config = config;
        }

        public Task<bool> ConnectScan1Async()
        {
            if (IsScan1Connected)
            {
                Log("扫码枪1已连接，跳过重复连接");
                return Task.FromResult(true);
            }

            return Task.Run(async () =>
            {
                try
                {
                    _scan1Cts?.Cancel();
                    _scan1Stream?.Close();
                    _scan1Client?.Close();

                    _scan1Client = new TcpClient();
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    await _scan1Client.ConnectAsync(_config.Scan1IP, _config.Scan1Port, cts.Token);
                    _scan1Stream = _scan1Client.GetStream();

                    Log($"扫码枪1连接成功: {_config.Scan1IP}:{_config.Scan1Port}");

                    _scan1Cts = new CancellationTokenSource();
                    _ = Task.Run(() => ListenScan1(_scan1Cts.Token));

                    return true;
                }
                catch (OperationCanceledException)
                {
                    Log($"扫码枪1连接超时: {_config.Scan1IP}:{_config.Scan1Port}");
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"扫码枪1连接失败: {ex.Message}");
                    return false;
                }
            });
        }

        public bool ConnectScan1()
        {
            return ConnectScan1Async().GetAwaiter().GetResult();
        }

        public Task<bool> ConnectScan2Async()
        {
            if (IsScan2Connected)
            {
                Log("扫码枪2已连接，跳过重复连接");
                return Task.FromResult(true);
            }

            return Task.Run(async () =>
            {
                try
                {
                    _scan2Cts?.Cancel();
                    _scan2Stream?.Close();
                    _scan2Client?.Close();

                    _scan2Client = new TcpClient();
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    await _scan2Client.ConnectAsync(_config.Scan2IP, _config.Scan2Port, cts.Token);
                    _scan2Stream = _scan2Client.GetStream();

                    Log($"扫码枪2连接成功: {_config.Scan2IP}:{_config.Scan2Port}");

                    _scan2Cts = new CancellationTokenSource();
                    _ = Task.Run(() => ListenScan2(_scan2Cts.Token));

                    return true;
                }
                catch (OperationCanceledException)
                {
                    Log($"扫码枪2连接超时: {_config.Scan2IP}:{_config.Scan2Port}");
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"扫码枪2连接失败: {ex.Message}");
                    return false;
                }
            });
        }

        public bool ConnectScan2()
        {
            return ConnectScan2Async().GetAwaiter().GetResult();
        }

        private void ListenScan1(CancellationToken token)
        {
            var buffer = new byte[1024];
            
            while (!token.IsCancellationRequested && _scan1Stream != null)
            {
                try
                {
                    if (_scan1Stream.DataAvailable)
                    {
                        var bytesRead = _scan1Stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            var data = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                            if (!string.IsNullOrEmpty(data))
                            {
                                Log($"扫码枪1收到数据: {data}");
                                OnScan1DataReceived?.Invoke(this, data);
                            }
                        }
                    }
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Log($"扫码枪1监听异常: {ex.Message}");
                    }
                    break;
                }
            }
        }

        private void ListenScan2(CancellationToken token)
        {
            var buffer = new byte[1024];
            
            while (!token.IsCancellationRequested && _scan2Stream != null)
            {
                try
                {
                    if (_scan2Stream.DataAvailable)
                    {
                        var bytesRead = _scan2Stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            var data = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                            if (!string.IsNullOrEmpty(data))
                            {
                                Log($"扫码枪2收到数据: {data}");
                                OnScan2DataReceived?.Invoke(this, data);
                            }
                        }
                    }
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Log($"扫码枪2监听异常: {ex.Message}");
                    }
                    break;
                }
            }
        }

        public bool SendCommandToScan2()
        {
            if (_scan2Stream == null || _scan2Client == null || !_scan2Client.Connected)
            {
                Log("扫码枪2未连接，无法发送命令");
                return false;
            }

            try
            {
                var command = _config.Send2Command + "\r\n";
                var data = Encoding.UTF8.GetBytes(command);
                _scan2Stream.Write(data, 0, data.Length);
                _scan2Stream.Flush();
                
                Log($"向扫码枪2发送命令: {command.Trim()}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"向扫码枪2发送命令异常: {ex.Message}");
                return false;
            }
        }

        public void DisconnectAll()
        {
            try
            {
                _scan1Cts?.Cancel();
                _scan2Cts?.Cancel();
                
                _scan1Stream?.Close();
                _scan2Stream?.Close();
                _scan1Client?.Close();
                _scan2Client?.Close();
                
                Log("所有扫码枪连接已关闭");
            }
            catch (Exception ex)
            {
                Log($"关闭扫码枪连接异常: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            OnLog?.Invoke(this, $"[扫码枪] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DisconnectAll();
                _scan1Cts?.Dispose();
                _scan2Cts?.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
