using System;
using System.Collections.Generic;
using System.IO;
using Doomscroll.Core.Services;

namespace Doomscroll.Core.Tests
{
    /// <summary>Shared fixtures: the shipped Data/balance.json and the saves used by the parity scenarios.</summary>
    internal static class TestData
    {
        private static readonly Lazy<string> BalanceJson = new(() =>
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "balance.json")));

        /// <summary>A fresh parse of the real Data/balance.json (safe to mutate per test).</summary>
        public static BalanceConfig Balance() => BalanceConfig.Parse(BalanceJson.Value);

        private static readonly Lazy<string> ReferenceJson = new(() =>
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "balance_reference.json")));

        /// <summary>
        /// Frozen pre-retune balance with the newer mechanics neutral (no blur penalty, no boredom, no raise...).
        /// Golden-value tests use it so retuning Data/balance.json never rewrites them.
        /// </summary>
        public static BalanceConfig Reference() => BalanceConfig.Parse(ReferenceJson.Value);

        public static SaveData FreshSave() => new();

        /// <summary>Mid-run save with most upgrades, 7 Mythics and 4 achievements (night 5).</summary>
        public static SaveData LoadedSave() => new()
        {
            CurrentDay = 5,
            RunUpgradeLevels = new Dictionary<string, int>
            {
                ["DOP_01"] = 3, ["DOP_02"] = 2, ["DOP_03"] = 2, ["DOP_04"] = 6, ["DOP_05"] = 2, ["DOP_06"] = 1,
                ["DOP_07"] = 3, ["DOP_08"] = 2, ["DOP_09"] = 3, ["DOP_10"] = 1, ["DOP_11"] = 1,
                ["HW_01"] = 4, ["HW_02"] = 1, ["HW_03"] = 1, ["HW_05"] = 1, ["HW_08"] = 1
            },
            GrassUpgradeLevels = new Dictionary<string, int>
            {
                ["GRASS_01"] = 3, ["GRASS_02"] = 1, ["GRASS_03"] = 2, ["GRASS_04"] = 1, ["GRASS_05"] = 2, ["GRASS_06"] = 1
            },
            UnlockedMythicIds = new HashSet<string> { "MYTHIC_0", "MYTHIC_1", "MYTHIC_2", "MYTHIC_3", "MYTHIC_4", "MYTHIC_5", "MYTHIC_6" },
            UnlockedAchievementIds = new HashSet<string> { "ACH_0", "ACH_2", "ACH_5", "ACH_6" }
        };

        /// <summary>Late save with a full phone farm, no surge strip and the x2 Mythic (night 29).</summary>
        public static SaveData FarmSave() => new()
        {
            CurrentDay = 29,
            RunUpgradeLevels = new Dictionary<string, int>
            {
                ["DOP_02"] = 1, ["DOP_04"] = 2, ["DOP_07"] = 2, ["HW_01"] = 6, ["HW_06"] = 1
            },
            GrassUpgradeLevels = new Dictionary<string, int> { ["GRASS_04"] = 2 },
            UnlockedMythicIds = new HashSet<string> { "MYTHIC_3", "MYTHIC_7" }
        };
    }
}
