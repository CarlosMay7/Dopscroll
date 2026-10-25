using Doomscroll.Core.Services;

namespace Doomscroll.Core.UseCases
{
    public class PrestigeResetUseCase
    {
        private readonly EconomyService _economy;
        private readonly BalanceConfig _cfg;

        public PrestigeResetUseCase(EconomyService economy, BalanceConfig cfg)
        {
            _economy = economy;
            _cfg = cfg;
        }

        /// <param name="voluntary">True when the player chose to go (bonus Grass Points), false when the bill forced it.</param>
        public int ExecuteTouchGrass(SaveData saveData, bool voluntary = false)
        {
            int grassEarned = _economy.GrassPointsForTouchingGrass(saveData.TotalRunDopamine, saveData.CurrentDay, voluntary);
            saveData.GrassPoints += grassEarned;
            saveData.TotalPrestiges++;

            // Reset run state while keeping Grass upgrades and Mythic Album
            saveData.CurrentDay = 1;
            saveData.CurrentDopamine = 0;
            saveData.TotalRunDopamine = 0;
            saveData.LastPostNight = 0;
            saveData.RunUpgradeLevels.Clear();

            // Grandma's Envelope: starting cash per level
            saveData.CurrentMoney = saveData.GrassLevel("GRASS_02") * _cfg.GetUpgrade("GRASS_02").Value;

            // Grass perks that grant run upgrades at the start of every run (e.g. Muscle Memory)
            foreach (var perk in _cfg.UpgradesFor(UpgradeCurrency.Grass))
            {
                if (perk.Grants.Count == 0 || saveData.GrassLevel(perk.Id) <= 0) continue;
                foreach (var grant in perk.Grants)
                {
                    saveData.RunUpgradeLevels[grant.Key] = grant.Value;
                }
            }

            return grassEarned;
        }
    }
}
