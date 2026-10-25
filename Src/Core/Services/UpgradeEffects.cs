using System;
using System.Globalization;

namespace Doomscroll.Core.Services
{
    /// <summary>What an upgrade does at a given level, worded for the shop card ("Dopamine per video", "+140%").</summary>
    public readonly struct UpgradeEffect
    {
        public UpgradeEffect(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }

    /// <summary>
    /// Shop-card wording for each upgrade's effect. The numbers come from <see cref="BalanceConfig"/> and follow
    /// the same formulas as <see cref="RunFormulas"/>, so the cards stay right when balance.json is retuned.
    /// </summary>
    public static class UpgradeEffects
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>The effect at <paramref name="level"/>, or null when the upgrade has no numeric effect to show.</summary>
        public static UpgradeEffect? At(BalanceConfig cfg, UpgradeDefinition def, int level)
        {
            bool on = level > 0;
            switch (def.Id)
            {
                case "DOP_01":
                    double threshold = Math.Max(def.Param("minThreshold"), 1.0 - level * def.Value);
                    return new UpgradeEffect("Full reward at", Pct(threshold));
                case "DOP_02":
                    return new UpgradeEffect("Auto-swipe delay", on ? Num(AutoScrollDelay(def, level)) + "s" : "off");
                case "DOP_03":
                    return new UpgradeEffect("Overclock", level switch
                    {
                        0 => "off",
                        1 => "x" + Num(cfg.Speed.X2.Playback),
                        _ => "x" + Num(cfg.Speed.X5.Playback)
                    });
                case "DOP_04":
                    return new UpgradeEffect("Dopamine per video", "+" + Pct(level * def.Value));
                case "DOP_05":
                    double ads = Math.Max(cfg.Feed.MinAdChance, cfg.Feed.AdChance - level * def.Value);
                    return new UpgradeEffect("Ad chance", Pct(ads));
                case "DOP_06":
                    return new UpgradeEffect("Critical likes", on ? Pct(def.Value) + " hit x" + Num(cfg.Phone.CritMultiplier) : "none");
                case "DOP_07":
                    return new UpgradeEffect("Dopamine per rack phone", "+" + Pct(level * def.Value));
                case "DOP_08":
                    return new UpgradeEffect("Per same-niche like", "+" + Pct(level * def.Value));
                case "DOP_09":
                    return new UpgradeEffect("All dopamine", "x" + Math.Pow(def.Value, level).ToString("0.00", Inv));
                case "DOP_10":
                    return new UpgradeEffect("Phone #1 dopamine", "+" + Pct(on ? def.Value - 1.0 : 0.0));
                case "DOP_11":
                    return new UpgradeEffect("Screamer burst", on ? "x" + Num(def.Value) + " dopamine" : "none");
                case "HW_01":
                    return new UpgradeEffect("Rack phones", $"{level}/{def.MaxLevel}");
                case "HW_02":
                    return new UpgradeEffect("Overclock battery drain", on ? "-" + Pct(1.0 - def.Value) : "full");
                case "HW_03":
                    return new UpgradeEffect("Backup charge", on ? "+" + Pct(def.Value) : "none");
                case "HW_05":
                    return new UpgradeEffect("Fatigue and boss pings", on ? "-" + Pct(def.Value) : "normal");
                case "HW_06":
                    return new UpgradeEffect("Auto-reply to boss", on ? "on" : "off");
                case "HW_07":
                    return new UpgradeEffect("Safe outlet load", Num(on ? def.Value : cfg.Outlet.SafeLoad));
                case "HW_08":
                    return new UpgradeEffect("Blur penalty", on ? "-" + Pct(def.Value) : "full");
                case "SNK_01":
                    return new UpgradeEffect("Eye fatigue tonight", on ? "-" + Pct(def.Value) : "normal");
                case "SNK_02":
                    return new UpgradeEffect("Salary tonight", on ? "-" + Pct(def.Value) : "full");
                case "CRT_01":
                    return new UpgradeEffect("Post pays", on
                        ? "$" + Num(def.Param("cashBase") + def.Param("cashPerLevel") * (level - 1)) + " + $" + Num(def.Param("cashPerNight")) + " per night"
                        : "off");
                case "GRASS_01":
                    return new UpgradeEffect("Base dopamine", "+" + Pct(level * def.Value));
                case "GRASS_02":
                    return new UpgradeEffect("Starting cash", "$" + Num(level * def.Value));
                case "GRASS_03":
                    return new UpgradeEffect("Nightly salary", "+$" + Num(level * def.Value));
                case "GRASS_04":
                    return new UpgradeEffect("Electric bill", "-" + Pct(level * def.Value));
                case "GRASS_05":
                    return new UpgradeEffect("Battery", "+" + Pct(level * def.Value));
                case "GRASS_06":
                    return new UpgradeEffect("Head start", on ? "on" : "off");
                default:
                    return null;
            }
        }

        private static double AutoScrollDelay(UpgradeDefinition bot, int level) =>
            Math.Max(bot.Param("minDelaySeconds"), bot.Param("baseDelaySeconds") - level * bot.Param("delayReductionPerLevel"));

        private static string Pct(double fraction) => (fraction * 100.0).ToString("0.#", Inv) + "%";

        private static string Num(double value) => value.ToString("0.##", Inv);
    }
}
