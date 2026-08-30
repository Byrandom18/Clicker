using System;
using System.Collections.Generic;

namespace YG
{
    [Serializable]
    public class UpgradeSave
    {
        public string id;
        public int count;
    }

    public partial class SavesYG
    {
        public bool clickerInitialized;
        public int clickerSaveVersion;
        public double score;
        public int phaseIndex;
        public double hpLeft = -1d;
        public double pendingOverflow;
        public bool muted;
        public bool musicMuted;
        public float autoUpgradeLeft;
        public bool gameWon;
        public bool endlessMode;
        public List<UpgradeSave> upgrades = new List<UpgradeSave>();
    }
}
