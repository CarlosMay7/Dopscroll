using Doomscroll.Core.Events;
using Doomscroll.Core.Services;

namespace Doomscroll.Core.Tests
{
    /// <summary>
    /// Golden values recorded from the game before the balance moved into Data/balance.json
    /// (the controller's original hard-coded formula). They run against Fixtures/balance_reference.json,
    /// the pre-retune numbers with every newer mechanic neutral, so they stay bit-for-bit identical
    /// while Data/balance.json is retuned.
    /// </summary>
    public class RunFormulasTests
    {
        [Fact]
        public void FreshSave_PrimaryWatchingCats()
        {
            var ctx = new PhoneStatsContext
            {
                IsPrimary = true, Brightness = BrightnessMode.Normal, PrimarySpeed = SpeedMode.X1,
                PrimaryVideo = VideoCategory.Standard, PrimaryNiche = VideoNiche.Cats
            };

            var stats = RunFormulas.BuildPhoneStats(TestData.Reference(), TestData.FreshSave(), in ctx);

            Assert.Equal(11.0, stats.BaseDopamineReward);
            Assert.Equal(60f, stats.MaxBatterySeconds);
            Assert.Equal(1f, stats.BaseDrainPerSecond);
            Assert.Equal(1f, stats.AttentionThreshold);
            Assert.Equal(0.32f, stats.TransitionSeconds);
            Assert.Equal(50.0, stats.MythicMultiplier);
            Assert.Equal(0.25, stats.LikeBonusRatio);
            Assert.Equal(0f, stats.PowerBankRatio);
        }

        [Fact]
        public void FreshSave_FeverOnGamingAtHighBrightnessAndX5()
        {
            var ctx = new PhoneStatsContext
            {
                IsPrimary = true, Brightness = BrightnessMode.High, PrimarySpeed = SpeedMode.X5,
                PrimaryVideo = VideoCategory.Standard, PrimaryNiche = VideoNiche.Gaming, FeverActive = true
            };

            var stats = RunFormulas.BuildPhoneStats(TestData.Reference(), TestData.FreshSave(), in ctx);

            Assert.Equal(56.5, stats.BaseDopamineReward);
            Assert.Equal(1.6200001f, stats.BaseDrainPerSecond);
            Assert.Equal(5f, stats.SpeedMultiplier);
            Assert.Equal(3.5f, stats.SpeedDrainMultiplier);
        }

        [Fact]
        public void FreshSave_CryptoMinerSecondaryEarnsAQuarter()
        {
            var ctx = new PhoneStatsContext
            {
                IsPrimary = false, Role = SecondaryRole.CryptoMiner, Brightness = BrightnessMode.Low,
                PrimarySpeed = SpeedMode.X1, PrimaryVideo = VideoCategory.Standard, PrimaryNiche = VideoNiche.Cats
            };

            var stats = RunFormulas.BuildPhoneStats(TestData.Reference(), TestData.FreshSave(), in ctx);

            Assert.Equal(2.5, stats.BaseDopamineReward);
            Assert.Equal(0.65f, stats.BaseDrainPerSecond);
            Assert.Equal(0.18f, stats.TransitionSeconds);
        }

        [Fact]
        public void LoadedSave_ViralStreakOnGamingWithFanAndOverclock()
        {
            var ctx = new PhoneStatsContext
            {
                IsPrimary = true, Brightness = BrightnessMode.High, PrimarySpeed = SpeedMode.X2,
                PrimaryVideo = VideoCategory.Standard, PrimaryNiche = VideoNiche.Gaming,
                ViralStreak = 3, SecondaryPhoneCount = 4
            };

            var stats = RunFormulas.BuildPhoneStats(TestData.Reference(), TestData.LoadedSave(), in ctx);

            Assert.Equal(2313.3, stats.BaseDopamineReward);
            Assert.Equal(89.28001f, stats.MaxBatterySeconds);
            Assert.Equal(1.6200001f, stats.BaseDrainPerSecond);
            Assert.Equal(2f, stats.SpeedMultiplier);
            Assert.Equal(1.4549999f, stats.SpeedDrainMultiplier);
            Assert.Equal(0.7f, stats.AttentionThreshold);
            Assert.Equal(0.35f, stats.PowerBankRatio);
        }

