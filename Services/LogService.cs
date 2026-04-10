using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using ShotSkiMahiD.Models;

namespace ShotSkiMahiD.Services
{
    public class LogService : IDisposable
    {
        private readonly string _logDirectory;
        private readonly int _keepDays;
        private readonly object _lockObject = new();
        private readonly Timer? _cleanTimer;
        private bool _disposed;

        // 内存中保留最近的结构化日志（最多5000条）
        private readonly ConcurrentQueue<LocalLogEntry> _apiLogs = new();
        private const int MaxInMemoryLogs = 5000;

        // 批量写入优化相关
        private readonly ConcurrentQueue<LocalLogEntry> _apiLogBuffer = new();
        private const int BufferFlushThreshold = 50;
        private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(5);
        private Timer? _flushTimer;
        private readonly SemaphoreSlim _flushLock = new(1, 1);

        // 日志统计信息
        private int _totalApiCalls;
        private int _successCount;
        private int _failCount;
        private int _exceptionCount;
        private long _totalElapsedTimeMs;

        public event EventHandler<string>? OnLogAdded;
        public event EventHandler<LocalLogEntry>? OnApiLogAdded;
        public event EventHandler<LogStatisticsEventArgs>? OnStatisticsUpdated;

        /// <summary>
        /// 默认构造：日志保存7天
        /// </summary>
        public LogService(string logDirectory, int keepDays = 7)
        {
            _logDirectory = logDirectory;
            _keepDays = keepDays;

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // 启动时立即清理一次，之后每天清理
            _cleanTimer = new Timer(CleanOldLogs, null, TimeSpan.Zero, TimeSpan.FromHours(24));

            // 启动日志批量写入定时器
            _flushTimer = new Timer(async _ => await FlushApiLogBufferAsync(), 
                                   null, _flushInterval, _flushInterval);

            LogSystemInfo("日志系统初始化完成", logDirectory, keepDays);
        }

        #region 日志级别定义

        // 日志级别（按重要性排序）
        public const string LEVEL_DEBUG = "DEBUG";    // 调试信息：详细的请求/响应数据
        public const string LEVEL_INFO = "INFO";      // 一般信息：正常操作流程
        public const string LEVEL_WARNING = "WARN";    // 警告：潜在问题或非致命错误
        public const string LEVEL_ERROR = "ERROR";     // 错误：操作失败但系统可继续
        public const string LEVEL_FATAL = "FATAL";     // 致命错误：需要立即处理

        #endregion

        #region 基础日志方法（增强版）

        /// <summary>
        /// 记录日志 - 核心方法（带完整上下文）
        /// </summary>
        public void Log(string message, string category = LEVEL_INFO, 
            string? source = null, Exception? exception = null)
        {
            var timestamp = DateTime.Now;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            
            var logEntry = BuildLogLine(timestamp, category, message, source, threadId);
            
            OnLogAdded?.Invoke(this, logEntry);
            
            WriteToSystemFile(logEntry);

            // ERROR及以上级别追加异常详情到同一日志文件
            if ((category == LEVEL_ERROR || category == LEVEL_FATAL) && exception != null)
            {
                var errorDetail = new StringBuilder();
                errorDetail.AppendLine($"  异常类型: {exception.GetType().FullName}");
                errorDetail.AppendLine($"  异常消息: {exception.Message}");
                errorDetail.AppendLine($"  堆栈跟踪:");
                errorDetail.AppendLine(exception.StackTrace);
                
                if (exception.InnerException != null)
                {
                    errorDetail.AppendLine($"  内部异常: {exception.InnerException.GetType().FullName}: {exception.InnerException.Message}");
                }
                
                WriteToSystemFile(errorDetail.ToString());
            }
        }

        /// <summary>
        /// 构建格式化的日志行
        /// </summary>
        private string BuildLogLine(DateTime timestamp, string level, string message, 
            string? source, int threadId)
        {
            var sb = new StringBuilder();
            sb.Append($"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{level,-5}] ");
            
            if (!string.IsNullOrEmpty(source))
            {
                sb.Append($"[{source}] ");
            }
            
            sb.Append($"[T-{threadId:D4}] ");
            sb.Append(message);
            
            return sb.ToString();
        }

        // 便捷方法（增强版）
        public void LogInfo(string message, string? source = null) => Log(message, LEVEL_INFO, source);
        public void LogWarning(string message, string? source = null) => Log(message, LEVEL_WARNING, source);
        public void LogError(string message, string? source = null, Exception? ex = null) => Log(message, LEVEL_ERROR, source, ex);
        public void LogDebug(string message, string? source = null) => Log(message, LEVEL_DEBUG, source);
        public void LogFatal(string message, string? source = null, Exception? ex = null) => Log(message, LEVEL_FATAL, source, ex);

