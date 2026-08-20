using System;

namespace Clicker
{
    public static class NumberFormatter
    {
        static readonly string[] Suffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
        };

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "0";

            if (value < 0)
                return "-" + Format(-value);

            if (value < 1000d)
            {
                if (value >= 100d || Math.Abs(value - Math.Round(value)) < 0.05d)
                    return Math.Round(value).ToString("0");
                return value.ToString("0.0");
            }

            int group = 0;
            while (value >= 1000d && group < Suffixes.Length - 1)
            {
                value /= 1000d;
                group++;
            }

            if (group == Suffixes.Length - 1 && value >= 1000d)
                return (value * Math.Pow(1000d, group)).ToString("0.00e0");

            if (value >= 100d)
                return value.ToString("0") + Suffixes[group];
            if (value >= 10d)
                return value.ToString("0.0") + Suffixes[group];
            return value.ToString("0.00") + Suffixes[group];
        }

        public static string FormatPercent(float fraction)
        {
            return Math.Round(fraction * 100f).ToString("0") + "%";
        }
    }
}
