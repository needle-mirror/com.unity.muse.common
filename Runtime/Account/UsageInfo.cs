using System;
using System.Globalization;

namespace Unity.Muse.Common.Account
{
    [Serializable]
    class UsageInfo
    {
        public int used;
        public int total;

        static NumberFormatInfo s_Separator = new() {NumberDecimalDigits = 0, NumberGroupSeparator = ","};
        public string Label => $"{used.ToString("N", s_Separator)} / {total.ToString("N", s_Separator)}";
        public float Progress => total == 0 ? 0 : (float) used / total;
        public bool CanExceed => DateTime.Now <= new DateTime(2024, 01, 15, 0, 0, 0, DateTimeKind.Local);
    }
}
