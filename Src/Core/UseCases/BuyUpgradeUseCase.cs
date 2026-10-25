using System.Collections.Generic;
using Doomscroll.Core.Services;

namespace Doomscroll.Core.UseCases
{
    public class BuyUpgradeUseCase
    {
        private readonly EconomyService _economy;

        public BuyUpgradeUseCase(EconomyService economy)
        {
            _economy = economy;
        }

        public bool TryBuyUpgrade(UpgradeDefinition upgrade, ref double currentCurrency, IDictionary<string, int> upgradeLevels)
        {
            upgradeLevels.TryGetValue(upgrade.Id, out int currentLevel);
            if (upgrade.MaxLevel.HasValue && currentLevel >= upgrade.MaxLevel.Value)
                return false;

            double cost = _economy.CalculateUpgradeCost(upgrade, currentLevel);
            if (currentCurrency < cost)
                return false;

            currentCurrency -= cost;
            upgradeLevels[upgrade.Id] = currentLevel + 1;
            return true;
        }
    }
}
