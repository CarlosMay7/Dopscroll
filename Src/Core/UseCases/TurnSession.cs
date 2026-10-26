using System.Collections.Generic;
using System.Linq;
using Doomscroll.Core.Common;
using Doomscroll.Core.Entities;
using Doomscroll.Core.Events;

namespace Doomscroll.Core.UseCases
{
    public class TurnSession
    {
        public float ElapsedSeconds { get; private set; }
        public float NightDurationSeconds { get; }
        public bool IsFinished { get; private set; }
        public IReadOnlyList<PhoneEntity> Phones => _phones;
        public EyeState Eyes { get; }
        public WorkLaptop Laptop { get; }

        private readonly List<PhoneEntity> _phones;
        private readonly EventBus _eventBus;

        public TurnSession(
            IEnumerable<PhoneEntity> phones,
            EyeState eyes,
            WorkLaptop laptop,
            float nightDurationSeconds,
            EventBus eventBus)
        {
            _phones = phones.ToList();
            Eyes = eyes;
            Laptop = laptop;
            NightDurationSeconds = nightDurationSeconds;
            _eventBus = eventBus;
        }

        /// <summary>
        /// Marks the night as finished without publishing a TurnEndedEvent
        /// (used when a save is resumed directly into the Night Shop).
        /// </summary>
        public void Abort()
        {
            IsFinished = true;
        }

        public void Tick(
            float deltaTime,
            in PhoneStats primaryStats,
            IReadOnlyList<PhoneStats> secondaryStatsByRole,
            float eyeFatiguePerSecond,
            float coffeeSlowFactor,
            float clockScale,
            bool hasDrinkingBird,
            float primaryChargePerSecond = 0f,
            bool bossMuted = false)
        {
            if (IsFinished) return;

            ElapsedSeconds += deltaTime;
            if (ElapsedSeconds >= NightDurationSeconds)
            {
                ElapsedSeconds = NightDurationSeconds;
                IsFinished = true;
                _eventBus.Publish(new TurnEndedEvent(TurnEndReason.DawnReached));
                return;
            }

            bool anyAlive = false;
            foreach (var phone in _phones)
            {
                if (phone.IsPrimary)
                {
                    phone.Recharge(primaryChargePerSecond * deltaTime, primaryStats.MaxBatterySeconds);
                    phone.Tick(deltaTime, in primaryStats);
                }
                else
                {
                    int roleIndex = (int)phone.Role;
                    PhoneStats roleStats = roleIndex >= 0 && roleIndex < secondaryStatsByRole.Count
                        ? secondaryStatsByRole[roleIndex]
                        : secondaryStatsByRole[0];
                    phone.Tick(deltaTime, in roleStats);
                }

                if (phone.HasBattery) anyAlive = true;
            }

            Eyes.Tick(deltaTime, eyeFatiguePerSecond, coffeeSlowFactor);
            Laptop.Tick(deltaTime, clockScale, hasDrinkingBird, bossMuted);

            if (!anyAlive)
            {
                IsFinished = true;
                _eventBus.Publish(new TurnEndedEvent(TurnEndReason.AllBatteriesDepleted));
            }
        }
    }
}
