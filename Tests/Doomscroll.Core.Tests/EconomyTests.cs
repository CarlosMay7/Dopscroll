using System.Collections.Generic;
using Doomscroll.Core.Services;
using Doomscroll.Core.UseCases;

namespace Doomscroll.Core.Tests
{
    /// <summary>Bills, costs and prestige rewards, checked against values the original game produced (reference fixture).</summary>
    public class EconomyTests
    {
        [Theory]
        [InlineData(2, 0, false, 0, 42.00)]
        [InlineData(3, 0, false, 0, 48.00)]
        [InlineData(6, 4, true, 1, 130.74)]
        [InlineData(29, 6, false, 2, 692.84)]
        [InlineData(30, 6, false, 2, 807.00)]
        public void ElectricBill_MatchesTheOriginalGame(int day, int phones, bool fan, int gridLevel, double expected)
        {
            var economy = new EconomyService(TestData.Reference());
            Assert.Equal(expected, economy.CalculateElectricBill(day, phones, fan, gridLevel));
        }

        [Fact]
        public void BillSchedule_EveryThirdNight()
        {
            var economy = new EconomyService(TestData.Reference());
            Assert.True(economy.IsBillDay(3));
            Assert.False(economy.IsBillDay(4));
            Assert.Equal(2, economy.DaysUntilBill(1));
            Assert.Equal(1, economy.DaysUntilBill(5));
        }

        [Theory]
        [InlineData(395.0, 1, 1)]
        [InlineData(1091462.0, 5, 16)]
        [InlineData(0.0, 1, 0)]
        public void GrassReward_MatchesTheOriginalGame(double runDopamine, int day, int expected)
        {
            var economy = new EconomyService(TestData.Reference());
            Assert.Equal(expected, economy.CalculateGrassPointsReward(runDopamine, day));
        }

        [Fact]
        public void UpgradeCosts_ExponentialForTheShopAndLinearForGrass()
        {
            var cfg = TestData.Reference();
            var economy = new EconomyService(cfg);

            Assert.Equal(25.0, economy.CalculateUpgradeCost(cfg.GetUpgrade("DOP_01"), 0));
            Assert.Equal(41.25, economy.CalculateUpgradeCost(cfg.GetUpgrade("DOP_01"), 1), precision: 10);
            Assert.Equal(6.0, economy.CalculateUpgradeCost(cfg.GetUpgrade("GRASS_02"), 2));
            Assert.Equal(1.0, economy.CalculateUpgradeCost(cfg.GetUpgrade("GRASS_01"), 0));
        }

        [Fact]
        public void BuyUpgrade_RespectsCostAndMaxLevel()
        {
            var cfg = TestData.Reference();
            var buy = new BuyUpgradeUseCase(new EconomyService(cfg));
            var levels = new Dictionary<string, int>();
            double dopamine = 30;

            Assert.True(buy.TryBuyUpgrade(cfg.GetUpgrade("DOP_01"), ref dopamine, levels));
            Assert.Equal(5.0, dopamine);
            Assert.Equal(1, levels["DOP_01"]);
            Assert.False(buy.TryBuyUpgrade(cfg.GetUpgrade("DOP_01"), ref dopamine, levels));

            double cash = 1000;
            Assert.True(buy.TryBuyUpgrade(cfg.GetUpgrade("HW_02"), ref cash, levels));
            Assert.False(buy.TryBuyUpgrade(cfg.GetUpgrade("HW_02"), ref cash, levels));
        }

        [Fact]
        public void TouchGrass_ResetsTheRunAndAppliesPerks()
        {
            var cfg = TestData.Reference();
            var prestige = new PrestigeResetUseCase(new EconomyService(cfg), cfg);
            var save = new SaveData
            {
                CurrentDay = 1,
                TotalRunDopamine = 395,
                CurrentDopamine = 100,
                CurrentMoney = 12,
                RunUpgradeLevels = { ["DOP_04"] = 3 },
                GrassUpgradeLevels = { ["GRASS_02"] = 2, ["GRASS_06"] = 1 }
            };

            int earned = prestige.ExecuteTouchGrass(save);

            Assert.Equal(1, earned);
            Assert.Equal(1, save.GrassPoints);
            Assert.Equal(1, save.TotalPrestiges);
            Assert.Equal(60.0, save.CurrentMoney);
            Assert.Equal(0.0, save.CurrentDopamine);
            Assert.Equal(new Dictionary<string, int> { ["DOP_02"] = 1, ["DOP_01"] = 3 }, save.RunUpgradeLevels);
        }
    }
}
