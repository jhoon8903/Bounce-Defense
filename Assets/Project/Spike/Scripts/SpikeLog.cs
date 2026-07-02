using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Spike
{
    // Console read-back over MCP is paginated/lossy under bursty logging, and a mid-play
    // domain reload wipes static state, so this also appends to a file outside Assets/
    // (won't trigger Unity's asset watcher / reimport-recompile loop) that survives both.
    public static class SpikeLog
    {
        public static readonly List<string> Entries = new List<string>();
        static string _filePath;

        static string FilePath
        {
            get
            {
                if (_filePath == null)
                {
                    string root = Path.GetDirectoryName(Application.dataPath);
                    _filePath = Path.Combine(root, "SpikeLog.txt");
                }
                return _filePath;
            }
        }

        public static void Add(string message)
        {
            string line = $"[{Time.time:F2}] {message}";
            Entries.Add(line);
            Debug.Log("[Spike] " + message);
            try
            {
                File.AppendAllText(FilePath, line + "\n");
            }
            catch (IOException)
            {
                // best-effort; in-memory Entries + console log still cover this run
            }
        }

        public static void Clear()
        {
            Entries.Clear();
            try
            {
                File.WriteAllText(FilePath, string.Empty);
            }
            catch (IOException)
            {
            }
        }
    }
}
