using System;
using Doomscroll.Core.Entities;
using Doomscroll.Core.Events;

namespace Doomscroll.Core.Services
{
    /// <summary>
    /// Transient night state that the phone stats depend on (everything that is not stored in <see cref="SaveData"/>).
    /// </summary>
    public readonly struct PhoneStatsContext
    {
        public bool IsPrimary { get; init; }
        public SecondaryRole Role { get; init; }
        public BrightnessMode Brightness { get; init; }
        public SpeedMode PrimarySpeed { get; init; }
        public VideoCategory PrimaryVideo { get; init; }
        public VideoNiche PrimaryNiche { get; init; }
        public bool WifiLagActive { get; init; }
        public bool BlackoutActive { get; init; }
        public bool FeverActive { get; init; }
        public bool GoldenWaveActive { get; init; }
        public int ViralStreak { get; init; }
        public int SecondaryPhoneCount { get; init; }
        /// <summary>How blurry the eyes are, 0 (sharp) to 1 (glasses already applied).</summary>
        public float Blur { get; init; }
        /// <summary>Dopamine share lost because phone #1's current niche is boring (see <see cref="NicheTolerance"/>).</summary>
        public float NicheBoredom { get; init; }
    }

    /// <summary>
    /// Pure gameplay formulas (no Godot). Every number comes from <see cref="BalanceConfig"/>.
    /// Float/double types and the order of operations intentionally mirror the original controller code,
    /// so the refactor is bit-for-bit identical with the same balance values.
    /// </summary>
    public static class RunFormulas
    {
        public static PhoneStats BuildPhoneStats(BalanceConfig cfg, SaveData save, in PhoneStatsContext ctx)
        {
            int attentionLvl = save.RunLevel("DOP_01");
            int algoLvl = save.RunLevel("DOP_04");
            int syncLvl = save.RunLevel("DOP_07");
            int viralLvl = save.RunLevel("DOP_08");
            int infOverclock = save.RunLevel("DOP_09");
            bool splitScreen = save.RunLevel("DOP_10") > 0;
            int grassDopamine = save.GrassLevel("GRASS_01");
            bool hasFan = save.RunLevel("HW_02") > 0 && !ctx.BlackoutActive;
            bool hasPowerBank = save.RunLevel("HW_03") > 0;

            float speedMult = ctx.PrimarySpeed switch
            {
                SpeedMode.X2 => (float)cfg.Speed.X2.Playback,
                SpeedMode.X5 => (float)cfg.Speed.X5.Playback,
                _ => 1.0f
            };
            if (!ctx.IsPrimary) speedMult = 1.0f;
            if (ctx.IsPrimary && ctx.WifiLagActive)
            {
                speedMult *= (float)cfg.Night.DeskEvent.WifiSpeedMultiplier;
            }

            float speedDrain = ctx.PrimarySpeed switch
            {
                SpeedMode.X2 => (float)cfg.Speed.X2.Drain,
                SpeedMode.X5 => (float)cfg.Speed.X5.Drain,
                _ => 1.0f
            };
            if (hasFan && speedDrain > 1.0f)
            {
                speedDrain = 1.0f + ((speedDrain - 1.0f) * (float)cfg.GetUpgrade("HW_02").Value);
            }
            if (!ctx.IsPrimary) speedDrain = 1.0f;

            BrightnessSettings brightness = cfg.Brightness.For(ctx.Brightness);
            float brightnessDrain = (float)brightness.Drain;

            // Niche-specific battery drain & dopamine modifier on the primary phone
            float nicheDrain = 1.0f;
            double nicheDopamine = 1.0;
            if (ctx.IsPrimary && ctx.PrimaryVideo == VideoCategory.Standard)
            {
                NicheSettings? niche = cfg.Niches.For(ctx.PrimaryNiche);
                if (niche != null)
                {
                    nicheDopamine = niche.Dopamine;
                    nicheDrain = (float)niche.Drain;
                }
            }

            float brightnessDopamine = (float)brightness.Dopamine;
            var attention = cfg.GetUpgrade("DOP_01");
            float attentionThreshold = Math.Max((float)attention.Param("minThreshold"), 1.0f - (attentionLvl * (float)attention.Value));

            int activeSecondaryCount = Math.Max(0, ctx.SecondaryPhoneCount);
            double syncBonus = syncLvl > 0 ? (1.0 + activeSecondaryCount * cfg.GetUpgrade("DOP_07").Value * syncLvl) : 1.0;
            double viralBonus = (ctx.IsPrimary && viralLvl > 0) ? (1.0 + (ctx.ViralStreak * cfg.GetUpgrade("DOP_08").Value * viralLvl)) : 1.0;
            double splitBonus = (ctx.IsPrimary && splitScreen) ? cfg.GetUpgrade("DOP_10").Value : 1.0;
            double mythic0Bonus = save.HasMythic("MYTHIC_0") ? cfg.GetMythic("MYTHIC_0").Value : 1.0;
            double mythic7Doubler = save.HasMythic("MYTHIC_7") ? cfg.GetMythic("MYTHIC_7").Value : 1.0;
            double achievementBonus = 1.0 + (save.UnlockedAchievementIds.Count * cfg.Achievements.DopamineBonusEach);
            double goldenWaveBonus = ctx.GoldenWaveActive ? cfg.Night.DeskEvent.GoldenWaveDopamineMultiplier : 1.0;
            double feverBonus = (ctx.IsPrimary && ctx.FeverActive) ? cfg.Combo.FeverDopamineMultiplier : 1.0;

            double baseDopamine = cfg.Phone.BaseDopamine
                * nicheDopamine
                * (1.0 + (algoLvl * cfg.GetUpgrade("DOP_04").Value))
                * syncBonus
                * viralBonus
                * splitBonus
                * Math.Pow(cfg.GetUpgrade("DOP_09").Value, infOverclock)
                * (1.0 + (grassDopamine * cfg.GetUpgrade("GRASS_01").Value))
                * (1.0 + (save.UnlockedMythicIds.Count * cfg.Mythics.DopamineBonusEach))
                * achievementBonus
                * goldenWaveBonus
                * feverBonus
                * mythic0Bonus
                * mythic7Doubler
                * brightnessDopamine;

            if (ctx.IsPrimary)
            {
                baseDopamine *= BlurDopamineMultiplier(cfg, ctx.Blur);
                if (ctx.PrimaryVideo == VideoCategory.Standard) baseDopamine *= 1.0 - Math.Clamp(ctx.NicheBoredom, 0f, 1f);
            }

            if (!ctx.IsPrimary)
            {
                baseDopamine *= ctx.Role == SecondaryRole.CryptoMiner
                    ? cfg.SecondaryRoles.CryptoMinerDopamineFactor
                    : cfg.SecondaryRoles.DopamineFactor;
            }

            return new PhoneStats(
                maxBatterySeconds: MaxBatterySeconds(cfg, save),
                baseDrainPerSecond: (float)cfg.Phone.BaseDrainPerSecond * brightnessDrain * nicheDrain,
                speedMultiplier: speedMult,
                speedDrainMultiplier: speedDrain,
                attentionThreshold: attentionThreshold,
                transitionSeconds: ctx.IsPrimary ? (float)cfg.Phone.PrimaryTransitionSeconds : (float)cfg.Phone.SecondaryTransitionSeconds,
                baseDopamineReward: Math.Round(baseDopamine, 1),
                mythicMultiplier: cfg.Phone.MythicMultiplier,
                likeBonusRatio: cfg.Phone.LikeBonusRatio,
                powerBankRatio: (ctx.IsPrimary && hasPowerBank) ? (float)cfg.GetUpgrade("HW_03").Value : 0f,
                critMultiplier: cfg.Phone.CritMultiplier,
                adDrainMultiplier: (float)cfg.Phone.AdDrainMultiplier,
                mythicLikedCompletionMultiplier: cfg.Phone.MythicLikedCompletionMultiplier);
        }

