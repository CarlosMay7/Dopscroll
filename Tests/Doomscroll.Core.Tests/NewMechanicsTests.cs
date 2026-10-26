using System.Collections.Generic;
using Doomscroll.Core.Common;
using Doomscroll.Core.Entities;
using Doomscroll.Core.Events;
using Doomscroll.Core.Services;
using Doomscroll.Core.UseCases;

namespace Doomscroll.Core.Tests
{
    /// <summary>
    /// Charger, blur penalty, niche boredom, consumables, salary raise, creator posts and the voluntary
    /// Touch Grass bonus, checked against the shipped Data/balance.json.
    /// </summary>
    public class NewMechanicsTests
    {
        private static PhoneStatsContext Primary(VideoNiche niche, float blur = 0f, float boredom = 0f, VideoCategory video = VideoCategory.Standard) => new()
        {
            IsPrimary = true,
            Brightness = BrightnessMode.Normal,
            PrimarySpeed = SpeedMode.X1,
            PrimaryVideo = video,
            PrimaryNiche = niche,
            Blur = blur,
            NicheBoredom = boredom
        };

        [Fact]
        public void NeutralNewMechanics_KeepTheReferenceNumbers()
        {
            var cfg = TestData.Reference();
            var save = TestData.LoadedSave();

            var plain = RunFormulas.BuildPhoneStats(cfg, save, Primary(VideoNiche.Gaming));
            var blurred = RunFormulas.BuildPhoneStats(cfg, save, Primary(VideoNiche.Gaming, blur: 1f, boredom: 0f));

            Assert.Equal(plain.BaseDopamineReward, blurred.BaseDopamineReward);
        }

        [Theory]
        [InlineData(0f, 1.0)]
        [InlineData(0.5f, 0.75)]
        [InlineData(1f, 0.5)]
        public void Blur_CutsPhoneOneDopamine(float blur, double expected)
        {
            Assert.Equal(expected, RunFormulas.BlurDopamineMultiplier(TestData.Balance(), blur), precision: 10);
        }

        [Fact]
        public void Blur_AndBoredom_OnlyTouchPhoneOne()
        {
            var cfg = TestData.Balance();
            var save = TestData.FreshSave();

            double sharp = RunFormulas.BuildPhoneStats(cfg, save, Primary(VideoNiche.Asmr)).BaseDopamineReward;
            double tired = RunFormulas.BuildPhoneStats(cfg, save, Primary(VideoNiche.Asmr, blur: 1f, boredom: 0.4f)).BaseDopamineReward;
            var secondary = new PhoneStatsContext { IsPrimary = false, Role = SecondaryRole.Standard, Blur = 1f, NicheBoredom = 0.4f };
            var secondaryClean = new PhoneStatsContext { IsPrimary = false, Role = SecondaryRole.Standard };

            Assert.Equal(9.0, sharp);
            Assert.Equal(2.7, tired); // 9 * 0.5 (blur) * 0.6 (boredom)
            Assert.Equal(
                RunFormulas.BuildPhoneStats(cfg, save, secondaryClean).BaseDopamineReward,
                RunFormulas.BuildPhoneStats(cfg, save, secondary).BaseDopamineReward);
        }

        [Fact]
        public void Boredom_SkipsMythicVideos()
        {
            var cfg = TestData.Balance();
            var save = TestData.FreshSave();

            double fresh = RunFormulas.BuildPhoneStats(cfg, save, Primary(VideoNiche.None, video: VideoCategory.Mythic)).BaseDopamineReward;
            double bored = RunFormulas.BuildPhoneStats(cfg, save, Primary(VideoNiche.None, boredom: 0.4f, video: VideoCategory.Mythic)).BaseDopamineReward;

            Assert.Equal(fresh, bored);
        }

        [Fact]
        public void NicheTolerance_BuildsUpRecoversAndCaps()
        {
            var tol = TestData.Balance().Tolerance;
            var boredom = new NicheTolerance((float)tol.PerVideo, (float)tol.RecoveryPerVideo, (float)tol.MaxPenalty);

            for (int i = 0; i < 3; i++) boredom.RegisterWatched(VideoNiche.Cats);
            Assert.Equal(0.15f, boredom.Boredom(VideoNiche.Cats), 4);

            boredom.RegisterWatched(VideoNiche.Gaming);
            Assert.Equal(0.12f, boredom.Boredom(VideoNiche.Cats), 4);
            Assert.Equal(0.05f, boredom.Boredom(VideoNiche.Gaming), 4);

            for (int i = 0; i < 20; i++) boredom.RegisterWatched(VideoNiche.Memes);
            Assert.Equal(0.4f, boredom.Boredom(VideoNiche.Memes), 4);
            Assert.Equal(0f, boredom.Boredom(VideoNiche.Cats));
        }

