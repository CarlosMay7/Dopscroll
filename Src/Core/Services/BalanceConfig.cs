using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Doomscroll.Core.Events;

namespace Doomscroll.Core.Services
{
    public enum UpgradeCurrency { Dopamine, Cash, Grass }

    /// <summary>PRYM shop tab an upgrade is listed under.</summary>
    public enum UpgradeCategory { Mind, Desk, Snacks, Rack, Grass }

    public sealed class UpgradeDefinition
    {
        public string Id { get; set; } = string.Empty;
        public UpgradeCurrency Currency { get; set; }
        /// <summary>Short name on the shop card.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>One-line description on the shop card (no numbers: the card shows them).</summary>
        public string Desc { get; set; } = string.Empty;
        /// <summary>Glyph drawn on the shop card, one of <see cref="BalanceConfig.KnownIcons"/>.</summary>
        public string Icon { get; set; } = string.Empty;
        public UpgradeCategory? Category { get; set; }
        /// <summary>When set, the card is shown locked until this night.</summary>
        public int? UnlockNight { get; set; }
        /// <summary>Consumable: bought for the next night only, then used up when that night ends.</summary>
        public bool PerNight { get; set; }
        public double BaseCost { get; set; }
        /// <summary>Exponential cost growth per level (cost = baseCost * costMult^level).</summary>
        public double CostMult { get; set; } = 1.0;
        /// <summary>When set, cost grows linearly instead (cost = baseCost + costStep * level).</summary>
        public double? CostStep { get; set; }
        /// <summary>Null means the upgrade can be bought forever.</summary>
        public int? MaxLevel { get; set; }
        /// <summary>The upgrade's main number (meaning depends on the upgrade, see Data/balance.json).</summary>
        public double Value { get; set; }
        public Dictionary<string, double> Params { get; set; } = new();
        /// <summary>Run upgrade levels granted at the start of every run (prestige perks).</summary>
        public Dictionary<string, int> Grants { get; set; } = new();

        public double Param(string name)
        {
            if (Params.TryGetValue(name, out double value)) return value;
            throw new KeyNotFoundException($"balance.json: upgrade {Id} is missing params.{name}");
        }
    }

