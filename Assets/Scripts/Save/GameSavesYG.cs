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
        public double score;
        public int phaseIndex;
        public double hpLeft = -1d;
        public double pendingOverflow;
        public bool muted;
        public bool gameWon;
        public List<UpgradeSave> upgrades = new List<UpgradeSave>();
    }
}
