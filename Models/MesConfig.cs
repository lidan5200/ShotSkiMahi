namespace ShotSkiMahiD.Models
{
    public class MesConfig
    {
        public SystemSection System { get; set; } = new();
        public ConfigSection Config { get; set; } = new();
    }

    public class SystemSection
    {
        public string JSONURL { get; set; } = "http://10.6.78.14/Service.action";
        public string Factory { get; set; } = "AMF";
        public string URLIP { get; set; } = "10.6.78.14;10.6.79.99:81";
        public string ShareIP { get; set; } = "10.6.78.14;10.6.78.14";
        public string FtpIP { get; set; } = "10.6.79.99;10.6.79.99";
        public string CloudIP { get; set; } = "10.6.78.78;10.6.78.78";
        public string DownloadType { get; set; } = "FTP";
        public string RunEXE { get; set; } = "test.exe";
        public string IP { get; set; } = ";";
    }

    public class ConfigSection
    {
        public int LogKeepDays { get; set; } = 3;
        public string LoginUser { get; set; } = "4404194";
        public string LoginPassword { get; set; } = "";
        public string LoginID { get; set; } = "5581087";
        public string Client { get; set; } = "AMF";
        public int ClientID { get; set; } = 1;
        public string Section { get; set; } = "组装";
        public int SectionID { get; set; } = 2;
        public string Line { get; set; } = "F5-1F-Ano-B";
        public string Project { get; set; } = "Coriander";
        public string Product { get; set; } = "16700716-00";
        public string ClientPN { get; set; } = "NULL";
        public string Resource { get; set; } = "";
        public int ProjectID { get; set; } = 761;
        public int ProductID { get; set; } = 2171;
        public int ShopOrderID { get; set; } = 5330;
        public int SchedulingID { get; set; } = 8478;
        public int LoadID { get; set; } = -1;
        public int SchedulingQty { get; set; } = 20000000;
        public int ShoporderQty { get; set; } = 20000000;
        public int StationID { get; set; } = 11003;
        public string ModelID { get; set; } = "";
        public int FirstStation { get; set; } = 3;
        public string Operation { get; set; } = "CMM组装";
        public string SapShoporder { get; set; } = "";
        public string Remark { get; set; } = "barcode-transfer";
        public string TraceStationId { get; set; } = "";
    }
}
