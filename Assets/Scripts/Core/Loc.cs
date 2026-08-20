using YG;

namespace Clicker
{
    public static class Loc
    {
        public static bool UseEnglish
        {
            get
            {
                string lang = YG2.lang;
                if (string.IsNullOrEmpty(lang))
                    return false;

                switch (lang)
                {
                    case "ru":
                    case "be":
                    case "kk":
                    case "uk":
                    case "uz":
                        return false;
                    default:
                        return true;
                }
            }
        }

        public static string T(string ru, string en) => UseEnglish ? en : ru;

        public static string Score => T("Очки", "Score");
        public static string ClickPower => T("Сила клика", "Click power");
        public static string IdlePower => T("Пассив", "Idle");
        public static string PerSecond => T("/с", "/s");
        public static string Shop => T("Улучшения", "Upgrades");
        public static string TabClick => T("Клик", "Click");
        public static string TabIdle => T("Пассив", "Idle");
        public static string Buy => T("Купить", "Buy");
        public static string Locked => T("Следующий этап", "Next stage");
        public static string Maxed => T("Макс.", "Max");
        public static string Owned => T("куплено", "owned");
        public static string Continue => T("Далее", "Continue");
        public static string VictoryTitle => T("Победа!", "Victory!");
        public static string VictoryBody => T(
            "Все противники пали. Вы прошли игру!",
            "All opponents have fallen. You finished the game!");
        public static string MuteOn => T("Звук выкл.", "Sound off");
        public static string MuteOff => T("Звук вкл.", "Sound on");
        public static string Enemy => T("Противник", "Enemy");
        public static string Stage => T("Стадия", "Stage");
        public static string HowToPlay => T("Кликайте по центральному противнику. Очки наносят урон и тратятся на улучшения.", "Click the center opponent. Points deal damage and buy upgrades.");

        public static string RewardedButton(float percent)
        {
            string pct = NumberFormatter.FormatPercent(percent);
            return T($"−{pct} HP за рекламу", $"-{pct} HP for a video ad");
        }

        public static string PlusPower(double power, bool idle)
        {
            string value = NumberFormatter.Format(power);
            return idle ? $"+{value}{PerSecond}" : $"+{value}";
        }
    }
}