        /// <summary>
        /// 记录业务流程状态变更
        /// </summary>
        public void LogProcessStateChange(string processName, string fromState, string toState, 
            string? detail = null)
        {
            var message = $"[{processName}] 状态变更: {fromState} → {toState}";
            if (!string.IsNullOrEmpty(detail))
            {
                message += $" | 详情: {detail}";
            }
            
            LogInfo(message, "STATE_MACHINE");
        }

        /// <summary>
        /// 记录PLC通信事件
        /// </summary>
        public void LogPlcEvent(string plcName, string operation, bool success, 
            string address = "", object? value = null)
        {
            var status = success ? "成功" : "失败";
            var valueStr = value != null ? $"= {value}" : "";
            var message = $"[{plcName}] {operation} [{address}]{valueStr} - {status}";
            
            if (success)
            {
                LogDebug(message, "PLC");
            }
            else
            {
                LogWarning(message, "PLC");
            }
        }

        /// <summary>
        /// 记录扫码枪事件
        /// </summary>
        public void LogScanEvent(string scannerName, string data, bool isValid, 
            string? validationError = null)
        {
            var status = isValid ? "有效" : "无效";
            var message = $"[{scannerName}] 扫码: {data} ({status})";
            
            if (isValid)
            {
                LogInfo(message, "SCANNER");
            }
            else
            {
                LogWarning($"{message} | 错误: {validationError}", "SCANNER");
            }
        }

        /// <summary>
        /// 记录配置变更事件
        /// </summary>
        public void LogConfigChange(string configType, string key, string oldValue, string newValue)
        {
            var message = $"[{configType}] 配置变更: {key} = '{oldValue}' → '{newValue}'";
            LogInfo(message, "CONFIG");
        }

        /// <summary>
        /// 记录系统启动/关闭事件
        /// </summary>
        public void LogSystemInfo(string action, params object[] args)
        {
            var message = $"{action}";
            if (args.Length > 0)
            {
                message += $" | 参数: {string.Join(", ", args.Select(a => a?.ToString() ?? "null"))}";
            }
            LogInfo(message, "SYSTEM");
        }

        #endregion

        #region 结构化 API 日志

        /// <summary>
        /// 记录一条完整的API调用日志（包含URL、请求、响应等全部信息）
        /// URL保持原始完整格式，不进行任何序列化处理
        /// </summary>
        public void LogApiCall(LocalLogEntry entry)
        {
            // 写入内存缓存
            EnqueueApiLog(entry);

            // 更新统计数据
            UpdateStatistics(entry);

            // 同时写入简短信息到系统日志（使用原始URL，不序列化）
            var shortMsg = FormatShortApiMessage(entry);
            var logLevel = GetLogLevelFromCategory(entry.Category);
            Log(shortMsg, logLevel, "API");

            // 将完整结构化日志写入独立文件（URL保持原样）
            WriteApiLogToFile(entry);

            // 触发UI更新事件
            OnApiLogAdded?.Invoke(this, entry);

            // 触发统计更新事件
            TriggerStatisticsEvent();
        }

