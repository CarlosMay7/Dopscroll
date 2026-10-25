using System;
using Doomscroll.Core.Common;
using Doomscroll.Core.Events;

namespace Doomscroll.Core.Entities
{
    /// <summary>
    /// Immutable snapshot of derived phone stats calculated from upgrades, active niche,
    /// brightness mode, and unlocked Mythics. Avoids hardcoded magic numbers inside PhoneEntity.
    /// </summary>
    public readonly struct PhoneStats
    {
        public readonly float MaxBatterySeconds;
        public readonly float BaseDrainPerSecond;
        public readonly float SpeedMultiplier;
        public readonly float SpeedDrainMultiplier;
        public readonly float AttentionThreshold;
        public readonly float TransitionSeconds;
        public readonly double BaseDopamineReward;
        public readonly double MythicMultiplier;
        public readonly double LikeBonusRatio;
        public readonly float PowerBankRatio;
        public readonly double CritMultiplier;
        public readonly float AdDrainMultiplier;
        public readonly double MythicLikedCompletionMultiplier;

        public PhoneStats(
            float maxBatterySeconds,
            float baseDrainPerSecond,
            float speedMultiplier,
            float speedDrainMultiplier,
            float attentionThreshold,
            float transitionSeconds,
            double baseDopamineReward,
            double mythicMultiplier,
            double likeBonusRatio,
            float powerBankRatio,
            double critMultiplier,
            float adDrainMultiplier,
            double mythicLikedCompletionMultiplier)
        {
            MaxBatterySeconds = maxBatterySeconds;
            BaseDrainPerSecond = baseDrainPerSecond;
            SpeedMultiplier = speedMultiplier;
            SpeedDrainMultiplier = speedDrainMultiplier;
            AttentionThreshold = attentionThreshold;
            TransitionSeconds = transitionSeconds;
            BaseDopamineReward = baseDopamineReward;
            MythicMultiplier = mythicMultiplier;
            LikeBonusRatio = likeBonusRatio;
            PowerBankRatio = powerBankRatio;
            CritMultiplier = critMultiplier;
            AdDrainMultiplier = adDrainMultiplier;
            MythicLikedCompletionMultiplier = mythicLikedCompletionMultiplier;
        }
    }

    public class PhoneEntity
    {
        public int Id { get; }
        public bool IsPrimary { get; }
        public float CurrentBattery { get; private set; }
        public float VideoProgress { get; private set; }
        public float VideoRequiredDuration { get; private set; } = 3.0f;
        public SpeedMode CurrentSpeed { get; private set; } = SpeedMode.X1;
        public bool IsStunnedByScreamer { get; private set; }
        public bool IsInTransition { get; private set; }
        public bool IsCurrentVideoCompleted { get; private set; }
        public bool HasLikedCurrentVideo { get; private set; }
        public bool PowerBankUsed { get; private set; }
        public VideoCategory CurrentVideo { get; private set; } = VideoCategory.Standard;
        public VideoNiche CurrentNiche { get; private set; } = VideoNiche.Cats;
        public SecondaryRole Role { get; set; } = SecondaryRole.Standard;
        public float StateTimer => Math.Max(0f, _stateTimer);

        public bool HasBattery => CurrentBattery > 0f;

        private readonly EventBus _eventBus;
        private float _stateTimer;

        public PhoneEntity(int id, bool isPrimary, float initialBatterySeconds, EventBus eventBus)
        {
            Id = id;
            IsPrimary = isPrimary;
            CurrentBattery = initialBatterySeconds;
            _eventBus = eventBus;
        }

        public void SetSpeed(SpeedMode mode) => CurrentSpeed = mode;

        /// <summary>Charger: adds battery up to the phone's capacity (a dead phone comes back on).</summary>
        public void Recharge(float amount, float maxBatterySeconds)
        {
            if (amount <= 0f) return;
            CurrentBattery = Math.Min(maxBatterySeconds, CurrentBattery + amount);
        }

