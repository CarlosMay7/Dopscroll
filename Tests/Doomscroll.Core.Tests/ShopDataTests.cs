using System.Linq;
using Doomscroll.Core.Events;
using Doomscroll.Core.Services;

namespace Doomscroll.Core.Tests
{
    /// <summary>PRYM shop data (categories, icons, effect wording) and the saved rack roles.</summary>
    public class ShopDataTests
    {
        [Fact]
        public void ShippedBalanceFile_FillsEveryShopTab()
        {
            var cfg = TestData.Balance();

            Assert.Equal(
                new[] { "DOP_01", "DOP_02", "DOP_03", "DOP_04", "DOP_05", "DOP_06", "DOP_08", "DOP_10", "DOP_11", "CRT_01", "DOP_09" },
                cfg.UpgradesIn(UpgradeCategory.Mind).Select(u => u.Id));
            Assert.Equal(new[] { "DOP_07", "HW_01" }, cfg.UpgradesIn(UpgradeCategory.Rack).Select(u => u.Id));
            Assert.Equal(new[] { "HW_02", "HW_03", "HW_06", "HW_07", "HW_08" }, cfg.UpgradesIn(UpgradeCategory.Desk).Select(u => u.Id));
            Assert.Equal(new[] { "HW_05", "SNK_01", "SNK_02" }, cfg.UpgradesIn(UpgradeCategory.Snacks).Select(u => u.Id));
            Assert.Equal(cfg.UpgradesFor(UpgradeCurrency.Grass), cfg.UpgradesIn(UpgradeCategory.Grass));
        }

        [Fact]
        public void Validate_ReportsMissingShopFields()
        {
            var cfg = TestData.Balance();
            var algo = cfg.GetUpgrade("DOP_04");
            algo.Name = "";
            algo.Icon = "rocket";
            algo.Category = null;
            cfg.GetUpgrade("HW_02").Category = UpgradeCategory.Grass;
            cfg.GetUpgrade("HW_03").UnlockNight = 0;

            var problems = cfg.Validate();

            Assert.Contains("upgrade DOP_04: name is missing", problems);
            Assert.Contains("upgrade DOP_04: unknown icon 'rocket'", problems);
            Assert.Contains("upgrade DOP_04: category is missing", problems);
            Assert.Contains("upgrade HW_02: grass upgrades (and only those) go in the grass category", problems);
            Assert.Contains("upgrade HW_03: unlockNight must be >= 1", problems);
        }

        [Theory]
        [InlineData("DOP_04", 4, "Dopamine per video", "+140%")]
        [InlineData("DOP_04", 5, "Dopamine per video", "+175%")]
        [InlineData("DOP_01", 0, "Full reward at", "100%")]
        [InlineData("DOP_01", 5, "Full reward at", "50%")]
        [InlineData("DOP_02", 0, "Auto-swipe delay", "off")]
        [InlineData("DOP_02", 1, "Auto-swipe delay", "0.9s")]
        [InlineData("DOP_02", 4, "Auto-swipe delay", "0.3s")]
        [InlineData("DOP_03", 2, "Overclock", "x5")]
        [InlineData("DOP_05", 3, "Ad chance", "3%")]
        [InlineData("DOP_09", 2, "All dopamine", "x1.21")]
        [InlineData("SNK_01", 1, "Eye fatigue tonight", "-40%")]
        [InlineData("SNK_02", 1, "Salary tonight", "-50%")]
        [InlineData("CRT_01", 0, "Post pays", "off")]
        [InlineData("CRT_01", 3, "Post pays", "$16 + $1 per night")]
        [InlineData("HW_01", 3, "Rack phones", "3/6")]
        [InlineData("HW_07", 0, "Safe outlet load", "4.5")]
        [InlineData("HW_07", 1, "Safe outlet load", "10")]
        [InlineData("GRASS_02", 2, "Starting cash", "$60")]
        [InlineData("GRASS_04", 5, "Electric bill", "-30%")]
        public void EffectText_FollowsTheBalanceNumbers(string id, int level, string label, string value)
        {
            var cfg = TestData.Balance();

            var effect = UpgradeEffects.At(cfg, cfg.GetUpgrade(id), level);

            Assert.NotNull(effect);
            Assert.Equal(label, effect!.Value.Label);
            Assert.Equal(value, effect.Value.Value);
        }

        [Fact]
        public void EffectText_CoversEveryShippedUpgrade()
        {
            var cfg = TestData.Balance();
            foreach (var upgrade in cfg.Upgrades)
            {
                Assert.True(UpgradeEffects.At(cfg, upgrade, 0) != null, upgrade.Id);
            }
        }

        [Fact]
        public void RackRoles_DefaultToTheOldCycleAndPersist()
        {
            var save = new SaveData();

            Assert.Equal(
                new[] { SecondaryRole.Standard, SecondaryRole.AdShield, SecondaryRole.CryptoMiner, SecondaryRole.MythicRadar, SecondaryRole.Standard },
                Enumerable.Range(0, 5).Select(save.RackRole));

            save.SetRackRole(2, SecondaryRole.MythicRadar);

            Assert.Equal(SecondaryRole.MythicRadar, save.RackRole(2));
            Assert.Equal(SecondaryRole.AdShield, save.RackRole(1));
            Assert.Equal(SecondaryRole.MythicRadar, save.RackRole(3));
            Assert.Equal(3, save.RackRoles.Count);
        }

        [Fact]
        public void RackRoles_SurviveASaveRoundTrip()
        {
            var storage = new MemoryStorage();
            var service = new SaveDataService(storage);
            var save = new SaveData { LastNightVideos = 42 };
            save.SetRackRole(1, SecondaryRole.CryptoMiner);

            service.Save(save);
            var loaded = service.LoadOrCreate();

            Assert.True(service.HasSave);
            Assert.Contains("\"CryptoMiner\"", storage.Text);
            Assert.Equal(SecondaryRole.CryptoMiner, loaded.RackRole(1));
            Assert.Equal(42, loaded.LastNightVideos);
        }

        private sealed class MemoryStorage : ISaveStorage
        {
            public string? Text;
            public bool Exists() => Text != null;
            public string ReadAllText() => Text ?? "";
            public void WriteAllText(string json) => Text = json;
        }
    }
}
