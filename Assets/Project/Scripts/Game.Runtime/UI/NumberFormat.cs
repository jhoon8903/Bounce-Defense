using System.Globalization;

namespace Game.Runtime.UI
{
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

        private static string Trim(double d)
        {
            string s = d.ToString("0.0", CultureInfo.InvariantCulture);
            if (s.EndsWith(".0")) s = s.Substring(0, s.Length - 2);
            return s;
        }
    }
}