        public void Tick(float deltaTime, in PhoneStats stats)
        {
            if (!HasBattery) return;

            // Phone continues draining baseline battery during Screamer stun or scroll transition.
            float drainRate = stats.BaseDrainPerSecond;
            if (!IsStunnedByScreamer && !IsInTransition && !IsCurrentVideoCompleted)
            {
                drainRate *= stats.SpeedDrainMultiplier;
                if (CurrentVideo == VideoCategory.AdSpam)
                {
                    drainRate *= stats.AdDrainMultiplier;
                }
            }

            ConsumeBattery(drainRate * deltaTime, in stats);
            if (!HasBattery) return;

            if (IsStunnedByScreamer)
            {
                _stateTimer -= deltaTime;
                if (_stateTimer <= 0f) IsStunnedByScreamer = false;
                return;
            }

            if (IsInTransition)
            {
                _stateTimer -= deltaTime;
                if (_stateTimer <= 0f) IsInTransition = false;
                return;
            }

            // Once the current video reaches 100% and grants its completion reward, wait for Scroll.
            if (IsCurrentVideoCompleted) return;

            VideoProgress += deltaTime * stats.SpeedMultiplier;
            if (VideoProgress >= VideoRequiredDuration)
            {
                VideoProgress = VideoRequiredDuration;
                CompleteCurrentVideo(in stats);
            }
        }

        public bool IsEarlySwipeReady(float attentionThreshold)
        {
            if (!HasBattery || IsStunnedByScreamer || IsInTransition || IsCurrentVideoCompleted)
                return false;
            if (CurrentVideo == VideoCategory.AdSpam || CurrentVideo == VideoCategory.Screamer)
                return false;

            return VideoProgress >= (VideoRequiredDuration * attentionThreshold);
        }

        public bool TryGiveLike(in PhoneStats stats, bool isCritical, out double bonusEarned)
        {
            bonusEarned = 0;
            if (!HasBattery || IsStunnedByScreamer || IsInTransition || HasLikedCurrentVideo)
                return false;

            HasLikedCurrentVideo = true;

            if (CurrentVideo == VideoCategory.AdSpam)
            {
                _eventBus.Publish(new VideoLikedEvent(Id, CurrentVideo, CurrentNiche, 0, false));
                return true;
            }

            bonusEarned = stats.BaseDopamineReward * stats.LikeBonusRatio * (isCritical ? stats.CritMultiplier : 1.0);
            if (CurrentVideo == VideoCategory.Mythic)
            {
                bonusEarned *= stats.MythicMultiplier;
            }

            _eventBus.Publish(new VideoLikedEvent(Id, CurrentVideo, CurrentNiche, bonusEarned, isCritical));
            return true;
        }

        public bool ScrollToNextVideo(
            VideoCategory nextCategory,
            VideoNiche nextNiche,
            float duration,
            float transitionSeconds,
            in PhoneStats stats,
            float screamerStunSeconds,
            bool isInitialLoad = false)
        {
            if (!HasBattery || IsStunnedByScreamer || IsInTransition)
                return false;

            // If the player swipes early after crossing the Short Attention Span threshold,
            // grant the full video completion Dopamine before transitioning to the next video!
            if (!isInitialLoad && IsEarlySwipeReady(stats.AttentionThreshold))
            {
                CompleteCurrentVideo(in stats);
            }

            VideoProgress = 0f;
            IsCurrentVideoCompleted = false;
            HasLikedCurrentVideo = false;
            CurrentVideo = nextCategory;
            CurrentNiche = nextNiche;
            VideoRequiredDuration = duration;

            if (CurrentVideo == VideoCategory.Screamer)
            {
                IsStunnedByScreamer = true;
                _stateTimer = screamerStunSeconds;
                _eventBus.Publish(new ScreamerTriggeredEvent(Id));
            }
            else if (transitionSeconds > 0f)
            {
                IsInTransition = true;
                _stateTimer = transitionSeconds;
            }

            return true;
        }

        private void CompleteCurrentVideo(in PhoneStats stats)
        {
            IsCurrentVideoCompleted = true;
            double reward = CurrentVideo switch
            {
                VideoCategory.Standard => stats.BaseDopamineReward,
                VideoCategory.Mythic => stats.BaseDopamineReward * stats.MythicMultiplier * (HasLikedCurrentVideo ? stats.MythicLikedCompletionMultiplier : 1.0),
                _ => 0
            };

            _eventBus.Publish(new VideoCompletedEvent(Id, CurrentVideo, CurrentNiche, reward));
        }

        private void ConsumeBattery(float amount, in PhoneStats stats)
        {
            CurrentBattery -= amount;
            if (CurrentBattery <= 0f && IsPrimary && !PowerBankUsed && stats.PowerBankRatio > 0f)
            {
                PowerBankUsed = true;
                CurrentBattery += stats.MaxBatterySeconds * stats.PowerBankRatio;
            }

            if (CurrentBattery <= 0f)
            {
                CurrentBattery = 0f;
                _eventBus.Publish(new PhoneBatteryDepletedEvent(Id));
            }
        }
    }
}