        public static float MaxBatterySeconds(BalanceConfig cfg, SaveData save)
        {
            int grassBattery = save.GrassLevel("GRASS_05");
            float mythic6 = save.HasMythic("MYTHIC_6") ? (float)cfg.GetMythic("MYTHIC_6").Value : 1.0f;
            return (float)cfg.Phone.BaseBatterySeconds * (1f + grassBattery * (float)cfg.GetUpgrade("GRASS_05").Value) * mythic6;
        }

        public static float EyeFatiguePerSecond(
            BalanceConfig cfg, SaveData save, BrightnessMode brightness, bool blackoutActive,
            VideoCategory primaryVideo, VideoNiche primaryNiche)
        {
            float fatigue = (float)cfg.Eyes.FatiguePerSecond;
            fatigue *= (float)cfg.Brightness.For(brightness).Fatigue;
            if (blackoutActive) fatigue *= (float)cfg.Eyes.BlackoutFatigueMultiplier;

            // Niche-specific eye fatigue modifier on the primary phone
            if (primaryVideo == VideoCategory.Standard)
            {
                NicheSettings? niche = cfg.Niches.For(primaryNiche);
                if (niche != null) fatigue *= (float)niche.Fatigue;
            }

            if (save.HasMythic("MYTHIC_2")) fatigue *= (float)cfg.GetMythic("MYTHIC_2").Value;
            if (save.RunLevel("SNK_01") > 0) fatigue *= 1f - (float)cfg.GetUpgrade("SNK_01").Value;
            return fatigue;
        }

