namespace YG
{
    public partial class SavesYG
    {
        public bool clickerInitialized;
        public double score;
        public int phaseIndex;
        public double hpLeft;
        public double pendingOverflow;
        public int[] clickCounts = new int[12];
        public int[] idleCounts = new int[12];
        public bool muted;
        public bool gameWon;
    }
}
