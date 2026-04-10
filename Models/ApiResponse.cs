namespace ShotSkiMahiD.Models
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class StartResponse : ApiResponse
    {
        public string SFC { get; set; } = string.Empty;
    }

    public class GetSfcKeyResponse : ApiResponse
    {
        public string Key { get; set; } = string.Empty;
    }

    public class TestDataCollectResponse : ApiResponse
    {
        public int TestValue1 { get; set; }
        public int TestValue2 { get; set; }
    }
}
