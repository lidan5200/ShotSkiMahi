using System;
using System.Collections.Generic;

namespace ShotSkiMahiD.Services
{
    public static class IniFileBL
    {
        public static Dictionary<string, string> ReadIniFile(string configFilePath)
        {
            Dictionary<string, string> dary = new Dictionary<string, string>();
            try
            {
                INIFile iNIFile = new INIFile(configFilePath);
                string Ver = iNIFile.IniReadValue("config", "Ver");//0
                string LoginUser = iNIFile.IniReadValue("config", "LoginUser");//1
                string LoginPassword = iNIFile.IniReadValue("config", "LoginPassword");//2
                string LoginID = iNIFile.IniReadValue("config", "LoginID");//3
                string Client = iNIFile.IniReadValue("config", "Client");//4
                string ClientID = iNIFile.IniReadValue("config", "ClientID");//5
                string Section = iNIFile.IniReadValue("config", "Section");//6
                string SectionID = iNIFile.IniReadValue("config", "SectionID");//7
                string Line = iNIFile.IniReadValue("config", "Line");//8
                string SchedulingID = iNIFile.IniReadValue("config", "SchedulingID");//9
                string SchedulingQty = iNIFile.IniReadValue("config", "SchedulingQty");//10
                string ShoporderQty = iNIFile.IniReadValue("config", "ShoporderQty");//11
                string PROJECT = iNIFile.IniReadValue("config", "PROJECT");//12
                string PRODUCT = iNIFile.IniReadValue("config", "PRODUCT");//13
                string Resource = iNIFile.IniReadValue("config", "Resource");//14
                string PROJECT_ID = iNIFile.IniReadValue("config", "PROJECT_ID");//15
                string PRODUCT_ID = iNIFile.IniReadValue("config", "PRODUCT_ID");//16
                string SHOPORDER_ID = iNIFile.IniReadValue("config", "SHOPORDER_ID");//17
                string Load_ID = iNIFile.IniReadValue("config", "Load_ID");//18
                string SapShoporder = iNIFile.IniReadValue("config", "SapShoporder");//19
                string Operation = iNIFile.IniReadValue("config", "Operation");//20
                string StationID = iNIFile.IniReadValue("config", "StationID");//21
                string FristStation = iNIFile.IniReadValue("config", "FristStation");//22
                string TraceStationId = iNIFile.IniReadValue("config", "TraceStationId");//23
                string Remark = iNIFile.IniReadValue("config", "Remark");//23
                dary.Add("Ver", Ver);//23
                dary.Add("LoginUser", LoginUser);//24
                dary.Add("LoginPassword", LoginPassword);//25
                dary.Add("LoginID", LoginID);//26
                dary.Add("Client", Client);//27
                dary.Add("ClientID", ClientID);//28
                dary.Add("Section", Section);//29
                dary.Add("SectionID", SectionID);//30
                dary.Add("Line", Line);//31
                dary.Add("SchedulingID", SchedulingID);//32
                dary.Add("SchedulingQty", SchedulingQty);//33
                dary.Add("ShoporderQty", ShoporderQty);//34
                dary.Add("PROJECT", PROJECT);//35
                dary.Add("PRODUCT", PRODUCT);//36
                dary.Add("Resource", Resource);//37
                dary.Add("PROJECT_ID", PROJECT_ID);//38
                dary.Add("PRODUCT_ID", PRODUCT_ID);//39
                dary.Add("SHOPORDER_ID", SHOPORDER_ID);//40
                dary.Add("Load_ID", Load_ID);//41
                dary.Add("SapShoporder", SapShoporder);//42
                dary.Add("Operation", Operation);//43
                dary.Add("StationID", StationID);//44
                dary.Add("FristStation", FristStation);//
                dary.Add("TraceStationId", TraceStationId);//
                dary.Add("Remark", Remark);//
            }
            catch (Exception ex)
            {
            }
            return dary;
        }

        public static void UpdataIni(string configFilePath, string systemCaching, string T0, string value)
        {
            try
            {
                INIFile iNIFile = new INIFile(configFilePath);
                iNIFile.IniWriteValue(systemCaching, T0, value);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
