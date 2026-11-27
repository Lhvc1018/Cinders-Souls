#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Codice.Multiplayer
{
    /* Opt-in development probe. Delays application messages, NOT Photon UDP packets.
       Drops only disposable world snapshots; never drops inventory or input actions. */
    public sealed class CoopNetworkSimulation : MonoBehaviour
    {
        static CoopNetworkSimulation instance;
        readonly List<(float at, Action deliver)> queue = new List<(float, Action)>();
        readonly System.Random random = new System.Random(26767);
        public static int Delivered, Dropped;
        public static int Arg(string key, int fallback = 0)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, key);
            return i >= 0 && i + 1 < a.Length && int.TryParse(a[i + 1], out int n) ? n : fallback;
        }
        public static bool Enqueue(string text, Action deliver)
        {
            int delay = Mathf.Clamp(Arg("-probe-delay"), 0, 1000);
            if (delay == 0) return false;
            if (!instance) { instance = new GameObject("Development network simulation").AddComponent<CoopNetworkSimulation>(); DontDestroyOnLoad(instance.gameObject); }
            bool world = text.StartsWith("{\"type\":\"poses\"", StringComparison.Ordinal) || text.StartsWith("{\"type\":\"motion\"", StringComparison.Ordinal);
            if (world && instance.random.Next(100) < Mathf.Clamp(Arg("-probe-loss"), 0, 50)) { Dropped++; return true; }
            float jitter = world ? (float)(instance.random.NextDouble() * 2 - 1) * Mathf.Clamp(Arg("-probe-jitter"), 0, delay) : 0;
            instance.queue.Add((Time.realtimeSinceStartup + (delay + jitter) * .001f, deliver));
            return true;
        }
        void Update()
        {
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < queue.Count;)
            {
                if (queue[i].at > now) { i++; continue; }
                var item = queue[i]; queue.RemoveAt(i); item.deliver(); Delivered++;
            }
        }
    }
}
#endif
