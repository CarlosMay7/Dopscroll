using System.Linq;
using System.Text.Json;
using Doomscroll.Core.Services;

namespace Doomscroll.Core.Tests
{
    public class BalanceConfigTests
    {
        [Fact]
        public void ShippedBalanceFile_IsValid()
        {
            var problems = TestData.Balance().Validate();
            Assert.Empty(problems);
        }

        [Fact]
        public void ReferenceFixture_IsValid()
        {
            Assert.Empty(TestData.Reference().Validate());
        }

        [Fact]
        public void ShippedBalanceFile_KeepsTheShopOrder()
        {
            var cfg = TestData.Balance();

            Assert.Equal(
                new[] { "DOP_01", "DOP_02", "DOP_03", "DOP_04", "DOP_05", "DOP_06", "DOP_07", "DOP_08", "DOP_10", "DOP_11", "CRT_01", "DOP_09" },
                cfg.UpgradesFor(UpgradeCurrency.Dopamine).Select(u => u.Id));
            Assert.Equal(
                new[] { "HW_01", "HW_02", "HW_03", "HW_05", "SNK_01", "SNK_02", "HW_06", "HW_07", "HW_08" },
                cfg.UpgradesFor(UpgradeCurrency.Cash).Select(u => u.Id));
            Assert.Equal(
                new[] { "GRASS_01", "GRASS_02", "GRASS_03", "GRASS_04", "GRASS_05", "GRASS_06" },
                cfg.UpgradesFor(UpgradeCurrency.Grass).Select(u => u.Id));
        }

        [Fact]
        public void Validate_ReportsMissingUpgradeAndBrokenNumbers()
        {
            var cfg = TestData.Balance();
            cfg.Upgrades.RemoveAll(u => u.Id == "DOP_04");
            cfg.Night.DurationSeconds = 0;

            var problems = cfg.Validate();

            Assert.Contains("upgrade DOP_04 is missing", problems);
            Assert.Contains(problems, p => p.StartsWith("night.durationSeconds must be > 0"));
        }

        [Fact]
        public void Validate_ReportsMissingUpgradeParam()
        {
            var cfg = TestData.Balance();
            cfg.GetUpgrade("DOP_01").Params.Remove("minThreshold");

            Assert.Contains("upgrade DOP_01 is missing params.minThreshold", cfg.Validate());
        }

        [Fact]
        public void Parse_RejectsEmptyOrBrokenJson()
        {
            Assert.ThrowsAny<JsonException>(() => BalanceConfig.Parse(""));
            Assert.ThrowsAny<JsonException>(() => BalanceConfig.Parse("{ not json"));
        }

        [Fact]
        public void GetUpgrade_UnknownId_Throws()
        {
            var cfg = TestData.Balance();
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => cfg.GetUpgrade("DOP_99"));
        }
    }
}
