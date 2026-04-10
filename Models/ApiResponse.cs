using Newtonsoft.Json;

namespace ShotSkiMahiD.Models
{
    public class ApiResponse
    {
        [JsonProperty("SUCCESS")]
        public bool Success { get; set; } = true;

        [JsonProperty("MESSAGE")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("RESULT")]
        public string Result { get; set; } = string.Empty;

        [JsonProperty("CODE")]
        public int? Code { get; set; }

        [JsonProperty("SERVICE_RUNTIME")]
        public int? ServiceRuntime { get; set; }

        [JsonProperty("IP_ADDRESS")]
        public string? IpAddress { get; set; }

        [JsonProperty("TIME")]
        public string? Time { get; set; }

        [JsonProperty("SERVER_IP")]
        public string? ServerIp { get; set; }

        [JsonProperty("DATA")]
        public object? Data { get; set; }
    }

    public class StartResponse : ApiResponse
    {
        [JsonProperty("SFC")]
        public string SFC { get; set; } = string.Empty;
    }

    public class GetSfcKeyResponse : ApiResponse
    {
        [JsonProperty("KEY")]
        public string Key { get; set; } = string.Empty;
    }

    public class TestDataCollectResponse : ApiResponse
    {
        [JsonProperty("TEST_VALUE1")]
        public int TestValue1 { get; set; }

        [JsonProperty("TEST_VALUE2")]
        public int TestValue2 { get; set; }
    }
}
