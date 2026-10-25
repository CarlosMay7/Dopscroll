using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Doomscroll.Core.Events;

namespace Doomscroll.Core.Services
{
    public sealed class SaveData
    {
        public int SchemaVersion { get; set; } = 1;

        // Current run state
        public int CurrentDay { get; set; } = 1;
        public double CurrentDopamine { get; set; }
        public double TotalRunDopamine { get; set; }
        public double CurrentMoney { get; set; }
        public Dictionary<string, int> RunUpgradeLevels { get; set; } = new();
        // Videos watched during the previous night (the Screen Time report compares against it).
        public long LastNightVideos { get; set; }
        // Night whose dopamine was already turned into a creator post (one post per night).
        public int LastPostNight { get; set; }

        // Night phase: true once night CurrentDay has ended and the player is in the Night Shop.
        public bool NightCompleted { get; set; }
        // True while an unpaid Electricity Bill forces a "Touch Grass" prestige.
        public bool PowerCutPending { get; set; }

        // Permanent meta-progression ("Touch Grass" & 3 AM Mythic Album)
        public int GrassPoints { get; set; }
        public int TotalPrestiges { get; set; }
        public int MythicPityCounter { get; set; }
        public Dictionary<string, int> GrassUpgradeLevels { get; set; } = new();
        public HashSet<string> UnlockedMythicIds { get; set; } = new();

        // Lifetime stats, achievements & settings
        public long LifetimeVideosWatched { get; set; }
        public long LifetimeEarlySwipes { get; set; }
        public long LifetimeBossAlertsReplied { get; set; }
        public long LifetimeScreamersSurvived { get; set; }
        public HashSet<string> UnlockedAchievementIds { get; set; } = new();
        public bool EndlessModeUnlocked { get; set; }
        // Legacy: Safe Screamer now lives in user://settings.cfg. Only read once to seed that file.
        public bool SafeScreamerMode { get; set; }

        // First-time tips already shown in this save (Settings can clear them).
        public HashSet<string> SeenTips { get; set; } = new();

        // Role of each rack phone by slot (0 = phone #2). Kept across runs; see RackRole for the default.
        public List<SecondaryRole> RackRoles { get; set; } = new();

        public int RunLevel(string upgradeId) => RunUpgradeLevels.TryGetValue(upgradeId, out int level) ? level : 0;
        public int GrassLevel(string upgradeId) => GrassUpgradeLevels.TryGetValue(upgradeId, out int level) ? level : 0;
        public bool HasMythic(string mythicId) => UnlockedMythicIds.Contains(mythicId);

        /// <summary>Saved role of rack slot <paramref name="slot"/>; unset slots cycle through the roles in enum order.</summary>
        public SecondaryRole RackRole(int slot) =>
            slot < RackRoles.Count ? RackRoles[slot] : (SecondaryRole)(slot % Enum.GetValues<SecondaryRole>().Length);

        public void SetRackRole(int slot, SecondaryRole role)
        {
            while (RackRoles.Count <= slot) RackRoles.Add(RackRole(RackRoles.Count));
            RackRoles[slot] = role;
        }
    }

    public sealed class SaveDataService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly ISaveStorage _storage;

        public SaveDataService(ISaveStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public bool HasSave => _storage.Exists();

        public SaveData LoadOrCreate()
        {
            if (!_storage.Exists())
            {
                return new SaveData();
            }

            try
            {
                var json = _storage.ReadAllText();
                return JsonSerializer.Deserialize<SaveData>(json, JsonOptions) ?? new SaveData();
            }
            catch
            {
                return new SaveData();
            }
        }

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var json = JsonSerializer.Serialize(data, JsonOptions);
            _storage.WriteAllText(json);
        }
    }
}
