using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GraduationDemo.Train
{
    [Serializable]
    public sealed class DetectiveMemoryEntry
    {
        public string id, source, topic, text, carriage, recordedAt, note;
        public bool testimony, pinned;
    }

    /// <summary>Stores only encountered dialogue and inspected objects, never unasked answers.</summary>
    public sealed class DetectiveMemory
    {
        [Serializable]
        private sealed class SaveData
        {
            public int version = 1;
            public List<DetectiveMemoryEntry> entries = new List<DetectiveMemoryEntry>();
        }

        private SaveData data = new SaveData();
        public IReadOnlyList<DetectiveMemoryEntry> Entries => data.entries;
        public string SavePath { get; private set; }
        public string Error { get; private set; }

        public DetectiveMemory(string savePath)
        {
            SavePath = savePath;
            Load();
        }

        public bool Remember(string id, string source, string topic, string text, string carriage, bool testimony)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(text)) return false;
            // A revised response is a new memory; repeating the same answer is not.
            foreach (var entry in data.entries)
                if (entry.id == id && entry.text == text) return false;
            data.entries.Add(new DetectiveMemoryEntry {
                id = id, source = source, topic = topic, text = text, carriage = carriage,
                testimony = testimony, recordedAt = DateTime.UtcNow.ToString("o"), note = ""
            });
            Save();
            return true;
        }

        public void SetPinned(DetectiveMemoryEntry entry, bool value)
        {
            if (!data.entries.Contains(entry)) return;
            entry.pinned = value;
            Save();
        }

        public void SetNote(DetectiveMemoryEntry entry, string value)
        {
            if (!data.entries.Contains(entry) || entry.note == value) return;
            entry.note = value;
            Save();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(SavePath)));
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(SavePath)) File.Replace(temporary, SavePath, SavePath + ".bak");
                else File.Move(temporary, SavePath);
                Error = null;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Error = "Notebook could not be saved. Check available disk space and file permissions.";
                Debug.LogWarning(Error + " " + exception.Message);
            }
        }

        private void Load()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (loaded == null || loaded.version != 1 || loaded.entries == null)
                    throw new ArgumentException("Unsupported notebook format.");
                foreach (var entry in loaded.entries)
                    if (entry == null || string.IsNullOrEmpty(entry.id) || entry.text == null)
                        throw new ArgumentException("Incomplete notebook entry.");
                data = loaded;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                // Preserve the unreadable file before a future autosave can replace it.
                try { File.Copy(SavePath, SavePath + ".unreadable-" + DateTime.UtcNow.Ticks); }
                catch (Exception copyError) when (copyError is IOException || copyError is UnauthorizedAccessException)
                {
                    // Use a recovery slot if preserving the original is not possible.
                    SavePath = SavePath + ".recovered";
                }
                Error = "Saved notebook could not be read. Starting an empty notebook; the original has been preserved.";
                Debug.LogWarning(Error + " " + exception.Message);
            }
        }
    }
}