        [Fact]
        public void Charger_RefillsPhoneOneAndRevivesIt()
        {
            var cfg = TestData.Balance();
            var bus = new EventBus();
            var phone = new PhoneEntity(1, isPrimary: true, initialBatterySeconds: 1f, bus);
            var stats = RunFormulas.BuildPhoneStats(cfg, TestData.FreshSave(), Primary(VideoNiche.Cats));
            phone.ScrollToNextVideo(VideoCategory.Standard, VideoNiche.Cats, 3f, 0f, stats, 1.5f, isInitialLoad: true);
            var session = new TurnSession(new[] { phone }, new EyeState(), Laptop(), 90f, bus);

            session.Tick(1.5f, stats, new List<PhoneStats> { stats }, 0f, 0f, 1f, false, RunFormulas.ChargePerSecond(cfg));

            Assert.True(phone.HasBattery);
            Assert.Equal(2.5f, phone.CurrentBattery, 3); // 1 + (2.0 - 1.0) * 1.5
            phone.Recharge(500f, stats.MaxBatterySeconds);
            Assert.Equal(stats.MaxBatterySeconds, phone.CurrentBattery);
        }

        [Fact]
        public void Charger_LoadsTheOutlet()
        {
            var cfg = TestData.Balance();
            var save = TestData.FreshSave();
            float idle = RunFormulas.OutletLoadPerSecond(cfg, save, 2, BrightnessMode.Normal, SpeedMode.X1);
            float charging = RunFormulas.OutletLoadPerSecond(cfg, save, 2, BrightnessMode.Normal, SpeedMode.X1, charging: true);

            Assert.Equal(2.8f, idle, 4);
            Assert.Equal(5.3f, charging, 4);
        }

        [Fact]
        public void EnergyDrink_SlowsEyeFatigue()
        {
            var cfg = TestData.Balance();
            var save = TestData.FreshSave();
            float normal = RunFormulas.EyeFatiguePerSecond(cfg, save, BrightnessMode.Normal, false, VideoCategory.AdSpam, VideoNiche.None);
            save.RunUpgradeLevels["SNK_01"] = 1;

            Assert.Equal(normal * 0.6f, RunFormulas.EyeFatiguePerSecond(cfg, save, BrightnessMode.Normal, false, VideoCategory.AdSpam, VideoNiche.None), 4);
        }

        [Fact]
        public void DoNotDisturb_MutesTheBossAndCutsPay()
        {
            var cfg = TestData.Balance();
            var save = new SaveData { CurrentDay = 5 };
            Assert.Equal(22.0, RunFormulas.NightSalary(cfg, save, 1f)); // 18 + 4 nights of raises

            save.RunUpgradeLevels["SNK_02"] = 1;
            Assert.True(RunFormulas.BossMuted(save));
            Assert.Equal(11.0, RunFormulas.NightSalary(cfg, save, 1f));

            var laptop = Laptop();
            for (int i = 0; i < 600; i++) laptop.Tick(0.1f, muted: true);
            Assert.Equal(0, laptop.TotalAlertsTriggered);
            Assert.Equal(1f, laptop.GetAttendanceRatio());
        }

        [Fact]
        public void CreatorPost_SpendsAShareAndPaysByLevelAndNight()
        {
            var cfg = TestData.Balance();
            var save = new SaveData { CurrentDay = 10, CurrentDopamine = 50_000 };
            Assert.False(RunFormulas.CanPostVideo(save));
            Assert.Equal(0.0, RunFormulas.CreatorPostCash(cfg, save));

            save.RunUpgradeLevels["CRT_01"] = 3;

            Assert.True(RunFormulas.CanPostVideo(save));
            Assert.Equal(15_000.0, RunFormulas.CreatorPostCost(cfg, save));
            Assert.Equal(26.0, RunFormulas.CreatorPostCash(cfg, save)); // 8 + 4*2 + 1*10
            save.LastPostNight = 10;
            Assert.False(RunFormulas.CanPostVideo(save));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(4, 1)]
        [InlineData(10, 3)]
        [InlineData(13, 3)]
        public void VoluntaryGrass_AddsAQuarterAndAtLeastOne(int baseReward, int bonus)
        {
            Assert.Equal(bonus, new EconomyService(TestData.Balance()).VoluntaryGrassBonus(baseReward));
        }

        [Fact]
        public void TouchGrass_VoluntaryPaysTheBonusAndForgetsTheLastPost()
        {
            var cfg = TestData.Balance();
            var economy = new EconomyService(cfg);
            var forced = new SaveData { CurrentDay = 5, TotalRunDopamine = 1_091_462, LastPostNight = 5 };
            var chosen = new SaveData { CurrentDay = 5, TotalRunDopamine = 1_091_462, LastPostNight = 5 };

            int forcedPoints = new PrestigeResetUseCase(economy, cfg).ExecuteTouchGrass(forced);
            int chosenPoints = new PrestigeResetUseCase(economy, cfg).ExecuteTouchGrass(chosen, voluntary: true);

            Assert.Equal(16, forcedPoints);
            Assert.Equal(20, chosenPoints);
            Assert.Equal(0, chosen.LastPostNight);
        }

        [Fact]
        public void MythicJackpot_IsCapped()
        {
            var phone = TestData.Balance().Phone;
            Assert.Equal(12.0, phone.MythicMultiplier);
            Assert.Equal(1.5, phone.MythicLikedCompletionMultiplier);
        }

        private static WorkLaptop Laptop() => new(new System.Random(1), 11f, 16f, 6f, 0.5, 3, 2.5f);
    }
}