        [Fact]
        public void LoadedSave_GoldenWaveOnAMythic()
        {
            var ctx = new PhoneStatsContext
            {
                IsPrimary = true, Brightness = BrightnessMode.Normal, PrimarySpeed = SpeedMode.X1,
                PrimaryVideo = VideoCategory.Mythic, PrimaryNiche = VideoNiche.None,
                GoldenWaveActive = true, SecondaryPhoneCount = 4
            };

            var stats = RunFormulas.BuildPhoneStats(TestData.Reference(), TestData.LoadedSave(), in ctx);

            Assert.Equal(1658.4, stats.BaseDopamineReward);
        }

        [Fact]
        public void LoadedSave_SecondaryIgnoresPrimaryOverclock()
        {
            var ctx = new PhoneStatsContext
            {
                IsPrimary = false, Role = SecondaryRole.Standard, Brightness = BrightnessMode.Normal,
                PrimarySpeed = SpeedMode.X5, PrimaryVideo = VideoCategory.Standard, PrimaryNiche = VideoNiche.Memes,
                SecondaryPhoneCount = 4
            };

            var stats = RunFormulas.BuildPhoneStats(TestData.Reference(), TestData.LoadedSave(), in ctx);

            Assert.Equal(371.7, stats.BaseDopamineReward);
            Assert.Equal(1f, stats.SpeedMultiplier);
            Assert.Equal(1f, stats.SpeedDrainMultiplier);
            Assert.Equal(0f, stats.PowerBankRatio);
        }

        [Fact]
        public void FarmSave_BlackoutWithMemesAtLowBrightness()
        {
            var ctx = new PhoneStatsContext
            {
                IsPrimary = true, Brightness = BrightnessMode.Low, PrimarySpeed = SpeedMode.X2,
                PrimaryVideo = VideoCategory.Standard, PrimaryNiche = VideoNiche.Memes,
                BlackoutActive = true, SecondaryPhoneCount = 6
            };

            var stats = RunFormulas.BuildPhoneStats(TestData.Reference(), TestData.FarmSave(), in ctx);

            Assert.Equal(91.7, stats.BaseDopamineReward);
            Assert.Equal(0.74749994f, stats.BaseDrainPerSecond);
            Assert.Equal(1.65f, stats.SpeedDrainMultiplier);
        }

        [Fact]
        public void SmallFormulas_MatchTheOriginalGame()
        {
            var cfg = TestData.Reference();
            var loaded = TestData.LoadedSave();

            Assert.Equal(38.0, RunFormulas.DailyBaseSalary(cfg, loaded));
            Assert.Equal(4, RunFormulas.SecondaryPhoneCount(cfg, loaded));
            Assert.Equal(2.0f, RunFormulas.VideoSeconds(cfg, VideoCategory.Standard, VideoNiche.Memes));
            Assert.Equal(3.0f, RunFormulas.VideoSeconds(cfg, VideoCategory.Mythic, VideoNiche.None));
            Assert.Equal(0.32f, RunFormulas.SecondaryAutoScrollDelay(cfg, loaded));
            Assert.Equal(0.5f, RunFormulas.SecondaryAutoScrollDelay(cfg, TestData.FreshSave()));
            Assert.Equal(1.0, RunFormulas.CritChance(cfg, TestData.FreshSave(), guaranteedCrit: true));
            Assert.Equal(0.3, RunFormulas.CritChance(cfg, loaded, guaranteedCrit: false), precision: 12);
            Assert.Equal(14.0f, RunFormulas.SafeOutletLoad(cfg, new SaveData { RunUpgradeLevels = { ["HW_07"] = 1 } }));
            Assert.Equal(4.5f, RunFormulas.SafeOutletLoad(cfg, TestData.FreshSave()));
            Assert.Equal(0.012f, RunFormulas.MythicChance(cfg, isPrimary: false, SecondaryRole.MythicRadar));
            Assert.Equal(0.004f, RunFormulas.MythicChance(cfg, isPrimary: true, SecondaryRole.MythicRadar));
        }
    }
}
