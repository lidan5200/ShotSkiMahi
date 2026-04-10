using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ShotSkiMahiD.Services
{
    public class INIFile
    {
        public string path;

        public INIFile(string INIPath)
        {
            path = INIPath;
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, byte[] retVal, int size, string filePath);

        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, path);
        }

        public string IniReadValue(string Section, string Key)
        {
            StringBuilder stringBuilder = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", stringBuilder, 255, path);
            return stringBuilder.ToString();
        }

        public byte[] IniReadValues(string section, string key)
        {
            byte[] array = new byte[255];
            GetPrivateProfileString(section, key, "", array, 255, path);
            return array;
        }

        public void ClearAllSection()
        {
            WritePrivateProfileString(string.Empty, string.Empty, string.Empty, path);
        }

        public void ClearSection(string Section)
        {
            WritePrivateProfileString(Section, string.Empty, string.Empty, path);
        }
    }
}
