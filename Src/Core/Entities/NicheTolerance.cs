using System;
using System.Collections.Generic;
using Doomscroll.Core.Events;

namespace Doomscroll.Core.Entities
{
    /// <summary>
    /// Niche boredom on phone #1: finishing a standard video makes its niche a bit more boring and lets the others
    /// recover. A bored niche pays less dopamine, so farming one niche (or one liked streak) has a cost.
    /// </summary>
    public sealed class NicheTolerance
    {
        private readonly Dictionary<VideoNiche, float> _boredom = new();
        private readonly float _perVideo;
        private readonly float _recoveryPerVideo;
        private readonly float _maxPenalty;

        public NicheTolerance(float perVideo, float recoveryPerVideo, float maxPenalty)
        {
            _perVideo = perVideo;
            _recoveryPerVideo = recoveryPerVideo;
            _maxPenalty = maxPenalty;
        }

        /// <summary>Dopamine share lost on this niche right now (0 = fresh).</summary>
        public float Boredom(VideoNiche niche) => _boredom.TryGetValue(niche, out float value) ? value : 0f;

        public void RegisterWatched(VideoNiche niche)
        {
            if (niche == VideoNiche.None) return;
            foreach (var other in new List<VideoNiche>(_boredom.Keys))
            {
                if (other != niche) _boredom[other] = Math.Max(0f, _boredom[other] - _recoveryPerVideo);
            }
            _boredom[niche] = Math.Min(_maxPenalty, Boredom(niche) + _perVideo);
        }
    }
}
