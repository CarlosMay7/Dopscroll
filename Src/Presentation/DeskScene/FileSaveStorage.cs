using System;
using System.IO;
using Doomscroll.Core.Services;

namespace Doomscroll.Presentation.DeskScene
{
    /// <summary>
    /// Concrete file-system implementation of ISaveStorage using atomic temporary writes
    /// and a backup file (.bak) to prevent save corruption on sudden power loss or crash.
    /// In Godot, pass ProjectSettings.GlobalizePath("user://save_v1.json") as the base path.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _saveFilePath;
        private readonly string _tempFilePath;
        private readonly string _backupFilePath;

        public FileSaveStorage(string saveFilePath)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath))
                throw new ArgumentException("Save file path cannot be empty.", nameof(saveFilePath));

            _saveFilePath = saveFilePath;
            _tempFilePath = saveFilePath + ".tmp";
            _backupFilePath = saveFilePath + ".bak";
        }

        public bool Exists()
        {
            return File.Exists(_saveFilePath) || File.Exists(_backupFilePath);
        }

        public string ReadAllText()
        {
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    return File.ReadAllText(_saveFilePath);
                }
                catch (IOException)
                {
                    // Fall back to backup file if primary save cannot be read
                }
            }

            if (File.Exists(_backupFilePath))
            {
                return File.ReadAllText(_backupFilePath);
            }

            return string.Empty;
        }

        public void WriteAllText(string json)
        {
            string? directory = Path.GetDirectoryName(_saveFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 1. Write to temporary file first
            File.WriteAllText(_tempFilePath, json);

            // 2. Rotate existing save to .bak and atomically move .tmp to primary save path
            if (File.Exists(_saveFilePath))
            {
                File.Copy(_saveFilePath, _backupFilePath, overwrite: true);
                File.Delete(_saveFilePath);
            }

            File.Move(_tempFilePath, _saveFilePath);
        }
    }
}
