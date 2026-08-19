using UnityEngine;

namespace Clicker
{
    public static class BalanceDefaults
    {
        public const int PhaseCount = 12;
        public const int UpgradeCount = 12;

        public static BalanceConfig CreateBalance()
        {
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            config.phaseCount = PhaseCount;
            config.hpBase = 120d;
            config.hpGrowth = 5d;
            config.baseClickPower = 1d;
            config.simulatedClicksPerSecond = 3d;
            config.targetPhaseSeconds = new[]
            {
                30d, 50d, 80d, 140d, 240d, 400d, 600d, 780d, 920d, 1320d, 1320d, 1320d
            };
            config.rewardedPercentByPhase = new[]
            {
                0.20f, 0.20f, 0.20f,
                0.15f, 0.15f, 0.15f,
                0.12f, 0.12f, 0.12f,
                0.10f, 0.10f, 0.10f
            };
            config.clickUpgrades = new[]
            {
                U("punch", "Удар", "Punch", 1d, 20d),
                U("combo", "Комбо", "Combo", 4d, 150d),
                U("heavy", "Тяжёлый", "Heavy", 18d, 1.2e3),
                U("burst", "Взрыв", "Burst", 80d, 1e4),
                U("storm", "Шторм", "Storm", 350d, 8e4),
                U("nova", "Нова", "Nova", 1.5e3, 6.5e5),
                U("overload", "Перегрузка", "Overload", 7e3, 5.5e6),
                U("breaker", "Крушитель", "Breaker", 3.2e4, 4.5e7),
                U("cataclysm", "Катаклизм", "Cataclysm", 1.5e5, 3.8e8),
                U("singularity", "Сингулярность", "Singularity", 7e5, 3.2e9),
                U("omega", "Омега", "Omega", 3.2e6, 2.8e10),
                U("transcend", "Трансценденция", "Transcend", 1.5e7, 2.4e11)
            };
            config.idleUpgrades = new[]
            {
                U("tick", "Тик", "Tick", 0.5d, 40d),
                U("flow", "Поток", "Flow", 2.5d, 300d),
                U("stream", "Струя", "Stream", 12d, 2.5e3),
                U("factory", "Фабрика", "Factory", 55d, 2e4),
                U("engine", "Мотор", "Engine", 250d, 1.6e5),
                U("core", "Ядро", "Core", 1.1e3, 1.3e6),
                U("reactor", "Реактор", "Reactor", 5e3, 1.1e7),
                U("dynamo", "Динамо", "Dynamo", 2.3e4, 9e7),
                U("orbit", "Орбита", "Orbit", 1.05e5, 7.5e8),
                U("pulsar", "Пульсар", "Pulsar", 4.8e5, 6.5e9),
                U("quasar", "Квазар", "Quasar", 2.2e6, 5.5e10),
                U("genesis", "Генезис", "Genesis", 1e7, 4.8e11)
            };
            return config;
        }

        public static EnemyCatalog CreateEnemies()
        {
            var catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
            catalog.enemies = new[]
            {
                Enemy("scarlet", "Алый", "Scarlet", new Color(0.91f, 0.36f, 0.30f)),
                Enemy("azure", "Лазурный", "Azure", new Color(0.30f, 0.80f, 0.77f)),
                Enemy("amber", "Янтарный", "Amber", new Color(0.96f, 0.83f, 0.37f))
            };
            return catalog;
        }

        public static DialogCatalog CreateDialogs()
        {
            var catalog = ScriptableObject.CreateInstance<DialogCatalog>();
            catalog.phaseLines = new[]
            {
                L("Лишь разминка. Дальше будет интереснее.", "Just a warm-up. It gets better."),
                L("Один упал — двое смотрят. Не зевай.", "One is down — two are watching. Stay sharp."),
                L("Круг замкнулся. Теперь по-настоящему.", "The circle is closed. Now it gets real."),
                L("Вторая кожа толще. Бей сильнее.", "The second hide is thicker. Hit harder."),
                L("Они учатся. Ты — тоже.", "They are learning. So are you."),
                L("Половина пути по кругу. Не отпускай ритм.", "Halfway around the circle. Keep the rhythm."),
                L("Третья стадия. Каждый удар звенит громче.", "Third stage. Every hit rings louder."),
                L("Пассив копится. Пусть работает за тебя.", "Idle is stacking. Let it work for you."),
                L("Ещё один круг — и финал рядом.", "One more lap — the finale is close."),
                L("Последняя тройка. Здесь решается всё.", "The last trio. This is where it is decided."),
                L("Предпоследний удар судьбы. Не останавливайся.", "The penultimate blow. Do not stop."),
                L("Последняя полоска. Дожми.", "The last bar. Finish it.")
            };
            catalog.victory = L(
                "Все противники пали. Вы прошли игру!",
                "All opponents have fallen. You finished the game!");
            return catalog;
        }

        static UpgradeDef U(string id, string ru, string en, double power, double cost)
        {
            return new UpgradeDef
            {
                id = id,
                nameRu = ru,
                nameEn = en,
                powerPerCopy = power,
                baseCost = cost,
                costMult = 1.15d
            };
        }

        static EnemyDef Enemy(string id, string ru, string en, Color color)
        {
            return new EnemyDef
            {
                id = id,
                nameRu = ru,
                nameEn = en,
                placeholderColor = color,
                stageSprites = new Sprite[4]
            };
        }

        static DialogLine L(string ru, string en)
        {
            return new DialogLine { ru = ru, en = en };
        }
    }
}
