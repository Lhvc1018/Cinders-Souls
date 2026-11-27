using UnityEngine;
namespace Codice.Presentation
{
    // Short impact freeze (Kingdom Hearts-style response). Uses a sentinel scale so a pause
    // that captured it can never leave the game slowed down.
    public sealed class HitStop : MonoBehaviour
    {
        const float Scale = .0613f;
        static HitStop instance;
        float until;
        public static void Do(float seconds)
        {
            if (Codice.Multiplayer.LanSession.Active || seconds <= 0) return;
            if (Time.timeScale < .99f && !Mathf.Approximately(Time.timeScale, Scale)) return; // paused or slowed by another system
            if (!instance) { var go = new GameObject("Hit stop"); DontDestroyOnLoad(go); instance = go.AddComponent<HitStop>(); }
            Time.timeScale = Scale;
            instance.until = Mathf.Max(instance.until, Time.unscaledTime + Mathf.Min(seconds, .2f));
        }
        void Update()
        {
            if (Mathf.Approximately(Time.timeScale, Scale) && Time.unscaledTime >= until) { Time.timeScale = 1; until = 0; }
        }
    }
}
