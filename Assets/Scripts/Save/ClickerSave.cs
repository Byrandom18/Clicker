using System.Collections.Generic;
using YG;

namespace Clicker
{
    public static class ClickerSave
    {
        public const int SchemaVersion = 2;
        const double MaxLegitScore = 20000000d;
        const double MaxLegitHp = 10000000d;

        public static bool Sanitize()
        {
            if (YG2.saves == null)
                return false;
            if (!NeedsReset(YG2.saves))
                return false;
            ResetProgress();
            return true;
        }

        public static void ResetProgress()
        {
            if (YG2.saves == null)
                return;
            var s = YG2.saves;
            s.clickerInitialized = false;
            s.clickerSaveVersion = SchemaVersion;
            s.score = 0d;
            s.phaseIndex = 0;
            s.hpLeft = -1d;
            s.pendingOverflow = 0d;
            s.gameWon = false;
            s.upgrades = new List<UpgradeSave>();
        }

        static bool NeedsReset(SavesYG s)
        {
            if (s.clickerSaveVersion != SchemaVersion)
                return true;
            if (double.IsNaN(s.score) || double.IsInfinity(s.score) || s.score < 0d)
                return true;
            if (s.score > MaxLegitScore)
                return true;
            if (s.phaseIndex < 0 || s.phaseIndex > 12)
                return true;
            if (double.IsNaN(s.hpLeft) || double.IsInfinity(s.hpLeft) || s.hpLeft > MaxLegitHp)
                return true;
            if (double.IsNaN(s.pendingOverflow) || double.IsInfinity(s.pendingOverflow))
                return true;
            if (s.upgrades == null)
                return false;

            for (int i = 0; i < s.upgrades.Count; i++)
            {
                var row = s.upgrades[i];
                if (row != null && (row.count < 0 || row.count > 500))
                    return true;
            }

            return false;
        }
    }
}
