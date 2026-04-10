using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ShotSkiMahiD.Models;

namespace ShotSkiMahiD.Services
{
    public class MesApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private Models.MesConfig _mesConfig;
        private readonly Models.ApiConfig _apiConfig;
        private readonly Models.SystemConfig _systemConfig;
        private LogService? _logService;
        private bool _disposed;

        public event EventHandler<string>? OnLog;

        public MesApiService(Models.MesConfig mesConfig, Models.ApiConfig apiConfig, Models.SystemConfig systemConfig)
        {
            _mesConfig = mesConfig;
            _apiConfig = apiConfig;
            _systemConfig = systemConfig;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public void SetLogService(LogService logService)
        {
            _logService = logService;
        }

        public void SubscribeConfigChanges(ConfigService configService)
        {
            configService.OnMesConfigChanged += OnMesConfigChanged;
        }

        private void OnMesConfigChanged(Models.MesConfig newConfig)
        {
            _mesConfig = newConfig;
            LogInfo("MES配置已实时更新");
        }

        #region API 方法

        /// <summary>
        /// Start接口（GET请求，param={JSON}格式）
        /// </summary>
        public async Task<Models.ApiResponse> StartAsync(string sfc)
        {
            if (!_apiConfig.StartEnabled)
            {
                LogWarning("Start接口未启用");
                return new Models.ApiResponse { Success = false, Message = "Start接口未启用" };
            }

            var sw = Stopwatch.StartNew();
            var paramObject = new Dictionary<string, string>
            {
                { "LOGIN_ID", _mesConfig.Config.LoginID },
                { "SFC", sfc },
                { "STATION_ID", _mesConfig.Config.StationID.ToString() },
                { "LINE", _mesConfig.Config.Line },
                { "SHOPORDER", _mesConfig.Config.Resource },
                { "SCHEDULING_ID", _mesConfig.Config.SchedulingID.ToString() },
                { "CLIENT_ID", _mesConfig.Config.ClientID.ToString() }
            };

            var (originalUrl, requestUrl) = BuildUrls("Start", paramObject);

            try
            {
                LogDebug($"[Start] 原始URL: {originalUrl}");

                var (response, statusCode, responseBody) = await ExecuteGetRequest(requestUrl);
                sw.Stop();

                LogApiCallComplete("Start", originalUrl, paramObject, response, statusCode, responseBody, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogApiCallError("Start", originalUrl, paramObject, ex, sw.ElapsedMilliseconds);
                return new Models.ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// GetSfcKey接口（GET请求，param={JSON}格式）
        /// </summary>
        public async Task<Models.ApiResponse> GetSfcKeyAsync(string magnetCode)
        {
            if (!_apiConfig.GetSfcKeyEnabled)
            {
                LogWarning("GetSfcKey接口未启用");
                return new Models.ApiResponse { Success = false, Message = "GetSfcKey接口未启用" };
            }

            var sw = Stopwatch.StartNew();
            var paramObject = new Dictionary<string, string>
            {
                { "LOGIN_ID", _mesConfig.Config.LoginID },
                { "SFC", magnetCode },
                { "CLIENT_ID", _mesConfig.Config.ClientID.ToString() },
                { "STATION", _mesConfig.Config.Operation },
                { "SHOPORDER", _mesConfig.Config.Resource }
            };

            var (originalUrl, requestUrl) = BuildUrls("GetSfcKey", paramObject);

            try
            {
                LogDebug($"[GetSfcKey] 原始URL: {originalUrl}");

                var (response, statusCode, responseBody) = await ExecuteGetRequest(requestUrl);
                sw.Stop();

                LogApiCallComplete("GetSfcKey", originalUrl, paramObject, response, statusCode, responseBody, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogApiCallError("GetSfcKey", originalUrl, paramObject, ex, sw.ElapsedMilliseconds);
                return new Models.ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// AddSfcKey接口（GET请求，param={JSON}格式）
        /// </summary>
        public async Task<Models.ApiResponse> AddSfcKeyAsync(string sfc, string magnetCode)
        {
            if (!_apiConfig.AddSfcKeyEnabled)
            {
                LogWarning("AddSfcKey接口未启用");
                return new Models.ApiResponse { Success = false, Message = "AddSfcKey接口未启用" };
            }

            var sw = Stopwatch.StartNew();
            var paramObject = new Dictionary<string, string>
            {
                { "LOGIN_ID", _mesConfig.Config.LoginID },
                { "CLIENT_ID", _mesConfig.Config.ClientID.ToString() },
                { "SFC", sfc },
                { "STATION_ID", _mesConfig.Config.StationID.ToString() },
                { "STATION_NAME", _mesConfig.Config.Operation },
                { "SHOPORDER", _mesConfig.Config.Resource },
                { "DATA_NAME", _systemConfig.MagnetConfig.RMName },
                { "DATA_VALUE", magnetCode },
                { "PROJECT_ID", _mesConfig.Config.ProjectID.ToString() },
                { "PRODUCT_ID", _mesConfig.Config.ProductID.ToString() }
            };

            var (originalUrl, requestUrl) = BuildUrls("AddSfcKey", paramObject);

            try
            {
                LogDebug($"[AddSfcKey] 原始URL: {originalUrl}");

                var (response, statusCode, responseBody) = await ExecuteGetRequest(requestUrl);
                sw.Stop();

                LogApiCallComplete("AddSfcKey", originalUrl, paramObject, response, statusCode, responseBody, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogApiCallError("AddSfcKey", originalUrl, paramObject, ex, sw.ElapsedMilliseconds);
                return new Models.ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// TestDataCollect2MainChild接口（GET请求，param={JSON}格式）
        /// </summary>
        public async Task<Models.ApiResponse> TestDataCollect2MainChildAsync(string sfc, int pressTime, int pressTemp)
        {
            if (!_apiConfig.TestDataCollect2MainChildEnabled)
            {
                LogWarning("TestDataCollect2MainChild接口未启用");
                return new Models.ApiResponse { Success = false, Message = "TestDataCollect2MainChild接口未启用" };
            }

            var sw = Stopwatch.StartNew();
            var paramObject = new
            {
                LOGIN_ID = _mesConfig.Config.LoginID,
                CLIENT_ID = _mesConfig.Config.ClientID.ToString(),
                SN = sfc,
                PRODUCT_NAME = _mesConfig.Config.Product,
                SHOPORDER_NO = _mesConfig.Config.Resource,
                PROJECT_NAME = _mesConfig.Config.Project,
                TDS_NAME = "LOKI",
                STARTIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                STATION_ID = _mesConfig.Config.StationID.ToString(),
                LINE_NO = _mesConfig.Config.Line,
                TEST_STATION = _mesConfig.Config.Operation,
                FIXTURE_NO = _mesConfig.Config.TraceStationId,
                TEST_RESULT = "PASS",
                TEST_DATA_LIST = new[]
                {
                    new { NAME = "压合时间", VALUE = pressTime.ToString() },
                    new { NAME = "压合温度", VALUE = pressTemp.ToString() }
                }
            };

            var (originalUrl, requestUrl) = BuildUrlsFromObject("TestDataCollect2MainChild", paramObject);

            try
            {
                LogDebug($"[TestDataCollect2MainChild] 原始URL: {originalUrl}");

                var (response, statusCode, responseBody) = await ExecuteGetRequest(requestUrl);
                sw.Stop();

                LogApiCallCompleteJson("TestDataCollect2MainChild", originalUrl, paramObject, response, statusCode, responseBody, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogApiCallErrorJson("TestDataCollect2MainChild", originalUrl, paramObject, ex, sw.ElapsedMilliseconds);
                return new Models.ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Complete接口（GET请求，param={JSON}格式）
        /// </summary>
        public async Task<Models.ApiResponse> CompleteAsync(string sfc)
        {
            if (!_apiConfig.CompleteEnabled)
            {
                LogWarning("Complete接口未启用");
                return new Models.ApiResponse { Success = false, Message = "Complete接口未启用" };
            }

            var sw = Stopwatch.StartNew();
            var paramObject = new Dictionary<string, string>
            {
                { "LOGIN_ID", _mesConfig.Config.LoginID },
                { "CLIENT_ID", _mesConfig.Config.ClientID.ToString() },
                { "SFC", sfc },
                { "SCHEDULING_ID", _mesConfig.Config.SchedulingID.ToString() },
                { "STATION_ID", _mesConfig.Config.StationID.ToString() }
            };

            var (originalUrl, requestUrl) = BuildUrls("Complete", paramObject);

            try
            {
                LogDebug($"[Complete] 原始URL: {originalUrl}");

                var (response, statusCode, responseBody) = await ExecuteGetRequest(requestUrl);
                sw.Stop();

                LogApiCallComplete("Complete", originalUrl, paramObject, response, statusCode, responseBody, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogApiCallError("Complete", originalUrl, paramObject, ex, sw.ElapsedMilliseconds);
                return new Models.ApiResponse { Success = false, Message = ex.Message };
            }
        }

        #endregion

        #region URL构建（原始URL + 请求URL双轨制）

        /// <summary>
        /// 构建原始URL和请求URL（Dictionary参数）
        /// 原始URL：保持JSON原始格式，用于日志记录
        /// 请求URL：URL编码格式，用于HTTP请求
        /// </summary>
        private (string originalUrl, string requestUrl) BuildUrls(string action, Dictionary<string, string> queryParams)
        {
            var baseUrl = _mesConfig.System.JSONURL;
            var jsonParam = JsonConvert.SerializeObject(queryParams, Formatting.None);

            // 原始URL：不编码，保持可读性，用于日志
            var originalUrl = $"{baseUrl}?method={action}&param={jsonParam}";

            // 请求URL：URL编码，用于HTTP请求
            var encodedParam = Uri.EscapeDataString(jsonParam);
            var requestUrl = $"{baseUrl}?method={action}&param={encodedParam}";

            return (originalUrl, requestUrl);
        }

        /// <summary>
        /// 构建原始URL和请求URL（匿名对象参数）
        /// </summary>
        private (string originalUrl, string requestUrl) BuildUrlsFromObject(string action, object paramObject)
        {
            var baseUrl = _mesConfig.System.JSONURL;
            var jsonParam = JsonConvert.SerializeObject(paramObject, Formatting.None);

            var originalUrl = $"{baseUrl}?method={action}&param={jsonParam}";

            var encodedParam = Uri.EscapeDataString(jsonParam);
            var requestUrl = $"{baseUrl}?method={action}&param={encodedParam}";

            return (originalUrl, requestUrl);
        }

        #endregion

        #region HTTP请求执行

        private async Task<(Models.ApiResponse response, int? statusCode, string responseBody)> ExecuteGetRequest(string requestUrl)
        {
            var response = await _httpClient.GetAsync(requestUrl);
            var statusCode = (int)response.StatusCode;

            string responseBody;
            if (response.IsSuccessStatusCode || response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync();
            }
            else
            {
                responseBody = $"HTTP {statusCode}: {response.ReasonPhrase}";
            }

            Models.ApiResponse apiResponse;
            if (!string.IsNullOrEmpty(responseBody))
            {
                try
                {
                    apiResponse = JsonConvert.DeserializeObject<Models.ApiResponse>(responseBody)
                        ?? new Models.ApiResponse { Success = false, Message = "响应解析失败" };
                }
                catch
                {
                    apiResponse = new Models.ApiResponse { Success = false, Message = responseBody };
                }
            }
            else
            {
                apiResponse = new Models.ApiResponse
                {
                    Success = false,
                    Message = $"HTTP请求失败: {response.StatusCode}"
                };
            }

            return (apiResponse, statusCode, responseBody);
        }

        #endregion

        #region 日志记录

        private void LogApiCallComplete(
            string apiName, string originalUrl, object paramObject,
            Models.ApiResponse apiResponse, int? statusCode, string responseBody, long elapsedMs)
        {
            var entry = new LocalLogEntry
            {
                Timestamp = DateTime.Now,
                Category = apiResponse.Success ? "API_SUCCESS" : "API_FAIL",
                ApiName = apiName,
                FullUrl = originalUrl,
                RequestMethod = "GET",
                RequestBody = JsonConvert.SerializeObject(paramObject, Formatting.Indented),
                StatusCode = statusCode,
                ResponseBody = FormatBody(responseBody),
                ElapsedMs = elapsedMs,
                IsSuccess = apiResponse.Success,
                Message = apiResponse.Message
            };

            _logService?.LogApiCall(entry);

            if (apiResponse.Success)
                LogInfo($"[{apiName}] ✓ 成功 | 耗时={elapsedMs}ms | Result={apiResponse.Result}");
            else
                LogError($"[{apiName}] ✗ 失败 | 耗时={elapsedMs}ms | Error={apiResponse.Message}");
        }

        private void LogApiCallCompleteJson(
            string apiName, string originalUrl, object paramObject,
            Models.ApiResponse apiResponse, int? statusCode, string responseBody, long elapsedMs)
        {
            var entry = new LocalLogEntry
            {
                Timestamp = DateTime.Now,
                Category = apiResponse.Success ? "API_SUCCESS" : "API_FAIL",
                ApiName = apiName,
                FullUrl = originalUrl,
                RequestMethod = "GET",
                RequestBody = JsonConvert.SerializeObject(paramObject, Formatting.Indented),
                StatusCode = statusCode,
                ResponseBody = FormatBody(responseBody),
                ElapsedMs = elapsedMs,
                IsSuccess = apiResponse.Success,
                Message = apiResponse.Message
            };

            _logService?.LogApiCall(entry);

            if (apiResponse.Success)
                LogInfo($"[{apiName}] ✓ 成功 | 耗时={elapsedMs}ms | Result={apiResponse.Result}");
            else
                LogError($"[{apiName}] ✗ 失败 | 耗时={elapsedMs}ms | Error={apiResponse.Message}");
        }

        private void LogApiCallError(
            string apiName, string originalUrl, object paramObject, Exception ex, long elapsedMs)
        {
            var entry = new LocalLogEntry
            {
                Timestamp = DateTime.Now,
                Category = "API_EXCEPTION",
                ApiName = apiName,
                FullUrl = originalUrl,
                RequestMethod = "GET",
                RequestBody = JsonConvert.SerializeObject(paramObject, Formatting.Indented),
                StatusCode = null,
                ResponseBody = $"{ex.GetType().Name}: {ex.Message}",
                ElapsedMs = elapsedMs,
                IsSuccess = false,
                Message = ex.Message
            };

            _logService?.LogApiCall(entry);

            LogError($"[{apiName}] ⚠ 异常 | 耗时={elapsedMs}ms | {ex.GetType().Name}: {ex.Message}");
            LogDebug($"[{apiName}] 异常URL: {originalUrl}");
            LogDebug($"[{apiName}] 异常堆栈:\n{ex.StackTrace}");
        }

        private void LogApiCallErrorJson(
            string apiName, string originalUrl, object paramObject, Exception ex, long elapsedMs)
        {
            var entry = new LocalLogEntry
            {
                Timestamp = DateTime.Now,
                Category = "API_EXCEPTION",
                ApiName = apiName,
                FullUrl = originalUrl,
                RequestMethod = "GET",
                RequestBody = JsonConvert.SerializeObject(paramObject, Formatting.Indented),
                StatusCode = null,
                ResponseBody = $"{ex.GetType().Name}: {ex.Message}",
                ElapsedMs = elapsedMs,
                IsSuccess = false,
                Message = ex.Message
            };

            _logService?.LogApiCall(entry);

            LogError($"[{apiName}] ⚠ 异常 | 耗时={elapsedMs}ms | {ex.GetType().Name}: {ex.Message}");
            LogDebug($"[{apiName}] 异常URL: {originalUrl}");
            LogDebug($"[{apiName}] 异常堆栈:\n{ex.StackTrace}");
        }

        private static string FormatBody(string body)
        {
            if (string.IsNullOrEmpty(body)) return "(空响应)";

            try
            {
                if (body.TrimStart().StartsWith("{") || body.TrimStart().StartsWith("["))
                {
                    var parsed = JsonConvert.DeserializeObject(body);
                    return JsonConvert.SerializeObject(parsed, Formatting.Indented);
                }
            }
            catch { }

            return body.Length > 2000 ? body.Substring(0, 2000) + "...(截断)" : body;
        }

        #endregion

        #region 日志辅助

        private void LogInfo(string message) => OnLog?.Invoke(this, $"[INFO] {DateTime.Now:HH:mm:ss} - {message}");
        private void LogWarning(string message) => OnLog?.Invoke(this, $"[WARN] {DateTime.Now:HH:mm:ss} - {message}");
        private void LogError(string message) => OnLog?.Invoke(this, $"[ERROR] {DateTime.Now:HH:mm:ss} - {message}");
        private void LogDebug(string message) => OnLog?.Invoke(this, $"[DEBUG] {DateTime.Now:HH:mm:ss.fff} - {message}");

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
