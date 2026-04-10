namespace ShotSkiMahiD.Models
{
    public enum ProcessState
    {
        Idle,
        WaitingForScan1,
        StartApiCalled,
        WaitingForD3003,
        WaitingForScan2,
        GetSfcKeyCalled,
        WaitingForD3005,
        WaitingForD3007,
        ReadingTestData,
        AddSfcKeyCalled,
        TestDataCollectCalled,
        CompleteCalled,
        Completed,
        Error
    }

    public class ProcessStatus
    {
        public ProcessState CurrentState { get; set; } = ProcessState.Idle;
        public ProcessState? FailedStep { get; set; }
        public string CurrentSFC { get; set; } = string.Empty;
        public string CurrentMagnetCode { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string LastError { get; set; } = string.Empty;
        public bool IsProcessing { get; set; }
        public int TestValue1 { get; set; }
        public int TestValue2 { get; set; }
    }

    public class ScanData
    {
        public string SFC { get; set; } = string.Empty;
        public string MagnetCode { get; set; } = string.Empty;
        public DateTime ScanTime { get; set; }
        public bool IsValid { get; set; }
        public string ValidationError { get; set; } = string.Empty;
    }
}