        /// <summary>
        /// 格式化简短的API消息（用于系统日志）
        /// </summary>
        private string FormatShortApiMessage(LocalLogEntry entry)
        {
            var sb = new StringBuilder();
            sb.Append($"[{entry.ApiName}] ");
            sb.Append($"{entry.RequestMethod} ");
            
            // URL直接使用，不截断不序列化
            if (entry.FullUrl.Length > 200)
            {
                sb.Append($"{entry.FullUrl.Substring(0, 200)}...");
            }
            else
            {
                sb.Append(entry.FullUrl);
            }
            
            sb.Append(" | ");
            sb.Append(entry.IsSuccess ? "✓" : "✗");
            sb.Append($" {entry.ElapsedMs}ms");
            
            if (!entry.IsSuccess && !string.IsNullOrEmpty(entry.Message))
            {
                sb.Append($" | Error: {entry.Message}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 根据分类获取日志级别
        /// </summary>
        private string GetLogLevelFromCategory(string category)
        {
            return category switch
            {
                "API_SUCCESS" => LEVEL_INFO,
                "API_FAIL" => LEVEL_WARNING,
                "API_EXCEPTION" => LEVEL_ERROR,
                _ => LEVEL_INFO
            };
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics(LocalLogEntry entry)
        {
            Interlocked.Increment(ref _totalApiCalls);
            Interlocked.Add(ref _totalElapsedTimeMs, entry.ElapsedMs);

            switch (entry.Category)
            {
                case "API_SUCCESS":
                    Interlocked.Increment(ref _successCount);
                    break;
                case "API_FAIL":
                    Interlocked.Increment(ref _failCount);
                    break;
                case "API_EXCEPTION":
                    Interlocked.Increment(ref _exceptionCount);
                    break;
            }
        }

        /// <summary>
        /// 触发统计更新事件
        /// </summary>
        private void TriggerStatisticsEvent()
        {
            try
            {
                var stats = new LogStatisticsEventArgs
                {
                    TotalCalls = _totalApiCalls,
                    SuccessCount = _successCount,
                    FailCount = _failCount,
                    ExceptionCount = _exceptionCount,
                    TotalElapsedTimeMs = _totalElapsedTimeMs,
                    SuccessRate = _totalApiCalls > 0 ? (double)_successCount / _totalApiCalls * 100 : 0,
                    AverageElapsedTimeMs = _totalApiCalls > 0 ? (double)_totalElapsedTimeMs / _totalApiCalls : 0
                };

                OnStatisticsUpdated?.Invoke(this, stats);
            }
            catch
            {
                // 统计事件触发失败不影响主流程
            }
        }

        /// <summary>
        /// 获取内存中的API日志列表
        /// </summary>
        public List<LocalLogEntry> GetApiLogs(int? maxCount = null)
        {
            var list = _apiLogs.ToList();
            list.Reverse(); // 最新的在前面
            return maxCount.HasValue ? list.Take(maxCount.Value).ToList() : list;
        }

        /// <summary>
        /// 按条件筛选API日志
        /// </summary>
        public List<LocalLogEntry> FilterApiLogs(
            string? apiName = null,
            string? keyword = null,
            bool? isSuccess = null,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            var logs = new List<LocalLogEntry>(_apiLogs);

            if (startTime.HasValue || endTime.HasValue)
            {
                var rangeStart = startTime ?? DateTime.MinValue;
                var rangeEnd = endTime ?? DateTime.MaxValue;
                var current = rangeStart.Date;
                while (current <= rangeEnd.Date)
                {
                    try
                    {
                        var fileLogs = LoadApiLogsFromFile(current);
                        foreach (var fileEntry in fileLogs)
                        {
                            if (!logs.Any(l => l.Timestamp == fileEntry.Timestamp && 
                                l.ApiName == fileEntry.ApiName && 
                                l.FullUrl == fileEntry.FullUrl))
                                logs.Add(fileEntry);
                        }
                    }
                    catch { }
                    current = current.AddDays(1);
                }
            }

            if (!string.IsNullOrEmpty(apiName))
            {
                logs = logs.Where(l => l.ApiName.Contains(apiName, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                logs = logs.Where(l =>
                    l.FullUrl.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    l.RequestBody.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    l.ResponseBody.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    l.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            if (isSuccess.HasValue)
            {
                logs = logs.Where(l => l.IsSuccess == isSuccess.Value).ToList();
            }
            if (startTime.HasValue)
            {
                logs = logs.Where(l => l.Timestamp >= startTime.Value).ToList();
            }
            if (endTime.HasValue)
                logs = logs.Where(l => l.Timestamp <= endTime.Value).ToList();

            logs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            return logs;
        }

        /// <summary>
        /// 从文件加载指定日期的API日志
        /// </summary>
        public List<LocalLogEntry> LoadApiLogsFromFile(DateTime date)
        {
            var fileName = $"ApiLog_{date:yyyyMMdd}.json";
            var filePath = Path.Combine(_logDirectory, fileName);

            if (!File.Exists(filePath))
                return new List<LocalLogEntry>();

            try
            {
                var content = File.ReadAllText(filePath);
                var trimmed = content.Trim();

                if (trimmed.StartsWith("["))
                {
                    var arrayMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\[[\s\S]*?\](?=\s*(?:\{|$))");
                    if (arrayMatch.Success)
                    {
                        try
                        {
                            var arrayResult = JsonConvert.DeserializeObject<List<LocalLogEntry>>(arrayMatch.Value);
                            if (arrayResult != null && arrayResult.Count > 0)
                            {
                                var result = new List<LocalLogEntry>(arrayResult);
                                var afterArray = trimmed.Substring(arrayMatch.Index + arrayMatch.Length).Trim();
                                if (!string.IsNullOrEmpty(afterArray))
                                {
                                    foreach (var line in SplitJsonLines(afterArray))
                                    {
                                        try
                                        {
                                            var entry = JsonConvert.DeserializeObject<LocalLogEntry>(line);
                                            if (entry != null) result.Add(entry);
                                        }
                                        catch { }
                                    }
                                }
                                return result;
                            }
                        }
                        catch { }
                    }

                    var lines = SplitJsonLines(trimmed);
                    var fallback = new List<LocalLogEntry>();
                    foreach (var line in lines)
                    {
                        try
                        {
                            var entry = JsonConvert.DeserializeObject<LocalLogEntry>(line);
                            if (entry != null) fallback.Add(entry);
                        }
                        catch { }
                    }
                    return fallback;
                }

                var allLines = SplitJsonLines(content);
                var logs = new List<LocalLogEntry>();
                foreach (var line in allLines)
                {
                    try
                    {
                        var entry = JsonConvert.DeserializeObject<LocalLogEntry>(line);
                        if (entry != null) logs.Add(entry);
                    }
                    catch { }
                }
                return logs;
            }
            catch
            {
                return new List<LocalLogEntry>();
            }
        }

        private static string[] SplitJsonLines(string text)
        {
            var results = new List<string>();
            var depth = 0;
            var start = 0;

            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '{') depth++;
                else if (ch == '}') depth--;

                if (depth == 0 && (ch == '}' || ch == '\n'))
                {
                    var segment = text.Substring(start, i - start + 1).Trim();
                    if (segment.StartsWith("{") && segment.EndsWith("}"))
                        results.Add(segment);
                    start = i + 1;
                }
            }

            if (start < text.Length)
            {
                var remaining = text.Substring(start).Trim();
                if (remaining.StartsWith("{") && remaining.EndsWith("}"))
                    results.Add(remaining);
            }

            return results.ToArray();
        }

        #endregion

        #region 文件写入方法

        private void EnqueueApiLog(LocalLogEntry entry)
        {
            _apiLogs.Enqueue(entry);
            
            while (_apiLogs.Count > MaxInMemoryLogs && _apiLogs.TryDequeue(out _)) { }
        }

        private void WriteToSystemFile(string logEntry)
        {
            try
            {
                lock (_lockObject)
                {
                    var fileName = $"Log_{DateTime.Now:yyyyMMdd}.txt";
                    var filePath = Path.Combine(_logDirectory, fileName);

                    File.AppendAllText(filePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入系统日志文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取最近N条系统日志（从文件加载）
        /// </summary>
        public List<string> GetRecentSystemLogs(int maxCount = 100)
        {
            var results = new List<string>();
            try
            {
                if (!Directory.Exists(_logDirectory))
                    return results;

                var files = Directory.GetFiles(_logDirectory, "Log_*.txt")
                    .OrderByDescending(f => f)
                    .Take(3);

                foreach (var file in files)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines.Reverse())
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                results.Insert(0, line);
                                if (results.Count >= maxCount)
                                    return results;
                            }
                        }
                    }
                    catch { }
                }

                results.Reverse();
            }
            catch { }

            return results;
        }

        private void WriteApiLogToFile(LocalLogEntry entry)
        {
            try
            {
                _apiLogBuffer.Enqueue(entry);

                if (_apiLogBuffer.Count >= BufferFlushThreshold)
                {
                    _ = FlushApiLogBufferAsync(waitForLock: true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入API日志缓冲区失败: {ex.Message}");
            }
        }

        private async Task FlushApiLogBufferAsync(bool waitForLock = false)
        {
            if (!_flushLock.Wait(waitForLock ? TimeSpan.FromSeconds(10) : TimeSpan.Zero))
                return;

            try
            {
                var batch = new List<LocalLogEntry>();

                while (_apiLogBuffer.TryDequeue(out var entry))
                {
                    batch.Add(entry);
                }

                if (batch.Count == 0)
                    return;

                var fileName = $"ApiLog_{DateTime.Now:yyyyMMdd}.json";
                var filePath = Path.Combine(_logDirectory, fileName);

                await Task.Run(() =>
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            var fileInfo = new FileInfo(filePath);
                            const long maxFileSize = 10 * 1024 * 1024; // 10MB
                            if (fileInfo.Length > maxFileSize)
                            {
                                var backupPath = filePath.Replace(".json", "_old.json");
                                File.Move(filePath, backupPath, overwrite: true);
                                LogWarning($"API日志文件过大({fileInfo.Length / 1024 / 1024}MB)，已备份", "LOG_SYSTEM");
                            }
                        }

                        // 使用追加模式写入，每条日志占一行JSON
                        using var writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
                        foreach (var entry in batch)
                        {
                            // 序列化时保留FullUrl的原始格式，不进行任何转义或修改
                            var jsonLine = JsonConvert.SerializeObject(entry, new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                Formatting = Formatting.None
                            });
                            writer.WriteLine(jsonLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"批量写入API日志失败: {ex.Message}", "LOG_SYSTEM", ex);
                    }
                });
            }
            finally
            {
                _flushLock.Release();
            }
        }

        private void CleanOldLogs(object? state)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-_keepDays);

                CleanFilesByPattern("Log_*.txt", cutoffDate);
                CleanFilesByPattern("ApiLog_*.json", cutoffDate);
                
                LogInfo($"日志清理完成，保留最近{_keepDays}天数据", "LOG_SYSTEM");
            }
            catch (Exception ex)
            {
                LogError($"清理日志文件失败: {ex.Message}", "LOG_SYSTEM", ex);
            }
        }

        private void CleanFilesByPattern(string pattern, DateTime cutoffDate)
        {
            var files = Directory.GetFiles(_logDirectory, pattern);
            string prefix = pattern.Split('*')[0];

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith(prefix) && fileName.Length >= prefix.Length + 8)
                {
                    var dateStr = fileName.Substring(prefix.Length, 8);
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                        System.Globalization.DateTimeStyles.None, out var fileDate))
                    {
                        if (fileDate < cutoffDate)
                        {
                            File.Delete(file);
                            LogDebug($"已删除过期日志: {fileName}", "LOG_SYSTEM");
                        }
                    }
                }
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    LogInfo("日志系统正在关闭...", "SYSTEM");
                    FlushApiLogBufferAsync().GetAwaiter().GetResult();
                    _flushTimer?.Dispose();
                    _flushLock.Dispose();