        /// <summary>Phone #1 dopamine multiplier for blurry eyes (1 when sharp).</summary>
        public static double BlurDopamineMultiplier(BalanceConfig cfg, float blur) =>
            1.0 - Math.Clamp(blur, 0f, 1f) * cfg.Eyes.BlurDopaminePenalty;

        /// <summary>Fraction by which the Triple Espresso slows eye fatigue once the blur starts.</summary>
        public static float CoffeeFatigueSlow(BalanceConfig cfg, SaveData save) =>
            save.RunLevel("HW_05") > 0 ? (float)cfg.GetUpgrade("HW_05").Value : 0.0f;

        /// <summary>Multiplier on the time between Boss pings (Triple Espresso).</summary>
        public static float LaptopIntervalScale(BalanceConfig cfg, SaveData save) =>
            save.RunLevel("HW_05") > 0 ? (float)cfg.GetUpgrade("HW_05").Param("laptopIntervalScale") : 1.0f;

        public static float OutletLoadPerSecond(
            BalanceConfig cfg, SaveData save, int activeSecondaryPhones, BrightnessMode brightness, SpeedMode primarySpeed,
            bool charging = false)
        {
            var outlet = cfg.Outlet;
            bool hasFan = save.RunLevel("HW_02") > 0;
            return (activeSecondaryPhones * (float)outlet.PerActiveSecondaryPhone)
                + (hasFan ? (float)outlet.Fan : 0f)
                + (brightness == BrightnessMode.High ? (float)outlet.HighBrightness : 0f)
                + (primarySpeed == SpeedMode.X5 ? (float)outlet.OverclockX5 : primarySpeed == SpeedMode.X2 ? (float)outlet.OverclockX2 : 0f)
                + (charging ? (float)cfg.Charger.OutletLoad : 0f);
        }

        /// <summary>Battery seconds phone #1 gains per second on the charger.</summary>
        public static float ChargePerSecond(BalanceConfig cfg) => (float)cfg.Charger.ChargePerSecond;

        /// <summary>Do Not Disturb (SNK_02) bought for tonight: the boss cannot ping.</summary>
        public static bool BossMuted(SaveData save) => save.RunLevel("SNK_02") > 0;

        public static float SafeOutletLoad(BalanceConfig cfg, SaveData save) =>
            save.RunLevel("HW_07") > 0 ? (float)cfg.GetUpgrade("HW_07").Value : (float)cfg.Outlet.SafeLoad;

        /// <summary>Delay before the Auto-Scroll Bot (DOP_02) swipes the primary phone.</summary>
        public static float PrimaryAutoScrollDelay(BalanceConfig cfg, SaveData save)
        {
            var bot = cfg.GetUpgrade("DOP_02");
            int level = save.RunLevel("DOP_02");
            return Math.Max((float)bot.Param("minDelaySeconds"), (float)bot.Param("baseDelaySeconds") - (level * (float)bot.Param("delayReductionPerLevel")));
        }

        public static float SecondaryAutoScrollDelay(BalanceConfig cfg, SaveData save) =>
            save.HasMythic("MYTHIC_3") ? (float)cfg.GetMythic("MYTHIC_3").Value : (float)cfg.Phone.SecondaryAutoScrollSeconds;

