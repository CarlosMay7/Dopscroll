namespace Doomscroll.Core.Services
{
    /// <summary>
    /// Engine-agnostic persistence interface so Core remains 100% free of Godot/Unity APIs.
    /// In Presentation, implement using Application.persistentDataPath (Unity) or
    /// ProjectSettings.GlobalizePath("user://") (Godot) with atomic temp-file writes and .bak fallback.
    /// </summary>
    public interface ISaveStorage
    {
        bool Exists();
        string ReadAllText();
        void WriteAllText(string json);
    }
}