                    // 输出最终统计
                    LogInfo($"日志系统关闭统计: 总调用={_totalApiCalls}, 成功={_successCount}, 失败={_failCount}, 异常={_exceptionCount}", "SYSTEM");
                }
                catch
                {
                }

                _cleanTimer?.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    /// 日志统计信息事件参数
    /// </summary>
    public class LogStatisticsEventArgs : EventArgs
    {
        public int TotalCalls { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public int ExceptionCount { get; set; }
        public long TotalElapsedTimeMs { get; set; }
        public double SuccessRate { get; set; }
        public double AverageElapsedTimeMs { get; set; }
    }

    /// <summary>
    /// 本地日志条目（用于结构化日志存储和查询）
    /// FullUrl字段保持原始完整格式，不进行任何序列化处理
    /// </summary>
    public class LocalLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ApiName { get; set; } = string.Empty;
        
        /// <summary>
        /// 完整的请求URL - 保持原始格式，不经过任何序列化或转义处理
        /// 可直接复制到浏览器测试
        /// </summary>
        public string FullUrl { get; set; } = string.Empty;
        
        public string RequestMethod { get; set; } = "GET";
        public string RequestBody { get; set; } = string.Empty;
        public int? StatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public long ElapsedMs { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 生成详细的日志字符串（用于UI显示）
        /// URL保持原始格式
        /// </summary>
        public string ToDetailedString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"═══ {ApiName} ═══");
            sb.AppendLine($"时间: {Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"状态: {(IsSuccess ? "✓ 成功" : "✗ 失败")}");
            sb.AppendLine($"耗时: {ElapsedMs}ms");
            
            if (StatusCode.HasValue)
                sb.AppendLine($"HTTP状态码: {StatusCode}");
            
            if (!string.IsNullOrEmpty(FullUrl))
            {
                sb.AppendLine($"请求URL (原始格式):");
                sb.AppendLine(FullUrl);
            }
            
            if (!string.IsNullOrEmpty(RequestBody))
            {
                sb.AppendLine($"请求参数:");
                sb.AppendLine(RequestBody);
            }
            
            if (!string.IsNullOrEmpty(ResponseBody))
            {
                sb.AppendLine($"响应内容:");
                sb.AppendLine(ResponseBody);
            }
            
            if (!string.IsNullOrEmpty(Message))
            {
                sb.AppendLine($"消息: {Message}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 生成简短的摘要信息
        /// </summary>
        public string ToShortString()
        {
            var statusIcon = IsSuccess ? "✓" : "✗";
            return $"[{Timestamp:HH:mm:ss}] {statusIcon} {ApiName} | {ElapsedMs}ms";
        }
    }
}
