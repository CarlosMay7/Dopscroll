using System;

namespace Doomscroll.Core.Entities
{
    public class WorkLaptop
    {
        public bool IsAlertActive { get; private set; }
        public bool IsAudit { get; private set; }
        public int AuditTapsRequired { get; private set; } = 1;
        public int AuditTapsCurrent { get; private set; } = 0;
        public bool LastAttendedWasAudit { get; private set; }
        public int TotalAlertsTriggered { get; private set; }
        public int AlertsAttended { get; private set; }
        public float RemainingTimer => Math.Max(0f, _timer);

        private readonly Random _rng;
        private readonly float _minInterval;
        private readonly float _maxInterval;
        private readonly float _alertWindowSeconds;
        private readonly double _auditChance;
        private readonly int _auditTaps;
        private readonly float _auditExtraSeconds;
        private float _timer;

        public WorkLaptop(
            Random rng,
            float minInterval,
            float maxInterval,
            float alertWindowSeconds,
            double auditChance,
            int auditTaps,
            float auditExtraSeconds)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _minInterval = minInterval;
            _maxInterval = maxInterval;
            _alertWindowSeconds = alertWindowSeconds;
            _auditChance = auditChance;
            _auditTaps = auditTaps;
            _auditExtraSeconds = auditExtraSeconds;
            ScheduleNextAlert(1f);
        }

        /// <param name="muted">Do Not Disturb: the boss cannot ping (the timer is frozen).</param>
        public void Tick(float deltaTime, float clockScale = 1f, bool hasAutoClickerBird = false, bool muted = false)
        {
            if (muted && !IsAlertActive) return;
            _timer -= deltaTime;

            if (!IsAlertActive)
            {
                if (_timer <= 0f)
                {
                    TotalAlertsTriggered++;
                    if (hasAutoClickerBird)
                    {
                        IsAudit = false;
                        AuditTapsCurrent = 0;
                        LastAttendedWasAudit = false;
                        AlertsAttended++;
                        ScheduleNextAlert(clockScale);
                    }
                    else
                    {
                        IsAlertActive = true;
                        bool auditRoll = _rng.NextDouble() < _auditChance;
                        IsAudit = auditRoll;
                        AuditTapsRequired = auditRoll ? _auditTaps : 1;
                        AuditTapsCurrent = 0;
                        _timer = auditRoll ? (_alertWindowSeconds + _auditExtraSeconds) : _alertWindowSeconds;
                    }
                }
            }
            else if (_timer <= 0f)
            {
                IsAlertActive = false;
                IsAudit = false;
                AuditTapsCurrent = 0;
                ScheduleNextAlert(clockScale);
            }
        }

        public bool TryAttendAlert(float clockScale = 1f)
        {
            if (!IsAlertActive) return false;
            AuditTapsCurrent++;
            if (AuditTapsCurrent < AuditTapsRequired)
            {
                return false;
            }

            IsAlertActive = false;
            LastAttendedWasAudit = IsAudit;
            IsAudit = false;
            AuditTapsCurrent = 0;
            AlertsAttended++;
            ScheduleNextAlert(clockScale);
            return true;
        }

        public float GetAttendanceRatio()
        {
            return TotalAlertsTriggered == 0 ? 1f : (float)AlertsAttended / TotalAlertsTriggered;
        }

        private void ScheduleNextAlert(float clockScale)
        {
            float baseWait = _minInterval + (float)_rng.NextDouble() * (_maxInterval - _minInterval);
            _timer = baseWait * Math.Max(1f, clockScale);
        }
    }
}
