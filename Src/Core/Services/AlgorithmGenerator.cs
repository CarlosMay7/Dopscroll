using System;
using System.Collections.Generic;
using Doomscroll.Core.Events;

namespace Doomscroll.Core.Services
{
    public class AlgorithmGenerator
    {
        private readonly Random _rng;
        private readonly Dictionary<VideoNiche, float> _nicheWeights = new()
        {
            { VideoNiche.Cats, 1f },
            { VideoNiche.Gaming, 1f },
            { VideoNiche.Asmr, 1f },
            { VideoNiche.Memes, 1f }
        };

        private int _badLikePenaltyRemaining;

        public int BadLikePenaltyRemaining => _badLikePenaltyRemaining;

        public AlgorithmGenerator(Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public float GetWeight(VideoNiche niche)
        {
            return _nicheWeights.TryGetValue(niche, out float w) ? w : 1f;
        }

        public float GetNormalizedShare(VideoNiche niche)
        {
            float total = 0f;
            foreach (var w in _nicheWeights.Values) total += w;
            if (total <= 0f) return 0.25f;
            return GetWeight(niche) / total;
        }

        public void ResetWeightsForNewTurn()
        {
            _nicheWeights[VideoNiche.Cats] = 1f;
            _nicheWeights[VideoNiche.Gaming] = 1f;
            _nicheWeights[VideoNiche.Asmr] = 1f;
            _nicheWeights[VideoNiche.Memes] = 1f;
            _badLikePenaltyRemaining = 0;
        }

        public void RegisterLike(VideoCategory category, VideoNiche niche, float weightGain, float maxWeight, int badLikeDuration)
        {
            if (category == VideoCategory.AdSpam)
            {
                _badLikePenaltyRemaining = badLikeDuration;
                return;
            }

            if (category == VideoCategory.Standard && _nicheWeights.TryGetValue(niche, out float current))
            {
                _nicheWeights[niche] = Math.Min(maxWeight, current + weightGain);
            }
        }

        public (VideoCategory Category, VideoNiche Niche) RollNextVideo(
            ref int mythicPityCounter,
            int mythicPityLimit,
            float mythicChance,
            float screamerChance,
            float baseAdChance,
            float pollutedAdChance)
        {
            mythicPityCounter++;
            if (mythicPityCounter >= mythicPityLimit)
            {
                mythicPityCounter = 0;
                return (VideoCategory.Mythic, VideoNiche.None);
            }

            double roll = _rng.NextDouble();
            if (roll < mythicChance)
            {
                mythicPityCounter = 0;
                return (VideoCategory.Mythic, VideoNiche.None);
            }
            roll -= mythicChance;

            if (roll < screamerChance)
            {
                return (VideoCategory.Screamer, VideoNiche.None);
            }
            roll -= screamerChance;

            float adChance = _badLikePenaltyRemaining > 0 ? pollutedAdChance : baseAdChance;
            if (_badLikePenaltyRemaining > 0) _badLikePenaltyRemaining--;

            if (roll < adChance)
            {
                return (VideoCategory.AdSpam, VideoNiche.None);
            }

            return (VideoCategory.Standard, RollWeightedNiche());
        }

        private VideoNiche RollWeightedNiche()
        {
            float total = 0f;
            foreach (var w in _nicheWeights.Values) total += w;

            float pick = (float)_rng.NextDouble() * total;
            foreach (var kvp in _nicheWeights)
            {
                if (pick <= kvp.Value) return kvp.Key;
                pick -= kvp.Value;
            }
            return VideoNiche.Cats;
        }
    }
}
