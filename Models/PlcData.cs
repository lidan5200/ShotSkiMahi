namespace ShotSkiMahiD.Models
{
    public class PlcData
    {
        public int D3000 { get; set; }
        public int D3003 { get; set; }
        public int D3005 { get; set; }
        public int D3007 { get; set; }
        public int D3010 { get; set; }
        public int D3105 { get; set; }
        public int D3107 { get; set; }
    }

    public class PlcRegisterAddress
    {
        public const string D3000 = "D3000";
        public const string D3003 = "D3003";
        public const string D3005 = "D3005";
        public const string D3007 = "D3007";
        public const string D3010 = "D3010";
        public const string D3105 = "D3105";
        public const string D3107 = "D3107";
    }

    public class PlcWriteValue
    {
        public const int Pass = 1;
        public const int Fail = 2;
    }
}