        public static double CritChance(BalanceConfig cfg, SaveData save, bool guaranteedCrit)
        {
            double chance = (save.RunLevel("DOP_06") > 0 ? cfg.GetUpgrade("DOP_06").Value : 0.0)
                            + (save.HasMythic("MYTHIC_5") ? cfg.GetMythic("MYTHIC_5").Value : 0.0);
            return guaranteedCrit ? 1.0 : chance;
        }

        public static float AdChance(BalanceConfig cfg, SaveData save, bool isPrimary, SecondaryRole role, bool sponsorPenaltyActive)
        {
            float adChance = Math.Max((float)cfg.Feed.MinAdChance, (float)cfg.Feed.AdChance - (save.RunLevel("DOP_05") * (float)cfg.GetUpgrade("DOP_05").Value));
            if (sponsorPenaltyActive) adChance += (float)cfg.Night.DeskEvent.SponsorAdPenalty;
            if (save.HasMythic("MYTHIC_4")) adChance *= (float)cfg.GetMythic("MYTHIC_4").Value;
            if (!isPrimary && role == SecondaryRole.AdShield)
            {
                adChance *= (float)cfg.SecondaryRoles.AdShieldAdMultiplier;
            }
            return adChance;
        }

        public static float MythicChance(BalanceConfig cfg, bool isPrimary, SecondaryRole role) =>
            (!isPrimary && role == SecondaryRole.MythicRadar)
                ? (float)cfg.SecondaryRoles.MythicRadarMythicChance
                : (float)cfg.Feed.MythicChance;

        public static float VideoSeconds(BalanceConfig cfg, VideoCategory category, VideoNiche niche)
        {
            if (category == VideoCategory.Standard && cfg.Niches.For(niche)?.VideoSeconds is double nicheSeconds)
            {
                return (float)nicheSeconds;
            }
            return (float)cfg.Phone.VideoSeconds;
        }

        public static double DailyBaseSalary(BalanceConfig cfg, SaveData save) =>
            cfg.Laptop.BaseSalary
            + (cfg.Laptop.RaisePerNight * Math.Max(0, save.CurrentDay - 1))
            + (save.GrassLevel("GRASS_03") * cfg.GetUpgrade("GRASS_03").Value)
            + (save.HasMythic("MYTHIC_1") ? cfg.GetMythic("MYTHIC_1").Value : 0.0);

        /// <summary>Tonight's pay: base salary times attendance, cut while Do Not Disturb was on.</summary>
        public static double NightSalary(BalanceConfig cfg, SaveData save, float attendance)
        {
            double salary = DailyBaseSalary(cfg, save) * attendance;
            if (BossMuted(save)) salary *= 1.0 - cfg.GetUpgrade("SNK_02").Value;
            return Math.Round(salary, 2);
        }

        /// <summary>Creator mode (CRT_01): can a video be posted after tonight?</summary>
        public static bool CanPostVideo(SaveData save) => save.RunLevel("CRT_01") > 0 && save.LastPostNight != save.CurrentDay;

        /// <summary>Dopamine a creator post spends: a share of the wallet, so it stays a choice at any scale.</summary>
        public static double CreatorPostCost(BalanceConfig cfg, SaveData save) =>
            Math.Floor(save.CurrentDopamine * cfg.GetUpgrade("CRT_01").Param("dopamineShare"));

        /// <summary>Cash a creator post pays: grows with the creator level and with the night number.</summary>
        public static double CreatorPostCash(BalanceConfig cfg, SaveData save)
        {
            var creator = cfg.GetUpgrade("CRT_01");
            int level = save.RunLevel("CRT_01");
            if (level <= 0) return 0.0;
            return creator.Param("cashBase") + creator.Param("cashPerLevel") * (level - 1) + creator.Param("cashPerNight") * save.CurrentDay;
        }

        /// <summary>Number of secondary phones on the desk (HW_01 level, capped by its maxLevel).</summary>
        public static int SecondaryPhoneCount(BalanceConfig cfg, SaveData save) =>
            Math.Min(MaxSecondaryPhones(cfg), save.RunLevel("HW_01"));

        public static int MaxSecondaryPhones(BalanceConfig cfg) => cfg.GetUpgrade("HW_01").MaxLevel ?? 0;
    }
}
