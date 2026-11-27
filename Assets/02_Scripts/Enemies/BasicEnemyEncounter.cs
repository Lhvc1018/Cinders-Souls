using System;
using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;

namespace Codice.Enemies
{
    // Owns one configured wave. RunDirector decides when to start the next one.
    public sealed class BasicEnemyEncounter : MonoBehaviour
    {
        public BasicEnemy[] prefabs;
        [NonSerialized] public BasicEnemy expeditionPrefab;
        [NonSerialized] public BasicEnemy[] expeditionResidents;
        public Transform[] spawnPoints;
        public PlayerCharacter player;
        public Merchant merchant;
        [Min(1)] public int level = 1;
        public bool startOnPlay = true;
        [Min(1)] public int maxConcurrent = 8;
        public EnemyKind[] composition = { EnemyKind.Swordsman, EnemyKind.Crossbowman, EnemyKind.Swordsman, EnemyKind.Crossbowman, EnemyKind.Swordsman };
        public int Defeated { get; private set; }
        public int Total => composition.Length;
        public event Action Completed;
        BasicEnemy[] spawned;
        int next;
        int spawnLimit;
        bool running;
        void Start()
        {
            if (startOnPlay) BeginEncounter(level, composition);
        }
        public bool BeginEncounter(int round, EnemyKind[] roster)
        {
            if (running || !player || !player.State.IsAlive || roster == null || roster.Length == 0 || spawnPoints.Length == 0 || prefabs.Length != 4) return false;
            level = round; composition = (EnemyKind[])roster.Clone(); Defeated = next = 0;
            spawned = new BasicEnemy[Total]; running = true;
            if (merchant&&!merchant.ExpeditionShop) { merchant.onRoundContinued.RemoveListener(Continue); merchant.onRoundContinued.AddListener(Continue); }
            spawnLimit = !expeditionPrefab&&merchant&&!merchant.ExpeditionShop ? (Total + 1) / 2 : Total;
            SpawnUntil(spawnLimit); return true;
        }
        void OnDestroy() { if (merchant) merchant.onRoundContinued.RemoveListener(Continue); }
        void SpawnUntil(int end)
        {
            while (next < end && next - Defeated < Mathf.Max(1, maxConcurrent))
            {
                if (spawnPoints.Length == 0 || !player) { Debug.LogError("Enemy encounter needs player and spawn points.", this); return; }
                var marker = spawnPoints[next % spawnPoints.Length];
                // Root cause of "empty" combat rooms: a marker slightly off the NavMesh (raised floors, galleries) aborted the
                // whole wave and left the room sealed with nobody inside. Widen the search, then fall back to around the player.
                NavMeshHit position = default;
                bool placed = marker && (NavMesh.SamplePosition(marker.position, out position, 2, NavMesh.AllAreas) || NavMesh.SamplePosition(marker.position, out position, 7, NavMesh.AllAreas));
                if (!placed) placed = NavMesh.SamplePosition(player.transform.position + Quaternion.Euler(0, next * 67, 0) * Vector3.forward * 7, out position, 9, NavMesh.AllAreas);
                if (!placed) { Debug.LogWarning("Enemy spawn: no NavMesh near marker or player; retrying.", this); return; }
                // Reusing four markers must not stack additional residents on the same point.
                if(next>=spawnPoints.Length){
                    Vector3 offset=new Vector3(next%2==0?1.5f:-1.5f,0,-1.5f*(next/spawnPoints.Length));
                    if(NavMesh.SamplePosition(position.position+offset,out var spread,1.2f,NavMesh.AllAreas)&&!NavMesh.Raycast(position.position,spread.position,out var edge,NavMesh.AllAreas))position=spread;
                }
                var selected=expeditionPrefab ? expeditionPrefab : prefabs[(int)composition[next]];
                if(expeditionPrefab&&expeditionResidents!=null&&expeditionResidents.Length>0)selected=expeditionResidents[next%expeditionResidents.Length];
                if(!expeditionPrefab && level>=2&&next%3==1){var variant=Resources.Load<BasicEnemy>("EnemyVariants/"+composition[next]);if(variant)selected=variant;}
                string extra=expeditionPrefab ? null : ExtraEnemy(level,next,Total);
                if(extra!=null){var native=Resources.Load<BasicEnemy>("EnemyVariants/"+extra);if(native)selected=native;}
                var enemy = Instantiate(selected, position.position, marker.rotation, transform);
                enemy.Initialize(player, level); enemy.Died += OnDefeated; spawned[next++] = enemy;
            }
        }
        public static string ExtraEnemy(int round,int index,int total)
        {
            if(round%5==4&&index==total-1)return "DungeonSkeleton";
            if(round>=3&&index%4==2)return round>=6&&index%8==6?"TurtleShell":"Slime";
            return null;
        }
        public void ApplyLan(int total,int defeated){composition=new EnemyKind[Mathf.Clamp(total,0,200)];Defeated=defeated;}
        void OnDefeated(BasicEnemy enemy)
        {
            enemy.Died -= OnDefeated; Defeated++;
            if (!expeditionPrefab&&merchant&&!merchant.ExpeditionShop && Defeated == (Total + 1) / 2)
            {
                // Remove in-flight attacks before the safe shopping interval.
                foreach (var shot in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                    if (shot.gameObject.scene == gameObject.scene) { shot.gameObject.SetActive(false); Destroy(shot.gameObject); }
                merchant.ReportRoundProgress(level, Defeated, Total);
            }
            else SpawnUntil(spawnLimit);
            if (Defeated == Total) { running = false; Completed?.Invoke(); }
        }
        void Continue() { spawnLimit = Total; SpawnUntil(spawnLimit); }
        // Watchdog: an encounter must never stall with nobody to fight. Lost enemies (fallen, outside the room, off the mesh)
        // are brought back near the player; missing spawns are retried; a wave whose last death was never reported closes.
        float watch; int silentChecks;
        void Update()
        {
            if (!running || !player || spawned == null) return;
            if ((watch -= Time.deltaTime) > 0) return; watch = 1;
            int alive = 0; var p = player.transform.position;
            for (int i = 0; i < next && i < spawned.Length; i++)
            {
                var e = spawned[i]; if (!e || !e.IsAlive) continue; alive++;
                var pos = e.transform.position;
                bool lost = pos.y < p.y - 8 || (pos - p).sqrMagnitude > 48 * 48 || !NavMesh.SamplePosition(pos, out _, 2f, NavMesh.AllAreas);
                if (lost && NavMesh.SamplePosition(p + Quaternion.Euler(0, i * 72, 0) * Vector3.forward * 7, out var hit, 6, NavMesh.AllAreas))
                { var agent = e.GetComponent<NavMeshAgent>(); if (agent && agent.enabled) agent.Warp(hit.position); else e.transform.position = hit.position; Debug.LogWarning($"[Encounter] Recovered lost enemy {e.name}"); }
            }
            if (alive == 0 && next < spawnLimit) SpawnUntil(spawnLimit);
            silentChecks = alive == 0 && next >= Total && Defeated < Total ? silentChecks + 1 : 0;
            if (silentChecks >= 3) { Debug.LogWarning($"[Encounter] Closing wave: {Defeated}/{Total} reported, none alive"); Defeated = Total; running = false; Completed?.Invoke(); }
        }
    }
}