    public sealed class MythicDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Lore { get; set; } = string.Empty;
        public string Buff { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public sealed class MythicsSection
    {
        public double DopamineBonusEach { get; set; }
        public List<MythicDefinition> Catalog { get; set; } = new();
    }

    public sealed class SpamPopupSection
    {
        public double FirstDelaySeconds { get; set; }
        public double FirstDelayRandomSeconds { get; set; }
        public double DelaySeconds { get; set; }
        public double DelayRandomSeconds { get; set; }
        public double ReliefDopamine { get; set; }
    }

    public sealed class DeskEventSection
    {
        public double TriggerSeconds { get; set; }
        public double WifiSpeedMultiplier { get; set; }
        public double WifiFixDopamine { get; set; }
        public double SponsorCash { get; set; }
        public double SponsorAdPenalty { get; set; }
        public double GoldenWaveSeconds { get; set; }
        public double GoldenWaveDopamineMultiplier { get; set; }
    }

    public sealed class NightSection
    {
        public double DurationSeconds { get; set; }
        public SpamPopupSection SpamPopup { get; set; } = new();
        public DeskEventSection DeskEvent { get; set; } = new();
    }

    public sealed class PhoneSection
    {
        public double BaseBatterySeconds { get; set; }
        public double BaseDrainPerSecond { get; set; }
        public double AdDrainMultiplier { get; set; }
        public double VideoSeconds { get; set; }
        public double BaseDopamine { get; set; }
        public double LikeBonusRatio { get; set; }
        public double CritMultiplier { get; set; }
        public double MythicMultiplier { get; set; }
        public double MythicLikedCompletionMultiplier { get; set; }
        public double ScreamerStunSeconds { get; set; }
        public double PrimaryTransitionSeconds { get; set; }
        public double SecondaryTransitionSeconds { get; set; }
        public double PrimaryScrollCooldownSeconds { get; set; }
        public double SecondaryAutoScrollSeconds { get; set; }
    }

    public sealed class SecondaryRolesSection
    {
        public double DopamineFactor { get; set; }
        public double CryptoMinerDopamineFactor { get; set; }
        public double CryptoMinerCashPerVideo { get; set; }
        public double AdShieldAdMultiplier { get; set; }
        public double MythicRadarMythicChance { get; set; }
    }

    public sealed class SpeedSettings
    {
        public double Playback { get; set; }
        public double Drain { get; set; }
    }

    public sealed class SpeedSection
    {
        public SpeedSettings X2 { get; set; } = new();
        public SpeedSettings X5 { get; set; } = new();
    }

    public sealed class BrightnessSettings
    {
        public double Dopamine { get; set; }
        public double Drain { get; set; }
        public double Fatigue { get; set; }
    }

    public sealed class BrightnessSection
    {
        public BrightnessSettings Low { get; set; } = new();
        public BrightnessSettings Normal { get; set; } = new();
        public BrightnessSettings High { get; set; } = new();

        public BrightnessSettings For(BrightnessMode mode) => mode switch
        {
            BrightnessMode.Low => Low,
            BrightnessMode.High => High,
            _ => Normal
        };
    }

    public sealed class EyesSection
    {
        public double FatiguePerSecond { get; set; }
        public double BlackoutFatigueMultiplier { get; set; }
        public double BlurStartsBelow { get; set; }
        public double EyeDropsCost { get; set; }
        /// <summary>Phone #1 dopamine lost at full blur (scaled by how blurry the eyes are).</summary>
        public double BlurDopaminePenalty { get; set; }
    }

    public sealed class ChargerSection
    {
        /// <summary>Battery seconds phone #1 gains per second while plugged in.</summary>
        public double ChargePerSecond { get; set; }
        /// <summary>Wall outlet load added while it charges.</summary>
        public double OutletLoad { get; set; }
    }

    public sealed class ToleranceSection
    {
        /// <summary>Boredom a niche gains each time phone #1 finishes one of its standard videos.</summary>
        public double PerVideo { get; set; }
        /// <summary>Boredom every other niche loses at the same time.</summary>
        public double RecoveryPerVideo { get; set; }
        /// <summary>Most dopamine a bored niche can lose.</summary>
        public double MaxPenalty { get; set; }
    }

    public sealed class NicheSettings
    {
        public double Dopamine { get; set; }
        public double Drain { get; set; }
        public double Fatigue { get; set; }
        public double LikeEyeRestore { get; set; }
        /// <summary>Overrides phone.videoSeconds for this niche when set.</summary>
        public double? VideoSeconds { get; set; }
    }

    public sealed class NichesSection
    {
        public NicheSettings Cats { get; set; } = new();
        public NicheSettings Gaming { get; set; } = new();
        public NicheSettings Asmr { get; set; } = new();
        public NicheSettings Memes { get; set; } = new();

        /// <summary>Settings for a niche, or null for <see cref="VideoNiche.None"/>.</summary>
        public NicheSettings? For(VideoNiche niche) => niche switch
        {
            VideoNiche.Cats => Cats,
            VideoNiche.Gaming => Gaming,
            VideoNiche.Asmr => Asmr,
            VideoNiche.Memes => Memes,
            _ => null
        };
    }

    public sealed class FeedSection
    {
        public double AdChance { get; set; }
        public double MinAdChance { get; set; }
        public double PollutedAdBonus { get; set; }
        public double ScreamerChance { get; set; }
        public double MythicChance { get; set; }
        public int MythicPityVideos { get; set; }
        public double NicheLikeWeightGain { get; set; }
        public double NicheMaxWeight { get; set; }
        public int AdLikePenaltyVideos { get; set; }
    }

    public sealed class ComboSection
    {
        public int SwipesToFever { get; set; }
        public double FeverSeconds { get; set; }
        public double FeverDopamineMultiplier { get; set; }
        public int ViralStreakMax { get; set; }
    }

    public sealed class LaptopSection
    {
        public double AlertIntervalMin { get; set; }
        public double AlertIntervalMax { get; set; }
        public double AlertWindowSeconds { get; set; }
        public double AuditChance { get; set; }
        public int AuditTaps { get; set; }
        public double AuditExtraSeconds { get; set; }
        public double AuditBonusCash { get; set; }
        public double BaseSalary { get; set; }
        /// <summary>Salary raise for every night survived in the current run.</summary>
        public double RaisePerNight { get; set; }
    }

    public sealed class BillSection
    {
        public int EveryDays { get; set; }
        public double Base { get; set; }
        public double PerSecondaryPhone { get; set; }
        public double PerFan { get; set; }
        public double PerDay { get; set; }
        public double GrowthPerCycle { get; set; }
    }

    public sealed class OutletSection
    {
        public double PerActiveSecondaryPhone { get; set; }
        public double Fan { get; set; }
        public double HighBrightness { get; set; }
        public double OverclockX2 { get; set; }
        public double OverclockX5 { get; set; }
        public double SafeLoad { get; set; }
    }

    public sealed class PrestigeSection
    {
        public double DopamineUnit { get; set; }
        public double Divisor { get; set; }
        public double PerDay { get; set; }
        /// <summary>Extra Grass Points (as a share, at least +1) for touching grass before the bill forces it.</summary>
        public double VoluntaryBonus { get; set; }
    }

    public sealed class VictorySection
    {
        public int Day { get; set; }
        public double MoneyCost { get; set; }
    }

    public sealed class AchievementsSection
    {
        public double DopamineBonusEach { get; set; }
    }

    /// <summary>
    /// Typed view of Data/balance.json, the single source of truth for every gameplay number.
    /// There are deliberately no numeric defaults here: a missing value shows up in <see cref="Validate"/>
    /// instead of silently falling back to a second copy of the balance.
    /// </summary>
    public sealed class BalanceConfig
    {
        /// <summary>Upgrade ids the game code refers to directly.</summary>
        public static readonly string[] RequiredUpgradeIds =
        {
            "DOP_01", "DOP_02", "DOP_03", "DOP_04", "DOP_05", "DOP_06", "DOP_07", "DOP_08", "DOP_09", "DOP_10", "DOP_11",
            "HW_01", "HW_02", "HW_03", "HW_05", "HW_06", "HW_07", "HW_08",
            "SNK_01", "SNK_02", "CRT_01",
            "GRASS_01", "GRASS_02", "GRASS_03", "GRASS_04", "GRASS_05", "GRASS_06"
        };

        /// <summary>Icons the shop knows how to draw (Src/Presentation/UI/GlowGlyph.cs).</summary>
        public static readonly string[] KnownIcons =
        {
            "brain", "bot", "fast", "chart", "shield", "star", "sync", "flame", "soap", "skull", "infinity",
            "phone", "fan", "battery", "cup", "bird", "plug", "glasses", "sprout", "envelope", "briefcase",
            "sun", "link", "can", "moon", "camera"
        };

        /// <summary>Mythic ids whose passive effects are implemented in code.</summary>
        public static readonly string[] RequiredMythicIds =
        {
            "MYTHIC_0", "MYTHIC_1", "MYTHIC_2", "MYTHIC_3", "MYTHIC_4", "MYTHIC_5", "MYTHIC_6", "MYTHIC_7"
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public NightSection Night { get; set; } = new();
        public PhoneSection Phone { get; set; } = new();
        public SecondaryRolesSection SecondaryRoles { get; set; } = new();
        public SpeedSection Speed { get; set; } = new();
        public BrightnessSection Brightness { get; set; } = new();
        public EyesSection Eyes { get; set; } = new();
        public ChargerSection Charger { get; set; } = new();
        public ToleranceSection Tolerance { get; set; } = new();
        public NichesSection Niches { get; set; } = new();
        public FeedSection Feed { get; set; } = new();
        public ComboSection Combo { get; set; } = new();
        public LaptopSection Laptop { get; set; } = new();
        public BillSection Bill { get; set; } = new();
        public OutletSection Outlet { get; set; } = new();
        public PrestigeSection Prestige { get; set; } = new();
        public VictorySection Victory { get; set; } = new();
        public AchievementsSection Achievements { get; set; } = new();
        public MythicsSection Mythics { get; set; } = new();
        public List<UpgradeDefinition> Upgrades { get; set; } = new();

        private Dictionary<string, UpgradeDefinition>? _upgradesById;
        private Dictionary<string, MythicDefinition>? _mythicsById;

        /// <summary>Parses balance JSON. Throws <see cref="JsonException"/> when the text is not valid JSON.</summary>
        public static BalanceConfig Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new JsonException("balance.json is empty or missing.");
            }

            return JsonSerializer.Deserialize<BalanceConfig>(json, JsonOptions)
                   ?? throw new JsonException("balance.json deserialized to null.");
        }

        public UpgradeDefinition GetUpgrade(string id)
        {
            _upgradesById ??= Upgrades.GroupBy(u => u.Id).ToDictionary(g => g.Key, g => g.First());
            if (_upgradesById.TryGetValue(id, out var upgrade)) return upgrade;
            throw new KeyNotFoundException($"balance.json: upgrade {id} is not defined.");
        }

        public MythicDefinition GetMythic(string id)
        {
            _mythicsById ??= Mythics.Catalog.GroupBy(m => m.Id).ToDictionary(g => g.Key, g => g.First());
            if (_mythicsById.TryGetValue(id, out var mythic)) return mythic;
            throw new KeyNotFoundException($"balance.json: mythic {id} is not defined.");
        }

        /// <summary>Upgrades of one currency, in the order they appear in balance.json (shop order).</summary>
        public IEnumerable<UpgradeDefinition> UpgradesFor(UpgradeCurrency currency) =>
            Upgrades.Where(u => u.Currency == currency);

        /// <summary>Upgrades of one shop tab, in balance.json order.</summary>
        public IEnumerable<UpgradeDefinition> UpgradesIn(UpgradeCategory category) =>
            Upgrades.Where(u => u.Category == category);

        /// <summary>Returns every problem found; an empty list means the config is usable.</summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();

            foreach (var dup in Upgrades.GroupBy(u => u.Id).Where(g => g.Count() > 1))
                problems.Add($"upgrade {dup.Key} is defined {dup.Count()} times");
            foreach (string id in RequiredUpgradeIds)
                if (Upgrades.All(u => u.Id != id)) problems.Add($"upgrade {id} is missing");
            foreach (string id in RequiredMythicIds)
                if (Mythics.Catalog.All(m => m.Id != id)) problems.Add($"mythic {id} is missing");

            foreach (var u in Upgrades)
            {
                if (u.BaseCost < 0 || (u.BaseCost == 0 && !u.PerNight)) problems.Add($"upgrade {u.Id}: baseCost must be > 0 (0 is allowed for perNight items)");
                if (u.PerNight && u.MaxLevel != 1) problems.Add($"upgrade {u.Id}: perNight items need maxLevel 1");
                if (u.CostStep is null && u.CostMult <= 0) problems.Add($"upgrade {u.Id}: costMult must be > 0");
                if (u.MaxLevel is < 1) problems.Add($"upgrade {u.Id}: maxLevel must be >= 1 or null");
                if (string.IsNullOrWhiteSpace(u.Name)) problems.Add($"upgrade {u.Id}: name is missing");
                if (string.IsNullOrWhiteSpace(u.Desc)) problems.Add($"upgrade {u.Id}: desc is missing");
                if (Array.IndexOf(KnownIcons, u.Icon) < 0) problems.Add($"upgrade {u.Id}: unknown icon '{u.Icon}'");
                if (u.Category is null) problems.Add($"upgrade {u.Id}: category is missing");
                else if ((u.Category == UpgradeCategory.Grass) != (u.Currency == UpgradeCurrency.Grass))
                    problems.Add($"upgrade {u.Id}: grass upgrades (and only those) go in the grass category");
                if (u.UnlockNight is < 1) problems.Add($"upgrade {u.Id}: unlockNight must be >= 1");
            }

            RequireParam(problems, "DOP_01", "minThreshold");
            RequireParam(problems, "DOP_02", "baseDelaySeconds");
            RequireParam(problems, "DOP_02", "delayReductionPerLevel");
            RequireParam(problems, "DOP_02", "minDelaySeconds");
            RequireParam(problems, "HW_05", "laptopIntervalScale");
            RequireParam(problems, "CRT_01", "dopamineShare");
            RequireParam(problems, "CRT_01", "cashBase");
            RequireParam(problems, "CRT_01", "cashPerLevel");
            RequireParam(problems, "CRT_01", "cashPerNight");
            if (Upgrades.FirstOrDefault(u => u.Id == "HW_01") is { MaxLevel: null })
                problems.Add("upgrade HW_01 needs a maxLevel (it is the number of secondary phone cradles)");

            Positive(problems, "night.durationSeconds", Night.DurationSeconds);
            Positive(problems, "phone.baseBatterySeconds", Phone.BaseBatterySeconds);
            Positive(problems, "phone.baseDrainPerSecond", Phone.BaseDrainPerSecond);
            Positive(problems, "phone.videoSeconds", Phone.VideoSeconds);
            Positive(problems, "phone.baseDopamine", Phone.BaseDopamine);
            Positive(problems, "speed.x2.playback", Speed.X2.Playback);
            Positive(problems, "speed.x5.playback", Speed.X5.Playback);
            Positive(problems, "brightness.normal.drain", Brightness.Normal.Drain);
            Positive(problems, "eyes.fatiguePerSecond", Eyes.FatiguePerSecond);
            Positive(problems, "eyes.blurStartsBelow", Eyes.BlurStartsBelow);
            Fraction(problems, "eyes.blurDopaminePenalty", Eyes.BlurDopaminePenalty);
            Positive(problems, "charger.chargePerSecond", Charger.ChargePerSecond);
            Fraction(problems, "tolerance.maxPenalty", Tolerance.MaxPenalty);
            if (Tolerance.PerVideo < 0 || Tolerance.RecoveryPerVideo < 0)
                problems.Add("tolerance.perVideo and tolerance.recoveryPerVideo must be >= 0");
            Positive(problems, "niches.cats.dopamine", Niches.Cats.Dopamine);
            Positive(problems, "niches.gaming.dopamine", Niches.Gaming.Dopamine);
            Positive(problems, "niches.asmr.dopamine", Niches.Asmr.Dopamine);
            Positive(problems, "niches.memes.dopamine", Niches.Memes.Dopamine);
            Positive(problems, "feed.mythicPityVideos", Feed.MythicPityVideos);
            Positive(problems, "combo.swipesToFever", Combo.SwipesToFever);
            Positive(problems, "laptop.alertIntervalMin", Laptop.AlertIntervalMin);
            Positive(problems, "laptop.alertWindowSeconds", Laptop.AlertWindowSeconds);
            Positive(problems, "laptop.auditTaps", Laptop.AuditTaps);
            if (Laptop.AlertIntervalMax < Laptop.AlertIntervalMin)
                problems.Add("laptop.alertIntervalMax must be >= laptop.alertIntervalMin");
            Positive(problems, "bill.everyDays", Bill.EveryDays);
            Positive(problems, "outlet.safeLoad", Outlet.SafeLoad);
            Positive(problems, "prestige.dopamineUnit", Prestige.DopamineUnit);
            Positive(problems, "prestige.divisor", Prestige.Divisor);
            Positive(problems, "victory.day", Victory.Day);

            return problems;
        }

        private void RequireParam(List<string> problems, string upgradeId, string param)
        {
            var upgrade = Upgrades.FirstOrDefault(u => u.Id == upgradeId);
            if (upgrade != null && !upgrade.Params.ContainsKey(param))
                problems.Add($"upgrade {upgradeId} is missing params.{param}");
        }

        private static void Positive(List<string> problems, string name, double value)
        {
            if (!(value > 0)) problems.Add($"{name} must be > 0 (is {value})");
        }

        private static void Fraction(List<string> problems, string name, double value)
        {
            if (!(value >= 0 && value < 1)) problems.Add($"{name} must be in [0, 1) (is {value})");
        }
    }
}
