using UnityEngine;
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
        public static string IdlePower => T("Авто", "Auto");
        public static string PerSecond => T("/с", "/s");
        public static string Shop => T("УЛУЧШЕНИЯ", "UPGRADES");
        public static string TabClick => T("Клик", "Click");
        public static string TabIdle => T("Авто", "Auto");
        public static string Buy => T("КУПИТЬ", "BUY");
        public static string Locked => T("Разблокируйте предыдущее", "Unlock the previous one");
        public static string Owned => T("куплено", "owned");
        public static string Continue => T("Далее", "Continue");
        public static string ContinueEndless => T("Продолжить", "Continue");
        public static string VictoryTitle => T("Победа!", "Victory!");
        public static string VictoryBody => T(
            "Порча спала. Ведьмы побеждены, удача снова твоя. Можешь идти или остаться и бить их дальше.",
            "The hex is broken. The witches are spent, and the luck is yours again. You can leave or stay and keep knocking them down.");
        public static string MegaAttack => T("МЕГА-АТАКА ЗА РЕКЛАМУ", "MEGA ATTACK FOR AN AD");
        public static string SmartAutoUpgrade => T(
            "АКТИВИРОВАТЬ УМНУЮ АВТОПРОКАЧКУ ЗА РЕКЛАМУ",
            "ACTIVATE SMART AUTO-UPGRADE FOR AN AD");

        public static string AutoUpgradeLeft(float secondsLeft)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
            return T($"ОСТАЛОСЬ: {seconds}", $"LEFT: {seconds}");
        }
        public static string UpgradeClickPower => T("Сила клика", "Click power");
        public static string UpgradeIdlePower => T("Автоматически", "Automatically");

        public static string PlusPower(double power, bool idle)
        {
            string value = NumberFormatter.Format(power);
            string label = idle ? UpgradeIdlePower : UpgradeClickPower;
            return idle ? $"{label} +{value}{PerSecond}" : $"{label} +{value}";
        }
    }
}
