using UnityEngine;

namespace Clicker
{
    public static class BalanceDefaults
    {
        public const int PhaseCount = 15;
        public const int UpgradeCount = 12;
        public const double EndlessHpMult = 1.15d;

        public static readonly double[] PhaseHp =
        {
            200d, 3153d, 17834.5d, 50782.5d, 122338d, 235506.25d,
            663669.25d, 1391510.25d, 2390320d, 8126706.5d, 10688701.75d, 12871729d,
            31248362.75d, 38198008.5d, 44074261.75d
        };

        public static readonly double[] TargetPhaseSeconds =
        {
            15d, 30d, 45d, 60d, 90d, 120d, 240d, 360d, 480d,
            1200d, 1200d, 1200d, 2400d, 2400d, 2400d
        };

        public static readonly float[] RewardedPercent =
        {
            0.40f, 0.40f, 0.40f,
            0.25f, 0.25f, 0.25f,
            0.18f, 0.18f, 0.18f,
            0.10f, 0.10f, 0.10f,
            0.05f, 0.05f, 0.05f
        };

        public static void ApplyTo(BalanceConfig config)
        {
            if (config == null)
                return;

            config.phaseCount = PhaseCount;
            config.baseClickPower = 1d;
            config.simulatedClicksPerSecond = 3d;
            config.tweenDuration = 0.6f;
            config.endlessHpMult = EndlessHpMult;
            config.phaseHp = (double[])PhaseHp.Clone();
            config.targetPhaseSeconds = (double[])TargetPhaseSeconds.Clone();
            config.rewardedPercentByPhase = (float[])RewardedPercent.Clone();
        }

        public static UpgradeSpec[] ClickSpecs()
        {
            return new[]
            {
                Spec("click_00_punch", "Удар", "Punch", 1d, 15d),
                Spec("click_01_combo", "Серия", "Combo", 2d, 130d),
                Spec("click_02_heavy", "Тяжёлый", "Heavy", 3d, 310d),
                Spec("click_03_burst", "Залп", "Burst", 4d, 720d),
                Spec("click_04_storm", "Шторм", "Storm", 5d, 1420d),
                Spec("click_05_nova", "Нова", "Nova", 6d, 2390d),
                Spec("click_06_overload", "Перегрузка", "Overload", 7d, 3640d),
                Spec("click_07_breaker", "Ломатель", "Breaker", 8d, 5300d),
                Spec("click_08_cataclysm", "Катаклизм", "Cataclysm", 9d, 7520d),
                Spec("click_09_singularity", "Сингулярность", "Singularity", 10d, 10300d),
                Spec("click_10_omega", "Омега", "Omega", 11d, 13600d),
                Spec("click_11_transcend", "Трансценденция", "Transcend", 12d, 17000d)
            };
        }

        public static UpgradeSpec[] IdleSpecs()
        {
            return new[]
            {
                Spec("idle_00_tick", "Тик", "Tick", 0.4d, 25d),
                Spec("idle_01_flow", "Поток", "Flow", 0.8d, 210d),
                Spec("idle_02_stream", "Струя", "Stream", 1.2d, 500d),
                Spec("idle_03_factory", "Фабрика", "Factory", 1.6d, 1200d),
                Spec("idle_04_engine", "Мотор", "Engine", 2.0d, 2400d),
                Spec("idle_05_core", "Ядро", "Core", 2.4d, 4000d),
                Spec("idle_06_reactor", "Реактор", "Reactor", 2.8d, 6200d),
                Spec("idle_07_dynamo", "Динамо", "Dynamo", 3.2d, 9000d),
                Spec("idle_08_orbit", "Орбита", "Orbit", 3.6d, 12800d),
                Spec("idle_09_pulsar", "Пульсар", "Pulsar", 4.0d, 17500d),
                Spec("idle_10_quasar", "Квазар", "Quasar", 4.4d, 23000d),
                Spec("idle_11_genesis", "Генезис", "Genesis", 4.8d, 29000d)
            };
        }

        public static void FillUpgrade(UpgradeDef def, UpgradeSpec spec, UpgradeKind kind, UpgradeDef requires)
        {
            def.id = spec.id;
            def.kind = kind;
            def.nameRu = spec.nameRu;
            def.nameEn = spec.nameEn;
            def.powerPerCopy = spec.power;
            def.baseCost = spec.baseCost;
            def.costMult = 1.15d;
            def.requires = requires;
        }

        public static void FillEnemy(EnemyDef def, string id, string ru, string en, Color color)
        {
            def.id = id;
            def.nameRu = ru;
            def.nameEn = en;
            def.placeholderColor = color;
        }

        public static void FillDialogs(DialogCatalog catalog)
        {
            catalog.phaseLines = new[]
            {
                Line("Это всё, на что ты способен?", "Is that all you've got?"),
                Line("Щекотно. Почти.", "Ticklish. Almost."),
                Line("Неплохо. Для разминки.", "Not bad. For a warm-up."),
                Line("Так, теперь я серьёзно.", "Alright, now I'm serious."),
                Line("Ты ещё здесь? Упрямо.", "Still here? Stubborn."),
                Line("Очки не равны победе.", "Points aren't victory."),
                Line("Мы можем делать это часами.", "We can do this for hours."),
                Line("Поздняя игра только начинается.", "The late game is just starting."),
                Line("Держись. Дальше будет дольше.", "Hang on. It only gets longer."),
                Line("Двадцать минут? Я подожду.", "Twenty minutes? I'll wait."),
                Line("Предпоследний раунд. Не зевай.", "Second to last. Stay sharp."),
                Line("Финал. Бей, пока я стою.", "Finale. Hit me while I'm standing."),
                Line("Последняя форма. Это ещё не конец.", "Last form. This isn't over."),
                Line("Дальше уже некуда. Почти.", "Nowhere left to go. Almost."),
                Line("После этого — только бесконечность.", "After this, only endless remains.")
            };
            catalog.victory = Line(
                "Все противники пали. Вы прошли игру!",
                "All opponents have fallen. You finished the game!");
        }

        static UpgradeSpec Spec(string id, string ru, string en, double power, double cost)
        {
            return new UpgradeSpec { id = id, nameRu = ru, nameEn = en, power = power, baseCost = cost };
        }

        static DialogLine Line(string ru, string en)
        {
            return new DialogLine { ru = ru, en = en };
        }

        public struct UpgradeSpec
        {
            public string id;
            public string nameRu;
            public string nameEn;
            public double power;
            public double baseCost;
        }
    }
}
