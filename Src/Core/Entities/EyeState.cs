using System;

namespace Doomscroll.Core.Entities
{
    public class EyeState
    {
        public float CurrentFreshness { get; private set; } = 100f;
        public float BlurStartsBelow { get; }
        public int RemainingEyeDrops { get; private set; }

        public EyeState(int eyeDropsCount = 0, float blurStartsBelow = 55f)
        {
            RemainingEyeDrops = eyeDropsCount;
            BlurStartsBelow = blurStartsBelow;
        }

        public float GetNormalizedBlur(float lensBlurReduction = 0f)
        {
            if (CurrentFreshness >= BlurStartsBelow) return 0f;
            float rawBlur = Math.Clamp((BlurStartsBelow - CurrentFreshness) / BlurStartsBelow, 0f, 1f);
            return rawBlur * (1f - Math.Clamp(lensBlurReduction, 0f, 0.9f));
        }

        public void Tick(float deltaTime, float fatiguePerSecond, float coffeeSlowFactor = 0f, float dropsUseBelow = 20f, float dropsRestore = 40f)
        {
            float rate = fatiguePerSecond;
            if (CurrentFreshness < BlurStartsBelow && coffeeSlowFactor > 0f)
            {
                rate *= 1f - coffeeSlowFactor;
            }

            CurrentFreshness = Math.Max(0f, CurrentFreshness - rate * deltaTime);

            if (CurrentFreshness < dropsUseBelow && RemainingEyeDrops > 0)
            {
                RemainingEyeDrops--;
                CurrentFreshness = Math.Min(100f, CurrentFreshness + dropsRestore);
            }
        }

        public void RestorePartial(float amount)
        {
            CurrentFreshness = Math.Min(100f, CurrentFreshness + amount);
        }

        public void RestoreFull()
        {
            CurrentFreshness = 100f;
        }
    }
}
