using UnityEngine;

namespace Clicker
{
    public static class BalanceDefaults
    {
        public const int PhaseCount = 12;
        public const int ShopCount = 24;

        public static readonly double[] TargetSeconds =
        {
            30d, 50d, 80d, 140d, 240d, 400d, 600d, 780d, 920d, 1320d, 1320d, 1320d
        };

        public static BalanceConfig CreateBalance()
        {
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            config.phaseCount = PhaseCount;
            config.hpBase = 120d;
            config.hpGrowth = 5d;
            config.baseClickPower = 1d;
            config.simulatedClicksPerSecond = 3d;
            config.targetPhaseSeconds = (double[])TargetSeconds.Clone();
            config.rewardedPercentByPhase = new[]
            {
                0.20f, 0.20f, 0.20f,
                0.15f, 0.15f, 0.15f,
                0.12f, 0.12f, 0.12f,
                0.10f, 0.10f, 0.10f
            };
            config.shop = new[]
            {
                Click("punch", "Удар", "Punch", 0.25d, 15d),
                Idle("tick", "Тик", "Tick", 0.20d, 22d),
                Click("combo", "Комбо", "Combo", 0.75d, 40d),
                Idle("flow", "Поток", "Flow", 0.90d, 55d),
                Click("heavy", "Тяжёлый", "Heavy", 2.20d, 120d),
                Idle("stream", "Струя", "Stream", 3.00d, 170d),
                Click("burst", "Взрыв", "Burst", 3.00d, 340d),
                Idle("factory", "Фабрика", "Factory", 4.50d, 480d),
                Click("storm", "Шторм", "Storm", 6.00d, 950d),
                Idle("engine", "Мотор", "Engine", 9.00d, 1350d),
                Click("nova", "Нова", "Nova", 14d, 2800d),
                Idle("core", "Ядро", "Core", 22d, 3900d),
                Click("overload", "Перегрузка", "Overload", 38d, 9000d),
                Idle("reactor", "Реактор", "Reactor", 62d, 12500d),
                Click("breaker", "Крушитель", "Breaker", 140d, 34000d),
                Idle("dynamo", "Динамо", "Dynamo", 240d, 48000d),
                Click("cataclysm", "Катаклизм", "Cataclysm", 700d, 1.5e5),
                Idle("orbit", "Орбита", "Orbit", 1200d, 2.1e5),
                Click("singularity", "Сингулярность", "Singularity", 2500d, 5.2e5),
                Idle("pulsar", "Пульсар", "Pulsar", 4500d, 7.3e5),
                Click("omega", "Омега", "Omega", 15000d, 2.6e6),
                Idle("quasar", "Квазар", "Quasar", 28000d, 3.6e6),
                Click("transcend", "Трансценденция", "Transcend", 60000d, 1.4e7),
                Idle("genesis", "Генезис", "Genesis", 120000d, 2.0e7)
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

        static UpgradeDef Click(string id, string ru, string en, double power, double cost)
        {
            return U(id, ru, en, false, power, cost);
        }

        static UpgradeDef Idle(string id, string ru, string en, double power, double cost)
        {
            return U(id, ru, en, true, power, cost);
        }

        static UpgradeDef U(string id, string ru, string en, bool idle, double power, double cost)
        {
            return new UpgradeDef
            {
                id = id,
                nameRu = ru,
                nameEn = en,
                isIdle = idle,
                powerPerCopy = power,
                baseCost = cost,
                costMult = 1.18d,
                maxCopies = 12
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
