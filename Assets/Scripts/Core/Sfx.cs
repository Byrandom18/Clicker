namespace Clicker
{
    public static class Sfx
    {
        static SfxController C => SfxController.Instance;

        public static void Click() => C?.PlayClick();

        public static void Buy() => C?.PlayBuy();

        public static void Phase() => C?.PlayPhase();

        public static void Explosion() => C?.PlayExplosion();

        public static void Ui() => C?.PlayUi();
    }
}
