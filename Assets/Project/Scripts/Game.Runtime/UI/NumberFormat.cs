using System.Globalization;

namespace Game.Runtime.UI
{
    // 숫자 약식 표기(공용): 1,000↑ = K, 1,000,000↑ = M. 소수 1자리(끝 .0은 생략).
    //  예) 850→"850", 1000→"1K", 1500→"1.5K", 12000→"12K", 1250000→"1.3M".
    public static class NumberFormat
    {
        public static string Abbreviate(long value)
        {
            if (value < 0) return "-" + Abbreviate(-value);
            if (value < 1000) return value.ToString(CultureInfo.InvariantCulture);
            if (value < 1000000) return Trim(value / 1000.0) + "K";
            if (value < 1000000000) return Trim(value / 1000000.0) + "M";
            return Trim(value / 1000000000.0) + "B";
        }

        // 소수 1자리 + 끝 ".0" 생략(1.0K가 아니라 1K).
        private static string Trim(double d)
        {
            string s = d.ToString("0.0", CultureInfo.InvariantCulture);
            if (s.EndsWith(".0")) s = s.Substring(0, s.Length - 2);
            return s;
        }
    }
}
