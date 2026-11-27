using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using UnityEngine;

namespace Codice.Mechanics
{
    public sealed class RunScene : MonoBehaviour
    {
        public PlayerCharacter player;
        public BasicEnemyEncounter dungeon;
        public BossEncounter arena;
        public RunPortal portal;
        public Transform[] lootMarkers;
        public Codice.Expedition.ExpeditionFloor Expedition => GetComponent<Codice.Expedition.ExpeditionFloor>();
        public bool IsArena => arena && !dungeon;
        public bool UsesBossEncounter(int round) => Expedition ? Expedition.IsBossRoom : arena && RoundRules.IsBoss(round);
        public bool UsesDungeonEncounter(int round) => dungeon && !UsesBossEncounter(round);
        void Awake()
        {
            if (dungeon) dungeon.startOnPlay = false;
            if (arena) { arena.startOnPlay = false; arena.managedByRun = true; }
        }
        System.Collections.IEnumerator Start()
        {
            yield return null;
            if(!Expedition){Codice.Presentation.PlayerVisibilityLight.ConfigureAmbient();foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())if(light.type==LightType.Point)light.intensity=Mathf.Max(light.intensity,2.4f);
            var profile=ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();var volume=new GameObject("Main level visibility grade").AddComponent<UnityEngine.Rendering.Volume>();volume.transform.SetParent(transform);volume.isGlobal=true;volume.priority=11;volume.profile=profile;
            profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true).postExposure.value=.6f;
            profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true).mode.value=UnityEngine.Rendering.Universal.TonemappingMode.ACES;
            if(player&&player.view)(player.view.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()??player.view.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()).renderPostProcessing=true;
            }

            if(Codice.Multiplayer.LanSession.Client){TreasureChest.InstallInScene();Codice.Multiplayer.LanSession.Instance.EnterClient(this);yield break;}
            if(!Expedition)TreasureChest.InstallInScene();
            if (!RunDirector.Instance) new GameObject("Run director").AddComponent<RunDirector>();
            Codice.UI.RunHUD.Install(this);
            RunDirector.Instance.Enter(this);
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.EnterHost(this);
        }
    }
}
