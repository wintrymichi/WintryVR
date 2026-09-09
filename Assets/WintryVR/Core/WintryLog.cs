using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace WintryVR.Core
{
    /// <summary>
    /// Central logger. Keeps an in-memory ring buffer so the debug panel can show
    /// and export recent entries. Never surfaces raw technical errors to the user;
    /// that is the job of <see cref="Localization"/> + the orchestrator.
    /// </summary>
    public static class WintryLog
    {
        public enum Level { Verbose, Info, Warning, Error }

        public struct Entry
        {
            public DateTime Time;
            public Level Level;
            public string Category;
            public string Message;
        }

        public const int Capacity = 2000;
        private static readonly Queue<Entry> _buffer = new Queue<Entry>(Capacity);
        private static readonly object _lock = new object();

        public static bool VerboseEnabled = false;
        public static event Action<Entry> OnEntry;

        public static void V(string category, string message) { if (VerboseEnabled) Write(Level.Verbose, category, message); }
        public static void I(string category, string message) => Write(Level.Info, category, message);
        public static void W(string category, string message) => Write(Level.Warning, category, message);
        public static void E(string category, string message) => Write(Level.Error, category, message);
        public static void E(string category, string message, Exception ex) => Write(Level.Error, category, message + " :: " + ex.GetType().Name + ": " + ex.Message);

        private static void Write(Level level, string category, string message)
        {
            var entry = new Entry { Time = DateTime.UtcNow, Level = level, Category = category, Message = message };
            lock (_lock)
            {
                if (_buffer.Count >= Capacity) _buffer.Dequeue();
                _buffer.Enqueue(entry);
            }
            string line = "[Wintry/" + category + "] " + message;
            switch (level)
            {
                case Level.Error: Debug.LogError(line); break;
                case Level.Warning: Debug.LogWarning(line); break;
                default: Debug.Log(line); break;
            }
            try { OnEntry?.Invoke(entry); } catch { /* listeners must not break logging */ }
        }

        public static List<Entry> Snapshot()
        {
            lock (_lock) return new List<Entry>(_buffer);
        }

        /// <summary>Exports the buffered log to persistentDataPath and returns the file path.</summary>
        public static string Export()
        {
            var sb = new StringBuilder();
            sb.AppendLine("WintryVR log export " + DateTime.UtcNow.ToString("o"));
            sb.AppendLine("Device: " + SystemInfo.deviceModel + " / " + SystemInfo.operatingSystem);
            foreach (var e in Snapshot())
                sb.Append(e.Time.ToString("HH:mm:ss.fff")).Append(' ').Append(e.Level).Append(' ').Append(e.Category).Append(": ").AppendLine(e.Message);
            string dir = Path.Combine(Application.persistentDataPath, "logs");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "wintry_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".log");
            File.WriteAllText(path, sb.ToString());
            I("Log", "Exported log to " + path);
            return path;
        }
    }
}
