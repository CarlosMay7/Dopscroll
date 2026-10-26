namespace Doomscroll.Core.Events
{
    public enum VideoCategory { Standard, AdSpam, Mythic, Screamer }
    public enum VideoNiche { None, Cats, Gaming, Asmr, Memes }
    public enum SpeedMode { X1 = 1, X2 = 2, X5 = 5 }
    public enum TurnEndReason { AllBatteriesDepleted, DawnReached }
    public enum SecondaryRole { Standard, AdShield, CryptoMiner, MythicRadar }
    public enum BrightnessMode { Low, Normal, High }

    public readonly struct VideoCompletedEvent
    {
        public readonly int PhoneId;
        public readonly VideoCategory Category;
        public readonly VideoNiche Niche;
        public readonly double DopamineEarned;

        public VideoCompletedEvent(int phoneId, VideoCategory category, VideoNiche niche, double dopamineEarned)
        {
            PhoneId = phoneId;
            Category = category;
            Niche = niche;
            DopamineEarned = dopamineEarned;
        }
    }

    public readonly struct VideoLikedEvent
    {
        public readonly int PhoneId;
        public readonly VideoCategory Category;
        public readonly VideoNiche Niche;
        public readonly double BonusDopamine;
        public readonly bool IsCritical;

        public VideoLikedEvent(int phoneId, VideoCategory category, VideoNiche niche, double bonusDopamine, bool isCritical)
        {
            PhoneId = phoneId;
            Category = category;
            Niche = niche;
            BonusDopamine = bonusDopamine;
            IsCritical = isCritical;
        }
    }

    public readonly struct ScreamerTriggeredEvent
    {
        public readonly int PhoneId;
        public ScreamerTriggeredEvent(int phoneId) => PhoneId = phoneId;
    }

    public readonly struct PhoneBatteryDepletedEvent
    {
        public readonly int PhoneId;
        public PhoneBatteryDepletedEvent(int phoneId) => PhoneId = phoneId;
    }

    public readonly struct TurnEndedEvent
    {
        public readonly TurnEndReason Reason;
        public TurnEndedEvent(TurnEndReason reason) => Reason = reason;
    }
}
