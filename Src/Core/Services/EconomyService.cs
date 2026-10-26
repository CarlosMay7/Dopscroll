using System;

namespace Doomscroll.Core.Services
{
    /// <summary>Costs, bills and prestige rewards. Every number comes from <see cref="BalanceConfig"/>.</summary>
    public class EconomyService
    {
        private readonly BalanceConfig _cfg;

        public EconomyService(BalanceConfig cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        public double CalculateUpgradeCost(UpgradeDefinition upgrade, int currentLevel)
        {
            if (upgrade.CostStep.HasValue)
            {
                return upgrade.BaseCost + upgrade.CostStep.Value * currentLevel;
            }

            return upgrade.BaseCost * Math.Pow(upgrade.CostMult, currentLevel);
        }

        public bool IsBillDay(int day) => (day % _cfg.Bill.EveryDays) == 0;

        /// <summary>Nights left until the next bill, counted at the end of night <paramref name="day"/>.</summary>
        public int DaysUntilBill(int day) => _cfg.Bill.EveryDays - (day % _cfg.Bill.EveryDays);

        public double CalculateElectricBill(int day, int secondaryPhonesCount, bool hasDeskFan, int subsidizedGridLevel)
        {
            var bill = _cfg.Bill;
            double raw = bill.Base
                         + (bill.PerSecondaryPhone * secondaryPhonesCount)
                         + (hasDeskFan ? bill.PerFan : 0.0)
                         + (bill.PerDay * day);

            int cycleIndex = Math.Max(0, (day / bill.EveryDays) - 1);
            raw *= Math.Pow(bill.GrowthPerCycle, cycleIndex);

            double discountPerLevel = _cfg.GetUpgrade("GRASS_04").Value;
            double discountMultiplier = Math.Max(0.1, 1.0 - (discountPerLevel * subsidizedGridLevel));
            return Math.Round(raw * discountMultiplier, 2);
        }

        public int CalculateGrassPointsReward(double totalRunDopamine, int dayReached)
        {
            var prestige = _cfg.Prestige;
            double fromDopamine = Math.Pow(Math.Max(0.0, totalRunDopamine) / prestige.DopamineUnit, 1.0 / prestige.Divisor);
            return (int)Math.Floor(fromDopamine + (dayReached * prestige.PerDay));
        }

        /// <summary>Extra Grass Points for touching grass by choice (at least 1 when there is anything to earn).</summary>
        public int VoluntaryGrassBonus(int baseReward) =>
            baseReward > 0 && _cfg.Prestige.VoluntaryBonus > 0
                ? Math.Max(1, (int)Math.Round(baseReward * _cfg.Prestige.VoluntaryBonus, MidpointRounding.AwayFromZero))
                : 0;

        /// <summary>Grass Points for touching grass now; <paramref name="voluntary"/> is false when the bill forced it.</summary>
        public int GrassPointsForTouchingGrass(double totalRunDopamine, int dayReached, bool voluntary)
        {
            int reward = CalculateGrassPointsReward(totalRunDopamine, dayReached);
            return voluntary ? reward + VoluntaryGrassBonus(reward) : reward;
        }
    }
}
